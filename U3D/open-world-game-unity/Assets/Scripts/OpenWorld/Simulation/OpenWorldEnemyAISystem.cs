using UnityEngine;
using System.Collections.Generic;

namespace OpenWorld
{
    /// <summary>
    /// 敌方 AI 系统：管理敌方势力的自动扩张、资源采集、防御建设和攻击策略
    /// </summary>
    public class OpenWorldEnemyAISystem
    {
        private readonly OpenWorldState _world;
        private readonly UnitSystem _units;
        private readonly SurfaceTerrainSystem _terrain;
        private readonly BlueprintSystem _blueprints;
        private int _aiStep;

        private const int EnemyUnitBudget = 14;
        private const int EnemyRoadblockBudget = 2;
        private const int EnemyRaidSize = 3;

        public string PressureSummary { get; private set; } = "Stable";

        public OpenWorldEnemyAISystem(OpenWorldState world, UnitSystem units, SurfaceTerrainSystem terrain, BlueprintSystem blueprints)
        {
            _world = world;
            _units = units;
            _terrain = terrain;
            _blueprints = blueprints;
        }

        public void Tick()
        {
            #if UNITY_EDITOR
            if (OpenWorldSimulationSystem.TestBotIsActive)
                return;
            #else
            if (UnityEngine.Object.FindFirstObjectByType<OpenWorld.Testing.TestBotManager>() != null)
                return;
            #endif

            if (_world.Buildings.Count == 0) return;
            _aiStep++;
            PressureSummary = "Stable";
            var center = new Vector2Int(_world.MapSize / 2, _world.MapSize / 2);
            var enemyCenter = center + new Vector2Int(80, 0);
            var pressureCell = enemyCenter + new Vector2Int((_aiStep % 5) * 2, (_aiStep % 3) * 2);

            int enemyUnits = 0;
            foreach (var unit in _world.Units.Values)
                if (unit.FactionId == OpenWorldConstants.EnemyFactionId) enemyUnits++;
            int enemyBuildings = 0;
            foreach (var building in _world.Buildings.Values)
                if (building.FactionId == OpenWorldConstants.EnemyFactionId) enemyBuildings++;
            int enemyRoadblocks = 0;
            foreach (var building in _world.Buildings.Values)
                if (building.FactionId == OpenWorldConstants.EnemyFactionId && building.Kind == BuildableKind.Roadblock) enemyRoadblocks++;
            foreach (var blueprint in _world.Blueprints)
                if (blueprint.FactionId == OpenWorldConstants.EnemyFactionId && blueprint.BuildKind == BuildableKind.Roadblock && blueprint.Status != BlueprintStatus.Cancelled) enemyRoadblocks++;

            // Phase 0: Scout nearby terrain for resources (early game)
            if (_aiStep <= 8 && enemyUnits < EnemyUnitBudget * 2 / 5)
            {
                _units.Spawn(UnitKind.Militia, pressureCell, OpenWorldConstants.EnemyFactionId);
                PressureSummary = "Enemy scouting";
            }

            // Phase 1: Build mines at resource nodes (steps 3-12)
            if (_aiStep >= 3 && _aiStep <= 12 && _aiStep % 2 == 0)
            {
                var resourceCell = FindEnemyResourceNode(enemyCenter);
                if (resourceCell.HasValue)
                {
                    _terrain.ApplyBrush(TerrainTool.Flatten, resourceCell.Value, 3, 1f);
                    _blueprints.QueueBuilding(BuildableKind.MinePost, resourceCell.Value, OpenWorldConstants.EnemyFactionId, 3);
                    _aiStep++;
                }
            }

            // Phase 2: Build barracks and defenses (steps 6-16)
            if (_aiStep >= 6 && _aiStep <= 16)
            {
                if (enemyBuildings < 6 && _aiStep % 3 == 0)
                    _blueprints.QueueBuilding(BuildableKind.Barracks, pressureCell + new Vector2Int(_aiStep % 4, _aiStep / 4 % 2), OpenWorldConstants.EnemyFactionId, 2);
                if (enemyRoadblocks < EnemyRoadblockBudget && _aiStep % 2 == 0)
                    _blueprints.QueueBuilding(BuildableKind.Roadblock, pressureCell + new Vector2Int(2, _aiStep % 4), OpenWorldConstants.EnemyFactionId, 2);
            }

            // Phase 3: Train mixed unit armies (steps 6+)
            if (_aiStep >= 6 && enemyUnits < EnemyUnitBudget)
            {
                UnitKind[] types = { UnitKind.Militia, UnitKind.Melee, UnitKind.Ranged, UnitKind.Scout, UnitKind.Spearman };
                _units.Spawn(types[_aiStep % types.Length], pressureCell, OpenWorldConstants.EnemyFactionId);
                if (_aiStep >= 10 && _aiStep % 2 == 0 && enemyUnits < EnemyUnitBudget)
                    _units.Spawn(UnitKind.Musketeer, pressureCell + new Vector2Int(1, 1), OpenWorldConstants.EnemyFactionId);
            }

            // Phase 4: Attack player logistics routes (steps 15+)
            if (_aiStep >= 15)
            {
                var routeTarget = FindEnemyRaidTarget();
                int activeRaiders = 0;
                foreach (var agent in _units.AllAgents())
                {
                    if (agent.Entity.FactionId != OpenWorldConstants.EnemyFactionId) continue;
                    if (agent.Entity.CurrentOrder?.Kind == UnitOrderKind.Attack) activeRaiders++;
                }
                if (routeTarget.HasValue && activeRaiders < EnemyRaidSize)
                {
                    foreach (var agent in _units.AllAgents())
                    {
                        if (agent.Entity.FactionId != OpenWorldConstants.EnemyFactionId) continue;
                        if (agent.Entity.CurrentOrder?.Kind == UnitOrderKind.Attack) continue;
                        if (agent.Entity.Task is UnitTask.Attacking or UnitTask.Moving) continue;
                        agent.IssueOrder(new UnitOrder { Kind = UnitOrderKind.Attack, TargetCell = routeTarget.Value, Priority = 4 });
                        if (++activeRaiders >= EnemyRaidSize) break;
                    }
                    PressureSummary = "Enemy raiding logistics routes";
                }
            }

            // Phase 5: Siege player base (steps 20+) with massed forces
            if (_aiStep >= 20)
            {
                var playerBase = FindNearestPlayerBuilding(enemyCenter);
                if (playerBase.HasValue)
                {
                    int siegers = 0;
                    int siegeMax = 6;
                    foreach (var agent in _units.AllAgents())
                    {
                        if (agent.Entity.FactionId != OpenWorldConstants.EnemyFactionId) continue;
                        if (agent.Entity.CurrentOrder?.Kind == UnitOrderKind.Attack && agent.Entity.CurrentOrder.TargetCell == playerBase.Value)
                            siegers++;
                    }
                    if (siegers < siegeMax)
                    {
                        foreach (var agent in _units.AllAgents())
                        {
                            if (agent.Entity.FactionId != OpenWorldConstants.EnemyFactionId) continue;
                            if (agent.Entity.CurrentOrder?.Kind == UnitOrderKind.Attack) continue;
                            if (agent.Entity.Task is UnitTask.Attacking or UnitTask.Moving) continue;
                            agent.IssueOrder(new UnitOrder { Kind = UnitOrderKind.Attack, TargetCell = playerBase.Value, Priority = 6 });
                            if (++siegers >= siegeMax) break;
                        }
                        PressureSummary = _aiStep % 2 == 0 ? "Enemy forces moving on player base" : $"Siege in progress ({siegers} advancing)";
                    }
                }
            }

            // Ensure enemy capital fort remains stocked
            EnsureEnemyCapital(enemyCenter);
            if (PressureSummary == "Enemy scouting")
                PressureSummary = "Logistics stable";
        }

        private Vector2Int? FindEnemyResourceNode(Vector2Int enemyCenter)
        {
            Vector2Int? best = null;
            int bestDist = int.MaxValue;
            int mapHalfX = _world.MapSize / 2;
            for (int z = -18; z <= 18; z++)
            {
                for (int x = -18; x <= 18; x++)
                {
                    var cell = enemyCenter + new Vector2Int(x, z);
                    if (cell.x < mapHalfX + 10) continue; // restrict to right half
                    if (!_world.InBounds(cell)) continue;
                    var surface = _world.GetCell(cell);
                    if (surface.ResourceRichness < 2) continue;
                    bool occupied = false;
                    foreach (var b in _world.Buildings.Values)
                        if ((b.Origin - cell).sqrMagnitude < 9) { occupied = true; break; }
                    if (occupied) continue;
                    int d = Mathf.Abs(x) + Mathf.Abs(z);
                    if (d >= bestDist) continue;
                    best = cell;
                    bestDist = d;
                }
            }
            return best;
        }

        private Vector2Int? FindNearestPlayerBuilding(Vector2Int from)
        {
            Vector2Int? best = null;
            int bestDist = _world.MapSize * _world.MapSize;
            foreach (var building in _world.Buildings.Values)
            {
                if (building.FactionId != OpenWorldConstants.PlayerFactionId) continue;
                int d = (building.Origin - from).sqrMagnitude;
                if (d >= bestDist) continue;
                best = building.Origin;
                bestDist = d;
            }
            return best;
        }

        private Vector2Int? FindEnemyRaidTarget()
        {
            if (_world.LogisticsRoutes.Count == 0) return FindNearestPlayerBuilding(new Vector2Int(_world.MapSize / 2 + 80, _world.MapSize / 2));
            var route = _world.LogisticsRoutes[_aiStep % _world.LogisticsRoutes.Count];
            if (route.Status.Contains("moving") || route.Status.Contains("delivering"))
                return route.Target;
            return route.Source;
        }

        private void EnsureEnemyCapital(Vector2Int enemyCenter)
        {
            // TestBotManager handles enemy AI now — method retained for signature stability
        }
    }
}
