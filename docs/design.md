# Verdant Vigil Design

## Premise

Long before the present age, artisans learned that certain temporal minerals
respond to disciplined intent. Their surviving craft, the Verdant Vigil, uses
etched metal foci to stabilize that resonance into short-lived physical forms.
The power is neither effortless nor limitless: using it drains stored resonance
and strains the wielder's resolve.

## Vocabulary

- **Vigil focus:** A crafted wearable that shapes resonance.
- **Resonance vessel:** A placed device that stores and restores energy.
- **Resolve:** The wielder resource that limits sustained abilities.
- **Resonant form:** A temporary tool, barrier, platform, or projectile.
- **Verdant shard:** A temporal crafting material used in advanced foci.

These terms are working names and remain independent from any third-party
fiction.

## Core Loop

1. Recover verdant shards and inscriptions from ruins or temporal events.
2. Smith a vigil focus and assemble a resonance vessel.
3. Charge the focus at the vessel.
4. Spend resonance and resolve to manifest forms.
5. Discover new form patterns and improve efficiency through exploration.

## Mechanical Principles

- Server authority controls energy, cooldowns, damage, and spawned forms.
- Client code handles input, previews, effects, and HUD presentation.
- Every ability has a survival cost and a clear counterplay window.
- Constructs are temporary and must not bypass claims or protected blocks.
- State must survive reconnects and work consistently in multiplayer.
- Content should complement temporal storms, ruins, smithing, and exploration.

## Milestones

1. **Foundation (0.1.0):** Project setup, metadata, asset domain, build, and packaging.
2. **Focus (0.2.0-0.3.0 prototype):** A held ring focus with synchronized charge, a temporary barrier form, and early flight.
3. **Vessel (0.3.0):** A craftable charging pedestal and first progression recipe.
4. **Forms:** Barrier, utility tool, traversal platform, and simple projectile.
5. **Progression:** Ruin discoveries, pattern unlocks, balancing, and config.
6. **Polish:** Original models, particles, audio, handbook pages, and tests.

## First Vertical Slice

Version 0.3.0 implements the focus, vessel, and barrier portion of this slice.
The flight test uses the game's normal client movement while the server grants,
validates, drains once per minute, and revokes the state. Hovering, cruising,
and sprinting forward select distinct bracer flight poses; sprinting adds a
temporary boost. This proves inventory state,
activation input, claim-aware placement, persistent temporary world behavior,
and multiplayer authority without prematurely building a large ability
framework.
