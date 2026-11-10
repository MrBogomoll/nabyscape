# Settings regression fixes

User report 2026-11-10: settings can move outside work area and snap back; decorative dots are off-center; Save note from the hover view does not populate the library; idle dock is blank.

Root causes: containment is deferred until WM_EXITSIZEMOVE; grip is centered in the remaining caption column; hover SavePeekNote only flushes the draft and All notes opens the old editor; clock-disabled idle logic hides both clock and summary.

- [x] Constrain WM_MOVING rectangle before paint, using pointer monitor for cross-monitor movement.
- [x] Enforce native WM_GETMINMAXINFO minimum-track sizes so native resizing cannot cut below WPF layout minimum.
- [x] Remove decorative caption grip.
- [x] Route hover Save into the library with visible saved feedback and deduplication; All notes opens Settings Notes.
- [x] Keep an idle status when clock is disabled without re-enabling the clock preference.
- [x] Validate unit checks, actual native message hook, exact hover buttons, persistent library and idle rendering.
- [x] Back up, install and preserve data; commit/push.

Validation: 609 logic checks passed. Final isolated settings diagnostics reported 0 failures for native minimum tracking size, live drag rectangles, exact hover Save/All notes actions, localized feedback, deduplication and idle visibility. Closed-lyrics fallback diagnostics reported 0 failures; their timer assertion now uses the localized Timer caption. Installed package hash matched, profile hashes were unchanged at installation, and draft/tasks/saved-note data remained identical after startup.
