# Return Of Thunderstorms 1.2.3

Source readability and a quieter default outdoor rain level.

- Reformatted all C# source and tests with conventional four-space indentation, Allman braces, individual statements and declarations, and expanded calls.
- Added comments explaining native integration strings, local audio files, SPT settings requests and cleanup responsibilities.
- Added .editorconfig and contribution guidelines to keep future source readable.
- Changed the default Outdoor rain volume from 100% to 70%. Existing saved values are preserved.
- Documented the weather-settings request to the user's configured SPT server. No external endpoints, telemetry or data collection were added.
- Rebuilt both DLLs from the tagged source. Build information and SHA-256 checksums accompany the release.

The installer ZIP still contains only README.md, BepInEx/ and SPT_Runtime/ at its root. All four local WAV recordings remain included. No profiles, personal settings or game binaries are bundled. Apart from the new default volume, weather behavior is unchanged.

Targets SPT 4.1.6 / EFT 0.16.9.40743. Other-mod and Fika compatibility remain unverified. Loading pauses are still under investigation. Created by killdg with assistance from ChatGPT.
