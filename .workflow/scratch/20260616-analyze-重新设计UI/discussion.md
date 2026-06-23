# Discussion: 重新设计UI

## Session Metadata
- **ID**: ANL-重新设计UI-2026-06-16
- **Topic**: 重新设计UI
- **Dimensions**: architecture, implementation, performance
- **Perspectives**: Technical, Architectural, Domain Expert
- **Depth**: Standard

## User Intent
全面重构开放世界游戏的UI，评估并分析 Blueprint 中的 MVVM 架构迁移与拟玻璃化渲染的具体实现可行性。

## Table of Contents
- [Current Understanding](#current-understanding)
- [Round 1](#round-1)
- [Round 2](#round-2)

## Current Understanding
UI的旧代码强依赖于 `Update()` 轮询，造成极大性能浪费。我们需要通过建立 ViewModel 抽象防腐层与 `WorldEvents` 进行通信。针对 Glassmorphism 设计带来的高亮背景对比度下降问题，将结合 Blur Shader 与深色底层阴影保障可见度。
实现层上，我们决定**全面采用 UniRx**。虽然它相比原生 C# 事件有一点轻微的装箱分配开销，但其 `AddTo(gameObject)` 的能力能自动将订阅生命周期与 UI GameObject 绑定，从根源上消除了“忘记解绑导致的内存泄漏”这一最大风险，使得整体的实现信心（Implementation Confidence）大幅增加。

## Timeline

### Round 1
**起点**: 开始对旧版代码进行扫描，重点关注 MonoBehaviour.Update() 和 WorldEvents 的使用方式。
**关键进展**: 发现了 `HUDController.cs:45` 的硬编码调用。各视角得出共识，引入 UniRx 及 VContainer 是彻底解决耦合的方法，并提出了建立 `UIViewModel` 基类来管理内存生命周期的具体技术方案。
**决策影响**: 明确了架构向 MVVM 转移不仅是可行的，而且是必须的。
**当前理解**: 需要彻底重写资源获取方式。
**遗留问题**: 采用 UniRx 还是 C# 原生事件？内存泄漏如何做系统级防护？

#### Intent Coverage Matrix (R1)
| # | Original Intent | Status | Where Addressed | Notes |
|---|----------------|--------|-----------------|-------|
| 1 | MVVM 架构迁移分析 | 🔄 in-progress | Round 1 | 已探明修改路径，等待落实框架选项 |
| 2 | 拟玻璃化渲染可行性分析 | 🔄 in-progress | Round 1 | 确认了 Blur + 黑色遮罩/阴影的必要性 |

#### Technical Solutions
> **Solution**: 引入 UniRx 并构建 UIViewModel 基类统一 Dispose。
> - **Status**: Proposed
> - **Problem**: 避免事件驱动带来的潜在内存泄漏。
> - **Rationale**: 规范化生命周期管理
> - **Alternatives**: C# 原生 Action event +=/-=
> - **Evidence**: `Assets/Scripts/Core/WorldEvents.cs`
> - **Next Action**: 需要确定具体是采用 UniRx 还是原生的 async/await

### Round 2
**起点**: 基于上一轮的遗留问题，本轮从“UniRx 与原生 C# 事件对比及选型”切入。
**关键进展**: 探索确认了原生 C# `Action` 虽然是 0 GC，但在复杂的 UI 组件中，开发者经常忘记 `-=` 取消订阅，导致幽灵对象和严重内存泄漏。而 UniRx 提供的 `Observable.Subscribe().AddTo(this)` 完美契合了 MonoBehavior 的生命周期。
**决策影响**: 用户选择探讨选型，我们据此锁定了技术栈。
**当前理解**: 经过本轮，核心认知更新为“宁愿牺牲极微小的 GC，也要换取绝对安全的事件内存回收”。
**遗留问题**: 所有技术验证点基本闭环。

#### 压力测试 (Pressure Pass)
> **Finding**: UniRx 的 `AddTo` 可以防范内存泄漏。
> **压力探测**: 如果 GameObject 是被 `SetActive(false)` 而不是 `Destroy` 呢？
> **结论修正**: `AddTo(this)` 仅在 `OnDestroy` 触发。如果 UI 面板只是隐藏而非销毁，内部订阅依然活跃，可能会造成计算浪费。因此必须在 ViewModel 层引入 `isActive` 开关（结合 `Where(x => isActive)`）来切断不可见 UI 的计算流。这是一个非常关键的技术补充。

#### Intent Coverage Matrix (R2)
| # | Original Intent | Status | Where Addressed | Notes |
|---|----------------|--------|-----------------|-------|
| 1 | MVVM 架构迁移分析 | ✅ Addressed | Round 2 | 敲定 UniRx，并补充了防范隐藏组件计算浪费的拦截方案。 |
| 2 | 拟玻璃化渲染可行性分析 | ✅ Addressed | Round 1 | 确认了 Blur + 黑色遮罩/阴影的必要性 |

#### Updated Confidence
| Dimension | Score | Delta | Assessment |
|---|---|---|---|
| Architecture | 85% | +15% | ViewModel 的单向数据流与生命周期解绑完全明确。 |
| Implementation | 90% | +30% | 敲定 UniRx，压力测试揭示了隐藏对象的陷阱并有了对策。 |
| Performance | 85% | +10% | 确认了采用 Where 拦截隐藏面板计算的性能兜底方案。 |
| **Overall** | **86%** | **+18%** | **建议收敛，进入最终评分** |

#### Technical Solutions
> **Solution**: 采用 UniRx + `.Where(isActive)` 的事件拦截流。
> - **Status**: Validated
> - **Problem**: 彻底根除 UI 事件轮询和内存泄漏。
> - **Rationale**: `AddTo` 防止销毁泄漏，`Where` 防止隐藏耗能。
> - **Alternatives**: C# 原生 Action
> - **Evidence**: 架构视角与技术视角的综合压力测试。
> - **Next Action**: None
