# F-002: Construction Drawer (UI Designer Analysis)

*Visual system: Glassmorphism from DESIGN.md — this analysis covers UX structure only.*

## 1. User Experience Assessment
To avoid obscuring the center viewport, the construction menu MUST slide out from the screen edge (D-UX-01). It serves as a primary interaction point for the core loop and MUST be highly accessible (D-PM-01).

## 2. Information Architecture & Flow
- **Entry Point**: Clicking the "Build" HUD icon, edge trigger, or pressing a dedicated hotkey.
- **Content Hierarchy**: 
  - Top: Tabs/Categories (Economy, Military, Defense).
  - Middle: Grid of buildable items.
  - Bottom/Side: Contextual detail area showing cost, description, and requirements on hover.

## 3. Component Behavior & ASCII Wireframe

```text
+---------------------------------------------------------------------------------+
|                                                                    [X] Close    |
|                                                                 +---------------+
|                                                                 | BUILD MENU    |
|                                                                 |---------------|
|                                                                 | [Eco] [Mil]   |
|                                                                 |               |
|                                                                 | [++] [++]     |
|                                                                 | [++] [++]     |
|                                                                 |               |
|                                                                 | Selected:     |
|                                                                 | Barracks      |
|                                                                 | Cost: 500W    |
|                                                                 +---------------+
+---------------------------------------------------------------------------------+
```

## 4. Interaction Patterns
- **Slide-in Animation**: Drawer animates smoothly from the edge. It MUST NOT instantly pop in.
- **Item Hover**: Highlights the item and immediately updates the contextual info pane via the data binding system (D-SA-02).
- **Item Click**: Initiates placement mode in the game world, temporarily hiding the drawer to maximize viewport space.

## 5. Edge Cases & Error States
- **Insufficient Resources**: Items lacking required resources MUST appear visually disabled (e.g., greyed out) but MUST remain focusable/hoverable so players can see the missing requirements.
