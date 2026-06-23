# CLAUDE.md

This file provides guidance for the open-world-game Unity project.

## 项目概览
基于 Unity 6 的开放世界单机策略游戏（URP 渲染管线）。包含核心子系统：地形分块编辑、基建物流、兵种战斗、AI 调度。所有开发均基于 Unity 内工作流。

## 本地运行步骤（基线环境）
1. **启动项目**：使用 Unity Hub 打开 `U3D/open-world-game-unity`。
2. **场景检查**：确保当前打开的主场景包含 `OpenWorldBootstrap` GameObject（整个世界子系统的装配入口）。
3. **运行游戏**：点击 Play（▶）进入运行模式（或使用 MCP `manage_editor play`）。
4. **状态确认**：Console 无红错，且出现 `[OpenWorld] Loaded from save` 或 `SeedStartingBase` 即代表核心 Simulation Tick 启动成功。
5. **重置测试环境**：如果想触发全新的自动化探针冒烟测试（含自动生成双阵营开局），需要先删除已有的进度存档：`/Users/junma/Library/Application Support/DefaultCompany/open-world-game-unity/open_world_save_*.json`。

## ⚠️ 接近时要小心（高复杂度/可疑区域）
- **空间哈希与战斗距离**：`FindHostilesInGrid`。动态网格注册刷新与攻击距离校验边界极易出错（Git 记录显示此区域反复出现 Bug）。
- **PlayTester 探针与 AI 状态机**：自动化集成测试深度依赖异步时序（如等待物流车抵达），非常脆弱（Flaky）。请勿随意调整测试中断言的等待阈值。
- **上帝类 (God Classes)**：
  - `OpenWorldState.cs` (800+行)：作为唯一状态池管理了全局所有字典结构，缺乏领域隔离。
  - `OpenWorldHudController.cs` (1000+行)：UI 逻辑高度膨胀，强依赖大量底层事件源。
- **循环依赖警告**：`Terrain ↔ Geology`。由于地质系统和地形分块存在反向注入（`Terrain.SetGeology`），修改地形或矿产生成逻辑时极易破坏对方。
- **无 Edit Mode 单测的重灾区**：核心的物流流转 (`OpenWorldLogisticsSystem`) 和 生产线 (`ProductionSystem`) 完全没有单元测试。逻辑修改只能靠 Play Mode 集成测试兜底，极难定位故障点。

## 隐性约定（Implicit Knowledge）
- **命名规范**：类/接口/方法名用 `PascalCase`（接口加 `I` 前缀）；局部变量 `camelCase`；私有字段**强制加 `_` 前缀**（无论是否有 `[SerializeField]`）。测试文件必须为 `*Tests.cs` (NUnit) 或 `*Tester.cs`。
- **异常与错误处理**：
  - **业务层静默**：业务错误（如找不到实体、建造失败）**一律返回 bool 或 null** 并静默处理（附带 `Debug.Log`），不上浮 UI，不返回 Result，无 Error Code。
  - **异常边界**：仅构造器最底层校验允许抛出异常（如 `ArgumentException`）；文件 IO 必须包裹 `try/catch`。
- **日志规范**：统一使用 Unity 原生 `Debug.Log` 并在开头加方括号模块标签（如 `[OpenWorld]`、`[PROBE]`）。高度依赖 Emoji 作为日志可视化归类过滤（如 ✅❌⏩）。作为单机游戏，无 PII（隐私数据）安全隔离。
- **文件与编译架构**：目录内**无 `.asmdef`**，所有业务逻辑与 UI 强制混编进同一个 `Assembly-CSharp`。类名必须与文件名完全一致。
- **代码提交**：`类型: 描述`（全中文）。单人极速工作流：无 Branch、无 PR，写完直接 Push 入 `main`。

## 架构核心原则（红线）
1. **启动流不可侵犯**：`OpenWorldBootstrap` 单例通过 `Awake -> BuildWorld()` 按严格硬编码顺序装配所有 `*System`。
2. **禁止直接 new Material()**：必须使用 `Rendering.MaterialCache` 获取材质以复用 GPU 资源池，防止内存泄漏。
3. **网格销毁责任**：`SurfaceChunkView.Rebuild()` 等重构 Mesh 的逻辑，必须先手动 `Destroy` 历史 Mesh 对象。
4. **禁止 Update() 轮询状态**：数据变更必须通过 `WorldEvents` 下发，UI/Renderer 只能被动订阅事件刷新。
5. **长效集合防爆**：快照、日志等长期存在的 List/Dictionary 集合必须配套 `TrimExcess()` 机制防止长时挂机的内存溢出。
