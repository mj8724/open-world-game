---
id: ADR-002
title: Glassmorphism 与自适应动态模糊渲染策略
session_id: BLP-ui-redesign-2026-06-16
status: complete
---

# ADR-002: Glassmorphism 与自适应动态模糊渲染策略

## 1. Context
UI 视觉方向被定为现代极简的拟玻璃化 (Glassmorphism)，要求无边框、半透明。但在如沙漠、雪地等高亮地形背景下，浅色半透明 UI 会严重降低可读性，导致玩家无法看清资源数据。

## 2. Decision
我们决定在 UI 面板的最底层加入一个 **自适应动态模糊 (Dynamic Gaussian Blur) Shader 材质**，并叠加一层参数可调的 **黑色半透明遮罩 (Alpha 0.2~0.4)**。

## 3. Alternatives Considered
- **全动态根据背景计算文字颜色 (黑/白翻转)**: 计算量过大（需要每帧采样屏幕背面颜色），且容易在复杂多色背景下造成文字花屏。
- **全局深色 UI**: 违背了产品经理和 UI 设计师追求的“实验室浅色现代风”视觉愿景。

## 4. Consequences
- **Positive**: 完美解决了在高亮地形下文字对比度低的问题，同时保留了高级的毛玻璃质感。
- **Negative**: Blur Shader 会带来额外的 GPU 渲染负担 (Overdraw)。这在低端机型上可能是瓶颈，需要在代码中实现优雅降级（如在低端画质关闭 Blur，只用深色半透）。

## 5. Traceability
- **REQ**: NFR-PERF-001-glassmorphism-blur
- **Evidence**: 基于脑暴冲突阶段解决的视觉对比度妥协方案。
