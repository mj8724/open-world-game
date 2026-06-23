# System Architect Analysis

## 1. Role Mandate
负责整体UI重构的底层系统架构设计，特别是UI渲染层与底层游戏状态的解耦，以及数据绑定机制。

## 2. Decision Digest

### 2.1 Decisions
| ID | Area | Decision | Status | Rationale |
|---|---|---|---|---|
| D-SA-01 | Event System | MUST全面转向基于 `WorldEvents` 的事件驱动，彻底移除UI层的 `Update()` 轮询 | Locked | 提高性能，减少不必要的每帧渲染 |
| D-SA-02 | Data Binding | MUST采用 MVVM / 响应式绑定架构 | Locked | 实现数据驱动视图的自动更新，简化UI层代码 |

### 2.2 Interfaces
| Provider | Interface | Consumer | Purpose |
|---|---|---|---|
| system-architect | `IReactiveProperty<T>` | ui-designer, ux-expert | 提供可订阅的数据流用于UI绑定 |

### 2.3 Cross-Cutting Positions
| Role | Area | Position |
|---|---|---|
| ux-expert | Feedback | 支持响应式的UI反馈需要架构层保证低延迟 |

### 2.4 Findings Summary
| ID | Area | Finding | Impact |
|---|---|---|---|
| S-SA-01 | Memory | 响应式绑定可能引发额外的 GC 压力 | 高 |

## 3. Cross-Cutting Foundations
### Data Model
- 采用只读的 ViewModel 封装游戏底层 StateStore。
### State Machine
- UI 具有 Loading, Active, Hidden 等状态。

## 4. File Index
- `analysis.md`

## 5. Outstanding TODOs
- [ ] 确定是否引入 UniRx 作为响应式框架。
