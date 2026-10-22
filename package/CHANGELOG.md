# Changelog

## 0.5.12
- **Pick the gate on the map.** The dial window has a new **Pick on map** button. It opens the map with a banner ("Click a gate to dial it from ..."), and a plain click on a gate's icon dials it. Esc or closing the map cancels. (Shift+click on a gate icon while standing at a free gate still works too.)

## 0.5.11
- **The ramp starts below the ground.** It used to dip 0.4 m below the base and run 1.25 m, so where the ground fell away in front of a portal, its end stood above the ground and was a step too high to walk onto when overencumbered. It now starts 1 m down and runs out further at the same gentle angle, meeting the ground however it falls (`Look / RampDepth` to change it).

## 0.5.10
- **The incoming gate plays one burst as it opens.** A gate that's opening but isn't the one you're standing at (someone dialed it, or it's where you arrive) no longer plays the five spaced chevrune thuds. Instead it plays one chevrune thud together with the opening sound, at the moment it opens. The gate you start from still plays everything. `Sound / IncomingGate` chooses: `Burst` (default), `Silent` (nothing) or `Normal` (the full sequence). This replaces `MuteIncomingChevrunes` and `MuteIncomingOpening`.

## 0.5.9
- **No more "PortalHeim loading" message** a few seconds after you spawn. The status line now goes to the log only (`BepInEx/LogOutput.log`); it shows on screen only if something goes wrong, or if you turn on `Diagnostics / ShowStatusMessage`. (The old `ShowStatusOnSpawn` setting is replaced by it.)
- **The gate you arrive at is quiet too.** It starts up as you step out of the trip, and its chevrune and opening sounds were still playing. For 15 seconds after you arrive, the gate you're standing at counts as the incoming one (`Sound / MuteIncomingChevrunes` and `MuteIncomingOpening`).

## 0.5.8
- **The transit sound is half as loud.** The music on the portal-travel loading screen now plays at 50% of its old volume. `Sound / TravelMusicVolume` still scales it (its 1.0 is now the old 0.5).

## 0.5.7
- New icon and screenshots in the description. No changes to how the mod works.

## 0.5.6
- **Clicking in the dial window no longer swings your weapon.** The game's player controller has its own input gate, which the window wasn't blocking.

## 0.5.5
- **Portals wake up from closer.** You have to be within 3 metres of a portal now (it was 5) before the ring starts up and the portal can be used. Change it with `Portals / ActivationRange`.
- **No door creak.** The chevrune lock sound is now just the heavy thud: the lock-and-door recording that was mixed in behind it is gone, for every gate. The stone scrape is unchanged.

## 0.5.4
- **No chevrune lock sounds and no opening sound on the incoming gate.** A gate that's opening but isn't the one you're standing at (someone else dialed it, or it's the far end of your trip) no longer thuds as each chevrune locks, and no longer plays the opening rumble and splash. The gate you're at still does. Switch them back on with `Sound / MuteIncomingChevrunes` and `Sound / MuteIncomingOpening`.

## 0.5.3
- The ramp on the **back** of the gate is gone: only the front ramp (the side you step out of) is built. `Look / BackRamp` brings it back.

## 0.5.2
- **Dial from or to any gate, even ones paired by name.** The gate it was paired with is set aside while the dial is open, and when the dial closes the original pair is put back exactly as it was. Switch off with `Dialing / AllowDialingLinkedGates`.

## 0.5.1
- Carts now hook back on to you after going through a portal (before, they arrived but weren't attached, so you had to press use on them).

## 0.5.0
- **Carry anything through a portal.** The game's limit on ore, metal and similar items is switched off (`Portals / AllowAllItems`, on by default).
- **Take your cart with you.** Pull a cart into a gate and it comes through with you and hooks back on, ready to keep pulling (`Portals / CartsFollow`). You step out a little further from the gate so the cart behind you clears it (`Portals / CartExitDistance`). Carts need the mod on the server too.

## 0.4.1
- The map and dial lists now update by themselves whenever a gate is built, removed, renamed, or starts or stops being connected (the server checks every second and tells everyone), instead of only when a gate was dialed.

## 0.4.0
- **Dial any gate.** With the mod on the server too, using a free gate opens a window listing the gates you know. Pick one and it dials, with the usual start-up sequence. Mark a **home gate** and Shift+use dials it straight away.
- **Gates on the map** with a small gate icon (the powered-off gate sunk in the ground with its ramp, flat like the game's own pins), and Shift+click a pin to dial it from the gate you're standing at.
- By default you only see gates you've been near or built (`Dialing / OnlyShowVisitedGates`); each character remembers its own.
- Dialed gates close by themselves once everyone has left. Gates that share a name still pair as in vanilla.
- Runs on dedicated servers: the server decides every connection, and puts gate names back if it stops mid-dial.
- Needs the mod installed on the server as well; without it, nothing changes.

## 0.3.0
- **New travel sound.** The loading screen for a long portal trip now loops a deep bass line, **"Deepbassloop80bpm"** by deep into perspectives (CC0, credited in `sounds/CREDITS.txt`). The synthesized bass line from 0.2.x is gone.
- Config: `Sound / TravelSounds` (on/off), `TravelMusicVolume` (0.75) and `TravelEffectVolume` (0.5, for an optional second `travelfx` layer; none ships). Your own `travel` and `travelfx` clips in the config sounds folder replace or add to them. (`TravelBassLine` and `TravelBassVolume` from 0.2.x no longer do anything.)

## 0.2.4
- The travel bass line is now a plain bass line: every note sits on the chord's root, with **no octave jumps**. The drone underneath is unchanged.

## 0.2.3
- The travel bass line now has a **low drone underneath** that follows the chord: a sustained A (for the IV chord) that glides down to a sustained E (for the I chord) and back again, in step with the notes above it.

## 0.2.2
- The travel bass line is **slower** (116 bpm instead of 138) and now plays **eight notes on each chord** (eight on the IV chord, A, then eight on the I chord, E) instead of four.

## 0.2.1
- **Travel bass line.** While you're on the loading screen for a long portal trip, a driving synth bass loops: four notes on the IV chord (A), then four on the I chord (E), alternating, in the spirit of the classic British sci-fi theme basslines. It's an original piece, synthesized for this mod. It fades in as you enter the tunnel and out as you arrive. Turn it off with `Sound / TravelBassLine`, change the level with `Sound / TravelBassVolume`, or replace it with your own `travel` clip.

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
