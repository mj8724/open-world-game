# 开发必读 (DEVELOPMENT.md)

## 项目概述

开放世界策略游戏 Unity 客户端，基于 URP 渲染管线，支持地形编辑、建筑系统、兵种战斗、物流运输、科技研究、地质勘查、AI 敌人等子系统。

## 目录结构

```
Assets/Scripts/
├── Core/               # 入口 & 全局常量
│   ├── GameApp.cs      # 游戏入口（MonoBehaviour 单例）
│   └── Constants.cs    # 渲染常量（地形/城墙/城市/道路/军队/相机）
├── Camera/             # 相机控制
├── Input/              # 输入/选择管理
├── Networking/         # 网络层（WebSocket + 状态同步）
│   ├── WebSocketClient.cs      # C# WebSocket 客户端
│   ├── StateStore.cs           # 客户端状态缓存
│   ├── MessageDispatcher.cs    # 消息路由
│   ├── CommandSender.cs        # 指令发送
│   └── MainThreadDispatcher.cs # 主线程回调
├── State/              # 网络数据模型
│   └── ComponentModels.cs      # Newtonsoft.Json 序列化模型
├── Rendering/          # 3D 渲染层
│   ├── MaterialCache.cs        # **材质缓存池（重要！禁止 new Material）**
│   ├── SceneManager.cs         # URP 场景设置
│   ├── TerrainGenerator.cs     # 地形高度图生成
│   ├── NodeRenderer.cs         # 城市节点渲染
│   ├── ArmyRenderer.cs         # 军队渲染
│   ├── EdgeRenderer.cs         # 道路渲染
│   ├── LogisticsRenderer.cs    # 物流路线渲染
│   ├── WildRenderer.cs         # 野外实体渲染
│   ├── MinimapController.cs    # 小地图
│   ├── WorldRendererBridge.cs  # 事件到渲染桥接
│   ├── CoordinateUtils.cs      # 坐标转换
│   └── EntityReference.cs      # 实体引用组件
└── OpenWorld/          # 游戏逻辑
    ├── Core/           # 启动 & 类型定义
    │   ├── OpenWorldBootstrap.cs    # 世界初始化
    │   ├── OpenWorldTypes.cs        # 所有枚举和实体类型
    │   ├── OpenWorldCommandSystem.cs # 指令系统
    │   ├── OpenWorldInputController.cs
    │   ├── OpenWorldInput.cs
    │   ├── OpenWorldCameraController.cs
    │   └── I18nSystem.cs
    ├── World/          # 状态数据层
    │   ├── OpenWorldState.cs    # 世界状态（数据核心）
    │   └── WorldEvents.cs       # 数据变更事件总线
    ├── Data/           # 数据配置
    │   ├── GameDataConfig.cs    # **ScriptableObject 配置（建筑/兵种/配方/科技）**
    │   └── OpenWorldDataCatalog.cs
    ├── Building/       # 建筑系统
    ├── Units/          # 单位系统
    ├── Logistics/      # 物流系统
    ├── Simulation/     # 模拟系统（经济/战斗/AI）
    ├── Terrain/        # 地形系统
    ├── Jobs/           # 工作任务系统
    ├── Strategic/      # 战略地图
    ├── Save/           # 存档系统
    └── UI/             # UI
```

## 架构设计原则

### 数据层（Model）
- `OpenWorldState` 是纯数据类，存放所有游戏状态
- `OpenWorldTypes` 定义所有枚举和实体类
- 数据变更通过 `WorldEvents` 静态事件发出通知

### 配置层（Config）
- **所有游戏数值在 `GameDataConfig.asset` 中配置**（ScriptableObject）
- 建筑属性 → `GameDataConfig.BuildingDefs`
- 兵种属性 → `GameDataConfig.UnitDefs`
- 生产配方 → `GameDataConfig.RecipeDefs`
- 科技树   → `GameDataConfig.TechDefs`
- 车辆属性 → `GameDataConfig.VehicleDefs`
- **修改数值在 Unity Editor 中打开 .asset 文件即可，无需改代码**

### 表现层（View）
- 渲染层通过 `WorldEvents` 订阅数据变更
- **禁止在渲染层直接遍历数据字典**
- 使用 `MaterialCache` 获取材质，勿 `new Material()`

## 关键规则

### ⚠️ 禁止事项
1. **禁止 `new Material()`** — 必须使用 `MaterialCache.GetLit()` 等静态方法
2. **禁止 `new Mesh()` 不销毁旧 mesh** — rebuild 前先 `Destroy(sharedMesh)`
3. **禁止渲染层直接读写 `OpenWorldState` 字典** — 通过事件订阅
4. **禁止在 `Update()` 中做 O(n²) 遍历** — 使用空间哈希索引
5. **禁止不调用 `TrimExcess()` 让数据无限增长**

### ✅ 推荐做法
1. 修改数值 → 编辑 `GameDataConfig.asset`
2. 创建材质 → `MaterialCache.GetLit(color)` / `GetLitWithParams(color, gloss, metal)`
3. 数据变更 → `WorldEvents.FireBuildingAdded(id, entity)`
4. 渲染响应 → 在 `WorldRendererBridge` 中订阅事件
5. 性能监控 → 使用 Unity MCP 的 `manage_profiler` 工具

## 数据字典索引

| 字典 | 位置 | 键类型 | 值类型 | 用途 |
|------|------|--------|--------|------|
| `Buildings` | `OpenWorldState` | `int` | `BuildingEntity` | 所有建筑实例 |
| `Units` | `OpenWorldState` | `int` | `UnitEntity` | 所有单位实例 |
| `Vehicles` | `OpenWorldState` | `int` | `VehicleEntity` | 所有车辆实例 |
| `Jobs` | `OpenWorldState` | `List` | `JobRecord` | 工作任务队列 |
| `Blueprints` | `OpenWorldState` | `List` | `BlueprintJob` | 建造蓝图 |
| `LogisticsRoutes` | `OpenWorldState` | `List` | `LogisticsRoute` | 物流路线 |
| `ProductionOrders` | `OpenWorldState` | `List` | `ProductionOrder` | 生产订单 |
| `ResearchOrders` | `OpenWorldState` | `List` | `ResearchOrder` | 研究订单 |
| `Surveys` | `OpenWorldState` | `List` | `SurveyRecord` | 地质勘察记录 |
| `Diplomacy` | `OpenWorldState` | `List` | `DiplomacyRecord` | 外交关系 |
| `Nodes` | `StateStore` | `string` | `NodeComponent` | 服务端节点数据 |
| `Armies` | `StateStore` | `int` | `ArmyComponent` | 服务端军队数据 |
| `BuildingDefs` | `GameDataConfig` | `List` | `BuildableDef` | 建筑定义配置 |
| `UnitDefs` | `GameDataConfig` | `List` | `UnitKindDef` | 兵种定义配置 |

## 内存管理

- `MaterialCache` 在场景销毁时调用 `ClearAll()` 清理所有材质
- `OpenWorldState.TrimExcess()` 每 15 秒自动调用
- `SurfaceChunkView.Rebuild()` 自动销毁旧 mesh
- 渲染器 `OnDestroy()` 中清理所有 GameObject

## MCP 工具集成

项目已安装 `com.coplaydev.unity-mcp`。在 Unity Editor 中：
- Window → MCP for Unity → Auto-Setup
- 详见 [TESTING.md](./TESTING.md)
