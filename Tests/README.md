# Renderer tests

Run from the XNAUI repository root:

```sh
dotnet test Tests/Rampastring.XNAUI.Tests.csproj -c UniversalGLRelease
```

These tests exercise the real renderer and font-manager trimming methods without creating a window or graphics device. A deterministic measuring font covers narrow limits, variable widths, shaping-sensitive joins, Unicode boundaries and preservation of both ends. The existing end-trimming method has a compatibility check. Tests run serially because FontManager stores global font state.

The production project excludes the test sources. The test project targets .NET 8 and the UniversalGL configuration; Windows and .NET Framework builds are checked separately.
