# F-001: HUD Framework (UI Designer Analysis)

*Visual system: Glassmorphism from DESIGN.md — this analysis covers UX structure only.*

## 1. User Experience Assessment
The HUD MUST permanently display core resources, population, and research progress (D-SME-01) while remaining minimal and unobtrusive (D-UI-01, D-UI-02). It MUST clearly visualize economic rates to reduce player guesswork (D-PM-02).

## 2. Information Architecture
- **Global Top Bar**: Contains primary resources (Money, Wood/Minerals, Power), Population, and Tech Progress.
- **Minimap & Alerts**: Bottom-left corner.
- **Objectives/Quests**: Top-right corner (collapsible).

## 3. Component Behavior & ASCII Wireframe

```text
+---------------------------------------------------------------------------------+
| [Money: 1500 (+15/s)]  [Wood: 300 (-5/s)]  [Power: 50/100]  [Pop: 15/50] | [Tech: 45%] |
|                                                                                 |
|                                                                                 |
|                                     MAIN                                        |
|                                                                                 |
|                                   VIEWPORT                                      |
|                                                                                 |
|                                                                                 |
| +---------+                                                                     |
| |         |                                                                     |
| | Minimap |                                                                     |
| |         |                                                                     |
| +---------+                                                                     |
+---------------------------------------------------------------------------------+
```

## 4. State Matrix & Interaction Patterns
- **Resource Items**:
  - *Default*: Shows current value and rate.
  - *Hover*: Displays a detailed tooltip breakdown of income vs. consumption.
  - *Warning/Error State*: Text turns red (or uses an alert icon) when the rate is severely negative or capacity is reached.

## 5. Design Integration
The HUD glassmorphism background MUST include a dark under-mask and dynamic blur (D-UI-03) to ensure the light-themed text (D-UI-01) is always readable regardless of the game world background.
