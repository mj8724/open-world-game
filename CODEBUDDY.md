# CODEBUDDY.md This file provides guidance to CodeBuddy when working with code in this repository.

## 项目概览

开放世界策略游戏，Unity 6 单机客户端（URP 渲染管线）。子系统：地形编辑（含流式分块）、建筑、兵种战斗、车辆/铁路物流、生产配方、科技研究、地质勘查、AI 敌人、外交、存档。

> 历史背景：项目从 Three.js + WebSocket 状态同步架构迁移到 Unity。仓库根目录的 `DEVELOPMENT.md` / `DEVLOG.md` / `TESTING.md` 描述的是迁移前旧架构（含 `Networking/`、`State/`、多个独立 Renderer），**已过时**，仅供历史参考。以本文件和实际代码为准。

## 仓库布局

```
open-world-game/                        # 仓库根（Markdown 文档 + 测试脚本）
├── CLAUDE.md                           # 同等权威的开发指引
├── DEVELOPMENT.md / DEVLOG.md / TESTING.md  # 旧架构文档（迁移前，参考用）
├── verify_test_scenario.sh             # Testing 模块 API 用法的手动校验脚本
└── U3D/open-world-game-unity/          # ★ Unity 项目根，所有代码在此
    └── Assets/Scripts/
        ├── Animation/ Data/ Localization/ UI/  # 跨子系统共用层
        ├── Rendering/MaterialCache.cs          # 唯一渲染层文件（材质缓存池）
        └── OpenWorld/                          # 游戏逻辑主体
```

Unity 项目路径：`U3D/open-world-game-unity/`。Assets 内路径都相对 `Assets/`。注意：仓库根的 `Assets/` 目录是残留的旧文件，实际代码在 `U3D/open-world-game-unity/Assets/`。

## 开发环境

- **Unity**: 6000.4.9f1 (Unity 6)，见 `U3D/open-world-game-unity/ProjectSettings/ProjectVersion.txt`
- **渲染**: URP `com.unity.render-pipelines.universal` 17.4.0
- **关键包**: `com.unity.localization`（本地化）、`com.unity.inputsystem`、`com.unity.test-framework` 1.6.0、`com.coplaydev.unity-mcp`（编辑器 MCP 桥）。依赖见 `U3D/open-world-game-unity/Packages/manifest.json`
- `.gitignore` 排除 Library/Temp/Obj/Builds/Logs/UserSettings/csproj/sln 等 Unity 生成物；**StreamingAssets 不入库**

## 常用命令

Unity 项目无 CLI 构建/测试入口（标准 Unity 工作流）。以下为实际开发操作：

- **打开项目**: Unity Hub 打开 `U3D/open-world-game-unity`。
- **运行游戏**: 打开主场景 → Play。场景中 `OpenWorldBootstrap` GameObject 在 Awake 时自动构建世界。按 F 进入 TimeScale=10 加速，S 恢复。
- **运行单测**: 无 NUnit `[Test]` 用例。测试靠 Play Mode 自驱动探针（见「测试方式」）。
- **API 校验**: 在仓库根执行 `./verify_test_scenario.sh`，静态检查 Testing 模块使用的 API 是否存在。
- **代码诊断**: 改完脚本后必须用 MCP `read_console` 检查编译错误，编译通过后新类型才可用；或查 `editor_state` 资源的 `isCompiling` 字段。
- **MCP 自动化**（需 Unity 编辑器开启 MCP for Unity 窗口且 Connected）: `manage_editor play/stop`、`read_console`、`execute_code`、`manage_profiler`。详见 `TESTING.md`。

> 按全局约定，编译/测试类任务可委托 `agy` CLI delegate 执行。

## 架构（当前）

### 入口与子系统装配

`OpenWorld/Core/OpenWorldBootstrap.cs` 是唯一入口（场景内 MonoBehaviour，`DontDestroyOnLoad` 单例）。`Awake → BuildWorld()` 按固定顺序创建并 `Initialize()` 所有子系统（每个子系统是 Bootstrap 的子 GameObject + Component）：

```
SurfaceTerrainSystem → BuildingSystem → UnitSystem → VehicleSystem
→ OpenWorldJobSystem → BlueprintSystem → OpenWorldGeologySystem
→ WorldKnowledgeSystem → OpenWorldLogisticsSystem → OpenWorldSimulationSystem
→ OpenWorldPerformanceSystem → OpenWorldCommandSystem → OpenWorldInputController → HUD
```

初始化顺序有依赖（如 Geology 需要 Terrain，Commands 需要几乎所有系统），改动顺序前先读 `BuildWorld()`。存档存在时走 `RestoreFrom` + `RebuildLoadedWorld`，否则 `SeedStartingBase()` 生成对称双阵营开局（玩家 Frontier Union / 敌人 Iron Dominion，镜像布局 + 中立据点）。

### 分层

- **数据层 `OpenWorldState`**（`OpenWorld/World/`）：纯数据核心，实现 `IWorldState`。所有实体存在 Dictionary/List 中（`Buildings`/`Units`/`Vehicles` 为 `Dictionary<int,*>`，Jobs/Blueprints/LogisticsRoutes/ProductionOrders 等为 List）。是所有子系统的共享状态。
- **类型层 `OpenWorldTypes.cs`**（`OpenWorld/Core/`）：所有枚举（`BuildableKind`/`UnitKind`/`VehicleKind`/`ResourceKind`/`TerrainTool`…）和实体类。`OpenWorldConstants`（PlayerFactionId/EnemyFactionId/NeutralFactionId）。
- **配置层 `GameDataConfig`**（`OpenWorld/Data/`）：ScriptableObject，建筑/兵种/配方/科技/车辆定义。**改数值编辑 `.asset` 文件，不改代码**。
- **子系统层**（`OpenWorld/{Simulation,Units,Building,Logistics,Terrain,Jobs,Strategic,Save,...}`）：各 `*System` 持有 `OpenWorldState` 引用并修改数据。
- **事件层 `WorldEvents`**（`OpenWorld/World/`）：静态事件总线（`OnBuildingAdded/Updated/Removed` 等），数据变更通知，供 UI/渲染订阅，避免轮询。
- **接口层 `OpenWorld/Interfaces/`**：`IWorldState`/`ISimulation`/`ILogistics`/`IResourceProvider`，解耦系统间依赖。

### 模拟循环

`OpenWorldSimulationSystem` 驱动经济/生产/战斗/AI tick。战斗目标查找用**空间哈希网格**（`RebuildCombatGrid`/`FindHostilesInGrid`，cell size 常量），不得回退为 O(n²) 全单位遍历。`TrimExcess()` 限制 IntelSnapshots/Surveys/Commands/ModifiedCells 等列表容量，每 ~15 秒自动调用，防止长时运行数据膨胀。

### 测试方式

无标准单元测试框架用例，测试分三类：

1. **Play Mode 自驱动探针** `OpenWorld/Testing/OpenWorldPlayTester.cs`：Play 时自动起，分阶段跑（BaselineCheck→EconomyChain→LogisticsFlow→UnitCommands→CombatTest→EnemyAI→UIHealthCheck→SaveLoadRoundtrip），PASS/FAIL/WARN 实时打 Console，结束输出 JSON 报告。
2. **对称对战 bot** `Testing/TestBotManager.cs` + `TestMonitor.cs`：编辑器内 1v1 自动对战，两边自动建造交战，Console 看 `[PROBE]` 警告。
3. **Unity MCP 自动化**：`execute_code` 跑内省代码（如读 `MaterialCache.Stats`、`world.Buildings.Count`）、`manage_profiler` 内存快照对比、`read_console` 查异常。

## 关键规则（违反会引入内存泄漏/卡死，历史已踩坑）

1. **禁止 `new Material()`** —— 一律用 `Rendering.MaterialCache.GetLit(color)` / `GetUnlit` / `GetLitTransparent` / `GetLitWithEmission` / `GetLitWithParams` / `GetNamed`。同类颜色共享实例。
2. **Mesh 重建前先 `Destroy` 旧 mesh** —— `SurfaceChunkView.Rebuild()` 等模式，否则 GPU mesh 泄漏。
3. **数据变更走 `WorldEvents`** —— UI/渲染订阅事件，不要在 `Update()` 里轮询 `OpenWorldState` 字典。
4. **战斗用空间哈希，禁止 O(n²)** —— 单位目标查找走 `FindHostilesInGrid`。
5. **长生命周期列表要 `TrimExcess()`** —— 新增持续增长的数据集合时配套加上限清理。
6. **改游戏数值编辑 `GameDataConfig.asset`**，不要硬编码进脚本。

## 本地化

`OpenWorld/Core/I18nSystem.cs` + Unity Localization 包。新增文案走 Localization 而非硬编码字符串。
