using UnityEngine;

namespace OpenWorld
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    public class SurfaceChunkView : MonoBehaviour
    {
        public Vector2Int Coord { get; private set; }
        public WorldChunk Chunk { get; private set; }

        private OpenWorldState _world;
        private MeshFilter _filter;
        private MeshCollider _collider;
        private Material _material;

        public void Initialize(OpenWorldState world, WorldChunk chunk, Material material)
        {
            _world = world;
            Chunk = chunk;
            Coord = chunk.Coord;
            _material = material;
            _filter = GetComponent<MeshFilter>();
            _collider = GetComponent<MeshCollider>();
            GetComponent<MeshRenderer>().sharedMaterial = material;
            gameObject.layer = LayerMask.NameToLayer("Default");
            Rebuild();
        }

        public void Rebuild()
        {
            if (_world == null || Chunk == null) return;

            int size = Chunk.Size;
            int vSize = size + 1;
            var vertices = new Vector3[vSize * vSize];
            var colors = new Color[vertices.Length];
            var triangles = new int[size * size * 6];

            for (int z = 0; z <= size; z++)
            {
                for (int x = 0; x <= size; x++)
                {
                    int wx = Chunk.Coord.x * size + x;
                    int wz = Chunk.Coord.y * size + z;
                    var cell = _world.GetCell(new Vector2Int(Mathf.Min(wx, _world.MapSize - 1), Mathf.Min(wz, _world.MapSize - 1)));
                    int i = z * vSize + x;
                    vertices[i] = new Vector3(x, cell.Height, z);
                    colors[i] = ColorFor(cell);
                }
            }

            int t = 0;
            for (int z = 0; z < size; z++)
            {
                for (int x = 0; x < size; x++)
                {
                    int a = z * vSize + x;
                    int b = a + 1;
                    int c = a + vSize;
                    int d = c + 1;
                    triangles[t++] = a; triangles[t++] = c; triangles[t++] = b;
                    triangles[t++] = b; triangles[t++] = c; triangles[t++] = d;
                }
            }

            var mesh = _filter.sharedMesh;
            if (mesh != null && mesh.name.StartsWith("SurfaceChunk_"))
            {
                Object.Destroy(mesh);
            }
            mesh = new Mesh { name = $"SurfaceChunk_{Coord.x}_{Coord.y}" };
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            _filter.sharedMesh = mesh;

            mesh.Clear();
            mesh.vertices = vertices;
            mesh.colors = colors;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            _collider.sharedMesh = null;
            _collider.sharedMesh = mesh;

            transform.position = new Vector3(Coord.x * size, 0, Coord.y * size);
            Chunk.DirtyVisual = false;
        }

private static Color ColorFor(SurfaceCell cell)
    {
        if (cell.HasBridge) return new Color(0.50f, 0.32f, 0.18f);
        if (cell.HasRoad) return new Color(0.46f, 0.38f, 0.28f);
        if (cell.HasTrench) return new Color(0.22f, 0.18f, 0.13f);
        if (cell.Terrain == SurfaceTerrain.Water) return new Color(0.08f, 0.28f, 0.52f);
        if (cell.Terrain == SurfaceTerrain.Shallows) return new Color(0.12f, 0.36f, 0.48f);
        return cell.TopMaterial switch
        {
            GroundMaterial.Stone => new Color(0.42f, 0.42f, 0.40f),
            GroundMaterial.IronOre => new Color(0.38f, 0.35f, 0.32f),
            _ => cell.Terrain switch
            {
                SurfaceTerrain.Forest => new Color(0.16f, 0.36f, 0.15f),
                SurfaceTerrain.Hills => new Color(0.34f, 0.50f, 0.24f),
                SurfaceTerrain.Mountain => new Color(0.46f, 0.46f, 0.44f),
                _ => new Color(0.30f, 0.55f, 0.24f)
            }
        };
    }
    }
}
