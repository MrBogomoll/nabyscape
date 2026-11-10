# Widget settings and notes implementation plan

> **For agentic workers:** Execute inline with independent bounded tasks for notes persistence and native window geometry.

**Goal:** Organize settings under Widgets, make saved notes/tasks discoverable, and stabilize the preview and maximized window.

**Architecture:** Reuse current category IDs and search routing inside one collapsible Widgets group plus an overview. Keep the existing shared Note draft and add a bounded saved library in productivity.json. Preserve the layered rounded normal window while allowing native square maximized geometry.

**Tech Stack:** C#, WPF, existing productivity store and isolated diagnostics.

**Spec:** User request 2026-11-10: place preview modes vertically at right; keep preview dimensions static across modes; fix maximized layout/corners; add saved Notes and Tasks pages; collect panel-related features under one understandable subsection. Implementation choice: call the subsection Widgets; saved notes are explicit library entries, the existing draft continues autosaving.

## Global constraints
- Preserve profile, current draft, saved tasks, themes, logo, update protection and feature IDs.
- Existing searches/home links must still reach nested feature pages.
- Normal windows retain true rounded corners; maximized windows fit the working area and use square corners on all sides.

## Review focus
- Narrow width keeps all three preview modes usable with stable preview height.
- Nested pages remain reachable when Show all settings is off.
- Note CRUD survives restart and avoids losing a draft on opening a library entry.
- Task completion synchronizes with the panel.
- Maximize/restore and light/dark geometry remain consistent.

## Tasks
- [x] Shared preview: fixed specimen viewport, mode buttons in a right column.
- [x] Navigation: Widgets overview/group, feature child pages including Notes, Tasks and Timers; centered wider layout for large windows.
- [x] Persistence/editors: saved notes library and tasks pages with Russian captions and meaningful service tests.
- [x] Native geometry: maximize work area, square corners, normal restore.
- [x] Build, unit and isolated UI diagnostics; inspect screenshots including notes, tasks, widgets, maximized and narrow previews.
- [x] Back up EXE/profile, install, verify preservation; commit/push the fork.

User steering: move search to the marked caption area before the window action buttons. Implemented one responsive title-bar host; Ctrl+F and search results retain existing routing. Independent tasks were integrated and reviewed directly; no agents remain active.

Validation: 608 unit checks passed. Final isolated theme and Russian UI runs each reported 0 failures; fixed preview dimensions, native caption input, note persistence, task completion, maximize/work-area bounds and restore verified. Light/dark, narrow, search, library and maximized screenshots inspected. Installed ru.11 package hash matched; profile hashes were unchanged at installation, and existing draft/tasks remained identical after startup.
