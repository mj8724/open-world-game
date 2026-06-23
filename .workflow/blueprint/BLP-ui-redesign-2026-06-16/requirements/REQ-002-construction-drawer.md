---
id: REQ-002
title: 边缘滑出式建设抽屉面板
priority: must
session_id: BLP-ui-redesign-2026-06-16
status: complete
---

# REQ-002: 边缘滑出式建设抽屉面板

## 1. Description
取代目前可能遮挡屏幕中央的弹窗式建设菜单，新版建设界面作为一种 Drawer Panel，仅在呼出时从屏幕边缘平滑滑入。

## 2. User Story
As a RTS玩家,
I want 能够在选择建筑时依然能够观察战场中央的动态,
So that 我不会在紧急建造防御塔时由于视线被遮挡而错失敌人的动向。

## 3. Constraints & Rules
- 面板展开时 MUST 附带非线性的物理弹簧缓冲动效。
- 面板内部建筑列表 MUST 按分类（经济、军事、防御）进行 Tab 切换。
- 在面板外点击（屏幕中心任意位置）SHOULD 自动收起该抽屉面板。

## 4. Acceptance Criteria
1. **呼出与收起**: 按下快捷键或点击 HUD 按钮，抽屉面板在 0.2 秒内平滑滑出；再次点击则收回。
2. **防遮挡**: 滑出状态下，不阻碍主摄像机的点击射线（若射线未击中 UI 面板区域，可正常框选背后地形）。
3. **分类过滤**: 切换类别标签能立即刷新下属的可用建筑图标列表。
