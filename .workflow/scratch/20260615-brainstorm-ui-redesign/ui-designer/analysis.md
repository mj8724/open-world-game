# UI Designer Analysis: Brainstorming UI Redesign

*Visual system: Glassmorphism from DESIGN.md — this analysis covers UX structure only.*

## §1 Role Mandate
As the **UI Designer** (acting primarily as a UX Designer for this phase), my mandate is to focus on interaction design, user flows, information architecture, and state design. I am responsible for defining component behaviors, layout structures via ASCII wireframes, and managing edge cases (empty, error, loading states). Visual styling decisions are deferred to the upstream visual system, though I MUST accommodate architectural constraints such as the dynamic blur backgrounds.

## §2 Decision Digest

### Decisions (UI/UX)
| Decision ID | Description | Impact |
|---|---|---|
| D-UI-01 | Light theme, clean laboratory style | Establishes the contrast baseline for UX text and wireframing. |
| D-UI-02 | Glassmorphism background/containers | Requires careful spacing and typography to ensure readability without solid borders. |
| D-UI-03 | Dynamic blur and dark under-mask for panels | Solves contrast issues in bright terrains; impacts z-index and layering rules in UX. |
| D-UX-01 | Edge slide-out drawer for construction/details | Maximizes center viewport visibility; dictates drawer interaction flows. |
| D-UX-02 | Fixed area (bottom-right) for unit details | Anchors contextual interactions, reducing visual hunting for stats. |

### Interfaces (System/Architecture)
| Decision ID | Description | UX Impact |
|---|---|---|
| D-SA-01 | Event-driven UI updates | Defines how state changes propagate to the user; requires clear loading/sync indicators. |
| D-SA-02 | MVVM / Reactive binding | Ensures UI components strictly reflect data state; UX MUST define fallback states if data is missing. |

### Cross-Cutting Positions
| Decision ID | Description | UX Alignment |
|---|---|---|
| D-PM-01 | P0 priority for core loop UI | Focus initial wireframes on HUD, construction, and selection. |
| D-PM-02 | "What you see is what you get" economic visibility | Requires prominent display of rates and totals in the HUD architecture. |
| D-SME-01 | Permanent display of core resources/pop/tech | Dictates top-level HUD Information Architecture. |
| D-SME-02 | Prominent cover/armor stats | Requires specific hierarchical emphasis in the unit details panel. |

### Findings Summary
The redesign shifts from deep nested menus to a flattened, edge-aligned architecture. By utilizing slide-out drawers (D-UX-01) and fixed contextual zones (D-UX-02), the center viewport remains clear. The reactive architecture (D-SA-02) requires robust state definitions (loading, error, empty) to ensure smooth user feedback.

## §3 Cross-Cutting Foundations
- **Information Architecture**: Top edge for global state (resources, tech). Right/Left edges for contextual details and global actions.
- **Interaction Patterns**: Drawers slide in/out on toggle or context shift. Selection updates the bottom-right panel reactively.
- **State Handling**: 
  - *Loading*: Skeleton outlines within glass containers.
  - *Empty*: Ghost text indicating "No unit selected" or "No tasks queued".
- **Responsive & Accessibility**: UI MUST support dynamic scaling. High contrast text MUST be used over the blur masks (D-UI-03).

## §4 File Index
- [F-001: HUD Framework](analysis-F-001-hud-framework.md)
- [F-002: Construction Drawer](analysis-F-002-construction-drawer.md)
- [F-003: Unit Details Panel](analysis-F-003-unit-details-panel.md)
- [F-004: Data Binding System (UX View)](analysis-F-004-data-binding-system.md)

## §5 Outstanding TODOs
- Verify visual clarity of text over the dark under-mask with dynamic blur.
- Define keyboard shortcuts for drawer toggles.
- Specify animation timings for drawer slide-outs to ensure they feel responsive without causing cognitive overload.
