# Compact idle time and counter consistency

User instructions 2026-11-10: replace the idle empty-playback message with time, align idle preview with the real dock, show running timer/stopwatch time in the small panel, fix wrapped Russian page captions. Active music behavior should remain intact.

- [x] Shared compact timer/focus/stopwatch policy; forced idle clock without changing the clock-widget preference.
- [x] Preview playback/timed-state transitions, idle companion/date/clock, no stale paused media cover or waveform.
- [x] One-line wider footer captions.
- [x] Build, unit checks, idle/timer/stopwatch preview equivalence and active lyrics regression checks.
- [x] Back up profile/EXE, install, verify preserved data, commit/push.

Validation: 612 unit checks passed. Final settings diagnostics and active-lyrics fallback diagnostics each reported 0 failures. Verified forced idle clock, matching paused preview, stopwatch progress 01:05 to 01:06 in both views, timer priority, return to idle clock and one-line Russian captions. Inspected settled idle and stopwatch renders. Maximize/restore diagnostics now wait for the native state transition rather than sampling after a fixed delay. Installed ru.13 EXE hash matched package; settings/productivity/session hashes were unchanged during installation and note/task data remained identical after startup.
