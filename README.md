# PortalHeim-SG1

A [BepInEx](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/) mod for Valheim that turns portals into stone ring gates with Norse runes and glowing **Chevrunes**, **inspired by** a famous sci-fi gate (not a copy: runes instead of glyphs, original geometry). With the mod on the server too, you can also **dial any gate** from a menu or the map, carry any item through, and take a cart through. Thunderstore package: `PortalHeim_SG1` (Thunderstore names can't contain hyphens).

Install: [Svaha-PortalHeim_SG1 on Thunderstore](https://thunderstore.io/c/valheim/p/Svaha/PortalHeim_SG1/), or the Assets zip from a [GitHub release](https://github.com/mattbaya/PortalHeim-SG1/releases).

Player-facing docs: [package/README.md](package/README.md). Version history: [package/CHANGELOG.md](package/CHANGELOG.md).

## Layout

| Path | Purpose |
| --- | --- |
| `src/` | Mod source (`PortalHeimSG1.csproj`, targets `netstandard2.1`): the plugin, gate building (`GateBuilder`, `GateMeshes`), the start-up animation (`GateRing`), sounds (`GateSounds`), the travel screen and music (`TravelScreen`, `TunnelSurface`, `TravelSound`), carts (`Carts.cs`) |
| `src/Dialing/` | Dialing: the server's connection logic (`GateServer`), the messages between players and server (`GateNet`), what a player knows (`GateClient`), the dial window (`DialMenu`), the interaction and map patches (`DialPatches`) |
| `src/Art/` | Plain C# (no Unity types) that paints the stone, rune, water and tunnel textures and the map icon, so the artwork can be previewed outside the game |
| `tools/make_sounds.py` | Rebuilds the bundled gate sounds (CC0 recordings plus synthesis). The travel loop is cut from a CC0 track, see `package/sounds/CREDITS.txt` |
| `package/` | Thunderstore files: `manifest.json`, `README.md`, `CHANGELOG.md`, `icon.png`, `sounds/` |
| `docs/images/` | Screenshots used in the Thunderstore description (served from GitHub) |
| `build.sh` | Builds and zips a Thunderstore package into `dist/` |
| `libs/Managed/` | Game assemblies to compile against (**git-ignored, never commit**) |

## How it works

**The look.** A Harmony postfix on `TeleportWorld.Awake` hides the wooden frame (`forceRenderingOff` on `m_model`) and adds a procedurally built ring, rune ring, nine Chevrunes, water and a ramp. `GateRing` watches the portal's connection state and animates the start-up sequence (the ring turns, the Chevrunes lock in pairs and change from arrows to runes, the water fills). Gate sounds are bundled WAV files in `package/sounds/`, and can be overridden by files in the config folder.

**Dialing (needs the mod on the server).** Valheim pairs two portals when they have the same name tag, and only the server can change which portals are connected. A dial is a request to the server (`GateNet`, routed RPCs). `GateServer` gives the two gates a shared temporary tag and links them, exactly what the game does for same-named portals. If either gate was paired with another, that partner is set aside under a private tag so the game can't pair the freed gates with each other. When the dial ends (no one near for a while, a time limit, or a hang-up) every gate gets its real name back (kept in its own saved field, so a crash mid-dial is repaired on the next load) and the original pairs are restored exactly. The server also pushes the list of gates to every player whenever it changes. Each player's game decides which gates they can see: those they've been near or built, remembered in `Player.m_customData`.

**Map.** Gate pins on the minimap use a flat icon drawn in code. The dial window's **Pick on map** button opens the map, where a click on a gate dials it (`DialPatches` hooks `Minimap.OnMapLeftClick`).

**Carts.** The game lets go of a cart when you start teleporting. `Carts.cs` notes the cart you were pulling, asks the server to move its record to your side after you arrive, and hooks it back on.

**Items.** `TeleportWorld.m_allowAllItems` is set so ore and metal can go through.

## Building

Same setup as [Bag O' Heim](https://github.com/mattbaya/BagOHeim#building) without Jötunn, plus `UnityEngine.AudioModule`, `UnityEngine.UnityWebRequestModule`, `UnityEngine.UnityWebRequestAudioModule`, `UnityEngine.IMGUIModule`, `UnityEngine.TextRenderingModule`, `UnityEngine.InputLegacyModule`, `assembly_guiutils` and `Splatform` in `libs/Managed/`. Then `./build.sh`.

## Contributing

Bug reports and pull requests are welcome. Please open an issue first for anything larger than a small fix. Releases to Thunderstore are published by the maintainer.
