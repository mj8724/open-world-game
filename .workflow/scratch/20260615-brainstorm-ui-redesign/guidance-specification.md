# Guidance Specification: UI Redesign Brainstorming

## 1. Project Positioning & Goals
本项目的目标是对现有的实时开放世界策略游戏进行全方位的 UI 重构。游戏核心循环包含资源采集、经济建设、军事训练与领土征服。新版 UI 的首要目标是改善当前混乱的视觉表现和深层嵌套的操作逻辑，以现代极简（浅色基调+半透明磨砂质感）的风格重新梳理信息架构，提供直观的经济流水展示，并强化战斗状态（如掩体加成/抗性）等常驻信息的可见度，同时在架构层面引入数据驱动的绑定机制。

## 2. Concepts & Terminology
| Term | Definition | Category |
|---|---|---|
| HUD (头显) | Heads-up Display，游戏内常驻显示在屏幕边缘的状态与资源信息。 | Core |
| Glassmorphism (拟玻璃化) | 现代极简 UI 风格，特征为无边框、半透明磨砂背景与悬浮文字。 | Technical |
| Radial Menu (环形菜单) | 围绕光标或单位展开的指令选择菜单。 | Core |
| Drawer Panel (抽屉面板) | 从屏幕边缘滑出的二级菜单面板，不遮挡屏幕中心视口。 | Core |
| MVVM / 响应式绑定 | Model-View-ViewModel，通过声明式绑定将底层数据直接映射到 UI 元素的架构模式。 | Technical |

## 3. Non-Goals (Out of Scope)
- **不包含移动端 UI 适配** (仅针对 PC/Mac)
- **不包括联机功能相关界面** (当前聚焦单机体验)

## 4. UI Designer Decisions
- **D-UI-01**: 整体色彩体系 MUST 采用浅色主题为主，呈现简洁干净的实验室风格。
- **D-UI-02**: HUD 容器与背景 MUST 采用 Glassmorphism（无边框悬浮文字+半透明磨砂背景板）风格。
- **D-UI-03**: 为解决明亮地形下的对比度问题，所有浅色半透明 UI 面板底部 MUST 增加动态模糊 (Blur) 并在底层加入深色半透明遮罩。

## 5. UX Expert Decisions
- **D-UX-01**: 建设与单位的二级详细操作菜单 MUST 使用从边缘滑出的抽屉式面板，以保证不遮挡主视口中心。
- **D-UX-02**: 针对众多单位的状态信息，MUST 在选中单位后于屏幕固定区域（如右下角）详细显示其状态和指令菜单。

## 6. System Architect Decisions
- **D-SA-01**: UI 数据更新 MUST 全面转向基于 `WorldEvents` 的事件驱动，彻底移除 UI 层的 `Update()` 轮询。
- **D-SA-02**: UI 渲染层与数据状态层的解耦 MUST 采用 MVVM / 响应式绑定架构，实现数据驱动视图的自动更新。

## 7. Product Manager Decisions
- **D-PM-01**: 在重构过程中，MUST 将“核心基础循环（资源显示、建造面板、单位基础选择）”设为 P0 级优先处理。
- **D-PM-02**: HUD 呈现理念 MUST 强调“所见即所得”，减少玩家猜测，将经济产出和消耗直接可视化。

## 8. Subject Matter Expert Decisions
- **D-SME-01**: 核心基础资源（如金钱、木材/矿物、电力）当前总量和速率、人口占用/上限与科技树研究进度 MUST 在 HUD 界面永久常驻。
- **D-SME-02**: 单位状态面板 MUST 强化显示单位当前的“掩体加成”与“护甲抗性”等关键战斗收益数据。

## 9. Risks & Constraints
- **约束**: 必须保证原有的底层开放世界数据循环不受影响，仅解耦 UI 层。
- **风险**: MVVM/响应式框架在 Unity 中可能引发额外的 GC (垃圾回收)，如果事件绑定没被正确释放，可能引发内存泄漏。
- **风险**: 动态模糊 (Blur) 若全屏过度使用可能会影响 GPU 性能。

## 10. Feature Decomposition
| Feature ID | Slug | Title | Priority | Related Roles |
|---|---|---|---|---|
| F-001 | hud-framework | HUD基础框架 | MUST | UI, UX, SME, PM |
| F-002 | construction-drawer | 边缘滑出式建设面板 | MUST | UX, UI, PM |
| F-003 | unit-details-panel | 单位详情与指令面板 | MUST | UX, SME, UI |
| F-004 | data-binding-system | 响应式数据绑定底座 | MUST | SA |

## 11. Appendix: Decision Tracking
本规范在互动环节生成。用户确认了全方位重构意图，确定了浅色实验室+拟玻璃化的现代视觉基调。并在随后的冲突检查中，增加了深色遮罩+Blur以规避明亮场景下阅读困难的问题。架构上最终敲定 MVVM 响应式系统+全面事件驱动。

## 12. Cross-Role Resolutions
*(待跨角色复审阶段填充)*
