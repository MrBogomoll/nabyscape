# Note selection and deletion from collapsed cards

User request: selection marks for deleting groups of notes and a delete icon without opening a note. Keep the approved light/dark card design and the shared draft.

Design: persistent visible selection marks on each card, an outline trash icon at the right of each header, and a wrapping toolbar with Select all, Clear selection and Delete selected (count). Selection and trash are siblings of the expand toggle so they never open the note. Selected cards use the existing neutral selection fill and outline. Confirm once for the chosen group, with No as the default; retain the existing individual confirmation. A failed batch write restores all notes in their original order and retains selection. Stop deleted cards' autosave timers; leave the draft and other notes intact.

- [x] Add atomic batch removal and persistence/rollback tests. Self-tests: 617 passed, 0 failed. New batch checks failed before implementation, then passed.
- [x] Add independent card controls, selection styling and group toolbar.
- [x] Verify selection does not expand, cancel/delete paths, both themes and narrow layouts. Isolated UI: 0 failures, including actual mouse hit targets, native confirmation cancellation/default No, persistence failure and retry. Visually inspected light/dark/narrow layouts. Selection marks have a transparent full hit area; controls occupy separate header columns.
- [x] Build, back up and install with profile verification; synchronize fork. Installed 2.26.1-ru.16, executable hash matches the tested package. Verified responding Settings window and unchanged productivity/settings after startup. Backup: outputs/NabyScape-backup-before-ru16-20261110-210859. Source targets: origin main and feat/settings-monochrome.
