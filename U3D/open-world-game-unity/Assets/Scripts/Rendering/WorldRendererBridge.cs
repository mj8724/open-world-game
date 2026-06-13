using OpenWorld;
using UnityEngine;

namespace Rendering
{
    /// <summary>
    /// 世界渲染桥接器 — 订阅 WorldEvents，在数据变更时增量更新渲染，替代轮询模式
    /// </summary>
    [RequireComponent(typeof(NodeRenderer))]
    [RequireComponent(typeof(ArmyRenderer))]
    [RequireComponent(typeof(EdgeRenderer))]
    [RequireComponent(typeof(LogisticsRenderer))]
    [RequireComponent(typeof(WildRenderer))]
    public class WorldRendererBridge : MonoBehaviour
    {
        private NodeRenderer _nodeRenderer;
        private ArmyRenderer _armyRenderer;
        private EdgeRenderer _edgeRenderer;
        private LogisticsRenderer _logisticsRenderer;
        private WildRenderer _wildRenderer;
        private OpenWorldBootstrap _bootstrap;
        private bool _initialized;

        private void Awake()
        {
            _nodeRenderer = GetComponent<NodeRenderer>();
            _armyRenderer = GetComponent<ArmyRenderer>();
            _edgeRenderer = GetComponent<EdgeRenderer>();
            _logisticsRenderer = GetComponent<LogisticsRenderer>();
            _wildRenderer = GetComponent<WildRenderer>();
            _bootstrap = FindObjectOfType<OpenWorldBootstrap>();
        }

        private void OnEnable()
        {
            WorldEvents.OnWorldInitialized += OnWorldInitialized;
            WorldEvents.OnBuildingAdded += OnBuildingAdded;
            WorldEvents.OnBuildingRemoved += OnBuildingRemoved;
            WorldEvents.OnUnitAdded += OnUnitAdded;
            WorldEvents.OnUnitRemoved += OnUnitRemoved;
        }

        private void OnDisable()
        {
            WorldEvents.OnWorldInitialized -= OnWorldInitialized;
            WorldEvents.OnBuildingAdded -= OnBuildingAdded;
            WorldEvents.OnBuildingRemoved -= OnBuildingRemoved;
            WorldEvents.OnUnitAdded -= OnUnitAdded;
            WorldEvents.OnUnitRemoved -= OnUnitRemoved;
        }

        private void OnWorldInitialized()
        {
            // Full render on initialization
            var store = GameApp.Instance?.State;
            if (store?.Nodes != null) _nodeRenderer.CreateAllSettlements(store.Nodes);
            if (store?.Edges != null && store?.Nodes != null) _edgeRenderer.CreateAllEdges(store.Edges, store.Nodes);
            if (store?.Armies != null) _armyRenderer.CreateAllArmies(store.Armies);
            if (store?.Logistics != null) _logisticsRenderer.RefreshLogistics(store.Logistics);
            if (store?.WildResources != null) _wildRenderer.CreateWildResources(store.WildResources);
            if (store?.NeutralStructures != null) _wildRenderer.CreateNeutralStructures(store.NeutralStructures);
            _initialized = true;
        }

        private void OnBuildingAdded(int id, BuildingEntity entity) { /* handled by BuildingSystem now */ }
        private void OnBuildingRemoved(int id) { /* handled by BuildingSystem now */ }
        private void OnUnitAdded(int id, UnitEntity entity) { /* handled by UnitSystem now */ }
        private void OnUnitRemoved(int id) { /* handled by UnitSystem now */ }
    }
}
