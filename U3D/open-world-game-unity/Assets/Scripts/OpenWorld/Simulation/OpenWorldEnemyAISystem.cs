using UnityEngine;
using System.Collections.Generic;

namespace OpenWorld
{
    public class OpenWorldEnemyAISystem
    {
        private readonly OpenWorldState _world;
        private readonly UnitSystem _units;
        private readonly SurfaceTerrainSystem _terrain;
        private readonly BlueprintSystem _blueprints;
        
        private readonly EnemyEconomy _economy;
        private readonly EnemyScoutSystem _scouting;
        private readonly EnemyMilitarySystem _military;

        public string PressureSummary { get; private set; } = "Stable";

        public OpenWorldEnemyAISystem(OpenWorldState world, UnitSystem units, SurfaceTerrainSystem terrain, BlueprintSystem blueprints)
        {
            _world = world;
            _units = units;
            _terrain = terrain;
            _blueprints = blueprints;
            
            _economy = new EnemyEconomy(_world);
            _scouting = new EnemyScoutSystem();
            _military = new EnemyMilitarySystem(_economy);
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
            
            _economy.TickEconomy();
            _scouting.TickScouting(_units, _world);
            TickExpansion();
            _military.TickMilitary(_units, _world);
            UpdatePressureSummary();
        }

        private void TickExpansion()
        {
            var center = new Vector2Int(_world.MapSize / 2, _world.MapSize / 2);
            var enemyCenter = center + new Vector2Int(80, 0);
            
            int tcCount = 0;
            int barracksCount = 0;
            int smelterCount = 0;
            int steelworksCount = 0;
            int machineShopCount = 0;
            int minePostCount = 0;
            int roadblockCount = 0;

            foreach (var b in _world.Buildings.Values)
            {
                if (b.FactionId == OpenWorldConstants.EnemyFactionId)
                {
                    if (b.Kind == BuildableKind.TownCenter) tcCount++;
                    else if (b.Kind == BuildableKind.Barracks) barracksCount++;
                    else if (b.Kind == BuildableKind.Smelter) smelterCount++;
                    else if (b.Kind == BuildableKind.Steelworks) steelworksCount++;
                    else if (b.Kind == BuildableKind.MachineShop) machineShopCount++;
                    else if (b.Kind == BuildableKind.MinePost) minePostCount++;
                    else if (b.Kind == BuildableKind.Roadblock || b.Kind == BuildableKind.Bunker) roadblockCount++;
                }
            }
            foreach (var b in _world.Blueprints)
            {
                if (b.FactionId == OpenWorldConstants.EnemyFactionId && b.Status != BlueprintStatus.Cancelled)
                {
                    if (b.BuildKind == BuildableKind.TownCenter) tcCount++;
                    else if (b.BuildKind == BuildableKind.Barracks) barracksCount++;
                    else if (b.BuildKind == BuildableKind.Smelter) smelterCount++;
                    else if (b.BuildKind == BuildableKind.Steelworks) steelworksCount++;
                    else if (b.BuildKind == BuildableKind.MachineShop) machineShopCount++;
                    else if (b.BuildKind == BuildableKind.MinePost) minePostCount++;
                    else if (b.BuildKind == BuildableKind.Roadblock || b.BuildKind == BuildableKind.Bunker) roadblockCount++;
                }
            }

            EnsureEnemyCapital(enemyCenter, tcCount, barracksCount, smelterCount);

            foreach (var res in _scouting.DiscoveredResources)
            {
                var cell = _world.GetCell(res);
                OpenWorldState.NormalizeLayers(ref cell);
                var mat = cell.Layers[cell.CurrentLayer].Material;

                if (mat == GroundMaterial.IronOre && minePostCount == 0)
                {
                    _blueprints.QueueBuilding(BuildableKind.MinePost, res, OpenWorldConstants.EnemyFactionId, 3);
                    _blueprints.QueueBuilding(BuildableKind.Warehouse, res + new Vector2Int(2, 0), OpenWorldConstants.EnemyFactionId, 2);
                    minePostCount++;
                }
                else if (mat == GroundMaterial.Coal && smelterCount == 0)
                {
                    _blueprints.QueueBuilding(BuildableKind.Smelter, res, OpenWorldConstants.EnemyFactionId, 2);
                    smelterCount++;
                }
            }

            if (smelterCount > 0 && steelworksCount == 0)
                _blueprints.QueueBuilding(BuildableKind.Steelworks, enemyCenter + new Vector2Int(4, 4), OpenWorldConstants.EnemyFactionId, 2);
                
            if (steelworksCount > 0 && machineShopCount == 0)
                _blueprints.QueueBuilding(BuildableKind.MachineShop, enemyCenter + new Vector2Int(0, 8), OpenWorldConstants.EnemyFactionId, 2);

            int militaryCount = 0;
            foreach (var u in _world.Units.Values)
                if (u.FactionId == OpenWorldConstants.EnemyFactionId && u.Kind != UnitKind.Scout && u.Kind != UnitKind.Worker) militaryCount++;

            if (militaryCount > 8 && roadblockCount < 2)
            {
                _blueprints.QueueBuilding(BuildableKind.Roadblock, enemyCenter + new Vector2Int(Random.Range(-5, 5), Random.Range(-5, 5)), OpenWorldConstants.EnemyFactionId, 2);
            }
        }

        private void EnsureEnemyCapital(Vector2Int enemyCenter, int tcCount, int barracksCount, int smelterCount)
        {
            if (tcCount == 0) _blueprints.QueueBuilding(BuildableKind.TownCenter, enemyCenter, OpenWorldConstants.EnemyFactionId, 2);
            if (barracksCount == 0) _blueprints.QueueBuilding(BuildableKind.Barracks, enemyCenter + new Vector2Int(4, 0), OpenWorldConstants.EnemyFactionId, 2);
            if (smelterCount == 0) _blueprints.QueueBuilding(BuildableKind.Smelter, enemyCenter + new Vector2Int(0, 4), OpenWorldConstants.EnemyFactionId, 2);
        }

        private void UpdatePressureSummary()
        {
            int militaryCount = 0;
            foreach (var u in _world.Units.Values)
                if (u.FactionId == OpenWorldConstants.EnemyFactionId && u.Kind != UnitKind.Scout && u.Kind != UnitKind.Worker) militaryCount++;
            PressureSummary = $"Threat: {_scouting.ThreatLevel} | Troops: {militaryCount} | WPN: {_economy.GetWeapons()}";
        }
    }
}
