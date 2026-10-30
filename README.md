# Return Of Thunderstorms

**Created by killdg, with assistance from ChatGPT.**

Bring thunderstorms back into normal SPT weather, with heavy rain recordings, lightning, thunder, optional hurricane effects, and night-time electrical flickering.

This is a standalone weather mod. **Raid Roulette and its task pack are not required.** It does not add quests, change inventories, or accelerate the raid clock.

**Current test build:** client **1.2.1**, server **1.2.0-preview.2**  
**Target:** SPT **4.1.6**, EFT **0.16.9.40743**  
**Download:** [Friend-test package and release notes](https://github.com/killdg/Return-Of-Thunderstorms/releases/tag/v1.2.1-test)

## What the mod changes

| Feature | What changes in a raid |
| --- | --- |
| Thunderstorm chance | A normal rain period can become a thunderstorm. Leave rain boost at zero to preserve Tarkov's ordinary rain frequency. |
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
2. Download and extract the test ZIP from [Releases](https://github.com/killdg/Return-Of-Thunderstorms/releases).
3. Open its `INSTALL` folder. Copy **both** `BepInEx` and `SPT_Runtime` into your SPT root, beside `EscapeFromTarkov.exe`. Merge the folders.
4. Keep all four WAV files in the included `Audio` folder.
5. Start SPT.Server, then launch Tarkov normally.
6. Open **F1 → Return Of Thunderstorms**. If you rebound Configuration Manager, use that key instead.

The package contains this mod's client DLL, server DLL, example server configuration and four audio files. It contains no game binaries, profiles, saved personal settings, or RR/task-pack files. Profiles and inventories are not edited. SPT's included Configuration Manager supplies the F1 menu; no separate audio or effects mod is needed.

## Normal weather with a chance of thunderstorms

For a light addition to normal Tarkov weather:

- **Enabled:** on.
- **Thunderstorm chance:** 25–50%, or your preferred value.
- **Rain chance boost:** **0%**.
- **Hurricane effects:** off if you only want rain, lightning and thunder.
- **Night lightning flicker** and **Rain muffles other game sounds:** optional.

The thunderstorm chance is rolled **once per rain period, not once per raid**. A 25% setting means roughly one in four eligible rain periods becomes a storm. It does not make one in four raids rainy. Rain boost controls rainy-weather frequency separately.

## F1 settings

### Weather

| Setting | Default | What it does |
| --- | --- | --- |
| Enabled | On | Master switch. Off stops this mod's effects and restores normal sound levels. The DLL remains loaded. |
| Thunderstorm chance (%) | 50 | Chance that each eligible normal rain period becomes a thunderstorm. 0 disables conversion; 100 converts every eligible rain period. |
| Rain chance boost (%) | 0 | Raises native rainy-preset odds toward 100%. Zero preserves ordinary weather odds. Changes apply to future raids and need the server component. Winter snow stays native. |
| Rain muffles other game sounds | On | Applies your indoor/outdoor gameplay-volume sliders during storms. Off restores native gameplay volume. |

### Hurricane

| Setting | Default | What it does |
| --- | --- | --- |
| Hurricane effects | On | Enables gusts, wind audio, night-only litter and the hurricane layer. Off retains the basic thunderstorm. |
| Flying debris limit | 48 | Maximum pooled pieces; range 12–96, including at most six larger cardboard pieces. Lower this for slower systems. |
| Extra rain on eyes and goggles | On | In hurricane weather, native drop intervals shorten to about 2x normal at full storm strength and up to 2.75x during peak gusts. Only applies outdoors. |

### Lightning lights

| Setting | Default | What it does |
| --- | --- | --- |
| Night lightning flicker | On | Each night-time strike triggers short staggered electrical pulses, recovering in about 1.15 seconds. |
| Play test lightning | Action | Toggle once during an active storm to request another strike. It resets automatically and waits for current thunder playback to finish. Lights flicker only at night. |

### Sound balance

All four sliders range from **0–100%** and save automatically. They are independent gain controls and do not need to add up to 100%.

| Setting | Default |
| --- | --- |
| Outdoor rain volume (%) | 100 |
| Indoor rain volume (%) | 40 |
| Outdoor gameplay volume (%) | 25 |
| Indoor gameplay volume (%) | 60 |

Rain sliders control the recorded heavy-rain layer, not thunder or generated wind. Gameplay sliders apply only when muffling is enabled. Doorway transitions blend between your chosen indoor and outdoor values. Percentages are not calibrated real-world decibel measurements.

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

Before loading an outdoor raid, set both storm chance and rain boost to **100%**. Wait at least **90 seconds outdoors**. The first strike is scheduled after 30 seconds; later strikes use a 33% roll every three minutes. Use a night raid to check debris and lamp flicker. Restore rain boost to zero afterward if you want ordinary weather variety.

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

The script stages this mod's files under `artifacts/INSTALL`. No Tarkov/SPT proprietary binaries are distributed in the repository.

Current automated validation: **358 policy/audio checks**, **26 client compatibility checks**, and builds with zero warnings/errors. These checks complement playtests; they do not prove compatibility with every map, machine or mod combination.

Code is distributed under the [MIT license](LICENSE). Project authorship: **killdg**, with assistance from **ChatGPT**.
