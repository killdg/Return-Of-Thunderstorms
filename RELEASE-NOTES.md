# Return Of Thunderstorms 1.2.4

**Press F1 in-game, expand Return Of Thunderstorms, and choose a weather preset.** Keep Enabled on. If you rebound SPT's Configuration Manager key, use your chosen shortcut instead.

- New one-click presets: Occasional storms, Vanilla rain + thunderstorms, Frequent storms and Custom.
- Rain frequency can now reduce rain: Never, Less rain, Tarkov default, More rain or Always rain. Less rain halves native rainy-preset odds.
- Independent seasonal thunder chances: new installs use 30% in summer and 15% in other rainy seasons. Native winter snow remains unchanged.
- Occasional storms is the new-install default: less rain, hurricane off, muffling and night light flicker on, outdoor rain volume 70%.
- Advanced sound/hurricane controls and Testing actions are behind remembered foldouts.
- Previous sound and effect settings are migrated to Custom. The former single storm chance becomes both seasonal chances. Older rain boosts keep their strength under the appropriate named frequency option.
- If you previously left rain boost at 100% while testing, press F1 and select Occasional storms to reduce rain again.

Install both matching client and server DLLs. Rain settings affect future forecasts, not a weather curve already loaded in a raid. Thunderstorm chances apply once per rain period; weather can persist across several raids.

The ZIP root still contains only README.md, BepInEx/ and SPT_Runtime/. All four local WAV files are included. The source keeps conventional C# formatting and explanatory comments. Build information and checksums identify the exact tagged source used for both DLLs.

Validation: 371 behavior/audio checks, 31 client compatibility checks, clean builds and formatting checks. The new menu and migration paths need live user verification; automated checks do not establish Fika or other-mod compatibility. Existing storm presentation was developed on a clean SPT/Tarkov test install. SPT 4.1.6 / EFT 0.16.9.40743. Created by killdg with assistance from ChatGPT.
