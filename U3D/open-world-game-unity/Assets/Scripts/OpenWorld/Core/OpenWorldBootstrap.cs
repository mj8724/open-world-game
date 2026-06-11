using UnityEngine;

namespace OpenWorld
{
    public class OpenWorldBootstrap : MonoBehaviour
    {
        [Header("World")]
        [SerializeField] private int _mapSize = 512;
        [SerializeField] private int _chunkSize = 64;
        [SerializeField] private int _seed = 8724;
        [SerializeField] private int _visibleChunkRadius = 2;

        public OpenWorldState World { get; private set; }
        public SurfaceTerrainSystem Terrain { get; private set; }
        public BuildingSystem Buildings { get; private set; }
        public UnitSystem Units { get; private set; }
        public VehicleSystem Vehicles { get; private set; }
        public OpenWorldJobSystem Jobs { get; private set; }
        public BlueprintSystem Blueprints { get; private set; }
        public WorldKnowledgeSystem Knowledge { get; private set; }
        public OpenWorldLogisticsSystem Logistics { get; private set; }
        public OpenWorldSimulationSystem Simulation { get; private set; }
        public OpenWorldGeologySystem Geology { get; private set; }
        public OpenWorldPerformanceSystem Performance { get; private set; }
        public OpenWorldCommandSystem Commands { get; private set; }
        public OpenWorldInputController Input { get; private set; }
        public OpenWorldHudController Hud { get; private set; }
        public OpenWorldStrategicMapController StrategicMap { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureBootstrap()
        {
            if (FindObjectOfType<OpenWorldBootstrap>() != null) return;
            var go = new GameObject("OpenWorldBootstrap");
            go.AddComponent<OpenWorldBootstrap>();
        }

        private void Awake()
        {
            Application.runInBackground = true;
            if (FindObjectsOfType<OpenWorldBootstrap>().Length > 1)
            {
                Destroy(gameObject);
                return;
            }
            DontDestroyOnLoad(gameObject);
            BuildWorld();
        }

        private void Update()
        {
            var cam = Camera.main;
            if (Terrain != null && cam != null)
                Terrain.UpdateStreaming(cam.transform.position + cam.transform.forward * 50f);
        }

        private void BuildWorld()
        {
            var camera = EnsureCamera();
            EnsureLight();

            bool loaded = OpenWorldSaveService.TryLoad(out var saveData);
            World = loaded
                ? new OpenWorldState(saveData.MapSize, saveData.ChunkSize, saveData.Seed)
                : new OpenWorldState(_mapSize, _chunkSize, _seed);
            if (loaded)
                World.RestoreFrom(saveData);
            PositionCamera(camera, World.MapSize);

            Terrain = CreateSystem<SurfaceTerrainSystem>("SurfaceTerrainSystem");
            Terrain.Initialize(World, _visibleChunkRadius);

            Buildings = CreateSystem<BuildingSystem>("BuildingSystem");
            Buildings.Initialize(World, Terrain);

            Units = CreateSystem<UnitSystem>("UnitSystem");
            Units.Initialize(World, Terrain);

            Vehicles = CreateSystem<VehicleSystem>("VehicleSystem");
            Vehicles.Initialize(World, Terrain);

            Jobs = CreateSystem<OpenWorldJobSystem>("OpenWorldJobSystem");
            Jobs.Initialize(World, Terrain, Buildings, Units);

            Blueprints = CreateSystem<BlueprintSystem>("BlueprintSystem");
            Blueprints.Initialize(World, Terrain, Buildings, Units);

            Geology = CreateSystem<OpenWorldGeologySystem>("OpenWorldGeologySystem");
            Geology.Initialize(World, Units, Buildings, Blueprints);
            Terrain.SetGeology(Geology);

            Knowledge = CreateSystem<WorldKnowledgeSystem>("WorldKnowledgeSystem");
            Knowledge.Initialize(World);

            Logistics = CreateSystem<OpenWorldLogisticsSystem>("OpenWorldLogisticsSystem");
            Logistics.Initialize(World, Vehicles);

            Simulation = CreateSystem<OpenWorldSimulationSystem>("OpenWorldSimulationSystem");
            Simulation.Initialize(World, Terrain, Buildings, Units, Blueprints, Vehicles);

            Performance = CreateSystem<OpenWorldPerformanceSystem>("OpenWorldPerformanceSystem");
            Performance.Initialize(World, camera, Units, Vehicles);

            Commands = CreateSystem<OpenWorldCommandSystem>("OpenWorldCommandSystem");
            Commands.Initialize(World, Terrain, Units, Vehicles, Blueprints, Logistics, Geology, Simulation);

            StrategicMap = FindObjectOfType<OpenWorldStrategicMapController>();

            Input = CreateSystem<OpenWorldInputController>("OpenWorldInputController");
            Input.Initialize(camera, Terrain, Buildings, Units, Vehicles, Jobs, Commands, StrategicMap, Knowledge);

            if (loaded)
                RebuildLoadedWorld();
            else
                SeedStartingBase();
            InitializeHud();
            Debug.Log(loaded ? "[OpenWorld] Surface open-world slice loaded from save." : "[OpenWorld] Surface open-world slice initialized.");
        }

        private T CreateSystem<T>(string name) where T : Component
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            return go.AddComponent<T>();
        }

        private Camera EnsureCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                cam = go.AddComponent<Camera>();
            }

            cam.fieldOfView = 55f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 2500f;
            if (cam.GetComponent<OpenWorldCameraController>() == null)
                cam.gameObject.AddComponent<OpenWorldCameraController>();
            return cam;
        }

        private static void PositionCamera(Camera camera, int mapSize)
        {
            if (camera == null) return;
            float center = mapSize * 0.5f;
            camera.transform.position = new Vector3(center - 30f, 55f, center - 45f);
            camera.transform.rotation = Quaternion.Euler(55f, 38f, 0f);
        }

        private void EnsureLight()
        {
            var light = FindObjectOfType<Light>();
            if (light == null)
            {
                var go = new GameObject("Directional Light");
                light = go.AddComponent<Light>();
                light.type = LightType.Directional;
            }
            light.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
            light.intensity = 1.1f;
            light.shadows = LightShadows.Soft;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.72f, 0.82f);
            RenderSettings.ambientEquatorColor = new Color(0.45f, 0.50f, 0.45f);
            RenderSettings.ambientGroundColor = new Color(0.28f, 0.25f, 0.22f);
        }

        private void SeedStartingBase()
        {
            var center = new Vector2Int(_mapSize / 2, _mapSize / 2);
            Terrain.ApplyBrush(TerrainTool.Flatten, center, 9, 32f);
            Terrain.ApplyBrush(TerrainTool.Road, center + new Vector2Int(0, -7), 1, 0.5f);

            Buildings.TryPlace(BuildableKind.TownCenter, center + new Vector2Int(-2, -2), 0, 1, spendCost: false);
            Buildings.TryPlace(BuildableKind.Warehouse, center + new Vector2Int(5, -2), 0, 1, spendCost: false);
            Buildings.TryPlace(BuildableKind.House, center + new Vector2Int(-7, 3), 0, 1, spendCost: false);
            Buildings.TryPlace(BuildableKind.Farm, center + new Vector2Int(-12, -8), 0, 1, spendCost: false);
            Buildings.TryPlace(BuildableKind.ScoutTower, center + new Vector2Int(10, 4), 0, 1, spendCost: false);
            Buildings.TryPlace(BuildableKind.MinePost, center + new Vector2Int(24, -8), 0, 1, spendCost: false);
            Buildings.TryPlace(BuildableKind.Smelter, center + new Vector2Int(26, -6), 0, 1, spendCost: false);
            Buildings.TryPlace(BuildableKind.Steelworks, center + new Vector2Int(30, -6), 0, 1, spendCost: false);
            Buildings.TryPlace(BuildableKind.MachineShop, center + new Vector2Int(35, -6), 0, 1, spendCost: false);
            Buildings.TryPlace(BuildableKind.VehicleFactory, center + new Vector2Int(34, -12), 0, 1, spendCost: false);
            Buildings.TryPlace(BuildableKind.Garage, center + new Vector2Int(29, -12), 0, 1, spendCost: false);
            Buildings.TryPlace(BuildableKind.Barracks, center + new Vector2Int(-10, 6), 0, 1, spendCost: false);
            Buildings.TryPlace(BuildableKind.Clinic, center + new Vector2Int(-5, 8), 0, 1, spendCost: false);
            Buildings.TryPlace(BuildableKind.ControlPoint, center + new Vector2Int(-12, 10), 0, 1, spendCost: false);

            Units.Spawn(UnitKind.Worker, center + new Vector2Int(2, 4), 1);
            Units.Spawn(UnitKind.Worker, center + new Vector2Int(4, 4), 1);
            Units.Spawn(UnitKind.Worker, center + new Vector2Int(6, 4), 1);
            Units.Spawn(UnitKind.Engineer, center + new Vector2Int(7, 6), 1);
            Units.Spawn(UnitKind.Melee, center + new Vector2Int(-4, 5), 1);
            Units.Spawn(UnitKind.Scout, center + new Vector2Int(-6, 7), 1);
            Units.Spawn(UnitKind.Medic, center + new Vector2Int(-2, 7), 1);

            Vehicles.Spawn(VehicleKind.HandCart, center + new Vector2Int(7, 3), 1);
            Vehicles.Spawn(VehicleKind.Wagon, center + new Vector2Int(9, 3), 1);

            var warehouse = center + new Vector2Int(5, -2);
            var supplyDepot = center + new Vector2Int(28, -12);
            Terrain.ApplyBrush(TerrainTool.Flatten, supplyDepot, 4, 32f);
            Buildings.TryPlace(BuildableKind.Warehouse, supplyDepot, 0, OpenWorldConstants.PlayerFactionId, spendCost: false);
            ApplyRoadLine(warehouse, center + new Vector2Int(28, -2));
            ApplyRoadLine(center + new Vector2Int(28, -2), supplyDepot);
            Terrain.ApplyBrush(TerrainTool.Bridge, center + new Vector2Int(18, -2), 1, 0.5f);
            var westStationCell = center + new Vector2Int(12, -7);
            var eastStationCell = center + new Vector2Int(44, -7);
            Terrain.ApplyBrush(TerrainTool.Flatten, westStationCell, 4, 32f);
            Terrain.ApplyBrush(TerrainTool.Flatten, eastStationCell, 4, 32f);
            Buildings.TryPlace(BuildableKind.Station, westStationCell, 0, OpenWorldConstants.PlayerFactionId, spendCost: false);
            Buildings.TryPlace(BuildableKind.Station, eastStationCell, 0, OpenWorldConstants.PlayerFactionId, spendCost: false);
            Buildings.TryPlace(BuildableKind.TrainFactory, center + new Vector2Int(44, -13), 0, OpenWorldConstants.PlayerFactionId, spendCost: false);
            for (int x = westStationCell.x - 2; x <= eastStationCell.x + 6; x++)
                Terrain.ApplyBrush(TerrainTool.Rail, new Vector2Int(x, westStationCell.y - 2), 0, 0.5f);

            var westStation = FindBuilding(BuildableKind.Station, westStationCell);
            var eastStation = FindBuilding(BuildableKind.Station, eastStationCell);
            if (westStation != null && eastStation != null)
            {
                var westAccess = World.FindBuildingAccessCell(westStation, eastStation.Origin);
                var eastAccess = World.FindBuildingAccessCell(eastStation, westStation.Origin);
                ApplyRailLine(westAccess, eastAccess);
                World.AddToStorage(westStation, ResourceKind.IronOre, 60);
                var locomotive = Vehicles.SpawnScenarioVehicle(VehicleKind.Locomotive, westAccess, OpenWorldConstants.PlayerFactionId);
                var wagon = Vehicles.SpawnScenarioVehicle(VehicleKind.CargoWagon, westAccess + Vector2Int.left, OpenWorldConstants.PlayerFactionId);
                var railRoute = World.AddRoute(westStation.Id, eastStation.Id, westAccess, eastAccess, ResourceKind.IronOre, VehicleKind.Locomotive, 5, LogisticsMode.Automatic);
                railRoute.Name = "Foundry Rail Shuttle";
                railRoute.TargetStock = 40;
                if (locomotive != null && wagon != null)
                    World.RailSchedules.Add(new RailSchedule { Id = 1, LocomotiveId = locomotive.Entity.Id, WagonIds = new System.Collections.Generic.List<int> { wagon.Entity.Id }, StationBuildingIds = new System.Collections.Generic.List<int> { westStation.Id, eastStation.Id }, CargoKind = ResourceKind.IronOre, Status = "Ready" });
            }

            SeedWorldSites(center);
            var sourceBuilding = World.FindNearestStorage(warehouse, OpenWorldConstants.PlayerFactionId, 8);
            var targetBuilding = World.FindNearestStorage(supplyDepot, OpenWorldConstants.PlayerFactionId, 8);
            SeedStartingStorage(sourceBuilding);
            SeedStarterRoutes(center, sourceBuilding, targetBuilding);
            Knowledge.RevealCircle(center, 38);
        }

        private BuildingEntity FindBuilding(BuildableKind kind, Vector2Int origin)
        {
            foreach (var building in World.Buildings.Values)
                if (building.Kind == kind && building.Origin == origin) return building;
            return null;
        }

        private void ApplyRailLine(Vector2Int from, Vector2Int to)
        {
            var current = from;
            Terrain.ApplyBrush(TerrainTool.Rail, current, 0, 0.5f);
            while (current.x != to.x)
            {
                current.x += current.x < to.x ? 1 : -1;
                Terrain.ApplyBrush(TerrainTool.Rail, current, 0, 0.5f);
            }
            while (current.y != to.y)
            {
                current.y += current.y < to.y ? 1 : -1;
                Terrain.ApplyBrush(TerrainTool.Rail, current, 0, 0.5f);
            }
        }

        private void SeedStartingStorage(BuildingEntity warehouse)
        {
            if (warehouse == null) return;
            foreach (ResourceKind kind in System.Enum.GetValues(typeof(ResourceKind)))
            {
                int available = World.Inventory.Get(kind);
                if (available <= 0) continue;
                int move = Mathf.Max(1, Mathf.FloorToInt(available * 0.6f));
                int accepted = World.AddToStorage(warehouse, kind, move);
                World.Inventory.Add(kind, -accepted);
            }
            warehouse.LastStorageStatus = "Starter stock ready";
        }

        private void SeedStarterRoutes(Vector2Int center, BuildingEntity mainWarehouse, BuildingEntity supplyWarehouse)
        {
            if (World.LogisticsRoutes.Count > 0) return;
            var farm = FindNearestBuilding(center + new Vector2Int(-12, -8), BuildableKind.Farm);
            var mine = FindNearestBuilding(center + new Vector2Int(24, -8), BuildableKind.MinePost);
            var smelter = FindNearestBuilding(center + new Vector2Int(26, -6), BuildableKind.Smelter);
            var steelworks = FindNearestBuilding(center + new Vector2Int(30, -6), BuildableKind.Steelworks);
            var machineShop = FindNearestBuilding(center + new Vector2Int(35, -6), BuildableKind.MachineShop);

            AddStarterRoute(farm, mainWarehouse, ResourceKind.Food, 150, 7);
            AddStarterRoute(mainWarehouse, supplyWarehouse, ResourceKind.Food, 60, 4);
            AddStarterRoute(mine, smelter, ResourceKind.IronOre, 60, 8);
            AddStarterRoute(mine, smelter, ResourceKind.Coal, 40, 8);
            AddStarterRoute(smelter, steelworks, ResourceKind.IronIngot, 40, 6);
            AddStarterRoute(steelworks, machineShop, ResourceKind.Steel, 30, 5);
        }

        private BuildingEntity FindNearestBuilding(Vector2Int cell, BuildableKind kind)
        {
            BuildingEntity best = null;
            int bestDistance = int.MaxValue;
            foreach (var building in World.Buildings.Values)
            {
                if (building.FactionId != OpenWorldConstants.PlayerFactionId || building.Kind != kind) continue;
                int distance = (building.Origin - cell).sqrMagnitude;
                if (distance >= bestDistance) continue;
                best = building;
                bestDistance = distance;
            }
            return best;
        }

        private void AddStarterRoute(BuildingEntity source, BuildingEntity target, ResourceKind cargo, int targetStock, int priority)
        {
            if (source == null || target == null) return;
            var route = World.AddRoute(source.Id, target.Id, source.Origin, target.Origin, cargo, VehicleKind.HandCart, priority, LogisticsMode.Automatic);
            route.TargetStock = targetStock;
        }

        private void ApplyRoadLine(Vector2Int from, Vector2Int to)
        {
            var current = from;
            int guard = _mapSize * 2;
            while (guard-- > 0)
            {
                Terrain.ApplyBrush(TerrainTool.Road, current, 1, 0.5f);
                if (current == to) break;
                if (current.x != to.x)
                    current.x += current.x < to.x ? 1 : -1;
                else if (current.y != to.y)
                    current.y += current.y < to.y ? 1 : -1;
            }
        }

        private void RebuildLoadedWorld()
        {
            NormalizeLoadedEnemyOrders();
            Buildings.RebuildFromWorld();
            Units.RebuildFromWorld();
            Vehicles.RebuildFromWorld();
            Blueprints.RebuildFromWorld();
            Knowledge.RefreshVisibilityNow();
        }

        private void NormalizeLoadedEnemyOrders()
        {
            int activeRaiders = 0;
            foreach (var unit in World.Units.Values)
            {
                if (unit.FactionId != OpenWorldConstants.EnemyFactionId || unit.CurrentOrder?.Kind != UnitOrderKind.Attack) continue;
                activeRaiders++;
                if (activeRaiders <= 2) continue;
                unit.CurrentOrder = new UnitOrder();
                unit.Task = UnitTask.Idle;
            }
        }

        private void SeedWorldSites(Vector2Int center)
        {
            var enemy = center + new Vector2Int(95, 90);
            Terrain.ApplyBrush(TerrainTool.Flatten, enemy, 7, 32f);
            Buildings.TryPlace(BuildableKind.ControlPoint, enemy + new Vector2Int(-2, -2), 0, OpenWorldConstants.EnemyFactionId, spendCost: false);
            Buildings.TryPlace(BuildableKind.Barracks, enemy + new Vector2Int(5, -2), 0, OpenWorldConstants.EnemyFactionId, spendCost: false);
            Buildings.TryPlace(BuildableKind.Tower, enemy + new Vector2Int(-7, 4), 0, OpenWorldConstants.EnemyFactionId, spendCost: false);
            Units.Spawn(UnitKind.Militia, enemy + new Vector2Int(2, 5), OpenWorldConstants.EnemyFactionId);
            Units.Spawn(UnitKind.Ranged, enemy + new Vector2Int(4, 5), OpenWorldConstants.EnemyFactionId);

            var neutral = center + new Vector2Int(-80, 54);
            Terrain.ApplyBrush(TerrainTool.Flatten, neutral, 6, 28f);
            Buildings.TryPlace(BuildableKind.Market, neutral + new Vector2Int(-2, -2), 0, OpenWorldConstants.NeutralFactionId, spendCost: false);
            Buildings.TryPlace(BuildableKind.Warehouse, neutral + new Vector2Int(4, -2), 0, OpenWorldConstants.NeutralFactionId, spendCost: false);

            AddSite("Iron Ridge", center + new Vector2Int(34, -18), OpenWorldConstants.NeutralFactionId, BuildableKind.MinePost, ResourceKind.IronOre);
            AddSite("Coal Basin", center + new Vector2Int(60, 28), OpenWorldConstants.NeutralFactionId, BuildableKind.MinePost, ResourceKind.Coal);
            AddSite("Free Town Market", neutral, OpenWorldConstants.NeutralFactionId, BuildableKind.Market, ResourceKind.Food);
            AddSite("Dominion Fort", enemy, OpenWorldConstants.EnemyFactionId, BuildableKind.ControlPoint, ResourceKind.Weapons);
        }

        private void AddSite(string name, Vector2Int cell, int factionId, BuildableKind kind, ResourceKind resource)
        {
            World.StrategicSites.Add(new StrategicSiteRecord
            {
                Id = World.StrategicSites.Count + 1,
                Name = name,
                Cell = cell,
                FactionId = factionId,
                SiteKind = kind,
                Resource = resource,
                RegionId = World.GetCell(cell).RegionId,
                Revealed = factionId == OpenWorldConstants.PlayerFactionId
            });
        }

        private void InitializeHud()
        {
            Hud = FindObjectOfType<OpenWorldHudController>();
            if (Hud == null)
            {
                Debug.LogWarning("[OpenWorld] OpenWorldHudController not found. Gameplay systems initialized without HUD.");
                return;
            }

            if (StrategicMap == null)
                StrategicMap = Hud.GetComponent<OpenWorldStrategicMapController>();
            if (StrategicMap == null)
                StrategicMap = Hud.gameObject.AddComponent<OpenWorldStrategicMapController>();
            if (StrategicMap != null)
                StrategicMap.Initialize(World, Knowledge, Camera.main, Commands);
            Input.SetStrategicMap(StrategicMap);

            Hud.Initialize(World, Input, Units, Vehicles, Knowledge, Logistics, Simulation, Geology, Commands);
        }
    }
}
