# Source readability and release checks

Use four spaces for indentation and Allman braces on separate lines. Place each statement and field declaration on its own line. Always use braces for control flow. Use block-scoped namespaces, descriptive names and blank lines between methods. Wrap long calls and explanations; do not minify source. These conventions follow the ordinary C# layout illustrated by SPT-BigBrain, without copying its implementation.

Keep comments that explain native Tarkov integration, reflection, audio ownership and cleanup. Do not hide network activity: this mod only synchronizes enabled/rain-boost settings with the configured SPT server, as described in README.md.

The .editorconfig records formatting rules. Run `dotnet format whitespace . --folder --include src Tests` after editing (exclude generated bin/obj folders if present). Folder mode does not need to load the SPT projects. Run the server/policy and client compatibility checks before release.

Build a release using Build.ps1 from the exact Git commit being tagged. Never modify binaries after building or use obfuscation, packing or name mangling. Publish build information and SHA-256 checksums alongside the ZIP. The installer ZIP root must contain only README.md, BepInEx and SPT_Runtime; do not include profiles, personal settings or game DLLs.
