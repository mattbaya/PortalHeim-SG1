# PortalHeim-SG1

A [BepInEx](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/) mod for Valheim that turns portals into stone ring gates with Norse runes and glowing **Chevrunes**, **inspired by** a famous sci-fi gate (not a copy: runes instead of glyphs, original geometry). Thunderstore package: `PortalHeim_SG1` (Thunderstore names can't contain hyphens).

Player-facing docs: [package/README.md](package/README.md).

> Testing build: not yet tried in the game.

## Layout

| Path | Purpose |
| --- | --- |
| `src/` | Mod source (`PortalHeimSG1.csproj`, targets `netstandard2.1`) |
| `tools/make_sounds.py`, `tools/make_travel.py` | Rebuild the bundled sounds (CC0 recordings plus synthesis) and the synthesized travel bass line |
| `src/Art/` | Plain C# (no Unity types) that paints the stone and rune textures, so the artwork can be previewed outside the game |
| `package/` | Thunderstore files |
| `build.sh` | Builds and zips a Thunderstore package into `dist/` |
| `libs/Managed/` | Game assemblies to compile against (**git-ignored, never commit**) |

## How it works

A Harmony postfix on `TeleportWorld.Awake` hides the wooden frame (`forceRenderingOff` on `m_model`) and adds a procedurally built ring, rune ring and nine Chevrunes. `GateRing` watches the portal's connection state and animates the connect sequence. Gate sounds are bundled as WAV files in `package/sounds/` (CC0 recordings mixed with synthesized sounds; see `package/sounds/CREDITS.txt`) and can be overridden by files in the config folder. Rebuild them with `tools/make_sounds.py` (needs ffmpeg and the three CC0 packs listed in that script).

## Building

Same setup as [GearHeim](https://github.com/mattbaya/GearHeim#building) without Jötunn, plus `UnityEngine.AudioModule`, `UnityEngine.UnityWebRequestModule` and `UnityEngine.UnityWebRequestAudioModule` in `libs/Managed/`. Then `./build.sh`.

## Contributing

Bug reports and pull requests are welcome. Please open an issue first for anything larger than a small fix. Releases to Thunderstore are published by the maintainer.
