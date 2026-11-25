# Binarius Mobile (Android Port)

An Android-focused porting project for Binarius, the Need for Speed binary editor. This repository is an **unofficial work-in-progress port** built with .NET MAUI and C#.

> Current milestone: Android application shell, initial Swatch Picker port, and GitHub Actions ARM64 APK pipeline. The original BIN/BUN/LZC editing engine is **not yet connected**. The current app can select a file, copy it into app-private storage, and inspect its first 64 bytes; it must not be treated as a full editor yet.

## Target

- Android: `arm64-v8a` (`android-arm64`)
- UI: .NET MAUI
- Framework: .NET 10
- Minimum Android API: 24
- CI: GitHub Actions
- Artifact: installable Debug APK for ARM64

## Repository layout

```text
.
├── .github/workflows/android-ci.yml
├── Modules/                         # upstream modules, to be added as Git submodules
├── src/
│   ├── Binarius.Core/                # cross-platform domain and file services
│   └── Binarius.Android/              # .NET MAUI Android application
├── upstream/desktop/                 # supplied original Windows Forms source snapshot
└── PORTING_ANALYSIS.md
```

## Build locally

Install the .NET 10 SDK, Java JDK 21, Android SDK, and the .NET MAUI Android workload. On Linux:

```bash
dotnet workload install maui-android
dotnet restore src/Binarius.Android/Binarius.Android.csproj
dotnet run --project tests/Binarius.Core.SmokeTests/Binarius.Core.SmokeTests.csproj --configuration Release
dotnet publish src/Binarius.Android/Binarius.Android.csproj \
  -c Debug \
  -f net10.0-android \
  -r android-arm64 \
  -p:AndroidPackageFormats=apk \
  -p:EmbedAssembliesIntoApk=true
```

The APK is written under:

```text
src/Binarius.Android/bin/Debug/net10.0-android/android-arm64/publish/
```

## Build on GitHub

1. Create an empty GitHub repository, for example `Binarius-Android`.
2. Upload/push the contents of this folder to its `main` branch.
3. Open **Actions** in the repository.
4. Run **Android CI**, or push a commit. The workflow builds an ARM64 Debug APK and uploads it as the artifact `Binarius-Mobile-Android-ARM64-Debug`.
5. Open the completed workflow run and download the artifact.

The workflow uses GitHub-hosted Ubuntu, Java 21, .NET 10, Android SDK API 36, and the `maui-android` workload. The first build may take longer because the Android and MAUI toolchains must be installed.

### Push with Git

```bash
git init
git add .
git commit -m "Initial Binarius Android port scaffold"
git branch -M main
git remote add origin https://github.com/<YOUR-USERNAME>/Binarius-Android.git
git push -u origin main
```

Do not put GitHub passwords, personal access tokens, or Android signing passwords in source files or chat. Use GitHub's repository secrets for release signing when that stage is added.

## Upstream modules

The original desktop project references four repositories:

- `https://github.com/nlgxzef/Endscript.git`
- `https://github.com/nlgxzef/ILWrapper.git`
- `https://github.com/nlgxzef/Darius.git`
- `https://github.com/nlgxzef/CoreExtensions.git`

Their intended paths are recorded in `.gitmodules`. The ZIP supplied for this port did not contain the module working trees or Git submodule commit entries. The project references are deliberately **not** wired into the Android app until each module's target frameworks, native dependencies, and platform-specific code have been audited.

The `.gitmodules` file preserves the four upstream URLs and intended paths. The uploaded ZIP contains neither populated module working trees nor Git submodule commit pointers. The current Android scaffold deliberately does not reference these projects, so the MAUI shell and smoke tests can be built independently. Do not run `git submodule update` until the project is in a Git repository with actual submodule gitlinks committed. Before enabling recursive checkout, inspect the restored modules' own `.gitmodules` files and validate any relative paths.

## Licensing and attribution

The supplied Binarius source snapshot includes GPL-3.0. Keep the license and upstream notices when modifying or distributing derived code. Check each submodule's own license before redistributing a combined build. This repository is not an official release of the upstream Binarius project.
