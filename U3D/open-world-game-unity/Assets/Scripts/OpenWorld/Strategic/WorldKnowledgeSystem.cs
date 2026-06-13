using UnityEngine;

namespace OpenWorld
{
    public class WorldKnowledgeSystem : MonoBehaviour
    {
        public int ExploredCells { get; private set; }
        public int VisibleCells { get; private set; }
        public StrategicOverlay CurrentOverlay { get; private set; } = StrategicOverlay.Exploration;

        private OpenWorldState _world;
        private float _tickTimer;
        private int _mapSize;

        public void Initialize(OpenWorldState world)
        {
            _world = world;
            _mapSize = world.MapSize;
            if (world.KnowledgeCells == null || world.KnowledgeCells.Length != world.MapSize * world.MapSize)
                world.KnowledgeCells = new KnowledgeState[world.MapSize * world.MapSize];
        }

        private void Update()
        {
            if (_world == null) return;
            _tickTimer += Time.deltaTime;
            if (_tickTimer < 0.35f) return;
            _tickTimer = 0f;
            RefreshVisibility();
        }

        public void NextOverlay()
        {
            var next = (int)CurrentOverlay + 1;
            if (next > (int)StrategicOverlay.MoraleMedical) next = 0;
            CurrentOverlay = (StrategicOverlay)next;
        }

        public void RefreshVisibilityNow()
        {
            if (_world == null) return;
            RefreshVisibility();
        }

        public KnowledgeState GetState(Vector2Int cell)
        {
            if (_world == null || !_world.InBounds(cell)) return KnowledgeState.Unknown;
            return _world.KnowledgeCells[Index(cell)];
        }

        public void RevealCircle(Vector2Int center, int radius)
        {
            if (_world == null) return;
            int r2 = radius * radius;
            for (int z = -radius; z <= radius; z++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    if (x * x + z * z > r2) continue;
                    var cell = center + new Vector2Int(x, z);
                    if (!_world.InBounds(cell)) continue;
                    _world.KnowledgeCells[Index(cell)] = KnowledgeState.Visible;
                }
            }
        }

        public void RenderMap(Texture2D texture)
        {
            if (_world == null) return;
            RenderMap(texture, new RectInt(0, 0, _world.MapSize, _world.MapSize));
        }

        public void RenderMap(Texture2D texture, RectInt cellBounds)
        {
            if (_world == null || texture == null) return;
            int width = texture.width;
            int height = texture.height;
            int xMin = Mathf.Clamp(cellBounds.xMin, 0, _world.MapSize - 1);
            int yMin = Mathf.Clamp(cellBounds.yMin, 0, _world.MapSize - 1);
            int xMax = Mathf.Clamp(cellBounds.xMax, xMin + 1, _world.MapSize);
            int yMax = Mathf.Clamp(cellBounds.yMax, yMin + 1, _world.MapSize);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var cell = new Vector2Int(
                        Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(xMin, xMax - 1, x / (float)(width - 1))), 0, _world.MapSize - 1),
                        Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(yMin, yMax - 1, y / (float)(height - 1))), 0, _world.MapSize - 1));
                    texture.SetPixel(x, y, ColorFor(cell));
                }
            }
            texture.Apply(false);
        }

        private void RefreshVisibility()
        {
            VisibleCells = 0;
            ExploredCells = 0;
            for (int i = 0; i < _world.KnowledgeCells.Length; i++)
            {
                if (_world.KnowledgeCells[i] == KnowledgeState.Visible)
                    _world.KnowledgeCells[i] = KnowledgeState.Explored;
                if (_world.KnowledgeCells[i] == KnowledgeState.Explored)
                    ExploredCells++;
            }

            foreach (var unit in _world.Units.Values)
            {
                if (unit.FactionId == OpenWorldConstants.PlayerFactionId)
                    RevealCircle(unit.Cell, Mathf.Max(8, unit.VisionRange));
            }

            foreach (var vehicle in _world.Vehicles.Values)
            {
                if (vehicle.FactionId == OpenWorldConstants.PlayerFactionId)
                    RevealCircle(vehicle.Cell, Mathf.Max(8, vehicle.VisionRange));
            }

            foreach (var building in _world.Buildings.Values)
            {
                if (building.FactionId != OpenWorldConstants.PlayerFactionId) continue;
                int vision = building.Kind switch
                {
                    BuildableKind.ScoutTower => 32,
                    BuildableKind.ControlPoint => 24,
                    BuildableKind.TownCenter => 24,
                    _ => 12
                };
                RevealCircle(building.Origin + building.Size / 2, vision);
            }

            foreach (var site in _world.StrategicSites)
            {
                if (!site.Revealed && GetState(site.Cell) != KnowledgeState.Unknown)
                    site.Revealed = true;
            }

            foreach (var unit in _world.Units.Values)
            {
                if (unit.FactionId == OpenWorldConstants.PlayerFactionId || GetState(unit.Cell) != KnowledgeState.Visible) continue;
                IntelSnapshot snapshot = null;
                foreach (var intel in _world.IntelSnapshots)
                    if (intel.EntityId == unit.Id && intel.EntityType == "Unit") { snapshot = intel; break; }
                if (snapshot == null)
                {
                    snapshot = new IntelSnapshot { EntityId = unit.Id, EntityType = "Unit", FactionId = unit.FactionId };
                    _world.IntelSnapshots.Add(snapshot);
                }
                snapshot.Cell = unit.Cell;
                snapshot.SeenAt = Time.time;
            }

            for (int i = 0; i < _world.KnowledgeCells.Length; i++)
            {
                if (_world.KnowledgeCells[i] == KnowledgeState.Visible)
                {
                    VisibleCells++;
                    ExploredCells++;
                }
            }
        }

        private Color ColorFor(Vector2Int cell)
        {
            var state = GetState(cell);
            if (state == KnowledgeState.Unknown) return new Color(0.02f, 0.025f, 0.03f, 1f);

            var surface = _world.GetCell(cell);
            Color color = CurrentOverlay switch
            {
                StrategicOverlay.Geology => GeologyColor(cell),
                StrategicOverlay.Territory => TerritoryColor(surface.RegionId),
                StrategicOverlay.Resources => ResourceColor(surface.TopMaterial, surface.ResourceRichness),
                StrategicOverlay.RoadsRails => TransportColor(surface),
                StrategicOverlay.Blueprints => BlueprintColor(cell),
                StrategicOverlay.MoraleMedical => MoraleColor(),
                _ => TerrainColor(surface)
            };

            if (state == KnowledgeState.Explored)
                color = Color.Lerp(color, new Color(0.08f, 0.09f, 0.10f), 0.55f);

            return color;
        }

        private Color TerritoryColor(int regionId)
        {
            var owner = OpenWorldConstants.NeutralFactionId;
            foreach (var region in _world.Regions)
            {
                if (region.Id == regionId)
                {
                    owner = region.OwnerFactionId;
                    break;
                }
            }
            return owner switch
            {
                OpenWorldConstants.PlayerFactionId => new Color(0.12f, 0.34f, 0.68f),
                OpenWorldConstants.EnemyFactionId => new Color(0.62f, 0.10f, 0.08f),
                OpenWorldConstants.AllyFactionId => new Color(0.10f, 0.50f, 0.32f),
                _ => new Color(0.40f, 0.35f, 0.20f)
            };
        }

private static Color TerrainColor(SurfaceCell surface)
    {
        if (surface.HasBridge) return new Color(0.56f, 0.36f, 0.20f);
        if (surface.HasRail) return new Color(0.16f, 0.16f, 0.16f);
        if (surface.HasRoad) return new Color(0.52f, 0.44f, 0.32f);
        return surface.Terrain switch
        {
            SurfaceTerrain.Forest => new Color(0.12f, 0.32f, 0.15f),
            SurfaceTerrain.Hills => new Color(0.36f, 0.42f, 0.24f),
            SurfaceTerrain.Mountain => new Color(0.46f, 0.47f, 0.44f),
            SurfaceTerrain.Water => new Color(0.08f, 0.28f, 0.52f),
            SurfaceTerrain.Shallows => new Color(0.10f, 0.35f, 0.42f),
            _ => new Color(0.20f, 0.42f, 0.18f)
        };
    }

        private static Color ResourceColor(GroundMaterial material, int richness)
        {
            float boost = Mathf.Clamp01(richness / 4f);
            return material switch
            {
                GroundMaterial.IronOre => Color.Lerp(new Color(0.35f, 0.18f, 0.10f), new Color(0.90f, 0.35f, 0.18f), boost),
                GroundMaterial.Coal => Color.Lerp(new Color(0.05f, 0.05f, 0.05f), new Color(0.35f, 0.35f, 0.35f), boost),
                GroundMaterial.Oil => new Color(0.05f, 0.04f, 0.02f),
                GroundMaterial.Wood => new Color(0.12f, 0.40f, 0.15f),
                GroundMaterial.Food => new Color(0.55f, 0.62f, 0.22f),
                _ => new Color(0.28f, 0.28f, 0.24f)
            };
        }

        private static Color TransportColor(SurfaceCell surface)
        {
            if (surface.HasRail) return new Color(0.88f, 0.82f, 0.48f);
            if (surface.HasRoad) return new Color(0.70f, 0.58f, 0.36f);
            if (surface.HasBridge) return new Color(0.85f, 0.54f, 0.24f);
            return TerrainColor(surface) * 0.45f;
        }

        private Color BlueprintColor(Vector2Int cell)
        {
            foreach (var blueprint in _world.Blueprints)
            {
                if (blueprint.Status == BlueprintStatus.Cancelled || blueprint.Status == BlueprintStatus.Complete) continue;
                if ((blueprint.Cell - cell).sqrMagnitude <= Mathf.Max(1, blueprint.Radius * blueprint.Radius))
                    return blueprint.Status == BlueprintStatus.Paused ? new Color(0.75f, 0.72f, 0.20f) : new Color(0.30f, 0.75f, 0.95f);
            }
            return new Color(0.10f, 0.12f, 0.14f);
        }

        private Color MoraleColor()
        {
            float morale = Mathf.Clamp01(_world.Population.CityMorale / 100f);
            return Color.Lerp(new Color(0.65f, 0.10f, 0.12f), new Color(0.12f, 0.58f, 0.30f), morale);
        }

        private Color GeologyColor(Vector2Int cell)
        {
            var survey = _world.GetSurvey(cell);
            if (survey == null || survey.State == SurveyState.Unknown) return new Color(0.035f, 0.04f, 0.045f);
            if (survey.State == SurveyState.Exhausted) return new Color(0.22f, 0.22f, 0.22f);
            Color mineral = survey.EstimatedMaterial switch
            {
                GroundMaterial.IronOre => new Color(0.92f, 0.30f, 0.12f),
                GroundMaterial.Coal => new Color(0.16f, 0.16f, 0.18f),
                GroundMaterial.Oil => new Color(0.12f, 0.08f, 0.16f),
                GroundMaterial.Sulfur => new Color(0.92f, 0.78f, 0.12f),
                GroundMaterial.Nitrate => new Color(0.66f, 0.78f, 0.82f),
                GroundMaterial.Clay => new Color(0.62f, 0.34f, 0.22f),
                _ => new Color(0.48f, 0.48f, 0.44f)
            };
            float confidence = survey.State == SurveyState.Drilled ? 1f : Mathf.Clamp01(survey.Confidence);
            return Color.Lerp(new Color(0.08f, 0.09f, 0.10f), mineral, confidence);
        }

        private int Index(Vector2Int cell) => cell.y * _mapSize + cell.x;
    }
}
