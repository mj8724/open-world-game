# UX Expert Analysis

## §1 Role Mandate
作为 **UX Expert**，本角色的核心使命是用户体验优化、可用性测试与交互设计模式的制定。在此次 UI 重构中，职责集中在降低用户的认知负荷、确保信息架构的清晰度、规范微交互与状态管理，并验证拟玻璃化（Glassmorphism）等视觉决策在实际交互中的可用性与无障碍要求（符合 WCAG 2.1 AA/AAA 规范）。本角色不直接产出前端代码或视觉品牌图形，但 MUST 确保所有交互逻辑遵循以用户为中心的设计原则。

## §2 Decision Digest

### Decisions
| ID | Decision Statement | Rationale |
|---|---|---|
| D-UX-01 | 建设与单位的二级详细操作菜单 MUST 使用从边缘滑出的抽屉式面板。 | 保证主视口中心不被遮挡，维持沉浸式的开放世界体验。 |
| D-UX-02 | 选中单位后，其状态和指令菜单 MUST 固定显示在屏幕指定区域（如右下角）。 | 建立一致的空间记忆，降低玩家在复杂战斗中寻找信息的认知成本。 |

### Interfaces
| Interface / Pattern | Description | Contract |
|---|---|---|
| Drawer Panel (抽屉面板) | 边缘滑出组件 | MUST 提供明确的唤出/收起触发区域与平滑的过渡动画。 |
| Fixed Status Area (固定状态区) | 固定在角落的信息看板 | MUST 支持空状态（无选中）与信息切换时的无缝过渡。 |

### Cross-Cutting Positions
| Topic | Position / Stance |
|---|---|
| 无障碍与对比度 | 尽管采用拟玻璃化风格，文本与图标的对比度 MUST 满足 WCAG 2.1 AA 标准。支持引入遮罩以保障极端光照场景下的可读性。 |
| 认知负荷 | 经济消耗与战斗加成信息 MUST 直观可视化，避免深层嵌套，以降低玩家在实时战略环境中的记忆负担。 |

### Findings Summary
- 拟玻璃化与浅色主题（遵循相关 UI 决策）为交互层带来了挑战，但也提供了通过动效和层级区分信息深度的机会。
- 事件驱动的数据绑定机制将极大地提升 UI 响应的即时感，增强玩家微操时的反馈体验。

## §3 Cross-Cutting Foundations
- **交互一致性 (Interaction Consistency)**：所有的菜单展开/收起 MUST 遵循统一的动效曲线与时长（建议 200-300ms）。
- **微交互与反馈 (Microinteractions)**：所有的可交互元素（按钮、资源图标）MUST 具备清晰的 Hover 与 Active 状态反馈，以应对复杂背景。
- **错误预防与状态管理 (State Management)**：系统 MUST 清晰呈现不可用状态（如资源不足、前置科技未解锁），并提供明确的视觉提示而非仅仅禁用按钮。

## §4 File Index
- [F-001 HUD基础框架](./analysis-F-001-hud-framework.md)
- [F-002 边缘滑出式建设面板](./analysis-F-002-construction-drawer.md)
- [F-003 单位详情与指令面板](./analysis-F-003-unit-details-panel.md)
- [F-004 响应式数据绑定底座](./analysis-F-004-data-binding-system.md)

## §5 Outstanding TODOs
- [ ] 开展对动态模糊与深色遮罩在极端明亮/极端昏暗场景下的可用性对比测试。
- [ ] 细化抽屉面板（Drawer Panel）的滑出触发逻辑（点击 vs. 悬停展开）及防误触机制。
- [ ] 协同 UI 与架构团队确认响应式绑定中“数据突变”时的视觉缓冲（如数字滚动计数动效）规范。
