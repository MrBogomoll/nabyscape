# Preserve idle appearance

User request 2026-11-10: idle should not turn into a plain black panel. Current profile uses black base, no glass, and enabled closed ambient lighting; the forced idle suppression removes its visible tint.

Root causes: UpdateAmbientMotion excludes IdleContent and paints every collapsed surface black; settings preview copied both overrides.

- [x] Preserve configured idle fill/material and enabled static closed ambience. Keep tucked, explicit Media None and ambient Off behavior; active music remains unchanged.
- [x] Use the same background/light rules in the preview and update help text.
- [x] Check paused idle styling, no idle audio/animation demand, preview equivalence, Off/None controls and active media regression checks.
- [x] Back up, install with preserved data and sync fork.

Validation: final quiet/idle, reactive ambient and settings/theme diagnostic runs each reported 0 failures. Checked configured idle fill/glass/static light, pause repaint, matching preview, no idle animation/audio demand, ambient Off and Media None controls, active playback and shared sampling budget. Updated old diagnostics to use stable IDs/localized captions, current shared preview, explicit background restore state and monitor-relative width checks. Inspected idle and preview captures. Installed ru.14 matched the package hash; settings, productivity and session hashes were unchanged during installation.
