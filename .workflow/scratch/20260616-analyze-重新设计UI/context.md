# Context: 重新设计UI

**Date**: 2026-06-16
**Areas discussed**: 架构模式、底层数据流、性能优化

## Decisions

### Decision 1: 使用 UniRx 取代原生 Action
- **Context**: 需要在事件驱动架构下防止 UI 面板因未注销订阅造成的内存泄漏。
- **Options**:
  1. C# 原生 `event Action` (无 GC 但容易忘记 -=)。
  2. UniRx (极微小的 GC，但提供 `AddTo(this)`)。
- **Chosen**: UniRx
- **Reason**: 宁可牺牲极微小的 GC 也要换取绝对的内存安全。

### Decision 2: ViewModel 不可见时的性能挂起
- **Context**: 压力测试揭示，若面板被 `SetActive(false)`，生命周期并未结束，事件依旧在后台计算。
- **Chosen**: 必须在 ViewModel 层提供 `isActive` 属性，数据流在 `Where(v => isActive)` 处被拦截。
- **Reason**: 避免后台隐藏的面板空转，造成无意义的 ViewModel 渲染更新。

## Constraints

### Locked
- 彻底禁止在 UI 代码中使用 `Update()` 获取资源数据。
- 绑定层必须使用 UniRx 的 `AddTo(this)` 机制。

### Free
- 底层事件源 `WorldEvents` 到底如何挂载（单例或是静态类），实现者可自由调整。

### Deferred
- 是否要整体替换掉 UGUI 转为 UI Toolkit（这涉及太大范围的资产重做，先留作后续阶段）。

## Conclusions
- `scope_verdict`: medium (可以在1-2个 Sprint 内并行完成基础设施和核心面板的改造)。
