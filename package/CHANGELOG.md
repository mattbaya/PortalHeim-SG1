# Changelog

## 0.1.3
- **Water inside the gate.** When a gate opens, a pool of rippling water swells in with a little splash, in place of the portal's own swirl. Set `Interior` to `Vanilla` to keep the old swirl.
- Package page now lists free (CC0) places to find matching sounds.

## 0.1.2
- Chevrunes now light one at a time, half a second apart by default (`SecondsPerChevrune`). The inner ring turns one step between each.
- Hides every part of the wooden frame much more thoroughly (previously just its main model).
- If building the gate art fails, it now retries on the next portal and logs exactly which step failed, instead of leaving every portal vanilla for the whole session.
- New `Shading` option (`PortalShader` or `Standard`) in case the stone doesn't render right.
- The log now records what happened for the first gates, to make problems easy to diagnose.

## 0.1.1
- The glowing runic chevrons are now called **Chevrunes**. Config and sound names follow: `ChevruneColor`, `SecondsPerChevrune`, and the sound file `chevrune` (was `chevron`).

## 0.1.0
- First test build: every portal becomes a stone ring gate with a rotating Elder Futhark rune ring and nine chevrons that lock one by one when it connects.
- Sound slots filled from your own clips in `BepInEx/config/PortalHeim_SG1/sounds/`.
- Client-side only: no one else needs it, and portals behave exactly as before.
