---
id: EPIC-004
title: 单位详情与战斗指令面板
size: L
mvp: true
session_id: BLP-ui-redesign-2026-06-16
status: complete
---

# EPIC-004: 单位详情与战斗指令面板

## 1. Description
屏幕右下角固定的单位详情区。强化原本弱化的战斗隐藏数值（如掩体加成、护甲），并支持多单位聚合展示。

## 2. Stories

### 2.1 单体单位的关键战斗数值面板 (Size: M)
**As a** 指挥官,
**I want** 选中一辆载具时立即在右下角看到它的当前血量、护甲和地形带来的掩体加成,
**So that** 能够精准判断该单位是否需要撤退。
- **Acceptance Criteria**:
  1. 监听 `WorldEvents.OnSelectionChanged`。
  2. 在右下角面板绑定对应的 `UnitDetailsViewModel`，显示护甲与掩体百分比数值和高对比图标。

### 2.2 多选聚合与公共指令分发 (Size: L)
**As a** 指挥官,
**I want** 框选一支包含步兵和坦克的混合部队时看到他们的数量概览,
**So that** 能够同时向他们下达“攻击”指令。
- **Acceptance Criteria**:
  1. 多选状态下，面板从“详细状态”切换至“聚合概览”（统计步兵: 10, 坦克: 2）。
  2. 点击面板上的攻击按钮，通过底层发送群体指令，并等待 Ack 后更新按钮状态。
