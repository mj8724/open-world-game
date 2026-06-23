# Analysis: F-002 Construction Drawer

## Domain Context
The construction system drives the economic and military expansion of the game. A slide-out drawer (**D-UX-01**) is a modern approach that avoids obscuring the battlefield while allowing detailed interaction.

## Requirements & Constraints
- The items within the construction drawer MUST be categorized according to the game's logical domain taxonomy (e.g., Production, Military, Defense, Tech).
- For each structure, the UI MUST clearly display its resource cost, build time, and power requirements *before* placement.
- If a player lacks resources, the UI MUST provide clear visual feedback indicating which specific resource is blocking the construction.

## Industry Standards
- Progressive disclosure: Advanced structures SHOULD only appear when their prerequisites are met, or they MUST be clearly marked as locked with tooltip explanations of the required technology.

## Risks
- A drawer that holds too many items without proper categorization will become a bottleneck for actions-per-minute (APM). The taxonomy MUST be shallow and intuitive.
