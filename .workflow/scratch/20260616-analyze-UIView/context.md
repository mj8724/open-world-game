# Context: 重构UI表现层

## Locked Decisions (约束与固定决策)
- **着色器补全**: 必须完成 `UI_Glassmorphism_Blur.shader`，实现真正的模糊采样（GrabPass 或 CommandBuffer）与半透明颜色叠加。
- **视觉重构**: 左侧冗杂的灰色大面板废弃。改为多层级的“抽屉式”折叠菜单。
- **交互逻辑**: 菜单项需进行分类（例如：生产、军事、物流），默认只显示分类图标按钮，点击后侧边栏才平滑滑出显示具体内容。
- **绑定 ViewModel**: 这些新的 UI 面板需挂载之前写好的 `UIViewModel` 派生类。

## Free Variables (由 Plan/Execute 自由决定)
- 折叠动画的具体实现方式（DOTween 或 Unity Animator）。
- 抽屉菜单的具体分类细则和图标资源。

## scope_verdict
medium
