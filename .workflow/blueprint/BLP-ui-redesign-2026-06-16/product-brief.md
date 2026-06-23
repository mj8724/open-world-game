---
title: Open World Game UI Redesign
session_id: BLP-ui-redesign-2026-06-16
status: complete
date: 2026-06-16
---

# Product Brief: UI Redesign

## 1. Vision & Problem Statement
当前游戏的 UI 视觉风格陈旧、交互繁琐且深层级嵌套，导致玩家难以及时获取关键游戏状态（如经济流水、单位掩体抗性）。
本次 UI 重构的愿景是打造一个**现代极简、所见即所得**的游戏操作界面，采用 Glassmorphism 拟玻璃化风格，并在底层实现与游戏逻辑的深度解耦（MVVM 响应式绑定）。

## 2. Target Audience
- 核心向实时策略 (RTS) 游戏玩家
- 重视沉浸式大地图与便捷指挥体验的单机玩家

## 3. Goals & Success Criteria
- **提升操作效率 (APM)**：建设面板采用边缘抽屉滑出式，减少屏幕中心遮挡，一键分类选取。
- **信息高对比度清晰化**：在极端环境（沙漠、雪地）下，UI 自动应用底层遮罩与动态模糊（Blur）确保文本清晰可见。
- **底层架构解耦**：移除 UI 中所有的 `Update()` 轮询，完全通过 `WorldEvents` 驱动视图。

## 4. Scope & Boundaries
- **In Scope**:
  - HUD 基础框架及资源显示
  - 边缘滑出式建设面板
  - 单位详情面板（包含掩体/护甲数据）
  - 响应式数据绑定底座 (MVVM)
- **Out of Scope**:
  - 移动端 UI 适配（仅支持 PC/Mac 操作逻辑）。
  - 联机大厅及网络对战界面（聚焦单机体验）。

## 5. Key Dependencies
- Unity 底层游戏逻辑 `StateStore` 与事件总线 `WorldEvents`。
- 可能需引入响应式框架（如 `UniRx`）以支撑 MVVM 架构。
