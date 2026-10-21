# Changelog

## 0.2.0
- **The start-up sequence now plays whenever a gate goes live**, not just when a portal first connects. Walk up to a connected portal (at either end) and the ring spins while the Chevrunes lock on **in pairs, climbing the ring**: the bottom two first, then the next pair up on the left and right, and so on, with the top one last. Then the gate opens, the water swells in and the hum starts. When everyone has gone it shuts down again (Chevrunes dim, water drains, power-down sound). `SecondsPerChevrune` is now the time between steps.
- **Chevrunes are runes.** Each one sits as an arrow while idle and **turns into a glowing rune** as it locks. Around the ring the runes spell V-A-L-H-E-I-M in Elder Futhark (ᚹ ᚨ ᛚ ᚺ ᛖ ᛁ ᛗ), with ᛟ (Othala, "home") at the top and ᛞ to finish.
- A connected gate with nobody near glows faintly (`Look / DormantGlow`), so you can still tell which portals are connected; an unconnected one stays dark.
- **Ramps**: a gentle ramp up to the portal's base on both sides, so anyone can walk in, even when overencumbered (`Look / Ramps`).
- **The ring sits low** so the bottom of the opening is level with the ground, with no stone lip to step over (`Look / SinkRing`).
- The hum now starts once the gate is fully open. F8 replays the whole sequence.

## 0.1.9
- **The water now only shows while a gate is live**: when someone is standing at either end (the game's own portal glow is on, or a player is near the far portal). Walk away and it drains out; walk up and it swells in. Before, it stayed on whenever the portal was connected.

## 0.1.8
- **Endless tunnel.** The loading screen for a long portal trip is now a swirling blue time-vortex tunnel that fills the screen, flying toward a bright light, in the spirit of the classic 1970s-80s British "time tunnel" title sequences. It's an original, procedurally painted effect (not taken from any show). `Look / TravelScreen` now has three choices: `Tunnel` (default), `Gate` (the miniature gate from 0.1.7) and `Vanilla` (the game's own spinner).

## 0.1.7
- **New travel screen.** The spinning graphic shown on the loading screen during a long portal trip is replaced with a miniature gate: the rune ring slowly turning inside a frame of lit Chevrunes, with rippling water in the middle. Set `Look / TravelScreen` to `Vanilla` to keep the game's own spinner.

## 0.1.6
- **Fixed: the stone ring, rune ring and Chevrunes were being hidden along with the wooden frame**, so only the water showed.
- New **preview key (F8)**: stand near a gate and press it to replay the opening sequence with all its sounds, without disconnecting the portal. Change it with `Diagnostics / PreviewKey`.
- The spawn status line now counts the gates that exist right now (it counted every gate ever loaded, giving "4 of 2"), and says which sounds, if any, are missing.
- Sound loading now reports its own errors instead of failing silently.

## 0.1.5
- A one-line status message now appears a few seconds after you spawn: how many portals are gates, whether the water and sounds loaded, and any error. It makes it obvious whether the mod is working. Turn it off with `Diagnostics / ShowStatusOnSpawn`.

## 0.1.4
- **Gate sounds are now included**: a lock thud for each Chevrune, stone scrape, a deep rumble and splash as the gate opens, a low hum while it's open, a whoosh when you go through, and a power-down when it closes. They're mixes of free public-domain (CC0) recordings plus sounds made for the mod; none are from any show. Credits in `sounds/CREDITS.txt`.
- Your own clips in `BepInEx/config/PortalHeim_SG1/sounds/` still replace any of them. `UseBundledSounds` turns the included ones off.

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
