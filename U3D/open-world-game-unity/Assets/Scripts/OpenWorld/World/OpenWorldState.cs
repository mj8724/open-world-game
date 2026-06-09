using System.Collections.Generic;
using UnityEngine;

namespace OpenWorld
{
    public class WorldChunk
    {
        public readonly Vector2Int Coord;
        public readonly int Size;
        public readonly SurfaceCell[,] Cells;
        public bool DirtyVisual = true;
        public bool DirtyPath = true;

        public WorldChunk(Vector2Int coord, int size)
        {
            Coord = coord;
            Size = size;
            Cells = new SurfaceCell[size, size];
        }
    }

    public class OpenWorldState
    {
        public readonly int MapSize;
        public readonly int ChunkSize;
        public readonly int Seed;
        public readonly ResourceInventory Inventory = new();
        public readonly Dictionary<Vector2Int, WorldChunk> Chunks = new();
        public readonly Dictionary<int, BuildingEntity> Buildings = new();
        public readonly Dictionary<int, UnitEntity> Units = new();
        public readonly Dictionary<int, VehicleEntity> Vehicles = new();
        public readonly List<JobRecord> Jobs = new();
        public readonly List<BlueprintJob> Blueprints = new();
        public readonly List<FactionRecord> Factions = new();
        public readonly List<DiplomacyRecord> Diplomacy = new();
        public readonly List<RegionRecord> Regions = new();
        public readonly List<StrategicSiteRecord> StrategicSites = new();
        public readonly List<LogisticsRoute> LogisticsRoutes = new();
        public readonly Queue<OpenWorldCommand> Commands = new();
        public readonly HashSet<Vector2Int> ModifiedCells = new();
        public readonly PopulationState Population = new();
        public readonly TechState Tech = new();
        public KnowledgeState[] KnowledgeCells;

        private int _nextBuildingId = 1;
        private int _nextUnitId = 1;
        private int _nextVehicleId = 1;
        private int _nextJobId = 1;
        private int _nextBlueprintId = 1;
        private int _nextRouteId = 1;
        private int _nextCommandId = 1;

        public OpenWorldState(int mapSize, int chunkSize, int seed)
        {
            MapSize = mapSize;
            ChunkSize = chunkSize;
            Seed = seed;
            Inventory.Dirt = 200;
            Inventory.Stone = 160;
            Inventory.IronOre = 60;
            Inventory.Coal = 35;
            Inventory.Wood = 180;
            Inventory.Food = 100;
            Inventory.Lumber = 45;
            Inventory.IronIngot = 8;
            Inventory.Fuel = 40;
            Inventory.Medicine = 12;
            Inventory.Ammo = 20;
            Inventory.MachineParts = 6;
            KnowledgeCells = new KnowledgeState[mapSize * mapSize];
            InitializeFactions();
            InitializeRegions();
        }

        public bool InBounds(Vector2Int cell) => cell.x >= 0 && cell.y >= 0 && cell.x < MapSize && cell.y < MapSize;

        public Vector2Int ToChunkCoord(Vector2Int cell) => new(Mathf.FloorToInt(cell.x / (float)ChunkSize), Mathf.FloorToInt(cell.y / (float)ChunkSize));

        public Vector2Int ToLocalCell(Vector2Int cell)
        {
            int x = ((cell.x % ChunkSize) + ChunkSize) % ChunkSize;
            int y = ((cell.y % ChunkSize) + ChunkSize) % ChunkSize;
            return new Vector2Int(x, y);
        }

        public WorldChunk GetOrCreateChunk(Vector2Int chunkCoord)
        {
            if (Chunks.TryGetValue(chunkCoord, out var chunk)) return chunk;
            chunk = new WorldChunk(chunkCoord, ChunkSize);
            GenerateChunk(chunk);
            Chunks[chunkCoord] = chunk;
            return chunk;
        }

        public SurfaceCell GetCell(Vector2Int cell)
        {
            if (!InBounds(cell)) return BlockedCell();
            var chunk = GetOrCreateChunk(ToChunkCoord(cell));
            var local = ToLocalCell(cell);
            return chunk.Cells[local.x, local.y];
        }

        public void SetCell(Vector2Int cell, SurfaceCell value)
        {
            if (!InBounds(cell)) return;
            var chunk = GetOrCreateChunk(ToChunkCoord(cell));
            var local = ToLocalCell(cell);
            chunk.Cells[local.x, local.y] = value;
            ModifiedCells.Add(cell);
            MarkDirty(cell);
        }

        public void MarkDirty(Vector2Int cell)
        {
            if (!InBounds(cell)) return;
            var chunkCoord = ToChunkCoord(cell);
            MarkChunkDirty(chunkCoord);

            var local = ToLocalCell(cell);
            if (local.x == 0) MarkChunkDirty(chunkCoord + Vector2Int.left);
            if (local.y == 0) MarkChunkDirty(chunkCoord + Vector2Int.down);
            if (local.x == ChunkSize - 1) MarkChunkDirty(chunkCoord + Vector2Int.right);
            if (local.y == ChunkSize - 1) MarkChunkDirty(chunkCoord + Vector2Int.up);
        }

        public void MarkChunkDirty(Vector2Int chunkCoord)
        {
            if (Chunks.TryGetValue(chunkCoord, out var chunk))
            {
                chunk.DirtyVisual = true;
                chunk.DirtyPath = true;
            }
        }

        public float GetHeight(Vector2Int cell)
        {
            cell.x = Mathf.Clamp(cell.x, 0, MapSize - 1);
            cell.y = Mathf.Clamp(cell.y, 0, MapSize - 1);
            return GetCell(cell).Height;
        }

        public Vector3 CellToWorld(Vector2Int cell)
        {
            float h = GetHeight(cell);
            return new Vector3(cell.x + 0.5f, h, cell.y + 0.5f);
        }

        public Vector2Int WorldToCell(Vector3 world) => new(Mathf.FloorToInt(world.x), Mathf.FloorToInt(world.z));

        public BuildingEntity AddBuilding(BuildableDef def, Vector2Int origin, int rotation, int factionId)
        {
            var building = new BuildingEntity
            {
                Id = _nextBuildingId++,
                Kind = def.Kind,
                Origin = origin,
                Size = def.Size,
                Rotation = rotation,
                Hp = def.MaxHp,
                FactionId = factionId
            };
            Buildings[building.Id] = building;
            return building;
        }

        public UnitEntity AddUnit(UnitKind kind, Vector2Int cell, int factionId)
        {
            var unit = new UnitEntity
            {
                Id = _nextUnitId++,
                Kind = kind,
                Cell = cell,
                FactionId = factionId,
                WorldPosition = CellToWorld(cell) + Vector3.up * 0.08f,
                Task = UnitTask.Idle
            };
            ApplyUnitDefaults(unit);
            Units[unit.Id] = unit;
            return unit;
        }

        public VehicleEntity AddVehicle(VehicleKind kind, Vector2Int cell, int factionId)
        {
            var vehicle = new VehicleEntity
            {
                Id = _nextVehicleId++,
                Kind = kind,
                FactionId = factionId,
                Cell = cell,
                WorldPosition = CellToWorld(cell) + Vector3.up * 0.08f,
                Task = VehicleTask.Idle
            };
            ApplyVehicleDefaults(vehicle);
            Vehicles[vehicle.Id] = vehicle;
            return vehicle;
        }

        public JobRecord AddJob(UnitTask task, Vector2Int target, BuildableKind buildKind = BuildableKind.Warehouse)
        {
            var job = new JobRecord
            {
                Id = _nextJobId++,
                Task = task,
                TargetCell = target,
                BuildKind = buildKind,
                AssignedUnitId = 0,
                WorkRemaining = task == UnitTask.Building ? 3.0f : 1.2f
            };
            Jobs.Add(job);
            return job;
        }

        public BlueprintJob AddBlueprint(BlueprintKind kind, Vector2Int cell, int radius, int factionId)
        {
            var blueprint = new BlueprintJob
            {
                Id = _nextBlueprintId++,
                Kind = kind,
                Cell = cell,
                Radius = radius,
                FactionId = factionId,
                WorkRemaining = Mathf.Max(1.0f, radius + 1.0f)
            };
            Blueprints.Add(blueprint);
            return blueprint;
        }

        public LogisticsRoute AddRoute(Vector2Int source, Vector2Int target, ResourceKind cargo, VehicleKind vehicleKind, int priority, LogisticsMode mode)
        {
            var route = new LogisticsRoute
            {
                Id = _nextRouteId++,
                Name = $"{cargo} {source.x},{source.y}->{target.x},{target.y}",
                Source = source,
                Target = target,
                CargoKind = cargo,
                PreferredVehicle = vehicleKind,
                Priority = priority,
                Mode = mode
            };
            LogisticsRoutes.Add(route);
            return route;
        }

        public OpenWorldCommand EnqueueCommand(CommandKind kind, int factionId)
        {
            var command = new OpenWorldCommand
            {
                Id = _nextCommandId++,
                Kind = kind,
                FactionId = factionId
            };
            Commands.Enqueue(command);
            return command;
        }

        public void RestoreFrom(OpenWorldSaveData data)
        {
            if (data == null) return;

            CopyInventory(data.Inventory, Inventory);
            CopyPopulation(data.Population, Population);
            CopyTech(data.Tech, Tech);

            Buildings.Clear();
            Units.Clear();
            Vehicles.Clear();
            Jobs.Clear();
            Blueprints.Clear();
            Factions.Clear();
            Regions.Clear();
            StrategicSites.Clear();
            Diplomacy.Clear();
            LogisticsRoutes.Clear();
            Commands.Clear();
            Chunks.Clear();
            ModifiedCells.Clear();

            if (data.Factions != null && data.Factions.Count > 0) Factions.AddRange(data.Factions);
            else InitializeFactions();

            if (data.Regions != null && data.Regions.Count > 0) Regions.AddRange(data.Regions);
            else InitializeRegions();

            if (data.StrategicSites != null) StrategicSites.AddRange(data.StrategicSites);
            if (data.Diplomacy != null) Diplomacy.AddRange(data.Diplomacy);
            if (data.LogisticsRoutes != null) LogisticsRoutes.AddRange(data.LogisticsRoutes);
            if (data.Jobs != null) Jobs.AddRange(data.Jobs);
            if (data.Blueprints != null) Blueprints.AddRange(data.Blueprints);

            if (data.ModifiedCells != null)
            {
                foreach (var saved in data.ModifiedCells)
                    SetCell(new Vector2Int(saved.X, saved.Z), saved.Cell);
            }

            if (data.Buildings != null)
            {
                foreach (var building in data.Buildings)
                    Buildings[building.Id] = building;
            }
            if (data.Units != null)
            {
                foreach (var unit in data.Units)
                    Units[unit.Id] = unit;
            }
            if (data.Vehicles != null)
            {
                foreach (var vehicle in data.Vehicles)
                    Vehicles[vehicle.Id] = vehicle;
            }

            if (data.KnowledgeCells != null && data.KnowledgeCells.Length == MapSize * MapSize)
                KnowledgeCells = data.KnowledgeCells;
            else
                KnowledgeCells = new KnowledgeState[MapSize * MapSize];

            if (data.Knowledge != null && data.Knowledge.Count > 0)
            {
                for (int i = 0; i < KnowledgeCells.Length; i++)
                    KnowledgeCells[i] = KnowledgeState.Unknown;
                foreach (var saved in data.Knowledge)
                {
                    var cell = new Vector2Int(saved.X, saved.Z);
                    if (!InBounds(cell)) continue;
                    KnowledgeCells[cell.y * MapSize + cell.x] = saved.State;
                }
            }

            _nextBuildingId = NextId(Buildings.Keys);
            _nextUnitId = NextId(Units.Keys);
            _nextVehicleId = NextId(Vehicles.Keys);
            _nextJobId = NextId(Jobs, j => j.Id);
            _nextBlueprintId = NextId(Blueprints, b => b.Id);
            _nextRouteId = NextId(LogisticsRoutes, r => r.Id);
            _nextCommandId = 1;
        }

        private void GenerateChunk(WorldChunk chunk)
        {
            for (int z = 0; z < ChunkSize; z++)
            {
                for (int x = 0; x < ChunkSize; x++)
                {
                    int wx = chunk.Coord.x * ChunkSize + x;
                    int wz = chunk.Coord.y * ChunkSize + z;
                    chunk.Cells[x, z] = GenerateCell(wx, wz);
                }
            }
        }

        private static int NextId(IEnumerable<int> ids)
        {
            int max = 0;
            foreach (int id in ids)
                if (id > max) max = id;
            return max + 1;
        }

        private static int NextId<T>(IEnumerable<T> items, System.Func<T, int> selector)
        {
            int max = 0;
            foreach (var item in items)
            {
                int id = selector(item);
                if (id > max) max = id;
            }
            return max + 1;
        }

        private static void CopyInventory(ResourceInventory from, ResourceInventory to)
        {
            if (from == null || to == null) return;
            to.Dirt = from.Dirt;
            to.Stone = from.Stone;
            to.IronOre = from.IronOre;
            to.Coal = from.Coal;
            to.Clay = from.Clay;
            to.Wood = from.Wood;
            to.Food = from.Food;
            to.Sulfur = from.Sulfur;
            to.Nitrate = from.Nitrate;
            to.Oil = from.Oil;
            to.Lumber = from.Lumber;
            to.Brick = from.Brick;
            to.IronIngot = from.IronIngot;
            to.Steel = from.Steel;
            to.MachineParts = from.MachineParts;
            to.Medicine = from.Medicine;
            to.Ammo = from.Ammo;
            to.Gunpowder = from.Gunpowder;
            to.Fuel = from.Fuel;
            to.Power = from.Power;
            to.Weapons = from.Weapons;
            to.RailParts = from.RailParts;
        }

        private static void CopyPopulation(PopulationState from, PopulationState to)
        {
            if (from == null || to == null) return;
            to.Residents = from.Residents;
            to.Workers = from.Workers;
            to.Soldiers = from.Soldiers;
            to.Drivers = from.Drivers;
            to.Engineers = from.Engineers;
            to.Doctors = from.Doctors;
            to.Homeless = from.Homeless;
            to.Wounded = from.Wounded;
            to.CityMorale = from.CityMorale;
            to.MedicalPressure = from.MedicalPressure;
        }

        private static void CopyTech(TechState from, TechState to)
        {
            if (from == null || to == null) return;
            to.Era = from.Era;
            to.ResearchProgress = from.ResearchProgress;
            to.CurrentResearch = from.CurrentResearch;
        }

        private SurfaceCell GenerateCell(int x, int z)
        {
            float low = Mathf.PerlinNoise((x + Seed) * 0.010f, (z - Seed) * 0.010f);
            float high = Mathf.PerlinNoise((x - Seed) * 0.035f, (z + Seed) * 0.035f);
            float height = Mathf.Round((low * 12f + high * 3f) * 2f) / 2f;

            SurfaceTerrain terrain = height switch
            {
                < 2.0f => SurfaceTerrain.Plains,
                < 7.5f => SurfaceTerrain.Hills,
                _ => SurfaceTerrain.Mountain
            };

            float forestNoise = Mathf.PerlinNoise((x + 91) * 0.045f, (z + 37) * 0.045f);
            if (terrain == SurfaceTerrain.Plains && forestNoise > 0.66f) terrain = SurfaceTerrain.Forest;

            bool iron = Mathf.PerlinNoise((x + Seed * 3) * 0.025f, (z - Seed * 5) * 0.025f) > 0.72f && height > 3.5f;
            bool coal = Mathf.PerlinNoise((x - Seed * 4) * 0.021f, (z + Seed * 2) * 0.021f) > 0.76f && height > 4.0f;
            bool oil = Mathf.PerlinNoise((x + 444) * 0.015f, (z - 888) * 0.015f) > 0.83f && height < 4.5f;
            var layers = iron
                ? new[] { new MaterialLayer(GroundMaterial.Dirt, 2), new MaterialLayer(GroundMaterial.Stone, 3), new MaterialLayer(GroundMaterial.IronOre, 8) }
                : coal
                    ? new[] { new MaterialLayer(GroundMaterial.Dirt, 2), new MaterialLayer(GroundMaterial.Stone, 3), new MaterialLayer(GroundMaterial.Coal, 8) }
                    : oil
                        ? new[] { new MaterialLayer(GroundMaterial.Dirt, 2), new MaterialLayer(GroundMaterial.Clay, 2), new MaterialLayer(GroundMaterial.Oil, 6) }
                : new[] { new MaterialLayer(GroundMaterial.Dirt, 3), new MaterialLayer(GroundMaterial.Stone, 8) };

            return new SurfaceCell
            {
                Height = height,
                Terrain = terrain,
                Layers = layers,
                CurrentLayer = 0,
                BuildingId = 0,
                ResourceRichness = iron || coal || oil ? 3 : forestNoise > 0.66f ? 2 : 1,
                RegionId = RegionIdFor(new Vector2Int(x, z))
            };
        }

        private void InitializeFactions()
        {
            Factions.Add(new FactionRecord { Id = OpenWorldConstants.PlayerFactionId, Name = "Frontier Union", Kind = FactionKind.Player, Color = new Color(0.25f, 0.54f, 0.92f), Reputation = 60 });
            Factions.Add(new FactionRecord { Id = OpenWorldConstants.EnemyFactionId, Name = "Iron Dominion", Kind = FactionKind.Enemy, Color = new Color(0.78f, 0.18f, 0.15f), Reputation = 10 });
            Factions.Add(new FactionRecord { Id = OpenWorldConstants.NeutralFactionId, Name = "Free Towns", Kind = FactionKind.Neutral, Color = new Color(0.78f, 0.68f, 0.28f), Reputation = 50 });
            Factions.Add(new FactionRecord { Id = OpenWorldConstants.AllyFactionId, Name = "Relief League", Kind = FactionKind.Ally, Color = new Color(0.30f, 0.72f, 0.55f), Reputation = 65 });

            Diplomacy.Add(new DiplomacyRecord { FactionA = OpenWorldConstants.PlayerFactionId, FactionB = OpenWorldConstants.EnemyFactionId, Stance = DiplomacyStance.Hostile, Trust = 0 });
            Diplomacy.Add(new DiplomacyRecord { FactionA = OpenWorldConstants.PlayerFactionId, FactionB = OpenWorldConstants.NeutralFactionId, Stance = DiplomacyStance.Trade, Trust = 50 });
            Diplomacy.Add(new DiplomacyRecord { FactionA = OpenWorldConstants.PlayerFactionId, FactionB = OpenWorldConstants.AllyFactionId, Stance = DiplomacyStance.Allied, Trust = 65 });
        }

        private void InitializeRegions()
        {
            int regionSize = Mathf.Max(64, MapSize / 4);
            int id = 1;
            for (int z = 0; z < 4; z++)
            {
                for (int x = 0; x < 4; x++)
                {
                    var center = new Vector2Int(Mathf.Min(MapSize - 1, x * regionSize + regionSize / 2), Mathf.Min(MapSize - 1, z * regionSize + regionSize / 2));
                    Regions.Add(new RegionRecord
                    {
                        Id = id,
                        Name = $"Region {id}",
                        Center = center,
                        Radius = regionSize / 2,
                        OwnerFactionId = OpenWorldConstants.NeutralFactionId,
                        Control = 0,
                        StrategicResource = (id % 5) switch
                        {
                            0 => ResourceKind.Coal,
                            1 => ResourceKind.Food,
                            2 => ResourceKind.IronOre,
                            3 => ResourceKind.Oil,
                            _ => ResourceKind.Stone
                        },
                        HasRailHub = id % 4 == 0,
                        HasNeutralTown = id % 3 == 0
                    });
                    id++;
                }
            }
        }

        private int RegionIdFor(Vector2Int cell)
        {
            if (Regions.Count == 0) return 0;
            int columns = 4;
            int regionSize = Mathf.Max(64, MapSize / columns);
            int x = Mathf.Clamp(cell.x / regionSize, 0, columns - 1);
            int z = Mathf.Clamp(cell.y / regionSize, 0, columns - 1);
            return z * columns + x + 1;
        }

        private static void ApplyUnitDefaults(UnitEntity unit)
        {
            switch (unit.Kind)
            {
                case UnitKind.Worker:
                    unit.AttackPower = 5;
                    unit.VisionRange = 12;
                    break;
                case UnitKind.Scout:
                    unit.AttackPower = 4;
                    unit.VisionRange = 26;
                    unit.Ammo = 5;
                    break;
                case UnitKind.Medic:
                    unit.AttackPower = 2;
                    unit.VisionRange = 14;
                    break;
                case UnitKind.Rifleman:
                case UnitKind.Musketeer:
                    unit.AttackPower = 14;
                    unit.Ammo = 35;
                    unit.VisionRange = 16;
                    break;
                case UnitKind.MachineGunner:
                    unit.AttackPower = 22;
                    unit.Ammo = 70;
                    unit.VisionRange = 18;
                    break;
                default:
                    unit.AttackPower = 8;
                    unit.VisionRange = 12;
                    break;
            }
        }

        private static void ApplyVehicleDefaults(VehicleEntity vehicle)
        {
            switch (vehicle.Kind)
            {
                case VehicleKind.HandCart:
                    vehicle.CargoCapacity = 40;
                    vehicle.Fuel = 0;
                    vehicle.MaxHp = 80;
                    vehicle.Hp = 80;
                    vehicle.CrewRequired = 1;
                    break;
                case VehicleKind.Wagon:
                    vehicle.CargoCapacity = 85;
                    vehicle.Fuel = 0;
                    vehicle.MaxHp = 120;
                    vehicle.Hp = 120;
                    break;
                case VehicleKind.Truck:
                    vehicle.CargoCapacity = 180;
                    vehicle.Fuel = 100;
                    vehicle.MaxHp = 160;
                    vehicle.Hp = 160;
                    break;
                case VehicleKind.ArmoredCar:
                    vehicle.CargoCapacity = 30;
                    vehicle.Fuel = 100;
                    vehicle.MaxHp = 260;
                    vehicle.Hp = 260;
                    vehicle.VisionRange = 24;
                    break;
                case VehicleKind.Locomotive:
                    vehicle.CargoCapacity = 0;
                    vehicle.Fuel = 150;
                    vehicle.MaxHp = 300;
                    vehicle.Hp = 300;
                    break;
                case VehicleKind.CargoWagon:
                    vehicle.CargoCapacity = 500;
                    vehicle.Fuel = 0;
                    vehicle.MaxHp = 220;
                    vehicle.Hp = 220;
                    break;
            }
        }

        private static SurfaceCell BlockedCell() => new()
        {
            Height = 0,
            Terrain = SurfaceTerrain.Water,
            Occupied = true,
            BuildingId = -1,
            Layers = new[] { new MaterialLayer(GroundMaterial.Stone, 1) }
        };
    }
}
