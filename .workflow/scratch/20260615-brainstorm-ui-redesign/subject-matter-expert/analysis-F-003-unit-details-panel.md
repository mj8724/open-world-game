# Analysis: F-003 Unit Details Panel

## Domain Context
Micro-management is a core pillar of RTS combat. Players need immediate feedback on their units' tactical advantages and disadvantages to make split-second decisions and adapt to the battlefield.

## Requirements & Constraints
- Per **D-SME-02**, the panel MUST explicitly and prominently display combat-critical variables: "Cover Bonus" and "Armor Resistance".
- Health and Energy (if applicable) MUST be represented with both exact numerical values and visual progress bars.
- Status effects (buffs/debuffs) MUST be displayed with distinctive icons, and these icons MUST support hover states explaining the exact numerical modifier.
- When multiple units are selected (**D-UX-02**), the panel MUST aggregate this data sensibly, or provide a UI mechanism to sub-select units of a specific type.

## Industry Standards
- In modern RTS and MOBA games, dynamic modifiers (like cover) are often represented by distinct icon states or color changes in the health bar. We SHOULD adopt similar clear visual affordances.

## Risks
- Displaying too much raw data can overwhelm the player. We MUST differentiate between primary data (health, attack, cover) and secondary data (flavor text, minor stats), keeping the latter hidden behind tooltips or an "expand" toggle.
