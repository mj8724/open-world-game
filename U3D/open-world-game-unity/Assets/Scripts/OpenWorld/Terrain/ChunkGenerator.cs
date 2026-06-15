using UnityEngine;

namespace OpenWorld.Terrain
{
    public class ChunkGenerator
    {
        public int MapSize { get; }
        public int ChunkSize { get; }
        public int Seed { get; }
        private readonly System.Func<Vector2Int, int> _regionIdFor;

        public ChunkGenerator(int mapSize, int chunkSize, int seed, System.Func<Vector2Int, int> regionIdFor)
        {
            MapSize = mapSize;
            ChunkSize = chunkSize;
            Seed = seed;
            _regionIdFor = regionIdFor;
        }

        public void GenerateChunk(WorldChunk chunk)
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

        private SurfaceCell GenerateCell(int x, int z)
        {
            const int mapHalfX = 256;
            const float riverHalfWidth = 4f;

            // === Central River (symmetric barrier at X=256) ===
            int distFromCenter = Mathf.Abs(x - mapHalfX);
            if (distFromCenter <= riverHalfWidth)
            {
                float edgeFactor = distFromCenter / riverHalfWidth;
                SurfaceTerrain riverTerrain;
                float riverHeight;

                if (edgeFactor < 0.4f)
                {
                    riverTerrain = SurfaceTerrain.Water;
                    riverHeight = 1.0f;
                }
                else if (edgeFactor < 0.7f)
                {
                    riverTerrain = SurfaceTerrain.Water;
                    riverHeight = 2.0f;
                }
                else
                {
                    riverTerrain = SurfaceTerrain.Shallows;
                    riverHeight = 3.0f;
                }

                // Ford crossing points — shallow passable zones
                int[] fordZValues = { 80, 190, 300, 410 };
                foreach (int fordZ in fordZValues)
                {
                    if (Mathf.Abs(z - fordZ) <= 3)
                    {
                        riverTerrain = SurfaceTerrain.Shallows;
                        riverHeight = 3.5f;
                        break;
                    }
                }

                return new SurfaceCell
                {
                    Height = riverHeight,
                    Terrain = riverTerrain,
                    Layers = new[] { new MaterialLayer(GroundMaterial.Dirt, 1), new MaterialLayer(GroundMaterial.Stone, 6) },
                    CurrentLayer = 0,
                    BuildingId = 0,
                    ResourceRichness = 0,
                    RegionId = _regionIdFor(new Vector2Int(x, z))
                };
            }

            // === Symmetric Terrain: mirror right side to left side ===
            int genX = (x > mapHalfX) ? (mapHalfX * 2 - x) : x;

            float low = Mathf.PerlinNoise((genX + Seed) * 0.010f, (z - Seed) * 0.010f);
            float high = Mathf.PerlinNoise((genX - Seed) * 0.035f, (z + Seed) * 0.035f);
            float height = Mathf.Round((low * 12f + high * 3f) * 2f) / 2f;

            SurfaceTerrain terrain = height switch
            {
                < 2.0f => SurfaceTerrain.Plains,
                < 7.5f => SurfaceTerrain.Hills,
                _ => SurfaceTerrain.Mountain
            };

            float forestNoise = Mathf.PerlinNoise((genX + 91) * 0.045f, (z + 37) * 0.045f);
            if (terrain == SurfaceTerrain.Plains && forestNoise > 0.66f) terrain = SurfaceTerrain.Forest;

            // River generation - winding channels that require bridges (uses genX for symmetry)
            float riverX = Mathf.PerlinNoise((genX + Seed * 2) * 0.006f, (z - 137) * 0.0042f);
            float riverZ = Mathf.PerlinNoise((genX + 229) * 0.0042f, (z + Seed * 2) * 0.006f);
            float riverVal = Mathf.Abs(riverX - 0.5f) * 2f;
            float riverZVal = Mathf.Abs(riverZ - 0.5f) * 2f;
            float river = Mathf.Min(riverVal, riverZVal);
            float riverSecondary = Mathf.PerlinNoise((genX - 401) * 0.0055f, (z + 331) * 0.0055f);
            bool isRiver = river < 0.13f || (riverSecondary > 0.88f && river < 0.18f);
            if (isRiver)
            {
                float edge = river < 0.06f ? 0f : (river - 0.06f) / 0.07f;
                terrain = edge < 0.5f ? SurfaceTerrain.Water : SurfaceTerrain.Shallows;
                height = terrain == SurfaceTerrain.Water ? height - 1.5f : height - 0.8f;
            }

            bool iron = Mathf.PerlinNoise((genX + Seed * 3) * 0.025f, (z - Seed * 5) * 0.025f) > 0.72f && height > 3.5f;
            bool coal = Mathf.PerlinNoise((genX - Seed * 4) * 0.021f, (z + Seed * 2) * 0.021f) > 0.76f && height > 4.0f;
            bool oil = Mathf.PerlinNoise((genX + 444) * 0.015f, (z - 888) * 0.015f) > 0.83f && height < 4.5f;
            bool sulfur = Mathf.PerlinNoise((genX + Seed * 7) * 0.019f, (z + Seed * 11) * 0.019f) > 0.84f && height > 2.5f;
            bool nitrate = Mathf.PerlinNoise((genX - Seed * 9) * 0.018f, (z - Seed * 6) * 0.018f) > 0.85f && height < 6f;
            bool clay = Mathf.PerlinNoise((genX + 51) * 0.032f, (z - 73) * 0.032f) > 0.70f && height < 3.5f;
            var layers = iron
                ? new[] { new MaterialLayer(GroundMaterial.Dirt, 2), new MaterialLayer(GroundMaterial.Stone, 3), new MaterialLayer(GroundMaterial.IronOre, 8, 0.55f + high * 0.35f, 1.55f, 0.08f, 96) }
                : coal
                    ? new[] { new MaterialLayer(GroundMaterial.Dirt, 2), new MaterialLayer(GroundMaterial.Stone, 3), new MaterialLayer(GroundMaterial.Coal, 8, 0.62f + low * 0.25f, 1.15f, 0.12f, 110) }
                    : oil
                        ? new[] { new MaterialLayer(GroundMaterial.Dirt, 2), new MaterialLayer(GroundMaterial.Clay, 2), new MaterialLayer(GroundMaterial.Oil, 6, 0.72f + low * 0.20f, 0.65f, 0.48f, 140) }
                        : sulfur
                            ? new[] { new MaterialLayer(GroundMaterial.Dirt, 2), new MaterialLayer(GroundMaterial.Stone, 4), new MaterialLayer(GroundMaterial.Sulfur, 5, 0.45f + high * 0.30f, 1.25f, 0.15f, 65) }
                            : nitrate
                                ? new[] { new MaterialLayer(GroundMaterial.Dirt, 3), new MaterialLayer(GroundMaterial.Clay, 2), new MaterialLayer(GroundMaterial.Nitrate, 5, 0.42f + low * 0.32f, 1.05f, 0.24f, 60) }
                                : clay
                                    ? new[] { new MaterialLayer(GroundMaterial.Dirt, 2), new MaterialLayer(GroundMaterial.Clay, 6), new MaterialLayer(GroundMaterial.Stone, 5) }
                                    : new[] { new MaterialLayer(GroundMaterial.Dirt, 3), new MaterialLayer(GroundMaterial.Stone, 8) };

            return new SurfaceCell
            {
                Height = height,
                Terrain = terrain,
                Layers = layers,
                CurrentLayer = 0,
                BuildingId = 0,
                ResourceRichness = iron || coal || oil || sulfur || nitrate ? 3 : forestNoise > 0.66f ? 2 : 1,
                RegionId = _regionIdFor(new Vector2Int(x, z))
            };
        }
    }
}
