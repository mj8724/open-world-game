# F-003 单位详情与指令面板 (Unit Details Panel) - PM Analysis

## 1. Feature Strategy & Business Value
Unit management is crucial for the combat phase. The new panel MUST surface combat-critical data efficiently. This feature is P0 as it falls under the unit selection foundation defined in D-PM-01.

## 2. Requirements & User Stories
- **User Story**: As a player, when I select a unit, I MUST instantly see its tactical advantages (like cover and armor) in a dedicated area so that I can make quick combat decisions.
- **Acceptance Criteria**:
  - The panel MUST be anchored to a fixed screen region (e.g., bottom right) when a unit is selected (per D-UX-02).
  - It MUST prominently display cover bonuses and armor resistances (per D-SME-02).

## 3. Product Constraints & Dependencies
- The design MUST support both single unit and multi-unit group selection paradigms.
- Readability in varying lighting MUST be ensured via bottom dark layers and blur (per D-UI-03).

## 4. Success Metrics
- **KPI**: Increased utilization of cover and tactical positioning by players.
- **KPI**: Reduced time from unit selection to issuing a command.
