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
msbuild .\PexInterface.sln /m /p:Configuration=Release /p:Platform=x64
```

Dependency versions are recorded in `dependencies.json`. Restored DLLs are checksum-verified and remain
untracked in the `dependencies` directory.

### Releases

Push a version tag matching `v*` to build the managed library and create a GitHub Release. Each release contains
`PexInterface.dll` and `PexInterface.dll.sha256`.
