---
id: ADR-001
title: 采用 MVVM 与 UniRx 实现架构解耦
session_id: BLP-ui-redesign-2026-06-16
status: complete
---

# ADR-001: 采用 MVVM 与 UniRx 实现架构解耦

## 1. Context
目前游戏 UI 代码与底层逻辑强耦合，大部分 UI 通过 MonoBehavior 的 `Update()` 每帧轮询底层 `StateStore` 数据。这不仅消耗 CPU 性能，还使得逻辑复用和隔离测试变得不可能。

## 2. Decision
我们决定采用 **MVVM (Model-View-ViewModel)** 架构，并引入 **UniRx** 作为响应式扩展库（Reactive Extensions for Unity）。
UI 组件只负责渲染 (View)，所有数据和控制逻辑移入纯 C# 类 (ViewModel)。ViewModel 订阅 `WorldEvents` 事件，将数据转换为 `ReactiveProperty` 供 View 订阅。

## 3. Alternatives Considered
- **MVP 模式**: 较传统，需要手动编写大量回调与接口桥接代码（Presenter 调用 View）。
- **纯 ECS UI**: 将 UI 也变成游戏实体在 System 中更新。这种做法性能最高，但与目前的 Unity UI Toolkit 工作流不太契合，开发成本极高。

## 4. Consequences
- **Positive**: 真正做到了事件驱动更新，彻底消除冗余渲染，极大提高了性能。代码可读性与可测试性飙升。
- **Negative**: 需要团队学习并熟悉响应式编程 (Rx) 的理念。如果忘记在销毁时 `Dispose` 订阅，容易导致难以排查的内存泄漏。

## 5. Traceability
- **REQ**: NFR-ARCH-001-data-binding
- **Evidence**: 根据脑暴阶段 System Architect 提出的风险和收益分析，MVP/ECS 方案被否决。
