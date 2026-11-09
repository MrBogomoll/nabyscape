# Approved settings window design

User approved the supplied monochrome light/dark comparison on 2026-11-09.
Implement its rounded floating sidebar, monochrome pill selection, visible caption
with wave mark and six-dot grip, glass-like neutral surfaces, theme preview cards
and compact settings rows. Keep every existing feature accessible.

1. Add persisted window-only theme preferences, normalization and round-trip checks.
2. Use window-scoped dynamic resources for both palettes. Preserve actual island
   preview colors and high contrast. Switch without reopening or discarding edits.
3. Implement the approved shell and theme controls. Controls must actually affect
   the window. Add Russian captions and settings search metadata.
4. Capture both themes at normal and narrow sizes, verify contrast and switching,
   run the existing checks, publish a self-contained build.
5. Back up the installed executable/profile, install verified build, restart,
   commit and synchronize the user's fork.

Execution notes: continue in the existing dirty checkout because all current edits
belong to this approved design. No separate worktree or unrelated cleanup.

Completed: window-scoped live palettes, monochrome shell and vector wave mark,
persisted theme controls, Russian captions, independent preview resources and
native Acrylic tint with a solid fallback. Fixed legacy backup compatibility.
Validation: 598/598 self-tests; 23 theme checks (including native caption hit
testing, real blur, minimum-width layout and independent island text); 29 Russian
screen checks. Reviewed both themes and weather/AI screens visually.
Installed 2.26.1-ru.2 with an executable/profile backup. Installer left settings
unchanged. No GitHub Release requested; synchronize source to the user's fork.
