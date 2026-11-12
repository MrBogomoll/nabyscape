# Exact countdown durations (2026-11-12, Codex)

User requires hours/minutes/seconds in timer, focus and break: independent cyclic wheel and arrow buttons (hours 0..99; minutes/seconds 0..59), direct editable numbers, same controls in settings and panel. Preserve pause/resume/reset and running sessions; ready zero is allowed but cannot start. Wheel over a picker belongs to the picker rather than page navigation.

1. Verify wrapping and 99-hour formatting regressions.
2. Add shared DurationPicker and exact-second preferences with legacy minute fallback.
3. Use the picker inside the ready countdown ring, plus compact break picker; lock during active sessions.
4. Update focus break deadline, hover start, detached countdown and live previews to exact seconds.
5. Run numeric/state and isolated UI checks, including wheel routing, keyboard/paste, zero/max, pause/resume, settings and narrow layouts.
6. Build, back up/install preserving profile and synchronize the fork. Published 1.0 assets stay unchanged.

Completed: shared central HH:MM:SS editors, compact break editor, legacy-minute migration, exact-second persistence/reset/preview/deadline handling and detached reuse. A 24-hour formatting wrap was fixed; quick-start zero and extension maximum use the same limits.

Validation: 637 self-tests and 45 isolated countdown UI checks passed. UI checks cover timer/focus/break wrap, button/wheel/keyboard/manual entry, paste validation, five detents, zero/one-second/max duration, pause/resume, exact break transition, external synchronization, draft selection, narrow/classic panels and both settings themes. Screenshots inspected. Installed 1.1.6 matches the tested executable; backup created, five existing profile file hashes unchanged before startup. Source synchronized to fork main and feature branch; no published release assets replaced.
