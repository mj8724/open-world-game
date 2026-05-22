# 《文明模拟器》Demo 开发日志

---

## 2026-05-22 Phase 1：项目骨架搭建

### 上午：后端基础架构
- **[09:00]** 启动项目，阅读 PRD 文档，确定 Demo 范围：2 势力 / 17 节点 / 白底商务风
- **[09:30]** 创建实现计划，用户确认使用 C# .NET 8 后端 + Vite 前端
- **[10:00]** 初始化 `server/CivilizationSim` .NET 8 项目
- **[10:30]** 构建数据字典层 — 8 个 JSON 数据文件 + 8 个 C# Def 类 + DictRegistry 中央注册中心
- **[11:00]** 构建 ECS 核心 — World、EntityManager、6 种 Component
- **[11:30]** 构建 TickEngine 主循环 + 10 个 System（3 个实现 + 7 个 stub）
- **[12:00]** 构建 WebSocket 层 — 处理器 + 消息路由 + 状态序列化
- **[12:30]** Program.cs 整合 — CORS 配置、WebSocket 端点、Tick 循环

> **问题 1**：`dotnet build` 失败 — .csproj 中 `<Content>` 与 `<None>` 重复包含 JSON 文件  
> **解决**：改用 `<None Update="Dict/Data/*.json">` 配合 `CopyToOutputDirectory`

> **问题 2**：`dotnet run` 失败 — `System.InvalidOperationException: Unable to resolve ICorsService`  
> **解决**：在 `builder.Build()` 前添加 `builder.Services.AddCors()`

### 下午：前端构建
- **[13:00]** 初始化 Vite + Cytoscape.js 项目
- **[13:30]** 设计 CSS 变量系统（白底商务风 7 个样式文件）
- **[14:00]** 构建 i18n 引擎 + 完整中英双语包
- **[14:30]** 构建通信层 — GameBridge（WebSocket + mock 降级）、StateStore、CommandSender
- **[15:00]** 构建 UI 模块 — Cytoscape 地图、HUD、InfoPanel、BattleLog、Settings
- **[15:30]** main.js 入口整合

> **问题 3**：地图不显示 — CSS Grid 布局中 `.main-content` 包裹层导致 `.map-container` 高度为 0  
> **解决**：改 grid 为二级嵌套布局，`.main-content` 使用 `display: grid`

### 联调成功
- **[16:00]** 前后端首次联调成功 — WebSocket 握手、FULL_STATE 推送、Tick 更新正常
- **截图验证**：17 节点拓扑地图正确渲染，蓝/红/灰三色势力区分

---

## 2026-05-22 Phase 2：资源经济 + 建造系统

### 经济平衡修复
- **[14:10]** 发现经济崩溃：120 人口消耗 120 粮/tick，但仅 6 粮/tick 产出
- **分析**：每人每 tick 消耗 1 粮食太高，初始农田太少

| 修改项 | 修改前 | 修改后 |
|--------|--------|--------|
| 粮食消耗 | 1 food/pop/tick | 1 food/10pop/tick |
| 人口上限 | 无限制 | 5 + farmLevel × 25 |
| 增长门槛 | food > pop × 2 | food > pop × 5 |
| 初始粮食 | 200/节点 | 500/节点 |
| 初始铁矿 | 100/节点 | 200/节点 |
| 初始农田 | 仅 2 节点有 | 全部 8 节点有 Lv.1+ |

### 建造系统验证
- **[14:43]** 点击王城矿山「升级」→ 建造指令发送 → 6 tick 后完成 → 面板更新为 Lv.1
- **完整事件链**：前端按钮 → WS COMMAND → CommandProcessor → BuildQueue → BuildSystem → 完成事件

### 前端增强
- HUD 资源趋势指示器（绿色上升 / 红色下降 / 粮食警告脉冲）
- InfoPanel 人口上限显示（55/55）
- 建造进度条样式

---

## 2026-05-22 Phase 3：科技树 + 物流系统

### 后端实现
- **[14:50]** ResourceSystem 添加科技加成计算
  - 每 tick 遍历 faction 已解锁科技，累加 food_production_bonus
  - 修复 JsonElement 类型转换：`tech.Effects` 反序列化为 `Dictionary<string, object>` 时值是 `JsonElement` 而非原始类型
- **[14:54]** LogisticsSystem 完整实现（替换桩代码）
  - 运输实体沿边移动（speed/length 速率）
  - 到达目的地自动卸货并返程重新装载
  - 无限循环运输直到取消
- **[14:55]** CommandProcessor 添加 CREATE_ROUTE / CANCEL_ROUTE
  - 自动选择最高级已解锁运输工具
  - 校验：节点归属、科技前置、直连道路、库存充足
  - 取消路线时返还剩余货物
- **[14:57]** World.cs 添加 Faction delta 追踪
  - 研发进度每 tick 推送给前端

> **问题 4**：`Convert.ToDouble(JsonElement)` 抛出 `InvalidCastException`  
> **解决**：改为 `if (fb is JsonElement je) je.GetDouble()`

### 前端实现
- **[14:59]** 创建 tech-panel.js — 4 列科技树面板
  - 12 个科技节点，按 BUILD/TRANSIT/FOOD/WEAPON 分类
  - 前置条件检查（灰色锁定 / 蓝色可研发 / 紫色研发中 / 绿色已解锁）
  - 研发进度条实时更新
  - 研发按钮一键发送 RESEARCH 指令
- **[14:59]** 创建 tech-panel.css — 科技面板样式
  - Glassmorphic overlay + 4列网格 + 状态动画
  - 响应式设计（小屏幕 2 列）
- **[15:01]** i18n 更新 — 12 个科技名称 + 描述 + 4 个分类名 + 物流相关 key
- **[15:02]** 研究按钮绑定 → 打开科技面板
- **[15:04]** state-store 添加 faction delta 合并
- **[15:05]** command-sender 添加 sendCreateRoute / sendCancelRoute

### 验证结果
- ✅ 科技面板正确渲染 4 列 × 3 行
- ✅ 灌溉技术研发成功（12 tick 后解锁）
- ✅ 解锁后前置条件自动更新（下级科技可研发）
- ✅ 科技加成应用到 ResourceSystem（验证代码路径）
- ✅ dotnet build 编译通过

