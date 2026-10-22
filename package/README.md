# PortalHeim SG1

**Turn Valheim's wooden portals into ancient stone ring gates, then dial any gate you know from a menu or the map.**

Every portal becomes a ring of carved stone with nine glowing **Chevrunes** and a turning inner ring of **Elder Futhark runes**. Walk up to a connected gate and it wakes up: the ring spins, the Chevrunes lock on one pair at a time, and the opening fills with a pool of rippling water. It's *inspired by* a certain famous gate, but built from Norse runes and a design of its own.

And it isn't only cosmetic. With the mod on your server too, you can **dial any gate** instead of pairing by name, see your gates on the **map**, **carry ore and metal** through, and **pull your cart through** with you.

<!-- IMAGE: hero-gate.png (an open gate, front-on, at dusk) -->

## Contents

- [What you get](#what-you-get)
- [Quick start](#quick-start)
- [Dialing any gate](#dialing-any-gate)
- [Carts and heavy loads](#carts-and-heavy-loads)
- [Examples](#examples)
- [What needs the mod on the server](#what-needs-the-mod-on-the-server)
- [Sounds](#sounds)
- [Configuration](#configuration)
- [Troubleshooting](#troubleshooting)
- [Works with](#works-with)

## What you get

**The gate**
- A **stone ring** with an inner ring of rune carvings and **nine Chevrunes** around it.
- **Start-up sequence:** when someone walks up to a connected gate (at either end), the inner ring turns and the Chevrunes lock on in pairs, bottom to top, the top one last. They start as arrows and turn into glowing runes as they lock.
- **A pool of water** fills the opening once the gate is open, and drains away when everyone has gone. (Prefer the game's own swirl? Set `Interior` to `Vanilla`.)
- **Dormant glow:** a connected gate that nobody is near glows faintly, so you can tell it's live from across the base.
- **Ramps and a sunk ring:** the bottom of the ring sits at ground level and a gentle ramp on the front lets anyone walk in, even when overencumbered. (Want one on the back too? Set `Look / BackRamp` to true.)

<!-- IMAGE: gate-sequence.png (three frames: dormant, Chevrunes locking, open with water) -->

**The trip**
- A long portal trip shows an **endless swirling tunnel** flying toward a bright light, with a deep bass loop, instead of the game's spinner.

**The network**
- **Dial any gate** you've visited from a menu at the gate, or by clicking its pin on the map.
- **Set a home gate** and dial it in one keypress.
- **Carry anything** through a portal, ore and metal included.
- **Take your cart with you:** it comes through and hooks back on.

## Quick start

1. Install with **Gale** (or any Thunderstore mod manager). It needs **BepInExPack_Valheim**, which installs automatically.
2. Start the game. Every portal you see is now a ring gate. Existing portals change too, and nothing about them breaks: names, pairs and destinations are exactly as they were.
3. **Optional but recommended:** install the mod on your **server** (or the game you host) as well, to turn on dialing, gate pins and carts. See [what needs the mod on the server](#what-needs-the-mod-on-the-server).

That's all for the new look: there's nothing to build or craft. Portals are built from the Hammer exactly as before.

## Dialing any gate

With the mod on the server too, you aren't limited to portals that share a name.

1. Walk up to a gate and press **E** (the normal use key). A window opens listing the gates you know, **nearest first**, with their distance.
2. Pick a gate and press **Dial** (or highlight it and press **Enter**). Both gates run their start-up sequence and open. Walk in.
3. The gate closes by itself about 15 seconds after the last player has left both gates. **Hang up** in the same window closes it sooner.

<!-- IMAGE: dial-menu.png (the dial window with a few gates listed) -->

**Home gate.** Press **Set home** next to a gate. From then on it's highlighted first in the window, and **Shift+E** on any gate dials home straight away: the "I'm in a hurry" button.

**On the map.** Known gates appear on your map as a small gate icon (the powered-off gate, sunk in the ground with its ramp). Stand at a gate, open the map, and **Shift+click** a gate's icon to dial it. The map updates by itself whenever a gate is built, removed or renamed.

<!-- IMAGE: map-pins.png (the map with several gate icons and their names) -->

**Which gates do I see?** By default, only gates you've been near (within about 30 m) or built yourself, so exploring still matters. Each character remembers its own list. If you'd rather see every gate in the world, set `Dialing / OnlyShowVisitedGates` to false.

**Paired gates work too.** Portals that share a name still pair the way they always have, and walking through them works as usual. You can also dial from or to a paired gate: its old partner is set aside while the dial is open, and when the dial closes the original pair is put back exactly as it was. (Switch this off with `Dialing / AllowDialingLinkedGates`.)

**Naming.** Give each gate its own name so you can tell them apart in the list (use a gate, then **Rename this gate**). Gates sharing a name pair in twos at random, the way Valheim always has, which is confusing in a list. Unnamed gates pair with each other the same way, so name them all.

## Carts and heavy loads

- **No item limits.** Valheim refuses to let you through a portal carrying ore, metal bars and a few other items. PortalHeim lifts that limit (`Portals / AllowAllItems`, on by default), so hauling a pack full of copper to the forge takes one step.
- **Carts follow you.** Pull a cart into a gate and it comes through with you and hooks back on, ready to keep pulling. You step out a few metres further from the gate than usual so the cart behind you isn't inside it (`Portals / CartExitDistance`).

<!-- IMAGE: cart-through.png (someone pulling a cart out of a gate) -->

## Examples

**A home gate network.** Build a gate at your main base, name it *Home*, and press **Set home**. Build one at each outpost: *Mines*, *Swamp*, *Plains*, *Boss altar*. From any of them, **Shift+E** sends you home. From home, **E** and pick where to go. No more guessing which two portals share a name.

**Hauling ore.** Load a cart at the mine gate, pull it into the gate, dial *Home* and walk out the other side with the cart behind you. Unload at the smelter.

**A quick escape.** In trouble at an outpost? **Shift+E** on its gate dials home. (The gate closes again by itself once everyone has left.)

**Keeping old pairs.** You already have two pairs of gates named *SG1*. They keep working as two pairs. Dialing from one of them sets its partner aside until the dial closes, then puts the pair back as it was.

**Playing alone.** It all works in single player, with no server involved: the game is its own server.

## What needs the mod on the server

| Feature | Where it must be installed |
| --- | --- |
| Ring gate look, start-up sequence, water, sounds, ramps, travel tunnel | **Your game only.** Other players see normal portals (or their own look if they have the mod) |
| Carry any item through | **Your game only** |
| **Dial any gate**, gate list, **gate map icons**, **carts follow you** | **Your game and the server** (a dedicated server, or the host's game). Everyone who uses these needs it too |

On a server without the mod, everything still looks and sounds the same on your side, but gates behave as in vanilla: no dialing, no gate pins, carts stay behind. Nothing breaks. If you host from your own game, you and your friends all need the mod for dialing. A dedicated server needs BepInEx with this mod installed.

## Sounds

Gate sounds are **included**: a heavy lock thud for each Chevrune, a stone scrape as the ring turns, a deep rumble and splash as the gate opens, a low hum while it's open, a whoosh when you step through, and a power-down when it shuts. They're mixes of free public-domain (CC0) recordings and sounds made for this mod (credits below).

**Want your own?** Put clips (`.ogg`, `.wav` or `.mp3`) in `BepInEx/config/PortalHeim_SG1/sounds/`, named for the moment they play. A file there replaces the included one. Restart the game after adding files.

| File | Plays when |
| --- | --- |
| `chevrune` | each Chevrune locks |
| `spin` | the inner ring turns |
| `activate` | the gate opens |
| `idle` | the open gate hums (loops; replaces the game's own hum) |
| `teleport` | someone steps through |
| `close` | the gate shuts down |
| `travel` | the music that loops while you're in the portal tunnel (the loading screen for a long trip) |
| `travelfx` | optional: a second sound layered under it (loops); none ships |

### More free sounds

All **CC0** (public domain), none from any show:

- [100 CC0 SFX #2](https://opengameart.org/content/100-cc0-sfx-2) by rubberduck: stones, metal hits, doors, switches, air flowing, and loops including flowing water.
- [Metal Impact Sounds](https://opengameart.org/content/metal-impact-sounds) by BMacZero: thuds and clinks.
- [6 Short Water Splashes](https://opengameart.org/content/6-short-water-splashes) by ezwa: splashes.
- [Various Sound Effects](https://aleksicluka.itch.io/various-sound-effects) by Luka Aleksic: includes `grind_stone`, `lock` and door sounds.
- [Sci-Fi Sounds](https://kenney.nl/assets/sci-fi-sounds) by Kenney: 70 sci-fi and engine sounds.

### Credits for the included sounds

Built from CC0 recordings by **rubberduck** (100 CC0 SFX #2), **BMacZero / Brian MacIntosh** (Metal Impact Sounds) and **ezwa** (6 Short Water Splashes, cut by qubodup from public-domain recordings), plus sounds synthesized for this mod. The travel sound is **"Deepbassloop80bpm"** by **deep into perspectives**, CC0. Details in `sounds/CREDITS.txt`.

## Configuration

`BepInEx/config/mbaya.PortalHeimSG1.cfg`, or Gale's config editor:

| Key | Default | Notes |
| --- | --- | --- |
| Look / Reskin | true | Set to false to keep ordinary portals |
| Look / ChevruneColor | orange | Glow colour of a locked Chevrune |
| Look / Ramps | true | A gentle ramp up to the portal's base on the front |
| Look / BackRamp | false | Also add a ramp on the back |
| Look / SinkRing | true | The ring sits low so the opening is level with the ground |
| Look / DormantGlow | 0.3 | How brightly a connected but idle gate's Chevrunes glow (0 = dark) |
| Look / SecondsPerChevrune | 0.5 | Seconds between start-up steps (five steps: bottom pair, then up the sides, top last) |
| Look / TravelScreen | Tunnel | `Tunnel` = endless swirling tunnel, `Gate` = a miniature gate, `Vanilla` = the game's own spinner (reload a world to apply) |
| Look / Interior | Water | `Water` = rippling pool, `Vanilla` = the portal's own swirl |
| Look / Shading | PortalShader | If the ring looks wrong or is missing, try `Standard` (restart the game) |
| Portals / AllowAllItems | true | Carry ore, metal and other normally blocked items through portals |
| Portals / CartsFollow | true | A cart you are pulling comes through with you (needs the mod on the server) |
| Portals / CartExitDistance | 5.5 | Extra metres from the gate you come out when pulling a cart |
| Dialing / Enabled | true | Dialing on or off (set it on the server to switch it off for everyone) |
| Dialing / AllowDialingLinkedGates | true | Paired gates can be dialed from or to; the old partner is set aside until the dial closes |
| Dialing / OnlyShowVisitedGates | true | `false` lists and pins every gate in the world |
| Dialing / ShowGatePins | true | Gate pins on the map |
| Dialing / GateIconOnMap | true | Mark gates with the mod's own gate icon |
| Dialing / PinIcon | Icon4 | Built-in map icon used when GateIconOnMap is off |
| Dialing / CloseAfterIdleSeconds | 15 | Server: a dialed gate closes this long after the last player leaves |
| Dialing / MaxOpenSeconds | 300 | Server: a dialed gate always closes after this long |
| Dialing / DialRange | 15 | Server: how close you must be to a gate to dial from it |
| Dialing / KeepOpenRange | 15 | Server: a dialed gate stays open while a player is this close to either gate |
| Sound / TravelSounds | true | The bass loop on the portal-travel loading screen |
| Sound / TravelMusicVolume | 0.75 | Volume of the travel music |
| Sound / TravelEffectVolume | 0.5 | Volume of an optional second `travelfx` layer (none ships) |
| Sound / MuteIncomingChevrunes | true | No chevrune lock sounds on an incoming gate (one that's opening but isn't the gate you're standing at) |
| Sound / MuteIncomingOpening | true | No gate-opening sound on an incoming gate |
| Sound / UseBundledSounds | true | Turn off the included sounds (restart the game) |
| Sound / Volume | 1.0 | Also follows the game's sound-effects volume |
| Diagnostics / PreviewKey | F8 | Stand near a gate and press it to replay the opening sequence and sounds |
| Diagnostics / ShowStatusOnSpawn | true | Shows a status line a few seconds after you spawn (turn off once it all works) |

## Troubleshooting

- **Portals look the same as before.** Check that the mod is in the profile you launch from. A few seconds after you spawn, a status line says how many portals are gates and whether the water and sounds loaded. If the ring is missing or looks wrong, set `Look / Shading` to `Standard` and restart.
- **No water in the gate.** The water only shows while a gate is live: someone standing at either end. Stand next to it, or press **F8** to replay the opening sequence.
- **Pressing E still opens the old naming box.** The mod isn't on the server (or the server hasn't answered yet; give it a few seconds after spawning). Gates paired by name behave the old way when `Dialing / AllowDialingLinkedGates` is off.
- **The dial list is empty or missing a gate.** By default you only see gates you've been near or built. Walk past it once, or set `Dialing / OnlyShowVisitedGates` to false.
- **Two gates share a name and won't dial.** Gates sharing a name pair in twos at random. Rename each one so it's unique.
- **Something else.** Open an issue at [github.com/mattbaya/PortalHeim-SG1](https://github.com/mattbaya/PortalHeim-SG1) and attach `BepInEx/LogOutput.log`. The mod logs each dial and the server handshake there.

## Works with

- **[Bag O' Heim](https://thunderstore.io/c/valheim/p/Svaha/BagOHeim/)**: its *Bag O' Portals* carries two portals' worth of materials, so you can drop a new gate anywhere. A pack holding ore no longer blocks portals when PortalHeim is installed.
- **[GearHeim](https://thunderstore.io/c/valheim/p/Svaha/GearHeim/)**: switch to your "Mining" loadout, step through a gate to the mine.
- Other mods that change how portals *behave* may conflict with dialing, and ones that only change their look may fight over the visuals. If something misbehaves, open an issue and say which mod.
