# PortalHeim SG1

Turns every portal into a **stone ring gate**: an outer ring with nine glowing **Chevrunes** (chevrons set with runes), and an inner ring of **Elder Futhark runes** that spins when a portal connects while the Chevrunes **lock one by one** and glow. It's *inspired by* a certain famous gate, but built from Norse runes and a design of its own.

When a gate is live (someone is standing at either end), the opening **fills with a rippling pool of water**; it drains away when everyone has gone. On a long portal trip the loading screen becomes an **endless swirling tunnel** (a blue time-vortex flying toward a bright light) instead of the game's spinner. (Prefer the game's own swirl? Set `Interior` to `Vanilla`.)

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

### More free sounds

All **CC0** (public domain), none from any show:

- [100 CC0 SFX #2](https://opengameart.org/content/100-cc0-sfx-2) by rubberduck: stones, metal hits, doors, switches, air flowing, and loops including flowing water.
- [Metal Impact Sounds](https://opengameart.org/content/metal-impact-sounds) by BMacZero: thuds and clinks.
- [6 Short Water Splashes](https://opengameart.org/content/6-short-water-splashes) by ezwa: splashes.
- [Various Sound Effects](https://aleksicluka.itch.io/various-sound-effects) by Luka Aleksic: includes `grind_stone`, `lock` and door sounds.
- [Sci-Fi Sounds](https://kenney.nl/assets/sci-fi-sounds) by Kenney: 70 sci-fi and engine sounds.

### Credits for the included sounds

Built from CC0 recordings by **rubberduck** (100 CC0 SFX #2), **BMacZero / Brian MacIntosh** (Metal Impact Sounds) and **ezwa** (6 Short Water Splashes, cut by qubodup from public-domain recordings), plus sounds synthesized for this mod. Details in `sounds/CREDITS.txt`.

## Configuration

`BepInEx/config/mbaya.PortalHeimSG1.cfg`, or Gale's config editor:

| Key | Default | Notes |
| --- | --- | --- |
| Look / Reskin | true | Set to false to keep ordinary portals |
| Look / ChevruneColor | orange | Glow colour of a locked Chevrune |
| Look / SecondsPerChevrune | 0.5 | Seconds between each Chevrune lighting up (nine in all) |
| Diagnostics / PreviewKey | F8 | Stand near a gate and press it to replay the opening sequence and sounds |
| Diagnostics / ShowStatusOnSpawn | true | Shows a status line a few seconds after you spawn (turn off once it all works) |
| Look / TravelScreen | Tunnel | `Tunnel` = endless swirling tunnel, `Gate` = a miniature gate, `Vanilla` = the game's own spinner (reload a world to apply) |
| Look / Interior | Water | `Water` = rippling pool, `Vanilla` = the portal's own swirl |
| Look / Shading | PortalShader | If the ring looks wrong or is missing, try `Standard` (restart the game) |
| Sound / UseBundledSounds | true | Turn off the included sounds (restart the game) |
| Sound / Volume | 1.0 | Also follows the game's sound-effects volume |

## Roadmap

A list of every gate you can dial, and gates shown on every player's map, are planned for a later version. Those will need the mod on the server too.
