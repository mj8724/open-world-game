# Subject Matter Expert Analysis

## §1 Role Mandate
As the Subject Matter Expert (SME), the primary focus is to ensure that the UI redesign accurately reflects the domain knowledge, industry standards, and game mechanics of a real-time open-world strategy game. The mandate is to safeguard the representation of core systems (economy, combat, technology) and ensure that UI architectural decisions support the complex, data-heavy nature of the game without compromising performance or player comprehension.

## §2 Decision Digest

### Decisions
| Decision ID | Summary | Domain Implication | Status |
|---|---|---|---|
| D-SME-01 | Core resources, population, tech progress MUST be permanently visible. | Ensures critical economic and strategic situational awareness is never lost. | Accepted |
| D-SME-02 | Combat modifiers (cover, armor resistance) MUST be highly visible on unit panel. | Aligns UI with tactical gameplay loops, reducing cognitive load. | Accepted |

### Interfaces
| Interface | Domain Boundary | SME Requirement |
|---|---|---|
| HUD Framework (F-001) | Player Economy & Meta-State | MUST present accurate, real-time rates of change. |
| Construction Drawer (F-002) | Base Building & Logistics | MUST organize structures by domain taxonomy (e.g., Economy, Military). |
| Unit Details Panel (F-003) | Tactical Combat State | MUST expose dynamic combat variables clearly. |
| Data Binding System (F-004) | UI & Simulation Decoupling | MUST NOT introduce state desyncs or GC spikes. |

### Cross-Cutting Positions
| Topic | SME Position | Rationale |
|---|---|---|
| Visual Minimalism | SUPPORT with conditions | Glassmorphism (D-UI-02) MUST NOT obscure critical data readability. Contrast is vital. |
| Event-Driven UI | STRONGLY SUPPORT | D-SA-01 aligns with RTS best practices, avoiding costly per-frame polling. |

### Findings Summary
- The shift to an MVVM, event-driven architecture is appropriate for the domain complexity.
- Tactical data exposure (cover, armor) will directly improve the core gameplay loop.
- Memory management (GC) remains the highest technical risk for the binding system in a Unity environment.

## §3 Cross-Cutting Foundations
- **Domain Language**: Standardize terminology across the UI (e.g., "Credits", "Minerals", "Pop Cap"). UI elements MUST use these terms consistently.
- **Performance Constraints**: With the new binding system, data updates from the simulation to the UI MUST be batched where applicable to avoid GC pressure.
- **UX/Domain Intersection**: "What you see is what you get" (D-PM-02) requires the UI to accurately reflect simulation ticks, especially for resource rates.

## §4 File Index
- [F-001 HUD Framework](./analysis-F-001-hud-framework.md)
- [F-002 Construction Drawer](./analysis-F-002-construction-drawer.md)
- [F-003 Unit Details Panel](./analysis-F-003-unit-details-panel.md)
- [F-004 Data Binding System](./analysis-F-004-data-binding-system.md)

## §5 Outstanding TODOs
- [ ] Define precise data update frequency for resources (e.g., 4 ticks per second vs 60Hz).
- [ ] Audit the complete list of unit status effects that need representation in the Unit Details Panel.
