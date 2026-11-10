# Fix review findings

Scope: task mutation persistence and hit areas; timer search destination; missing-lyrics status; full fork version; obsolete preview/material controls; localized settings gallery. Preserve saved data and disabled upstream updates.

Correction to the initial review: ConfigurePreviewForSetting was already a no-op, and RefreshPreview restores the chosen content/mode. Automatic mode switching was not an active defect. Remove obsolete hover/focus handlers, unused mode assignments and dead preview controls rather than redesigning current preview behavior. Keep SettingsScene for its existing rendering tests and keep active ApplyMaterial behavior.

- [x] Regression test reproduces failed task mutation before the fix.
- [x] Implement atomic add/delete, surface failures in settings/panel, keep full checkbox hit area.
- [x] Correct timer search, missing-lyrics status text and full visible fork version.
- [x] Remove unused pinned-preview initialization and old material selector; remove empty preview handlers.
- [x] Run self-tests, complete localized gallery and targeted real UI regressions. Final executable: 618 passed, 0 failed; all 20 gallery categories plus narrow layouts and essential/full modes passed. Theme regression suite passed including timer search, task-center mouse hit and fork version in About/Updates. Also corrected the unreadable hardcoded timer duration field in light theme, verified with a rendered screenshot.
- [x] Back up, install with data verification and synchronize fork. Installed 2.26.1-ru.17; verified executable hash, responding Settings window and unchanged notes/tasks/settings after startup. Backup: outputs/NabyScape-backup-before-ru17-20261110-230020. Source targets: origin main and feat/settings-monochrome.
