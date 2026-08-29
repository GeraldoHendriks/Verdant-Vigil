# Verdant Vigil

Verdant Vigil is an original Vintage Story code mod about channeling verdant
temporal resonance through resolve, crafted foci, and temporary constructs. It
draws inspiration from will-powered fantasy while using its own setting,
terminology, progression, art, and lore.

Version 0.3.0 provides wearable Vigil Bracers, a Resonance Vessel charging
station, and early hover flight.

## Requirements

- Vintage Story 1.22.3
- .NET 10 SDK
- The `VINTAGE_STORY` environment variable set to the game installation folder

On Linux, for example:

```bash
export VINTAGE_STORY="$HOME/ApplicationData/vintagestory"
```

The path must contain `VintagestoryAPI.dll` and a `Mods/VSSurvivalMod.dll` file.
Add the export to your shell profile if you want it to persist.

## Development

Build the mod:

```bash
dotnet build verdantvigil.sln
```

Package a release:

```bash
./build.sh
```

The package task validates asset JSON and writes
`Releases/verdantvigil_0.3.0.zip`. VS Code launch configurations are included
for a client, a server, and the package task.

## Prototype Testing

1. Launch the StoryForge **Test modpack** installation with creative mode.
2. Search the creative inventory for **Vigil Bracers**, then right-click them to equip the Arm slot.
3. Search for and place a **Resonance Vessel** nearby.
4. Right-click the vessel to restore all five bracer charges.
5. Change to survival mode, then press `R` to toggle flight. Jump ascends and sneak descends.
6. Hover to use the asymmetric bracer pose, move forward to cruise, and hold sprint while moving forward to accelerate into the straight-flight pose.
7. Confirm one resonance charge drains every minute and flight ends at zero charge.

The vessel is craftable with brass ingots and fire bricks;
the bracers remain creative-only during prototyping. Flight is limited to
survival mode so creative and spectator movement state is never modified.

## Layout

- `verdantvigil/` contains the mod code, metadata, and assets.
- `verdantvigil/assets/verdantvigil/` is the mod's asset domain.
- `CakeBuild/` validates, builds, and packages releases.
- `docs/design.md` records the original fiction and planned mechanics.

## Project Boundaries

Do not add third-party character names, organizations, symbols, dialogue,
costumes, artwork, or other protected material. New mechanics should use the
Verdant Vigil vocabulary and fit Vintage Story's temporal and metallurgical
setting.
