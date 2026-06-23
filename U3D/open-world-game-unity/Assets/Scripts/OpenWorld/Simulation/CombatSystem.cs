using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace OpenWorld
{
    public class OpenWorldCombatSystem
    {
        private OpenWorldState _world;
        private Dictionary<Vector2Int, List<UnitEntity>> _combatGrid;
        private bool _gridDirty = true;
        private const int CombatGridCellSize = 12;

        public OpenWorldCombatSystem(OpenWorldState world)
        {
            _world = world;
        }

        public void MarkGridDirty() => _gridDirty = true;

        public Vector2Int CombatGridKey(Vector2Int cell) => new(cell.x / CombatGridCellSize, cell.y / CombatGridCellSize);

        public void RebuildCombatGrid()
        {
            _combatGrid ??= new Dictionary<Vector2Int, List<UnitEntity>>();
            foreach (var list in _combatGrid.Values) list.Clear();
            foreach (var unit in _world.GetUnitsListCached())
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
            if (_gridDirty)
            {
                RebuildCombatGrid();
                _gridDirty = false;
            }
            var dead = new List<int>();
            var allUnits = _world.GetUnitsListCached();
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
                    target = FindHostilesInGrid(attacker.Cell, Mathf.Max(5f, attacker.VisionRange * attacker.VisionRange), attacker.FactionId, 1).FirstOrDefault();
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

                bool ranged = attacker.AttackRange > GameBalance.C.RangedWeaponThreshold;
                if (ranged && attacker.Ammo < 1f) { attacker.Morale = Mathf.Max(0f, attacker.Morale - GameBalance.C.CombatAmmoMoralePenalty); continue; }
                if (ranged) attacker.Ammo -= GameBalance.C.CombatAmmoConsumption;
                float efficiency = Mathf.Clamp(attacker.Morale / GameBalance.C.CombatMoraleBase, GameBalance.C.CombatMoraleMin, 1f) *
                                   Mathf.Clamp(1f - attacker.Fatigue / GameBalance.C.CombatFatigueBase, GameBalance.C.CombatFatigueMin, 1f);
                float damage = Mathf.Max(1f, attacker.AttackPower * attacker.Accuracy * efficiency * GameBalance.C.CombatDamageMultiplier -
                                              target.Armor * GameBalance.C.CombatArmorReduction);
                target.Hp -= Mathf.RoundToInt(damage);
                target.Suppression = Mathf.Min(100f, target.Suppression + damage * GameBalance.C.CombatSuppressionMultiplier);
                target.Morale = Mathf.Max(0f, target.Morale - damage * GameBalance.C.CombatMoraleDamageMultiplier);
                if (target.Hp <= 0) dead.Add(target.Id);
                else if (target.Hp < target.MaxHp * GameBalance.C.CombatWoundedThreshold) target.Wounded = true;
            }

            foreach (int id in dead) units.RemoveUnit(id);
        }

        // TODO: 死代码 - 这两个方法从未被调用，且不走空间哈希网格（O(n) 遍历）。
        // 如需类似功能，应使用 FindHostilesInGrid 的空间哈希版本。
        // 保留仅为向后兼容，考虑在确认无外部调用后删除。
        [System.Obsolete("Use FindHostilesInGrid instead for better performance with spatial hashing")]
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

        [System.Obsolete("Use FindHostilesInGrid instead for better performance with spatial hashing")]
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
            // 默认规则：玩家与敌人互为敌对，中性阵营与所有方中立
            if (a == OpenWorldConstants.NeutralFactionId || b == OpenWorldConstants.NeutralFactionId)
                return false;
            return (a == OpenWorldConstants.PlayerFactionId && b == OpenWorldConstants.EnemyFactionId) ||
                   (a == OpenWorldConstants.EnemyFactionId && b == OpenWorldConstants.PlayerFactionId);
        }
    }
}
