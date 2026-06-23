---
id: EPIC-001
title: 响应式架构与绑定底座搭建
size: L
mvp: true
session_id: BLP-ui-redesign-2026-06-16
status: complete
---

# EPIC-001: 响应式架构与绑定底座搭建

## 1. Description
构建基于 MVVM 与 UniRx 的底层防腐层，实现与 `WorldEvents` 游戏状态的单向数据同步。

## 2. Stories

### 2.1 引入并封装响应式数据结构 (Size: M)
**As a** 系统架构师,
**I want** 引入类似 `ReactiveProperty` 的数据结构来包裹 UI 数据,
**So that** 视图层可以直接订阅变量修改事件，无需自行编写监听代码。
- **Acceptance Criteria**:
  1. 成功封装基础的 `UIStateProperty<T>` 类。
  2. 提供完整的 Dispose() 接口避免内存泄漏。

### 2.2 重构全局事件桥接 (Size: L)
**As a** 系统架构师,
**I want** 编写 `WorldEventsToViewModel` 的转换器,
**So that** 底层的 `OnResourceChanged` 能够安全地流转到 `ResourceViewModel.Gold.Value` 中。
- **Acceptance Criteria**:
  1. 新增 3 个基础 ViewModel (Resources, Construction, Selection)。
  2. 模拟触发 `WorldEvents`，观察 ViewModel 内部数值能在同帧内更新。
