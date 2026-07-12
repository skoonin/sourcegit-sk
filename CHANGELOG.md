# Changelog

Fork releases of SourceGit-SK. Upstream changes arrive through upstream release merges and are documented in the [upstream release notes](https://github.com/sourcegit-scm/sourcegit/releases).

## [Unreleased]

### Changed

- Local-changes diff view is now selection-driven: selecting a single file shows just that file, selecting several stacks those files, and the continuous stacked view of all changes appears when nothing is selected. The continuous-diff toggle has been removed.

## [2026.15-sk] - 2026-07-11

First fork release, based on upstream [v2026.14](https://github.com/sourcegit-scm/sourcegit/releases/tag/v2026.14).

### Added

- Continuous multi-file diff view for local changes, with a shared toolbar, file navigation, and per-file context controls ([skoonin/sourcegit#1](https://github.com/skoonin/sourcegit/pull/1))
- Recent-commits block in the repository sidebar, including the full commit graph ([skoonin/sourcegit#4](https://github.com/skoonin/sourcegit/pull/4), [skoonin/sourcegit#5](https://github.com/skoonin/sourcegit/pull/5))
- Switch tabs with Cmd+Shift+[ and Cmd+Shift+] on macOS (Ctrl+Shift on Windows/Linux); existing bindings kept ([#1](https://github.com/skoonin/sourcegit-sk/pull/1))

### Changed

- Version numbers carry an `-sk` suffix (shown in the About dialog) to distinguish fork releases from upstream builds.
- The update check and release links point at this fork's releases instead of upstream's, so new `-sk` releases are detected and offered ([#2](https://github.com/skoonin/sourcegit-sk/pull/2))

[2026.15-sk]: https://github.com/skoonin/sourcegit-sk/releases/tag/v2026.15-sk
