using UnityEngine;
using System.Collections.Generic;

namespace OpenWorld
{
    public class EnemyMilitarySystem
    {
        private readonly EnemyEconomy _economy;
        private int _step;

        public EnemyMilitarySystem(EnemyEconomy economy)
        {
            _economy = economy;
        }

        public void TickMilitary(UnitSystem units, OpenWorldState world)
        {
            _step++;
            int weapons = _economy.GetWeapons();
            int food = _economy.GetFood();

            BuildingEntity barracks = null;
            foreach (var b in world.Buildings.Values)
            {
                if (b.FactionId == OpenWorldConstants.EnemyFactionId && b.Kind == BuildableKind.Barracks)
                {
                    barracks = b;
                    break;
                }
            }

            if (barracks != null && _step % 2 == 0)
            {
                UnitKind? toTrain = null;
                int weaponCost = 0;
                int foodCost = 0;

                if (weapons < 5 && food >= 3 && weapons >= 1)
                {
                    toTrain = UnitKind.Militia;
                    weaponCost = 1; foodCost = 3;
                }
                else if (weapons >= 5 && weapons < 15 && food >= 4 && weapons >= 2)
                {
                    UnitKind[] options = { UnitKind.Melee, UnitKind.Spearman, UnitKind.Scout };
                    toTrain = options[Random.Range(0, options.Length)];
                    weaponCost = 2; foodCost = 4;
                }
                else if (weapons >= 15 && weapons < 30 && food >= 5 && weapons >= 2)
                {
                    UnitKind[] options = { UnitKind.Ranged, UnitKind.Musketeer };
                    toTrain = options[Random.Range(0, options.Length)];
                    weaponCost = 2; foodCost = 5;
                }
                else if (weapons >= 30 && food >= 6 && weapons >= 3)
                {
                    UnitKind[] options = { UnitKind.Rifleman, UnitKind.MachineGunner, UnitKind.Artillery };
                    toTrain = options[Random.Range(0, options.Length)];
                    weaponCost = 3; foodCost = 6;
                }

                if (toTrain.HasValue)
                {
                    _economy.Deduct(new[] { new ResourceAmount(ResourceKind.Weapons, weaponCost), new ResourceAmount(ResourceKind.Food, foodCost) });
                    units.Spawn(toTrain.Value, barracks.Origin + new Vector2Int(0, -2), OpenWorldConstants.EnemyFactionId);
                }
            }

            int militaryCount = 0;
            var idleMilitary = new List<UnitAgent>();
            var activeRaiders = new List<UnitAgent>();
            
            foreach (var unit in world.Units.Values)
            {
                if (unit.FactionId != OpenWorldConstants.EnemyFactionId || unit.Kind == UnitKind.Worker || unit.Kind == UnitKind.Scout) continue;
                militaryCount++;
                var agent = units.GetAgent(unit.Id);
                if (agent != null)
                {
                    if (unit.CurrentOrder?.Kind == UnitOrderKind.Attack) activeRaiders.Add(agent);
                    else if (unit.Task == UnitTask.Idle) idleMilitary.Add(agent);
                }
            }

            if (militaryCount < 8 && activeRaiders.Count < 3 && idleMilitary.Count >= 3)
            {
                var target = FindEnemyRaidTarget(world);
                if (target.HasValue)
                {
                    for (int i = 0; i < 3; i++) idleMilitary[i].IssueOrder(new UnitOrder { Kind = UnitOrderKind.Attack, TargetCell = target.Value, Priority = 4 });
                }
            }
            else if (militaryCount >= 8 && militaryCount < 15 && activeRaiders.Count < 6 && idleMilitary.Count >= 6)
            {
                var target = FindNearestPlayerBuilding(world, new Vector2Int(world.MapSize / 2 + 80, world.MapSize / 2));
                if (target.HasValue)
                {
                    for (int i = 0; i < 6; i++) idleMilitary[i].IssueOrder(new UnitOrder { Kind = UnitOrderKind.Attack, TargetCell = target.Value, Priority = 5 });
                }
            }
            else if (militaryCount >= 15 && activeRaiders.Count < 10 && idleMilitary.Count >= 10)
            {
                var target = FindNearestPlayerBuilding(world, new Vector2Int(world.MapSize / 2 + 80, world.MapSize / 2));
                if (target.HasValue)
                {
                    for (int i = 0; i < 10; i++) idleMilitary[i].IssueOrder(new UnitOrder { Kind = UnitOrderKind.Attack, TargetCell = target.Value, Priority = 6 });
                }
            }

            idleMilitary.RemoveAll(a => a.Entity.CurrentOrder?.Kind == UnitOrderKind.Attack);
            foreach (var agent in idleMilitary)
            {
                if (agent.Entity.CurrentOrder?.Kind == UnitOrderKind.Defend) continue;
                BuildingEntity defendTarget = null;
                foreach (var b in world.Buildings.Values)
                {
                    if (b.FactionId == OpenWorldConstants.EnemyFactionId && (b.Kind == BuildableKind.MinePost || b.Kind == BuildableKind.Steelworks || b.Kind == BuildableKind.Smelter || b.Kind == BuildableKind.MachineShop))
                    {
                        defendTarget = b;
                        break;
                    }
                }
                if (defendTarget != null)
                {
                    agent.IssueOrder(new UnitOrder { Kind = UnitOrderKind.Defend, TargetCell = defendTarget.Origin, SecondaryCell = defendTarget.Origin, Priority = 3 });
                }
            }
        }

        private Vector2Int? FindNearestPlayerBuilding(OpenWorldState world, Vector2Int from)
        {
            Vector2Int? best = null;
            int bestDist = world.MapSize * world.MapSize;
            foreach (var building in world.Buildings.Values)
            {
                if (building.FactionId != OpenWorldConstants.PlayerFactionId) continue;
                int d = (building.Origin - from).sqrMagnitude;
                if (d >= bestDist) continue;
                best = building.Origin;
                bestDist = d;
            }
            return best;
        }

        private Vector2Int? FindEnemyRaidTarget(OpenWorldState world)
        {
            if (world.LogisticsRoutes.Count == 0) return FindNearestPlayerBuilding(world, new Vector2Int(world.MapSize / 2 + 80, world.MapSize / 2));
            var route = world.LogisticsRoutes[Random.Range(0, world.LogisticsRoutes.Count)];
            if (route.Status != null && (route.Status.Contains("moving") || route.Status.Contains("delivering")))
                return route.Target;
            return route.Source;
        }
    }
}
