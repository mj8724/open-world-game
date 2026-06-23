---
id: EPIC-003
title: 边缘滑出式建设面板
size: M
mvp: true
session_id: BLP-ui-redesign-2026-06-16
status: complete
---

# EPIC-003: 边缘滑出式建设面板

## 1. Description
实现屏幕边缘（左侧/底侧）滑出的抽屉式建设界面，替代旧的中心弹窗模式，确保玩家建造建筑时不丢失中心战场的视野。

## 2. Stories

### 2.1 抽屉面板交互系统与动效 (Size: S)
**As a** UI/UX 设计师,
**I want** 面板在呼出和收回时带有非线性的平滑缓动动画 (Easing/Spring),
**So that** 操作过程显得现代且自然，符合极简 UI 调性。
- **Acceptance Criteria**:
  1. 使用 DOTween/UI Toolkit Transition 编写滑入滑出动画（0.2秒）。
  2. 点击屏幕空白区域自动触发收回动画。

### 2.2 建筑数据流绑定与分类展示 (Size: M)
**As a** 产品玩家,
**I want** 面板内的建筑能够按照“军事/经济/防御”进行 Tab 分类,
**So that** 能够快速找到想建的塔防设施。
- **Acceptance Criteria**:
  1. 订阅 `ConstructionViewModel` 的建筑分类数据。
  2. 切换 Tab 立即刷新可视元素列表，且没有卡顿。
