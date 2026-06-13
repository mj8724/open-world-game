# Project: open-world-game

## What This Is

实时开放世界策略游戏，玩家管理资源、建造建筑、训练军队、征服领土。基于 Unity 3D 引擎构建，从 Three.js + .NET WebSocket 架构迁移而来，现在是纯 Unity 单机游戏。

## Core Value

**实时策略游戏的核心游戏循环** — 资源采集 → 经济建设 → 军事训练 → 领土征服。如果其他都失败，这个循环必须流畅运作。

## Requirements

### Validated

- [x] 基础游戏循环（资源、建筑、单位、战斗）
- [x] 物流网络系统
- [x] 派系系统（玩家 vs 敌人 AI）
- [x] UI 框架（HUD、战略地图、建设面板）
- [x] P0 Bug 修复（派系切换、战斗休眠单位、单位属性同质化）
- [x] 1v1 AI 测试场景

### Active

- [ ] 测试基础设施（单元测试、集成测试覆盖）
- [ ] Bug 修复和稳定性提升
- [ ] AI 对战能力增强
- [ ] 框架完善和代码架构优化
- [ ] 新游戏功能和内容扩展

### Out of Scope

- 多人在线模式 — 当前聚焦单机体验
- 3D 渲染性能极限优化 — 先保证正确性再优化
- 多语言国际化 — 当前仅中文

## Context

项目从 Three.js + .NET WebSocket 客户端/服务器架构迁移到自包含 Unity 游戏。已完成遗留代码清理（删除 ~3,800 行孤立代码）。入口点 `OpenWorldBootstrap.cs`，通过 `[RuntimeInitializeOnLoadMethod]` 自启动。核心模块在 `Assets/Scripts/OpenWorld/` 下。

## Constraints

- **平台**: Unity 3D，macOS 开发环境，通过 Unity MCP 连接编辑器
- **语言**: C#（Unity 脚本）
- **向后兼容**: 现有游戏循环和 API 不能破坏
- **渐进式改进**: 小步提交，每步编译通过

## Tech Stack

- **引擎**: Unity 3D
- **语言**: C#
- **架构**: ECS-like（Entity + System 模式）
- **数据源**: OpenWorldDataCatalog（ScriptableObject 替代了原 GameDataConfig）

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| 删除 GameDataConfig，数据统一走 OpenWorldDataCatalog | 消除中间层，单一数据源 | 已完成 |
| 删除 WorldEvents 事件总线 | 零订阅者，纯死代码 | 已完成 |
| 从 Three.js 迁移到 Unity 3D | 更好的游戏引擎支持 | 已完成 |
| 使用 TestBotManager 替代 WebSocket 客户端测试 | 自包含测试，无需外部依赖 | 已完成 |

## Stakeholders

- 独立开发者

---
*Last updated: 2026-06-13 after initialization*