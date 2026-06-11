# OpenWorld 完整可玩切片 — 开发日志

## 项目概要

将 Unity 工程从基于 Three.js 原型的图论节点/边系统重构为"统一大地图"开放世界硬核物流 RTS。
视角为环世界/Kenshi 式天上视角，格子化城市、露天地形改造、采矿->冶炼->制造工业链、
道路铁路运输、载具调度、战争迷雾、地质勘探、外交贸易、部队战斗和区域控制。

## 阶段历史

### Phase 1 — Three.js 原型 (May 2026)
- 在 Three.js 上构建基于图论的文明模拟原型。
- 实现节点/边系统、基础物流网络、军队移动、AI 攻击。
- 约 20 次迭代，增补了物流、集结点、编队、撤退等功能。

### Phase 2 — Unity 迁移 (June 9, 2026)
- 将工程从 Three.js 迁至 Unity 3D。
- 创建 `open-world-game-unity`，移植核心状态+渲染。
- 接入 URP (Universal Render Pipeline) 获得更高质量渲染。
- 接入 MCP for Unity，启用 AI 驱动的开发工作流。

### Phase 3 — 可玩切片初始版 (June 9, 2026)
- 实现完整可玩核心：大地图、高度场改造、建筑放置、单位、车辆、物流、HUD。
- 构建蓝图系统、地质勘探、战斗系统、战略地图。
- 迁移场景到专用 `OpenWorldSurface.unity`。

### Phase 4 — UI 重做与架构重构 (June 9-10, 2026)
- HUD 从 IMGUI 替换为 UI Toolkit，绑定 USS/UXML。
- 完整重写 `OpenWorldHudController`：资源条、状态面板、生产控制、运输面板、建造菜单。
- 实现 `OpenWorldStrategicMapController`：覆盖层切换、缩放拖拽、标记命令。
- 实现 `OpenWorldSimulationSystem`：tick 经济、配方生产、研究、战斗、AI、士气医疗、区域控制。
- 实现 `OpenWorldCommandSystem`：50+ 命令类型，UI/输入只提交命令，模拟层统一执行。
- 实现 `OpenWorldLogisticsSystem`：路线创建、自动/手动运输、车辆调度、铁路调度。
- 实现 `OpenWorldGeologySystem`：地质调查、钻探取样、矿区分派。
- 实现 `WorldKnowledgeSystem`：战争迷雾、已探索区域、可见区域、Intel 快照。
- 实现 `OpenWorldPerformanceSystem`：LOD 分层 (High/Low/Dormant)、区块激活。
- 增加 `OpenWorldSaveService`：JSON 存档，含 v1->v2 迁移器。
- 增加 `I18nSystem`：中英文切换支持。
- 增加 `OpenWorldDataCatalog`：配方、车辆、科技定义。
- 增加 Editor UnitTests：确定性生成、迁移、采矿配方检查。
- 首张可玩截图：`Captures/OpenWorldPlayableSliceFinal.png`

## 当前架构

```
┌──────────────────────────────────────────────────────┐
│              表现层 (Unity MonoBehaviour)              │
│  SurfaceChunkView / UnitAgent / VehicleAgent / HUD    │
│  只读取数据，通过 CommandSystem 提交命令                │
├──────────────────────────────────────────────────────┤
│             模拟层 (MonoBehaviour Tick)                │
│  SimulationSystem / LogisticsSystem / GeologySystem   │
│  固定 tick 1s/0.25s/4s 执行经济/物流/生产/战斗/AI      │
├──────────────────────────────────────────────────────┤
│             数据层 (纯 C# 可序列化)                     │
│  OpenWorldState 含 Chunk/Building/Unit/Vehicle/...    │
│  OpenWorldTypes 含 30+ enum 和 40+ serializable class  │
│  ResourceInventory / MaterialLayer                    │
└──────────────────────────────────────────────────────┘
```

## 当前实现状态 (2026-06-11)

**已完成 (约 95%)**
- 完整类型系统：~1000 行 enum/serializable class
- 世界状态管理：Chunk/Cell/Layer/Building/Unit/Vehicle/Blueprint/Route
- 地形修改：Dig/Fill/Flatten/Ramp/Road/Rail/Bridge/Trench/Mine
- 建筑系统：TownCenter/Warehouse/House/Farm/Smelter/Steelworks/... 等 30+ 类型
- 蓝图队列：暂停/恢复/取消/优先级
- 单位系统：Worker/Engineer/Scout/Rifleman/MachineGunner/Artillery 等 13 种
- 车辆系统：HandCart/Wagon/Truck/ArmoredCar/Locomotive/CargoWagon 等 9 种
- 战争迷雾：Unknown/Explored/Visible 三层；Intel 快照
- 地质系统：调查/钻探/矿区分派
- 经济系统：配方生产、铁->锭->钢->机械件/铁轨/武器 工业链
- 研究系统：WoodStone->Iron->Gunpowder->Industrial->Aviation
- 物流系统：自动/手动路线、车辆调度、铁路调度
- 战斗系统：攻击、生命、护甲、射程、命中、弹药、士气、疲劳
- 医疗系统：Wounded/MedicalPressure、诊所治疗
- 敌方AI：基础扩军、路障建设、小队骚扰
- 战略地图：8种覆盖层、缩放拖拽、侦察/道路/铁路/桥梁/调查/钻孔/采矿标记命令
- HUD：资源条、人口/岗位、生产、运输、蓝图、地质、军队、外交面板
- 存档/读档：JSON 持久化，v1->v2 迁移
- 性能系统：HighFrequency/LowFrequency/Dormant 三层

**待完成 (约 5%)**
- 蓝图的工程师施工动画（目前是 timer 完成）
- 完整的战斗命令：Patrol/Defend/Escort 逻辑实现
- 所有单位种类从兵营训练（目前只支持 Militia/Rifleman）
- 车辆工厂/火车工厂的配方化生产
- 铁路时刻表执行（目前 RailSchedule 已定义但只是单条路线）
- 河流、桥梁与码头的水路系统
- 天气系统
- 更智能的敌方 AI（侦察/扩张/资源获取/围攻）
- 大规模压力的性能测试 (512x512 / 2000 unit)
