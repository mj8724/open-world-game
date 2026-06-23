# F-003: Unit Details Panel (UI Designer Analysis)

*Visual system: Glassmorphism from DESIGN.md — this analysis covers UX structure only.*

## 1. User Experience Assessment
Unit details MUST appear in a fixed area when selected (D-UX-02) to maintain a consistent reading location for the player. Critical combat stats like cover bonus and armor resistance MUST be prominently displayed (D-SME-02).

## 2. Information Architecture
- **Header Section**: Unit Icon, Unit Name, Level/Rank indicator.
- **Status Bars**: Health Points (HP), Energy/Morale.
- **Combat Stats Section**: Cover Bonus, Armor Resistance, Base Damage.
- **Action Grid**: Contextual abilities, stances, and commands.

## 3. Component Behavior & ASCII Wireframe

```text
+---------------------------------------------------------------------------------+
|                                                                                 |
|                                                                                 |
|                                                                                 |
|                                                                                 |
|                                                                                 |
|                                                                                 |
|                                                                                 |
|                                                                 +---------------+
|                                                                 | [Icon] Sniper |
|                                                                 | HP: [====---] |
|                                                                 | Cover: +50%   |
|                                                                 | Armor: Light  |
|                                                                 |               |
|                                                                 | (Atk) (Stop)  |
|                                                                 | (Move)(Abil)  |
|                                                                 +---------------+
+---------------------------------------------------------------------------------+
```

## 4. State Matrix
- **Empty State**: The panel MUST be completely hidden when no unit is selected.
- **Multi-Select State**: Displays a condensed list or grid of selected unit icons. The detailed stats area aggregates the health or hides specific unit stats, displaying only group commands.

## 5. Design Integration
The panel MUST utilize the dark under-mask and dynamic blur (D-UI-03) to ensure stat numbers and action icons are highly legible during chaotic combat scenes.
