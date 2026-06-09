using System.Collections.Generic;
using GameState;
using UnityEngine;

/// <summary>
/// 野外实体渲染器 — 资源点 + 中立建筑
/// 移植自 client/src/map3d/wild-renderer.js
/// </summary>
namespace Rendering
{
    public class WildRenderer : MonoBehaviour
    {
        private readonly Dictionary<string, GameObject> _wildObjects = new();
        private readonly Dictionary<string, GameObject> _structureObjects = new();
        private TerrainGenerator _terrain;

        private void Awake()
        {
            _terrain = FindObjectOfType<TerrainGenerator>();
        }

        /// <summary>创建所有野外资源点</summary>
        public void CreateWildResources(Dictionary<string, WildResource> wildResources)
        {
            ClearWildResources();
            if (wildResources == null) return;

            foreach (var (id, wr) in wildResources)
            {
                var obj = CreateWildResourceObject(wr);
                if (obj != null)
                {
                    obj.transform.SetParent(transform, false);
                    _wildObjects[id] = obj;
                }
            }
        }

        /// <summary>创建所有中立建筑</summary>
        public void CreateNeutralStructures(Dictionary<string, NeutralStructure> structures)
        {
            ClearNeutralStructures();
            if (structures == null) return;

            foreach (var (id, ns) in structures)
            {
                var obj = CreateNeutralStructureObject(ns);
                if (obj != null)
                {
                    obj.transform.SetParent(transform, false);
                    _structureObjects[id] = obj;
                }
            }
        }

        /// <summary>创建单个资源点</summary>
        private GameObject CreateWildResourceObject(WildResource wr)
        {
            var xz = CoordinateUtils.ToWorldXZ(wr.X, wr.Z);
            float y = (_terrain?.GetHeightAt(xz.x, xz.y) ?? 0f);

            var root = new GameObject($"Wild_{wr.Id}");
            root.transform.position = new Vector3(xz.x, y, xz.y);

            var data = root.AddComponent<EntityReference>();
            data.entityType = "wildResource";
            data.nodeId = wr.Id;

            switch (wr.ResourceType)
            {
                case "IRON":
                    CreateIronNode(root);
                    break;
                case "FOOD":
                    CreateFoodField(root);
                    break;
                case "AMMO":
                    CreateAmmoCache(root);
                    break;
                default:
                    // 通用资源标记
                    CreateGenericResource(root, wr.ResourceType);
                    break;
            }

            return root;
        }

        private static void CreateIronNode(GameObject root)
        {
            var rock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rock.name = "IronNode";
            DestroyImmediate(rock.GetComponent<Collider>());
            rock.transform.SetParent(root.transform, false);
            rock.transform.localScale = Vector3.one * 0.8f;
            rock.transform.localPosition = new Vector3(0, 0.4f, 0);
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = new Color(0.5f, 0.5f, 0.5f);
            mat.SetFloat("_Glossiness", 0.3f);
            mat.SetFloat("_Metallic", 0.5f);
            rock.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        private static void CreateFoodField(GameObject root)
        {
            for (int i = 0; i < 9; i++)
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float radius = Random.Range(0.1f, 0.5f);
                var plant = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                plant.name = $"FoodPlant_{i}";
                DestroyImmediate(plant.GetComponent<Collider>());
                plant.transform.SetParent(root.transform, false);
                plant.transform.localScale = new Vector3(0.08f, 0.15f, 0.08f);
                plant.transform.localPosition = new Vector3(
                    Mathf.Cos(angle) * radius,
                    0.08f,
                    Mathf.Sin(angle) * radius
                );
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = new Color(0.56f, 0.93f, 0.56f);
                plant.GetComponent<MeshRenderer>().sharedMaterial = mat;
            }
        }

        private static void CreateAmmoCache(GameObject root)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = "AmmoCache";
            DestroyImmediate(box.GetComponent<Collider>());
            box.transform.SetParent(root.transform, false);
            box.transform.localScale = new Vector3(0.4f, 0.3f, 0.3f);
            box.transform.localPosition = new Vector3(0, 0.15f, 0);
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = new Color(0.54f, 0.27f, 0.07f);
            box.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        private static void CreateGenericResource(GameObject root, string resourceType)
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = $"Resource_{resourceType}";
            DestroyImmediate(marker.GetComponent<Collider>());
            marker.transform.SetParent(root.transform, false);
            marker.transform.localScale = Vector3.one * 0.3f;
            marker.transform.localPosition = new Vector3(0, 0.2f, 0);
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = Color.yellow;
            marker.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        /// <summary>创建单个中立建筑</summary>
        private GameObject CreateNeutralStructureObject(NeutralStructure ns)
        {
            var xz = CoordinateUtils.ToWorldXZ(ns.X, ns.Z);
            float y = (_terrain?.GetHeightAt(xz.x, xz.y) ?? 0f);

            var root = new GameObject($"Structure_{ns.Id}");
            root.transform.position = new Vector3(xz.x, y, xz.y);

            var data = root.AddComponent<EntityReference>();
            data.entityType = "neutralStructure";
            data.nodeId = ns.Id;

            switch (ns.StructureType)
            {
                case "RUINS":
                    CreateRuins(root);
                    break;
                case "OUTPOST":
                    CreateOutpost(root);
                    break;
                case "SHRINE":
                    CreateShrine(root);
                    break;
            }

            return root;
        }

        private static void CreateRuins(GameObject root)
        {
            for (int i = 0; i < 4; i++)
            {
                float angle = (float)i / 4f * Mathf.PI * 2f;
                var pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pillar.name = $"Pillar_{i}";
                DestroyImmediate(pillar.GetComponent<Collider>());
                pillar.transform.SetParent(root.transform, false);
                pillar.transform.localScale = new Vector3(0.1f, 1.5f, 0.1f);
                pillar.transform.localPosition = new Vector3(Mathf.Cos(angle) * 0.5f, 0.75f, Mathf.Sin(angle) * 0.5f);
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = new Color(0.42f, 0.36f, 0.31f);
                mat.SetFloat("_Glossiness", 0.1f);
                pillar.GetComponent<MeshRenderer>().sharedMaterial = mat;
            }
        }

        private static void CreateOutpost(GameObject root)
        {
            var tower = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tower.name = "OutpostTower";
            DestroyImmediate(tower.GetComponent<Collider>());
            tower.transform.SetParent(root.transform, false);
            tower.transform.localScale = new Vector3(0.4f, 2.0f, 0.4f);
            tower.transform.localPosition = new Vector3(0, 1.0f, 0);
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = new Color(0.54f, 0.45f, 0.33f);
            tower.GetComponent<MeshRenderer>().sharedMaterial = mat;

            // 顶部平台
            var platform = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            platform.name = "OutpostRoof";
            DestroyImmediate(platform.GetComponent<Collider>());
            platform.transform.SetParent(root.transform, false);
            platform.transform.localScale = new Vector3(0.6f, 0.1f, 0.6f);
            platform.transform.localPosition = new Vector3(0, 2.05f, 0);
            var roofMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            roofMat.color = new Color(0.42f, 0.36f, 0.31f);
            platform.GetComponent<MeshRenderer>().sharedMaterial = roofMat;
        }

        private static void CreateShrine(GameObject root)
        {
            var baseObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseObj.name = "ShrineBase";
            DestroyImmediate(baseObj.GetComponent<Collider>());
            baseObj.transform.SetParent(root.transform, false);
            baseObj.transform.localScale = new Vector3(0.5f, 0.2f, 0.5f);
            baseObj.transform.localPosition = new Vector3(0, 0.1f, 0);
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = new Color(0.85f, 0.65f, 0.13f);
            baseObj.GetComponent<MeshRenderer>().sharedMaterial = mat;

            // 发光柱
            var beam = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            beam.name = "ShrineBeam";
            DestroyImmediate(beam.GetComponent<Collider>());
            beam.transform.SetParent(root.transform, false);
            beam.transform.localScale = new Vector3(0.1f, 1.0f, 0.1f);
            beam.transform.localPosition = new Vector3(0, 0.7f, 0);
            var beamMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            beamMat.color = new Color(1f, 0.84f, 0f);
            beamMat.EnableKeyword("_EMISSION");
            beamMat.SetColor("_EmissionColor", new Color(1f, 0.84f, 0f) * 0.3f);
            beam.GetComponent<MeshRenderer>().sharedMaterial = beamMat;
        }

        /// <summary>清理所有野外资源</summary>
        public void ClearWildResources()
        {
            foreach (var (id, obj) in _wildObjects) Destroy(obj);
            _wildObjects.Clear();
        }

        /// <summary>清理所有中立建筑</summary>
        public void ClearNeutralStructures()
        {
            foreach (var (id, obj) in _structureObjects) Destroy(obj);
            _structureObjects.Clear();
        }

        public void ClearAll()
        {
            ClearWildResources();
            ClearNeutralStructures();
        }

        private void OnDestroy() => ClearAll();
    }
}
