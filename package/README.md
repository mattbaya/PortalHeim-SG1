# PortalHeim SG1

Turns every portal into a **stone ring gate**: an outer ring with nine glowing **Chevrunes** (chevrons set with runes), and an inner ring of **Elder Futhark runes** that spins when a portal connects while the Chevrunes **lock one by one** and glow. It's *inspired by* a certain famous gate, but built from Norse runes and a design of its own.

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

## Configuration

`BepInEx/config/mbaya.PortalHeimSG1.cfg`, or Gale's config editor:

| Key | Default | Notes |
| --- | --- | --- |
| Look / Reskin | true | Set to false to keep ordinary portals |
| Look / ChevruneColor | orange | Glow colour of a locked Chevrune |
| Look / SecondsPerChevrune | 1.0 | Speed of the connect sequence |
| Sound / Volume | 1.0 | Also follows the game's sound-effects volume |

## Roadmap

A list of every gate you can dial, and gates shown on every player's map, are planned for a later version. Those will need the mod on the server too.
