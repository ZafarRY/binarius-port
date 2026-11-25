# Porting status

| Workstream | Status | Evidence / next gate |
|---|---|---|
| Inspect uploaded desktop source | Complete | `PORTING_ANALYSIS.md` |
| MAUI Android shell | Scaffolded | Needs GitHub Actions build and device launch test |
| File selection and header inspection | Implemented in scaffold | Copies the selected stream to app-private workspace; metadata/hex preview only, not a format parser |
| Swatch Picker RGB-to-paint-swatch calculation | Source ported; validation pending | Ported from `Binary/Tools/Swatcher.cs`; reference vectors in `PORTING_SWATCH_VALIDATION.md`; verify on desktop and Android |
| Core smoke tests | Configured | File-kind, header inspection, and RGB-to-swatch checks; needs first CI run |
| GitHub Actions ARM64 APK build | Configured | Needs first workflow run |
| CoreExtensions port | Blocked on source audit | Module source not in uploaded ZIP |
| Darius/Nikki port | Blocked on source audit | Module source not in uploaded ZIP |
| Endscript port | Blocked on source audit | Module source not in uploaded ZIP |
| ILWrapper / texture pipeline | Blocked on native dependency audit | Module source and native libraries not in uploaded ZIP |
| BIN/BUN/LZC parser/editor | Not started | Requires engine modules and format tests |
| Feature UI migration | Not started | Implement one editor at a time after core API is stable |
| Release signing | Not configured | Requires user-owned Android keystore and GitHub Secrets |
| Physical ARM64 test | Not run | Requires first APK and Android device |

## Local validation performed for this bundle

- XML parsing of project files, XAML, Android manifest, and SVG resources: passed.
- ZIP archive integrity check: passed.
- .NET compilation: not run; the current working environment does not have the .NET SDK installed.
- GitHub Actions build: not run; this bundle has not yet been pushed to a GitHub repository.
