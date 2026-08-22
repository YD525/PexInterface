## PexInterface

A tool for invoking **PexReader** to decompile scripts into **C#** or **Papyrus** source code, providing users with additional references during the translation process.

### Contributors

- [YD525](https://github.com/YD525)

### Special Thanks

- [Cutleast](https://github.com/cutleast) and [SkyHorizon3](https://github.com/SkyHorizon3) — Members of the **Modding Forge** team who provided valuable advice and support throughout the development of this project.

### Building from source

PexInterface requires Visual Studio 2022 and the .NET Framework 4.8.1 Developer Pack.

Restore the pinned native dependency and build the x64 Release configuration:

```powershell
.\scripts\Restore-Dependencies.ps1
msbuild .\PexInterface.sln /restore /m /p:Configuration=Release /p:Platform=x64
.\scripts\Run-Tests.ps1 -Configuration Release -Platform x64
```

Dependency versions are recorded in `dependencies.json`. The restored native DLL and canonical ABI header are
checksum-verified and remain untracked in the `dependencies` directory.
PexInterface 1.0.0.4 requires PEX native ABI version 1, published by PexReader 1.0.1.6 or a coordinated compatible
release. An older native DLL is rejected before a reader handle is created.
The MSTest suite uses an embedded, licensed synthetic PEX fixture and writes generated PEX files only to unique
temporary directories. It covers managed/native version compatibility, parsing, string extraction, decompilation,
Unicode modification and round trips, malformed input, exact ABI signatures and layouts, status translation, and
repeated ownership lifecycles. The lifecycle executable runs another 20,000 native ownership cycles.

### Releases

Push a version tag matching `v*` to build the managed library and create a GitHub Release. Each release contains
`PexInterface.dll` and `PexInterface.dll.sha256`.
