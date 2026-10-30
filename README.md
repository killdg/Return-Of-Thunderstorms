# Return Of Thunderstorms

**Created by killdg, with assistance from ChatGPT.**

Bring thunderstorms back into normal SPT weather, with heavy rain recordings, lightning, thunder, optional hurricane effects, and night-time electrical flickering.

This is a standalone weather mod. **Press F1 in-game to open Return Of Thunderstorms settings**, then choose a weather preset. Leave Enabled on to use it.

**Current release:** client **1.2.4**, server **1.2.4**
**Target:** SPT **4.1.6**, EFT **0.16.9.40743**
**Download:** [Download 1.2.4 and release notes](https://github.com/killdg/Return-Of-Thunderstorms/releases/tag/v1.2.4)

## What the mod changes

| Feature | What changes in a raid |
| --- | --- |
| Thunderstorm chance | Separate summer/other-season chances convert normal rain periods into thunderstorms. Choose Tarkov default to preserve native rain frequency. |
| Heavy rain | Native rain blends into the supplied heavy-rain recording through a gentle 45-second buildup. |
| Lightning and thunder | Brief flashes pair with one of three thunder recordings after a short delay. The same recording does not play twice in succession. |
| Indoor/outdoor sound | Rain is quieter and filtered under shelter. A three-second doorway fade avoids an abrupt audio switch. Four sliders customize rain and gameplay levels. |
| Optional hurricane effects | Gusts influence native vegetation, wires, rain direction and cloud movement; generated wind audio adds another layer. |
| Night-only flying litter | Weathered newspapers, leaves and occasional cardboard move through a wider ground-anchored field. They remain outdoors when you enter a building. Debris is cosmetic and causes no damage. |
| Extra rain on eyes/goggles | Shorter native drop intervals make water build up faster outdoors during hurricane weather, while retaining native eyewear handling and shelter behavior. |
| Night electrical flicker | Lightning briefly dims and flickers controllable lamps, street/building lights and supported emissive fixtures, then restores their brightness. |

Flying debris and electrical flicker use the **20:00–06:00** night window. Rain, thunder, wind and enhanced eye/goggle drops can still run during daytime storms. Daytime debris is disabled because its appearance was less convincing in testing.

## Installation

1. Close Tarkov, the SPT launcher and SPT.Server.
2. Download and extract the release ZIP from [Releases](https://github.com/killdg/Return-Of-Thunderstorms/releases).
3. Copy **both** `BepInEx` and `SPT_Runtime` directly from the extracted ZIP into your SPT root, beside `EscapeFromTarkov.exe`. Merge the folders. There is no `INSTALL` wrapper folder.
4. Keep all four WAV files in the included `Audio` folder.
5. Start SPT.Server, then launch Tarkov normally.
6. Open **F1 → Return Of Thunderstorms**. If you rebound Configuration Manager, use that key instead.

The ZIP root contains exactly `README.md`, `BepInEx/`, and `SPT_Runtime/`. The package contains this mod's client DLL, server DLL, example server configuration and four audio files. It contains no game binaries, profiles, saved personal settings, or RR/task-pack files. Profiles and inventories are not edited. SPT's included Configuration Manager supplies the F1 menu; no separate audio or effects mod is needed.

## Quick start: press F1

After installation, **press F1** and expand **Return Of Thunderstorms**. SPT's included Configuration Manager opens the settings. If you changed its shortcut, use that key instead. Keep **Enabled** on, select a preset, then start an outdoor raid. Settings save automatically between sessions.

| Weather preset | Rain frequency | Summer thunder chance | Other seasons | Hurricane |
| --- | --- | --- | --- | --- |
| Occasional storms (new-install default) | Less rain | 30% | 15% | Off |
| Vanilla rain + thunderstorms | Tarkov default | 30% | 15% | Off |
| Frequent storms | More rain | 75% | 50% | On |
| Custom | Your choices | Your choice | Your choice | Your choice |

Selecting a preset also resets sound levels to outdoor rain 70%, indoor rain 40%, outdoor gameplay 25% and indoor gameplay 60%, with storm muffling and night light flicker on. Editing an individual weather, effect or sound setting switches the preset to Custom. The master Enabled switch and opening foldouts do not change your preset.

## F1 settings

### Weather

- **Enabled:** stops this mod's effects immediately when off and restores native forecast generation through the server settings sync.
- **Weather preset:** one-click setup using the table above.
- **Rain frequency:** Never removes rain; Less rain halves native rainy-preset odds; Tarkov default preserves native odds; More rain normally moves the odds halfway toward 100%; Always rain selects rainy presets. Native winter snow is preserved.
- **Summer thunderstorm chance:** default 30%, applied when summer rain begins.
- **Other seasons thunderstorm chance:** default 15%, applied to rain outside summer. Winter precipitation stays native.

**Rain frequency changes future forecasts. Thunderstorm chances apply only when it rains.** A rain period is rolled once, not once per raid; changing a chance slider does not reroll a storm already selected. Tarkov weather can persist across several raids, so forecast percentages are not percentages of raids. Settings changed mid-raid do not replace the weather curve already loaded by that raid.

### Storm effects and sound

| Setting | New-install default | Purpose |
| --- | --- | --- |
| Muffle other sounds during storms | On | Reduces gameplay sound only during this mod's thunderstorms. |
| Hurricane effects | Off | Adds gusting wind and night-only flying debris; ordinary rain, thunder and lightning work without it. |
| Lights flicker during night lightning | On | Briefly changes accessible live lights at night and restores their brightness. |
| Outdoor rain volume (%) | 70 | Maximum recording volume outdoors; transitions smoothly at doorways. |

Expand **Advanced settings** for indoor rain volume (40%), outdoor gameplay volume (25%), indoor gameplay volume (60%), extra eye/goggle droplets and the debris limit (48). Rain sliders affect the recording, not thunder or generated wind. Gameplay sliders apply when muffling is on. These are independent gain controls, not calibrated decibel measurements.

Expand **Testing** for connection status and the **Play test lightning** button. It requires an active outdoor thunderstorm. For a deliberate test, set Rain frequency to Always rain and both seasonal chances to 100% before loading the raid. Restore your preferred preset afterward.

### Upgrading from 1.2.3 or earlier

Install **both matching 1.2.4 DLLs**. Previous sound values and effect switches are retained. The old single thunderstorm chance becomes both seasonal chances. A zero rain boost becomes Tarkov default, a partial boost becomes More rain (retaining its exact strength), and a 100% boost becomes Always rain. Migrated settings are labeled Custom rather than silently replacing your preferences.

If you previously tested with rain at 100%, **press F1 and select Occasional storms** to return to less frequent rain and the 30%/15% thunder chances. The old Rain chance boost control is no longer shown. Advanced and Testing are collapsed on a fresh install and remember your chosen open/closed state.

## Where it runs

Outdoor maps are supported, including Customs, Woods, Shoreline, Lighthouse, Reserve, Interchange, Streets and Ground Zero. **Factory, Labs and Labyrinth are excluded.** Native winter snow is preserved.

Native debug-weather overrides take priority. For example, RR's own Stormtide weather override suppresses the standalone storm effects rather than stacking two weather controllers.

## Testing, compatibility and Fika plans

Development and testing have used a **clean SPT + Tarkov test installation**, rather than a heavily modded setup. This does **not** establish compatibility with other mods. We cannot promise it will play nicely with another weather, lighting or audio mod; those combinations still need testing.

Solo tests have confirmed audible rain and thunder, lightning flashes, indoor/outdoor transitions and night light flickering. Debris placement/materials were iterated from playtests. Coverage and appearance can vary by map.

**Fika compatibility has not been established.** We plan to test it in the future and investigate synchronizing storm decisions, lightning and thunder timing between players. The current build has no co-op synchronization: clients can roll different storm decisions, and client weather settings can compete on a shared server. Do not treat this release as Fika-supported.

## Known limitations

- This is a **test release**, not a final compatibility guarantee.
- Long loading pauses were observed on the test installation even with all our feature switches disabled. The cause has **not** been isolated; a full plugin-removal comparison is still pending. We are not claiming this release fixes the pauses or proves their source.
- Baked map lighting cannot be switched off. Flicker affects accessible live lights and supported emission channels. Sun/moon and player/camera hierarchies are excluded.
- Light discovery runs incrementally after storm activation. The first strike may occur before every fixture has been discovered on a large map.
- Debris is cosmetic. Map objects do not become physically movable, and there is no structural destruction or flying-object damage.
- Ground sampling uses terrain where available, with an exposed-surface fallback in mesh-only areas. Some locations may still need placement adjustments.
- The night-only debris revision and RR Stormtide integration require further raid testing.

## Quick test and issue reports

Before loading an outdoor raid, press **F1**, set **Rain frequency** to **Always rain**, and set both seasonal thunderstorm chances to **100%**. Wait at least **90 seconds outdoors**. The first strike is scheduled after 30 seconds; later strikes use a 33% roll every three minutes. Use a night raid to check debris and lamp flicker. Select your preferred weather preset afterward to restore ordinary variety.

When reporting an issue, include the map, raid time, settings, other installed mods, what happened, and a copy of `BepInEx/LogOutput.log` from that session. Preserve the log before launching again. Logs include audio playback state, clip-upload timing, long frame gaps, debris counts and light-cache coverage.

## Uninstall

With the game/server closed, remove only:

- `BepInEx/plugins/ReturnOfThunderstorms`
- `SPT_Runtime/user/mods/ReturnOfThunderstorms`

Its BepInEx configuration may remain if you want to preserve your preferences.

## Source and building

Source code and all four supplied rain/thunder recordings are included in this repository. Procedural debris art and wind audio are generated by the client, so they do not need separate asset downloads. Recordings were supplied by killdg; Thunder3 was converted from MP3 to PCM WAV for runtime playback.

Build with .NET 10 and your own compatible SPT installation:

```powershell
./Build.ps1 -SptRoot 'C:/Your-SPT-Installation' -Dotnet dotnet
```

The script stages this mod's files under a fresh `artifacts/package-<version>-<build-id>` folder and builds a ZIP with the two install folders and README directly at its root. No Tarkov/SPT proprietary binaries are distributed in the repository.

Current automated validation: **371 policy/audio checks**, **31 client compatibility checks**, and builds with zero warnings/errors. These checks complement playtests; they do not prove compatibility with every map, machine or mod combination.

Code is distributed under the [MIT license](LICENSE). Project authorship: **killdg**, with assistance from **ChatGPT**.

## Installer layout

The download uses the SPT mod-page layout: extract the ZIP and copy its two folders directly into SPT. The older wrapper-based test archive remains in its original GitHub release.

## License notice

MIT License

Copyright (c) 2026 killdg

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
## Source transparency and network behavior

The readability work introduced in 1.2.3 formats all C# source and tests with four-space indentation, Allman braces, separate statements, expanded declarations and readable multiline calls. Comments explain native field names, shader parameters, local audio paths, SPT routing and restoration. No obfuscator, binary packer or source minifier is used. The formatting follows the conventional C# layout shown by [SPT-BigBrain](https://github.com/DrakiaXYZ/SPT-BigBrain), without incorporating its code.

The client sends only `Enabled`, `Frequency` and `RainBoost` to `/client/returnofthunderstorms/settings` through SPT's existing request handler, using the SPT server address configured by the player. This allows the F1 weather controls to affect the server's forecast. There are no external service endpoints, analytics, telemetry or audio downloads. The server saves `config.json` beside this mod's DLL; normal BepInEx settings and logs stay in the SPT installation. The mod does not modify operating-system files.

The v1.2.4 tag contains the source used for the release DLLs. Release assets include build information and SHA-256 checksums so reviewers can identify the source commit and verify the submitted files. Build.ps1 builds the server and client against a local SPT installation and produces the three-entry installer ZIP. See CONTRIBUTING.md for the formatting conventions.

New configurations default to **Occasional storms** and **70% outdoor rain volume**. Existing saved values are migrated to Custom; choosing a preset intentionally applies that preset's documented values. Readability improvements do not establish moderator approval; the SPT mod-page review is separate.
