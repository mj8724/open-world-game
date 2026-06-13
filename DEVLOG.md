# 开发日志 (DEVLOG)

## 2026-06-12 — 项目架构诊断与性能优化

### 诊断背景
游戏运行一段时间后 Unity 编辑器无响应卡死。经全面代码审查（48 个 `.cs` 文件，约 8000+ 行代码），定位到多个根本原因。

### 问题诊断

#### P0：Material GPU 泄漏（最致命）
- **影响文件**：12 个文件，约 50+ 处 `new Material()` 调用
- **原因**：每个渲染对象创建时 `new Material()`，但只 `Destroy(gameObject)` 不 `Destroy(material)`
- **后果**：GPU GfxDriver 内存持续增长，最终耗尽导致编辑器卡死

#### P0：Mesh 泄漏
- `SurfaceChunkView.Rebuild()` 每次重建不销毁旧 mesh
- `EdgeRenderer` 每个路段创建新 mesh 不销毁

#### P1：数据无限增长
- `OpenWorldState.ModifiedCells`、`Commands`、`IntelSnapshots`、`Surveys` 无清理机制
- 长时间运行实体数从几百增长到几千

#### P1：主线程 O(n²) 阻塞
- `TickCombat()` 中嵌套循环遍历所有单位查找敌对目标
- `UpdateRegionControl()` 中多级嵌套遍历

#### P2：数据/表现层紧耦合
- 渲染层直接读取 `GameApp.Instance.State` 网络数据
- 没有数据变更通知机制，渲染层通过 `Update()` 轮询

### 修复内容

#### 1. 材质缓存池 (`MaterialCache.cs`)
- 创建 `Rendering.MaterialCache` 静态缓存池
- 支持：`GetLit()`、`GetUnlit()`、`GetLitTransparent()`、`GetLitWithEmission()`、`GetLitWithParams()`、`GetNamed()`
- 同类颜色材质共享同一实例，杜绝重复创建
- 替换范围：`ArmyRenderer`、`NodeRenderer`、`EdgeRenderer`、`WildRenderer`、`LogisticsRenderer`、`BuildingSystem`、`BlueprintSystem`、`UnitAgent`、`VehicleAgent`、`SurfaceTerrainSystem`、`TerrainGenerator`

#### 2. Mesh 泄漏修复
- `SurfaceChunkView.Rebuild()` 重建时先 `Destroy()` 旧 mesh
- `EdgeRenderer` 路段 mesh 标记 `MarkDynamic()`

#### 3. 数据清理机制
- `OpenWorldState.TrimExcess()` 方法：限制 IntelSnapshots、Surveys、DrillReports、Commands、ModifiedCells 容量上限
- `OpenWorldSimulationSystem` 每 15 秒自动调用 `TrimExcess()`

#### 4. 战斗空间哈希索引
- `OpenWorldSimulationSystem` 新增 `RebuildCombatGrid()` / `FindHostilesInGrid()`
- 将 O(n²) 的单位查找优化为 O(n/k²)（k = CombatGridCellSize）
- 每次 `TickCombat()` 先重建网格，后续在附近格子查找

#### 5. ScriptableObject 数据配置 (`GameDataConfig.cs`)
- 建筑、兵种、配方、科技从硬编码迁移到 `ScriptableObject`
- 可在 Unity Editor 中可视化编辑数值，无需改代码重编译
- 创建路径：`Assets → Create → OpenWorld → Game Data Config`

#### 6. 数据变更事件系统 (`WorldEvents.cs`)
- 静态事件总线：`OnBuildingAdded/Updated/Removed`、`OnUnitAdded/Updated/Removed`、`OnVehicleAdded/Updated/Removed`
- `OnCellModified`、`OnChunkDirty`、`OnWorldInitialized`
- `WorldChangeSet` 类支持批量变更处理，减少冗余渲染
- `WorldRendererBridge` 桥接事件到渲染层

#### 7. 文档
- `DEVLOG.md`（本文件）：开发日志
- `DEVELOPMENT.md`：开发必读指南
- `TESTING.md`：MCP 测试指南

### 数据字典总览

| 字典 | 文件 | 类型 | 配置方式 |
|------|------|------|----------|
| 建筑定义 (`BuildableDef`) | `GameDataConfig.asset` | ScriptableObject | Editor 可视化 |
| 兵种定义 (`UnitKindDef`) | `GameDataConfig.asset` | ScriptableObject | Editor 可视化 |
| 车辆定义 (`VehicleDef`) | `GameDataConfig.asset` | ScriptableObject | Editor 可视化 |
| 生产配方 (`ProductionRecipeDef`) | `GameDataConfig.asset` | ScriptableObject | Editor 可视化 |
| 科技定义 (`TechDef`) | `GameDataConfig.asset` | ScriptableObject | Editor 可视化 |
| 运行时建筑数据 | `OpenWorldState.Buildings` | `Dictionary<int,BuildingEntity>` | 运行时 |
| 运行时单位数据 | `OpenWorldState.Units` | `Dictionary<int,UnitEntity>` | 运行时 |
| 运行时车辆数据 | `OpenWorldState.Vehicles` | `Dictionary<int,VehicleEntity>` | 运行时 |
| 网络数据 | `StateStore.Nodes/Armies/...` | 各 `Dictionary` | WebSocket 服务端下发 |
| 渲染常量 | `Constants.cs` | `static class` | 代码常量 |

### 待办
- [ ] 创建 `GameDataConfig.asset` 文件并挂载到 `OpenWorldBootstrap`
- [ ] 在 `OpenWorldBootstrap` 中添加 `GameDataConfig` 序列化引用字段
- [ ] MCP 回归测试
- [ ] 实现 `WorldRendererBridge` 与现有渲染器的完整集成
- [ ] 添加 `[RequireComponent]` 确保渲染器组件存在
