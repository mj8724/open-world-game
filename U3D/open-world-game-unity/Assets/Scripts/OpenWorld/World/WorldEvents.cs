using System;
using System.Collections.Generic;
using UnityEngine;

namespace OpenWorld
{
    /// <summary>
    /// 世界状态变更事件系统 — 解耦数据层和表现层
    /// 数据层通过此系统发出事件，渲染层等订阅事件响应，不再直接遍历数据字典。
    /// </summary>
    public static class WorldEvents
    {
        // ─── 实体变更 ───
        public static event Action<int, BuildingEntity> OnBuildingAdded;
        public static event Action<int, BuildingEntity> OnBuildingUpdated;
        public static event Action<int> OnBuildingRemoved;

        public static event Action<int, UnitEntity> OnUnitAdded;
        public static event Action<int, UnitEntity> OnUnitUpdated;
        public static event Action<int> OnUnitRemoved;

        public static event Action<int, VehicleEntity> OnVehicleAdded;
        public static event Action<int, VehicleEntity> OnVehicleUpdated;
        public static event Action<int> OnVehicleRemoved;

        // ─── Terrain ───
        public static event Action<Vector2Int> OnCellModified;
        public static event Action<Vector2Int> OnChunkDirty;

        // ─── 系统状态 ───
        public static event Action OnWorldInitialized;
        public static event Action OnWorldCleaned;
        public static event Action<float> OnTick; // delta time

        // ─── Fire methods ───

        public static void FireBuildingAdded(int id, BuildingEntity entity) => OnBuildingAdded?.Invoke(id, entity);
        public static void FireBuildingUpdated(int id, BuildingEntity entity) => OnBuildingUpdated?.Invoke(id, entity);
        public static void FireBuildingRemoved(int id) => OnBuildingRemoved?.Invoke(id);

        public static void FireUnitAdded(int id, UnitEntity entity) => OnUnitAdded?.Invoke(id, entity);
        public static void FireUnitUpdated(int id, UnitEntity entity) => OnUnitUpdated?.Invoke(id, entity);
        public static void FireUnitRemoved(int id) => OnUnitRemoved?.Invoke(id);

        public static void FireVehicleAdded(int id, VehicleEntity entity) => OnVehicleAdded?.Invoke(id, entity);
        public static void FireVehicleUpdated(int id, VehicleEntity entity) => OnVehicleUpdated?.Invoke(id, entity);
        public static void FireVehicleRemoved(int id) => OnVehicleRemoved?.Invoke(id);

        public static void FireCellModified(Vector2Int cell) => OnCellModified?.Invoke(cell);
        public static void FireChunkDirty(Vector2Int coord) => OnChunkDirty?.Invoke(coord);
        public static void FireWorldInitialized() => OnWorldInitialized?.Invoke();
        public static void FireWorldCleaned() => OnWorldCleaned?.Invoke();
        public static void FireTick(float delta) => OnTick?.Invoke(delta);
    }

    /// <summary>
    /// 世界状态变更跟踪器 — 记录一帧内的所有变更，减少冗余渲染
    /// 渲染层应在 LateUpdate 中批量处理变更集
    /// </summary>
    public class WorldChangeSet
    {
        public readonly HashSet<int> AddedBuildings = new();
        public readonly HashSet<int> UpdatedBuildings = new();
        public readonly HashSet<int> RemovedBuildings = new();

        public readonly HashSet<int> AddedUnits = new();
        public readonly HashSet<int> UpdatedUnits = new();
        public readonly HashSet<int> RemovedUnits = new();

        public readonly HashSet<int> AddedVehicles = new();
        public readonly HashSet<int> UpdatedVehicles = new();
        public readonly HashSet<int> RemovedVehicles = new();

        public readonly HashSet<Vector2Int> ModifiedCells = new();
        public readonly HashSet<Vector2Int> DirtyChunks = new();

        public bool HasChanges =>
            AddedBuildings.Count > 0 || UpdatedBuildings.Count > 0 || RemovedBuildings.Count > 0 ||
            AddedUnits.Count > 0 || UpdatedUnits.Count > 0 || RemovedUnits.Count > 0 ||
            AddedVehicles.Count > 0 || UpdatedVehicles.Count > 0 || RemovedVehicles.Count > 0 ||
            ModifiedCells.Count > 0 || DirtyChunks.Count > 0;

        public void Clear()
        {
            AddedBuildings.Clear(); UpdatedBuildings.Clear(); RemovedBuildings.Clear();
            AddedUnits.Clear(); UpdatedUnits.Clear(); RemovedUnits.Clear();
            AddedVehicles.Clear(); UpdatedVehicles.Clear(); RemovedVehicles.Clear();
            ModifiedCells.Clear(); DirtyChunks.Clear();
        }
    }
}
