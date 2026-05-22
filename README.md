# 🏛️ 文明模拟器 (Civilization Simulator) - PROJECT EVOLUTION

**基于 .NET 8 + Vite (Vanilla JS) + Cytoscape.js 的全栈宏大策略游戏。**

本项目是一个轻量级、响应式的“白底商务风”开放世界策略游戏 Demo。玩家在由节点和连线组成的拓扑地图上进行资源生产、科技研发、物流运输与扩张征服。

## 🎯 核心特色

- **宏大的拓扑地图**：基于 `Cytoscape.js` 构建无极缩放、自由平移的节点连线世界。
- **纯粹的“种田”体验**：深度的经济循环（粮食/铁矿/弹药）与人口机制（饥荒与繁荣）。
- **科技树系统**：4大类（建筑、物流、农业、军事）12个科技节点，解锁更高级的时代。
- **自动化物流网络**：在节点间建立全自动的物资运输路线，支持人力、马车、铁路三种运力。
- **ECS (Entity-Component-System) 架构**：后端采用 C# .NET 8 打造高性能、可扩展的实时推演引擎（Tick Engine）。
- **Data-Driven 数据驱动**：所有数值、属性、地图拓扑完全通过 JSON 文件配置，杜绝硬编码。
- **WebSocket 实时同步**：前端基于 Delta（增量更新）机制，实现极低延迟的状态同步。
- **i18n 多语言支持**：原生支持中文 (zh-CN) 和英文 (en-US) 无缝切换。

---

## 🛠️ 技术栈

| 领域 | 技术/框架 | 核心作用 |
|------|-----------|----------|
| **后端** | C# / .NET 8 | 提供高性能的 ECS 引擎与 Tick 循环 |
| **前端** | HTML + CSS + JS (Vanilla) | 极致轻量，零框架负担，白底商务极简风 UI |
| **构建** | Vite | 极速的热更新开发体验与打包 |
| **可视化** | Cytoscape.js | 渲染复杂的节点、连线拓扑地图 |
| **通信** | WebSockets | `FULL_STATE` 初始化 + `TICK_UPDATE` 增量同步 |

---

## 🚀 快速启动

### 1. 启动后端引擎 (Server)

确保你的机器上安装了 [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)。

```bash
cd server/CivilizationSim
dotnet build
dotnet run
```
> 服务器将启动在 `http://localhost:5000`，WebSocket 挂载于 `ws://localhost:5000/ws`。

### 2. 启动前端客户端 (Client)

确保你的机器上安装了 [Node.js](https://nodejs.org/) (建议 v18+)。

```bash
cd client
npm install
npm run dev
```
> 浏览器访问 `http://localhost:5173` 即可进入游戏！即使服务器未开启，前端也会自动进入**离线演示模式 (Mock Data)** 供你预览 UI。

---

## 🗺️ 游戏玩法概览

### 经济与人口
每个节点都有独立的人口和资源（粮食、铁矿、弹药）。粮食用于维持人口生存，铁矿是工业和科技的基础，弹药用于扩张与防御。
如果你不及时升级**农田 (Farm)**，人口过剩会导致可怕的饥荒。

### 科技与发展
在主城（玩家初始节点 `N01`）点击“研究”按钮打开**科技树**。你需要消耗铁矿来解锁更高等级的建筑、更先进的运输方式（如铁路）以及强大的火器。

### 物流系统
由于铁矿和粮食往往不在同一个节点，你需要通过**物流系统**创建运输路线，指派搬运工、马车或火车在节点之间自动搬运物资。

---

## 🏗️ 架构设计

系统设计遵循 **ECS (Entity Component System)** 与 **Tick-based** 同步机制：

1. **ECS World**：所有的节点、边、军队、物流车队均为 Entity。
2. **Tick Engine**：服务器每秒（或其他设定速度）执行一次 Tick，按严格顺序调用各种 System (如 `ResourceSystem`, `LogisticsSystem`, `TechSystem` 等)。
3. **Dirty Delta**：只有在当前 Tick 发生属性变化的 Entity 会被标记为 Dirty，并在 Tick 结束时打包成 `TickDelta` 发送给前端。
4. **Dict Registry**：所有的游戏规则（建筑等级、科技前提、地图初始状态）均由 `server/CivilizationSim/Dict/Data/*.json` 定义。

---

## 📅 开发计划 (Roadmap)

- [x] **Phase 1**: 项目骨架、ECS 核心、通信协议、基础 UI 和地图渲染。
- [x] **Phase 2**: 资源经济系统、人口自然增长与饥荒机制、建造与升级系统。
- [x] **Phase 3**: 科技树机制与面板、物流运输实体、前后端状态同步。
- [ ] **Phase 4**: 进攻与防御 (CombatSystem)、军队移动、士气机制、AI 扩张与决策。
- [ ] **Phase 5**: 随机历史事件、音效、数值平衡。

---

## 📜 许可协议 (License)

本项目仅作为技术演示和个人开源项目 (PROJECT EVOLUTION)，代码遵循 MIT License。

> *"Empire building is a marathon, not a sprint."*
