# PortalHeim SG1

Turns every portal into a **stone ring gate**: an outer ring with nine glowing **Chevrunes** (chevrons set with runes), and an inner ring of **Elder Futhark runes** that spins when a portal connects while the Chevrunes **lock one by one** and glow. It's *inspired by* a certain famous gate, but built from Norse runes and a design of its own.

When someone walks up to a connected gate (at either end), it **starts up**: the ring spins and the **Chevrunes** (arrows that turn into glowing runes) lock on in pairs, bottom to top, then the opening **fills with a rippling pool of water**. When everyone has gone it shuts down again. A connected gate that nobody is near glows faintly. Gentle **ramps** on both sides let anyone walk in, even when overencumbered. On a long portal trip the loading screen becomes an **endless swirling tunnel** (a blue time-vortex flying toward a bright light) instead of the game's spinner. (Prefer the game's own swirl? Set `Interior` to `Vanilla`.)

It only changes how portals **look and sound**. Portals work exactly as before, and it's **client-side only**: nobody else needs the mod, and other players see normal portals.

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

## Dialing any gate (needs the mod on the server too)

With PortalHeim_SG1 on the **server** as well as on each player's game, gates can be dialed instead of only pairing by name.

- **Use an unconnected gate** to open the dial window: a list of the gates you know, nearest first. Pick one and press **Dial** (or Enter). Both gates run the usual start-up sequence and open.
- **Home gate:** press **Set home** next to a gate. It is highlighted first in the window, and **Shift+use** on any free gate dials home immediately.
- **On the map:** known gates get a small gate icon. **Shift+click** a gate's pin while standing at a free gate to dial it.
- A dialed gate closes by itself 15 seconds after the last player leaves both gates (never later than 5 minutes). **Hang up** in the window closes it sooner.
- **Which gates you see:** by default only gates you have been near, or built yourself, so exploring still matters. Each character keeps its own list. Set `Dialing / OnlyShowVisitedGates` to false to see every gate.
- Gates that share a name still pair exactly as in vanilla (and use goes straight to the game's naming window for those). Dialing only connects free gates, and puts each gate's name back afterwards.

**Where to install it:** a dedicated server needs BepInEx and this mod, and so does every player. If you host from your own game, you and your friends all need it. Playing alone, it just works. On a server without the mod, everything looks and sounds the same but gates behave as in vanilla (no dialing, no gate pins).

## Configuration

`BepInEx/config/mbaya.PortalHeimSG1.cfg`, or Gale's config editor:

| Key | Default | Notes |
| --- | --- | --- |
| Look / Reskin | true | Set to false to keep ordinary portals |
| Look / ChevruneColor | orange | Glow colour of a locked Chevrune |
| Look / Ramps | true | A gentle ramp up to the portal's base on both sides |
| Look / SinkRing | true | The ring sits low so the opening is level with the ground |
| Look / DormantGlow | 0.3 | How brightly a connected but idle gate's Chevrunes glow (0 = dark) |
| Look / SecondsPerChevrune | 0.5 | Seconds between start-up steps (five steps: bottom pair, then up the sides, top last) |
| Diagnostics / PreviewKey | F8 | Stand near a gate and press it to replay the opening sequence and sounds |
| Diagnostics / ShowStatusOnSpawn | true | Shows a status line a few seconds after you spawn (turn off once it all works) |
| Look / TravelScreen | Tunnel | `Tunnel` = endless swirling tunnel, `Gate` = a miniature gate, `Vanilla` = the game's own spinner (reload a world to apply) |
| Look / Interior | Water | `Water` = rippling pool, `Vanilla` = the portal's own swirl |
| Look / Shading | PortalShader | If the ring looks wrong or is missing, try `Standard` (restart the game) |
| Dialing / Enabled | true | Dialing on or off (set it on the server to switch it off for everyone) |
| Dialing / OnlyShowVisitedGates | true | `false` lists and pins every gate in the world |
| Dialing / ShowGatePins | true | Gate pins on the map |
| Dialing / GateIconOnMap | true | Mark gates with the mod's own gate icon (orange Chevrunes, light-blue water) |
| Dialing / PinIcon | Icon4 | Built-in map icon used when GateIconOnMap is off |
| Dialing / CloseAfterIdleSeconds | 15 | Server: a dialed gate closes this long after the last player leaves |
| Dialing / MaxOpenSeconds | 300 | Server: a dialed gate always closes after this long |
| Dialing / DialRange | 15 | Server: how close you must be to a gate to dial from it |
| Dialing / KeepOpenRange | 15 | Server: a dialed gate stays open while a player is this close to either gate |
| Sound / TravelSounds | true | The bass loop on the portal-travel loading screen |
| Sound / TravelMusicVolume | 0.75 | Volume of the travel music |
| Sound / TravelEffectVolume | 0.5 | Volume of an optional second `travelfx` layer (none ships) |
| Sound / UseBundledSounds | true | Turn off the included sounds (restart the game) |
| Sound / Volume | 1.0 | Also follows the game's sound-effects volume |

## Roadmap

Sharing your visited-gate list between players, and naming gates with a nicer window, are possible later additions.
