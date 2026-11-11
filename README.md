# Uno Completion Animations

> **Build status in this package:** source implementation and static validation are complete. The ChatGPT execution container does not have the .NET SDK installed, so the included Android build attempt ends with `dotnet: command not found`. See `BUILD-REPORT.md`.


Standalone Uno Platform sample app containing two reusable completion interactions:

1. **DustCompletionControl** — confirmation ring/check → amount dissolves into dust → particles converge into the row checkmark → check pulse → row collapse.
2. **CascadingCompletionList** — confirmation ring/check → completed row lifts → one continuous waypoint animation passes each remaining row → siblings rise into place → row docks into Paid → host commits collection state.

## Targets

- `net10.0-android`
- `net10.0-desktop`

Pinned to **Uno.Sdk 6.7.30**, the latest stable Uno.Sdk version found during implementation. The project uses the Uno.Sdk single-project structure and WinUI XAML APIs.

## Build

Prerequisites:

```bash
uno-check
```

Then:

```bash
dotnet build UnoCompletionAnimations.csproj -f net10.0-desktop -c Debug
dotnet build UnoCompletionAnimations.csproj -f net10.0-android -c Debug
```

Run desktop:

```bash
dotnet run --project UnoCompletionAnimations.csproj -f net10.0-desktop
```

Android output is produced by the normal .NET Android build under `bin/Debug/net10.0-android/` after the Android workload/SDK are installed.

## Architecture

### Shared

- `CompletionMotionTokens.cs` centralizes durations/easing.
- `CompletionAnimationState.cs` exposes explicit animation phases.
- `StoryboardExtensions.cs` provides cancellable async storyboard execution.

### Dust

- `DustCompletionControl.xaml/.cs`
- `DustCompletionOptions.cs`
- `DustParticle.cs`
- `DustParticlePool.cs`

The host receives `AnimationCompleted` after the visual sequence. Business state is intentionally not persisted inside the control.

### Cascade

- `CascadingCompletionList.xaml/.cs`
- `CascadeTransitCoordinator.cs`
- `TransitRowPresenter.xaml/.cs`
- `CascadeCompletionOptions.cs`

The list keeps the source item in its old layout slot during transit, animates a duplicate presenter in an overlay, then invokes the host's `CommitCompletionAsync` callback only after docking.

## Reduced motion

Both controls inspect `Windows.UI.ViewManagement.UISettings.AnimationsEnabled` on supported platforms. An explicit options override is also available.

## Sample UI

Use the top navigation buttons to switch between the Dust and Cascade sample pages. Both samples provide Reset controls for repeated testing.
