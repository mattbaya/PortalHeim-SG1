# PortalHeim SG1

Turns every portal into a **stone ring gate**: an outer ring with nine glowing **Chevrunes** (chevrons set with runes), and an inner ring of **Elder Futhark runes** that spins when a portal connects while the Chevrunes **lock one by one** and glow. It's *inspired by* a certain famous gate, but built from Norse runes and a design of its own.

When a gate opens, the opening **fills with a rippling pool of water**. (Prefer the game's own swirl? Set `Interior` to `Vanilla`.)

It only changes how portals **look and sound**. Portals work exactly as before, and it's **client-side only**: nobody else needs the mod, and other players see normal portals.

## Sounds: bring your own

No sounds are included. On first run the mod creates `BepInEx/config/PortalHeim_SG1/sounds/` with a README. Put your own clips there (`.ogg`, `.wav` or `.mp3`), named for the moment they play:

| File | Plays when |
| --- | --- |
| `chevrune` | each Chevrune locks |
| `spin` | the inner ring turns (loops while it spins) |
| `activate` | the gate opens |
| `idle` | the open gate hums (loops; replaces the game's own hum) |
| `teleport` | someone steps through |
| `close` | the gate shuts down |

A missing file stays silent (or keeps the game's own sound). Restart the game after adding files.

### Where to find free sounds

These are all **CC0** (public domain), so you can use them freely. None are from the show; they just suit the feel (stone, heavy locks, water). I haven't tied any particular file to a slot, so audition and pick:

- [100 CC0 SFX #2](https://opengameart.org/content/100-cc0-sfx-2) by rubberduck: stones, metal hits, doors, switches, air flowing, and loops including flowing water (good for `idle`).
- [Metal Impact Sounds](https://opengameart.org/content/metal-impact-sounds) by BMacZero: thuds and clinks (good for `chevrune` and `close`).
- [6 Short Water Splashes](https://opengameart.org/content/6-short-water-splashes) by ezwa: splashes (good for `activate` and `teleport`).
- [Various Sound Effects](https://aleksicluka.itch.io/various-sound-effects) by Luka Aleksic: includes `grind_stone`, `lock` and door sounds (good for `spin` and `chevrune`).
- [Sci-Fi Sounds](https://kenney.nl/assets/sci-fi-sounds) by Kenney: 70 sci-fi and engine sounds (look here for a hum or whoosh).

Rename the files you pick to the slot names above (e.g. `activate.ogg`).

## Configuration

`BepInEx/config/mbaya.PortalHeimSG1.cfg`, or Gale's config editor:

| Key | Default | Notes |
| --- | --- | --- |
| Look / Reskin | true | Set to false to keep ordinary portals |
| Look / ChevruneColor | orange | Glow colour of a locked Chevrune |
| Look / SecondsPerChevrune | 0.5 | Seconds between each Chevrune lighting up (nine in all) |
| Look / Interior | Water | `Water` = rippling pool, `Vanilla` = the portal's own swirl |
| Look / Shading | PortalShader | If the ring looks wrong or is missing, try `Standard` (restart the game) |
| Sound / Volume | 1.0 | Also follows the game's sound-effects volume |

## Roadmap

A list of every gate you can dial, and gates shown on every player's map, are planned for a later version. Those will need the mod on the server too.
