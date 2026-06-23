---
title: Requirements Index
session_id: BLP-ui-redesign-2026-06-16
status: complete
---

# Requirements (PRD)

## 1. Summary
本次改版旨在解决目前“一团糟糕”的 UI 体验。核心功能侧重于表现层的重构与信息降噪，包括重制 HUD 常驻资源显示、边缘滑出式建设抽屉菜单，以及重点强化数据呈现的选定单位面板。底层则要求使用 MVVM 数据绑定提升开发体验与运行时性能。

## 2. MoSCoW Breakdown

### Must Have
- **REQ-001-hud-framework**: 常驻 HUD 显示基础经济和人口。
- **REQ-002-construction-drawer**: 从屏幕侧边呼出的防遮挡建造面板。
- **REQ-003-unit-details-panel**: 带有独立状态（护甲、掩体加成）展现的单位面板。
- **NFR-ARCH-001-data-binding**: 基于 WorldEvents 和 MVVM 架构的数据绑定底座。

### Should Have
- **NFR-PERF-001-glassmorphism-blur**: 动态模糊和深色遮罩在复杂地形上的对比度自适应。

### Could Have
- 后续更多维度的单位状态拓展（如士气等隐藏软属性）。

### Won't Have
- 移动端专门的触控热区适配。

## 3. Traceability Matrix
| Requirement | Parent Goal | Core Term |
|---|---|---|
| REQ-001-hud-framework | 改善信息高对比度清晰化 | HUD |
| REQ-002-construction-drawer | 提升操作效率 (APM) | Drawer Panel |
| REQ-003-unit-details-panel | 强化战斗状态呈现 | Glassmorphism |
| NFR-ARCH-001-data-binding | 底层架构解耦 | MVVM |
