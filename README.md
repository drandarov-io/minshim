Usage
-----

`minshim.exe <full-path-to-target.exe> [shim-file-name.exe]`

- If you omit the 2nd argument, the shim is given the same
  file-name as the target and is dropped in the current directory.
- The shim is a true *.exe* that
  – forwards **all** command-line arguments,
  – waits for the target to exit,
  – returns the target’s exit-code.

Requirements
------------

• Works on any Windows machine that has the classic **.NET Framework 4.x**
  (ships with Windows 10/11) because it relies on `csc.exe`.

---

Build
---

```pwsh
dotnet publish -c Release -r win-x64 `
  -p:PublishSingleFile=true `
  -p:PublishTrimmed=true `
  -p:SelfContained=true `
  -o .
```
