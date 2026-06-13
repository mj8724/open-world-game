using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OpenWorld.Testing
{
    /// <summary>
    /// 1v1 AI test bot that simulates real player behavior for both factions.
    /// Implements the full build→logistics→combat loop to expose design flaws.
    /// Simplified to use only available public APIs.
    /// </summary>
    public class TestBotManager : MonoBehaviour
    {
        private OpenWorldState _world;
        private OpenWorldSimulationSystem _simulation;
        private BuildingSystem _buildings;
        private UnitSystem _units;
        private VehicleSystem _vehicles;

        private float _tickTimer;
        private const float TickInterval = 2f; // Match enemy AI cadence

        private readonly Dictionary<int, BotState> _botStates = new();

        public void Initialize(OpenWorldState world, OpenWorldSimulationSystem simulation,
            BuildingSystem buildings, UnitSystem units, VehicleSystem vehicles, OpenWorldLogisticsSystem logistics)
        {
            _world = world;
            _simulation = simulation;
            _buildings = buildings;
            _units = units;
            _vehicles = vehicles;

            // Initialize bot state for both factions
            _botStates[OpenWorldConstants.PlayerFactionId] = new BotState();
            _botStates[OpenWorldConstants.EnemyFactionId] = new BotState();

#if UNITY_EDITOR
            OpenWorldSimulationSystem.TestBotIsActive = true;
#endif

            Debug.Log("[TestBot] Initialized for symmetric 1v1 test scenario");
        }

        private void Update()
        {
            if (_world == null) return;

            _tickTimer += Time.deltaTime;
            if (_tickTimer < TickInterval) return;
            _tickTimer = 0f;

            // Run both faction bots with identical logic
            RunBotTick(OpenWorldConstants.PlayerFactionId);
            RunBotTick(OpenWorldConstants.EnemyFactionId);
        }

        private void RunBotTick(int factionId)
        {
            var state = _botStates[factionId];
            state.TickCount++;

            // Priority system: critical needs → economic expansion → military
            CheckCriticalNeeds(factionId, state);
            ExpandEconomy(factionId, state);
            BuildMilitary(factionId, state);
        }

        private void CheckCriticalNeeds(int factionId, BotState state)
        {
            // 1. Worker starvation check
            var workers = _world.Units.Values.Where(u => u.FactionId == factionId && u.Kind == UnitKind.Worker).ToList();
            var idleWorkers = workers.Count(w => w.Task == UnitTask.Idle);
            var blueprints = _world.Blueprints.Where(b => b.FactionId == factionId).ToList();

            if (idleWorkers == 0 && blueprints.Count > 0)
            {
                state.WorkerStarvationTicks++;
                if (state.WorkerStarvationTicks >= 15) // 30s at 2s tick
                {
                    DiagnosticProbes.LogWorkerStarvation(factionId, workers.Count, blueprints.Count);
                }
            }
            else
            {
                state.WorkerStarvationTicks = 0;
            }

            // 2. Food shortage check
            var food = _world.Inventory.Get(ResourceKind.Food);
            var population = _world.Units.Values.Count(u => u.FactionId == factionId);
            if (food < population * 5) // Less than 5 food per unit
            {
                state.FoodShortageTicks++;
                if (state.FoodShortageTicks >= 10)
                {
                    DiagnosticProbes.LogFoodShortage(factionId, food, population);
                }
                // Emergency: queue more farms
                TryBuildBuilding(factionId, BuildableKind.Farm, state);
            }
            else
            {
                state.FoodShortageTicks = 0;
            }
        }

        private void ExpandEconomy(int factionId, BotState state)
        {
            var inventory = _world.Inventory;

            // Build order priority based on current era and resources
            if (state.TickCount % 5 == 0) // Every 10s
            {
                // Limit max concurrent blueprints to prevent backlog
                var activeBlueprints = _world.Blueprints.Count(b => b.FactionId == factionId);
                if (activeBlueprints >= 5)
                {
                    // Too many queued, wait for workers to catch up
                    return;
                }

                // Check resource bottlenecks
                if (inventory.Get(ResourceKind.Wood) < 50)
                    TryBuildBuilding(factionId, BuildableKind.LumberCamp, state);

                if (inventory.Get(ResourceKind.Stone) < 40)
                    TryBuildBuilding(factionId, BuildableKind.Quarry, state);

                if (inventory.Get(ResourceKind.IronOre) < 30 && _world.Tech.Era >= TechEra.Iron)
                    TryBuildBuilding(factionId, BuildableKind.MinePost, state);

                if (inventory.Get(ResourceKind.IronIngot) < 20 && _world.Tech.Era >= TechEra.Iron)
                    TryBuildBuilding(factionId, BuildableKind.Smelter, state);

                // Setup production recipes
                SetupProduction(factionId);
            }
        }

        private void BuildMilitary(int factionId, BotState state)
        {
            if (state.TickCount % 3 != 0) return; // Every 6s

            var barracks = _world.Buildings.Values.FirstOrDefault(b =>
                b.FactionId == factionId && b.Kind == BuildableKind.Barracks);

            if (barracks == null)
            {
                TryBuildBuilding(factionId, BuildableKind.Barracks, state);
                return;
            }

            // Train units based on current army composition
            var units = _world.Units.Values.Where(u => u.FactionId == factionId).ToList();
            var melee = units.Count(u => u.Kind == UnitKind.Melee);
            var ranged = units.Count(u => u.Kind == UnitKind.Ranged);

            if (melee < 5)
                TryTrainUnit(factionId, barracks.Id, UnitKind.Melee);
            if (ranged < 3)
                TryTrainUnit(factionId, barracks.Id, UnitKind.Ranged);
        }

        private bool TryBuildBuilding(int factionId, BuildableKind kind, BotState state)
        {
            var def = _buildings.GetDef(kind);

            // Check era requirement
            if (!OpenWorldDataCatalog.EraUnlocked(_world.Tech.Era, OpenWorldDataCatalog.RequiredEraFor(kind)))
                return false;

            // Check if already queued
            if (_world.Blueprints.Any(b => b.FactionId == factionId && b.BuildKind == kind))
                return false;

            // Check resources WITHOUT spending (just test)
            var inventory = _world.Inventory;
            if (inventory.Dirt < def.Cost.Dirt || inventory.Stone < def.Cost.Stone ||
                inventory.IronOre < def.Cost.IronOre || inventory.Wood < def.Cost.Wood ||
                inventory.Food < def.Cost.Food)
            {
                state.ResourceBlockedBuilds++;
                if (state.ResourceBlockedBuilds >= 5)
                {
                    DiagnosticProbes.LogResourceDeadlock(factionId, kind, _world.Inventory);
                }
                return false;
            }

            // Find placement near town center
            var townCenter = _world.Buildings.Values.FirstOrDefault(b =>
                b.FactionId == factionId && b.Kind == BuildableKind.TownCenter);
            if (townCenter == null) return false;

            // Spiral search for valid placement
            for (int radius = 3; radius < 20; radius++)
            {
                for (int angle = 0; angle < 360; angle += 45)
                {
                    var rad = angle * Mathf.Deg2Rad;
                    var offset = new Vector2Int(
                        Mathf.RoundToInt(Mathf.Cos(rad) * radius),
                        Mathf.RoundToInt(Mathf.Sin(rad) * radius)
                    );
                    var origin = townCenter.Origin + offset;

                    if (_buildings.CanPlace(kind, origin, 0, out _))
                    {
                        // TryPlace with spendCost=true (let it handle the spending)
                        if (_buildings.TryPlace(kind, origin, 0, factionId, spendCost: true))
                        {
                            state.ResourceBlockedBuilds = 0;
                            return true;
                        }
                    }
                }
            }

            // No valid placement found - resources never spent
            return false;
        }

        private void TryTrainUnit(int factionId, int buildingId, UnitKind kind)
        {
            if (!_world.Buildings.TryGetValue(buildingId, out var building)) return;

            // Check if already training
            var training = _world.ProductionOrders.Any(o => o.BuildingId == buildingId &&
                o.RemainingCycles > 0 && !o.Paused &&
                !o.Status.StartsWith("Trained") && !o.Status.StartsWith("Produced"));
            if (training)
                return;

            var unitDef = OpenWorldDataCatalog.GetUnit(kind);
            if (unitDef == null) return;

            // Simplified cost check
            var cost = new[] { new ResourceAmount(ResourceKind.Food, 20), new ResourceAmount(ResourceKind.Weapons, 1) };
            if (!OpenWorldDataCatalog.CanSpend(_world.Inventory, cost, out _))
                return;

            _simulation.QueueUnitTraining(building.Id, kind, 1);
        }

        private void SetupProduction(int factionId)
        {
            // Assign workers to production buildings
            var productionBuildings = _world.Buildings.Values.Where(b =>
                b.FactionId == factionId &&
                (b.Kind == BuildableKind.Farm || b.Kind == BuildableKind.LumberCamp ||
                 b.Kind == BuildableKind.Quarry || b.Kind == BuildableKind.Smelter)
            ).ToList();

            foreach (var building in productionBuildings)
            {
                // Check if production is already assigned
                if (_world.ProductionOrders.Any(o => o.BuildingId == building.Id))
                    continue;

                // Find matching recipe
                var recipe = OpenWorldDataCatalog.ProductionRecipes.FirstOrDefault(r => r.Building == building.Kind);
                if (recipe == null) continue;

                // Queue production
                _simulation.QueueProduction(building.Id, recipe.Id, 999, 1);
            }
        }

        private class BotState
        {
            public int TickCount;
            public int WorkerStarvationTicks;
            public int FoodShortageTicks;
            public int ResourceBlockedBuilds;
        }
    }
}
