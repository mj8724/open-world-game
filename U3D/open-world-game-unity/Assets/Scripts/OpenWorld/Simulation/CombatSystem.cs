using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace OpenWorld
{
    public class OpenWorldCombatSystem
    {
        private OpenWorldState _world;
        private Dictionary<Vector2Int, List<UnitEntity>> _combatGrid;
        private const int CombatGridCellSize = 12;

        public OpenWorldCombatSystem(OpenWorldState world)
        {
            _world = world;
        }

        public Vector2Int CombatGridKey(Vector2Int cell) => new(cell.x / CombatGridCellSize, cell.y / CombatGridCellSize);

        public void RebuildCombatGrid()
        {
            _combatGrid ??= new Dictionary<Vector2Int, List<UnitEntity>>();
            foreach (var list in _combatGrid.Values) list.Clear();
            foreach (var unit in _world.Units.Values)
            {
                if (unit.SimulationTier == SimulationTier.Dormant) continue;
                var key = CombatGridKey(unit.Cell);
                if (!_combatGrid.TryGetValue(key, out var list)) { list = new List<UnitEntity>(); _combatGrid[key] = list; }
                list.Add(unit);
            }
        }

        public List<UnitEntity> FindHostilesInGrid(Vector2Int center, float radiusSqr, int factionId, int maxResults = 12)
        {
            var result = new List<UnitEntity>();
            int gridR = Mathf.CeilToInt(radiusSqr > 0 ? Mathf.Sqrt(radiusSqr) / CombatGridCellSize + 1 : 1);
            var centerKey = CombatGridKey(center);
            for (int z = -gridR; z <= gridR; z++)
            {
                for (int x = -gridR; x <= gridR; x++)
                {
                    if (!_combatGrid.TryGetValue(centerKey + new Vector2Int(x, z), out var list)) continue;
                    foreach (var unit in list)
                    {
                        if (unit.FactionId == factionId || unit.Hp <= 0) continue;
                        float dSqr = (unit.Cell - center).sqrMagnitude;
                        if (dSqr > radiusSqr) continue;
                        result.Add(unit);
                        if (result.Count >= maxResults) return result;
                    }
                }
            }
            result.Sort((a, b) => (a.Cell - center).sqrMagnitude.CompareTo((b.Cell - center).sqrMagnitude));
            if (result.Count > maxResults) result.RemoveRange(maxResults, result.Count - maxResults);
            return result;
        }

        public void TickCombat(UnitSystem units)
        {
            RebuildCombatGrid();
            var dead = new List<int>();
            var allUnits = new List<UnitEntity>(_world.Units.Values);
            foreach (var attacker in allUnits)
            {
                if (attacker.Hp <= 0) { dead.Add(attacker.Id); continue; }
                if (attacker.SimulationTier == SimulationTier.Dormant) continue;
                if (attacker.SimulationTier == SimulationTier.LowFrequency && attacker.CurrentOrder?.Kind != UnitOrderKind.Attack) continue;
                
                // Defend: actively scan for hostiles within vision range
                if (attacker.Task == UnitTask.Defending && attacker.CurrentOrder?.Kind == UnitOrderKind.Defend && attacker.CurrentOrder?.TargetEntityId <= 0)
                {
                    if (attacker.CurrentOrder == null) continue;
                    var defendCenter = attacker.CurrentOrder.SecondaryCell;
                    var hostiles = FindHostilesInGrid(defendCenter, Mathf.Max(36f, attacker.VisionRange * attacker.VisionRange), attacker.FactionId);
                    foreach (var hostile in hostiles)
                    {
                        if ((attacker.Cell - hostile.Cell).sqrMagnitude <= attacker.AttackRange * attacker.AttackRange)
                        {
                            attacker.CurrentOrder.TargetEntityId = hostile.Id;
                            attacker.CurrentOrder.TargetCell = hostile.Cell;
                            break;
                        }
                        else
                        {
                            var agent = units.GetAgent(attacker.Id);
                            if (agent != null)
                                agent.IssueOrder(new UnitOrder { Kind = UnitOrderKind.Attack, TargetCell = hostile.Cell, TargetEntityId = hostile.Id, Priority = 5 });
                            break;
                        }
                    }
                }

                // Escort: follow assigned vehicle
                if (attacker.Task == UnitTask.Transporting && attacker.EscortVehicleId > 0 && _world.Vehicles.TryGetValue(attacker.EscortVehicleId, out var escortTarget))
                {
                    if (Vector2Int.Distance(attacker.Cell, escortTarget.Cell) > 3)
                    {
                        var escortAgent = units.GetAgent(attacker.Id);
                        if (escortAgent != null) escortAgent.MoveTo(escortTarget.Cell);
                    }
                    var escortThreats = FindHostilesInGrid(escortTarget.Cell, Mathf.Max(25f, attacker.VisionRange * attacker.VisionRange), attacker.FactionId, 6);
                    if (escortThreats.Count > 0)
                    {
                        attacker.CurrentOrder ??= new UnitOrder();
                        attacker.CurrentOrder.Kind = UnitOrderKind.Attack;
                        attacker.CurrentOrder.TargetCell = escortThreats[0].Cell;
                        attacker.CurrentOrder.TargetEntityId = escortThreats[0].Id;
                        attacker.CurrentOrder.Priority = 5;
                    }
                }

                UnitEntity target = null;
                if (attacker.CurrentOrder != null && attacker.CurrentOrder.Kind == UnitOrderKind.Attack && attacker.CurrentOrder.TargetEntityId > 0)
                    _world.Units.TryGetValue(attacker.CurrentOrder.TargetEntityId, out target);
                if (target == null)
                    target = FindHostilesInGrid(attacker.Cell, Mathf.Max(5f, attacker.VisionRange), attacker.FactionId, 1).FirstOrDefault();
                if (target == null) continue;

                float distance = Vector2Int.Distance(attacker.Cell, target.Cell);
                if (distance > attacker.AttackRange)
                {
                    if (attacker.CurrentOrder != null && attacker.CurrentOrder.Kind == UnitOrderKind.Attack)
                    {
                        var agent = units.GetAgent(attacker.Id);
                        if (agent != null && attacker.Task != UnitTask.Moving)
                            agent.IssueOrder(new UnitOrder { Kind = UnitOrderKind.Attack, TargetCell = target.Cell, TargetEntityId = target.Id, Priority = 5 });
                    }
                    continue;
                }

                bool ranged = attacker.AttackRange > 2f;
                if (ranged && attacker.Ammo < 1f) { attacker.Morale = Mathf.Max(0f, attacker.Morale - 0.5f); continue; }
                if (ranged) attacker.Ammo -= 1f;
                float efficiency = Mathf.Clamp(attacker.Morale / 100f, 0.2f, 1f) * Mathf.Clamp(1f - attacker.Fatigue / 120f, 0.25f, 1f);
                float damage = Mathf.Max(1f, attacker.AttackPower * attacker.Accuracy * efficiency * 0.32f - target.Armor * 0.15f);
                target.Hp -= Mathf.RoundToInt(damage);
                target.Suppression = Mathf.Min(100f, target.Suppression + damage * 1.5f);
                target.Morale = Mathf.Max(0f, target.Morale - damage * 0.35f);
                if (target.Hp <= 0) dead.Add(target.Id);
                else if (target.Hp < target.MaxHp * 0.45f) target.Wounded = true;
            }

            foreach (int id in dead) units.RemoveUnit(id);
        }

        public UnitEntity FindNearestHostile(UnitEntity attacker, float radius)
        {
            UnitEntity best = null;
            float bestDistance = radius * radius;
            foreach (var candidate in _world.Units.Values)
            {
                if (candidate.Id == attacker.Id || candidate.Hp <= 0 || candidate.SimulationTier == SimulationTier.Dormant || !AreHostile(attacker.FactionId, candidate.FactionId)) continue;
                float distance = (candidate.Cell - attacker.Cell).sqrMagnitude;
                if (distance >= bestDistance) continue;
                best = candidate;
                bestDistance = distance;
            }
            return best;
        }

        public List<UnitEntity> FindHostilesInRadius(Vector2Int center, float radius, int factionId)
        {
            var result = new List<UnitEntity>();
            float r2 = radius * radius;
            foreach (var candidate in _world.Units.Values)
            {
                if (candidate.Hp <= 0) continue;
                if (candidate.SimulationTier == SimulationTier.Dormant) continue;
                if (!AreHostile(factionId, candidate.FactionId)) continue;
                if ((candidate.Cell - center).sqrMagnitude <= r2)
                    result.Add(candidate);
            }
            result.Sort((a, b) => (a.Cell - center).sqrMagnitude.CompareTo((b.Cell - center).sqrMagnitude));
            return result;
        }

        public bool AreHostile(int a, int b)
        {
            if (a == b) return false;
            foreach (var relation in _world.Diplomacy)
                if ((relation.FactionA == a && relation.FactionB == b) || (relation.FactionA == b && relation.FactionB == a))
                    return relation.Stance == DiplomacyStance.Hostile;
            return a == OpenWorldConstants.EnemyFactionId || b == OpenWorldConstants.EnemyFactionId;
        }
    }
}
