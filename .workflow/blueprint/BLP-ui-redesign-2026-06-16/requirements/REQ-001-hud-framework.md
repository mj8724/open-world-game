---
id: REQ-001
title: HUD 基础框架重构
priority: must
session_id: BLP-ui-redesign-2026-06-16
status: complete
---

# REQ-001: HUD 基础框架重构

## 1. Description
重新设计大地图概览时的常驻界面 (HUD)。该界面旨在提供所见即所得的宏观经济状态与基础科技进度。

## 2. User Story
As a 策略玩家,
I want 能够随时在屏幕顶部/边缘清晰地看到我的金钱、木材、电力以及人口上限,
So that 我可以在激烈的战斗或探索中实时掌握我的帝国运作状态。

## 3. Constraints & Rules
- HUD 面板 MUST 采用 Glassmorphism 设计（无边框、半透明磨砂背景）。
- 资源变化时 MUST 提供可视化的数字跳动或过渡动效。
- 数据来源 MUST 绑定到底层的全局状态，而非通过 Update() 轮询抓取。

## 4. Acceptance Criteria
1. **渲染检查**: 在亮色地形（如沙漠）上，HUD 底部自动生成深色半透明遮罩以确保文本高对比度清晰。
2. **数据一致性**: 增加或消耗资源后，HUD 在 1 帧延迟内立即更新。
3. **响应式设计**: 屏幕分辨率从 1080p 切换到 4K 时，HUD 的相对占比和间距不发生错乱。
