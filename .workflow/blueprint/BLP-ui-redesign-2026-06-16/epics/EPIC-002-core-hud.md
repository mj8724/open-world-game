---
id: EPIC-002
title: HUD 基础框架与材质渲染
size: M
mvp: true
session_id: BLP-ui-redesign-2026-06-16
status: complete
---

# EPIC-002: HUD 基础框架与材质渲染

## 1. Description
制作首个基于 MVVM 的 UI 面板（HUD），同时解决 Glassmorphism 材质在亮色环境下的对比度问题。

## 2. Stories

### 2.1 HUD UI 界面搭建与资源绑定 (Size: M)
**As a** UI 开发人员,
**I want** 搭建无边框的极简 HUD 面板并绑定 `ResourceViewModel`,
**So that** 能够把游戏中的基础资源总量和人口动态展示在屏幕上方。
- **Acceptance Criteria**:
  1. UI 布局自适应 1080p 至 4K 屏幕。
  2. 资源改变时触发数据绑定更新，无需调用任何 MonoBehavior Update()。

### 2.2 Glassmorphism 自适应模糊与降级渲染 (Size: M)
**As a** 渲染工程师,
**I want** 给 HUD 背景面板应用动态模糊 (Blur) 并且增加 30% 透明度的黑底,
**So that** 文本能够在任何游戏地形上都保持高对比度清晰可见。
- **Acceptance Criteria**:
  1. 在亮色沙漠材质前测试，文字白色可见度满足 WCAG 准则。
  2. 增加在低画质设置下关闭 Blur，回退到普通半透明色的降级逻辑。
