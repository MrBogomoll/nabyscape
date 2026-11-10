# Expandable note cards with optional titles

User request 2026-11-10: saved notes should be compact rounded blocks like the Widgets overview, expand on click, accept a custom heading and fall back to the first phrase when heading is blank.

Implementation: responsive 1/2/3-column card wrap, full-card header toggle, lazy editor, optional title field in the expanded card. Keep the existing quick draft and Save behavior; users can rename a saved note inside its card. Existing records gain an empty optional Title field and retain IDs/text/timestamps. Automatic headings stop at sentence punctuation or a line break and remain bounded. Latest steering also requires the optional title on the actual island: share NoteTitle across main, hover, detached and settings editors, save it with snapshots and preserve outgoing draft titles during replacement. Draft whitespace remains intact while typing.

- [x] Add optional note titles, automatic first-phrase heading and persistence/rollback checks.
- [x] Replace plain saved-note rows with responsive expandable cards in both themes; preserve editor autosave, delete and quick-draft actions.
- [x] Verify real card click, rename/clear/body changes, restart persistence and narrow layouts. Release self-tests: 615 passed, 0 failed; isolated settings UI: 0 failures. Verified main/hover/detached/settings title synchronization and title snapshots, visually inspected both themes and expanded panel.
- [x] Back up, install, verify original note data and sync fork. Installed 2.26.1-ru.15; verified executable hash, responding Settings window, unchanged settings and preserved draft/notes/tasks. Local backup: outputs/NabyScape-backup-before-ru15-20261110-155236. Source targets: origin main and feat/settings-monochrome.
