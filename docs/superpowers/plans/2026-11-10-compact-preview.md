# Compact shared preview implementation plan

> **For agentic workers:** Use superpowers:executing-plans to implement this small approved change inline.

**Goal:** Keep the shared settings preview small, collapsible and useful.

**Architecture:** Retain the existing preview renderer and selected mode/content. Replace its two-column presentation with a mode toolbar, a height-adaptive specimen and a collapsed Advanced section.

**Tech Stack:** C#, WPF, existing isolated UI diagnostics.

**Spec:** User approved the proposal in chat on 2026-11-10: keep the specimen and three modes, hide content/replay under Advanced, remove the decorative screen edge, preserve a shared collapsible preview.

## Global constraints

- Preserve user settings, preview state across categories, light/dark themes and existing logo.
- Do not alter the actual desktop panel geometry or behavior.

## Review focus

- Closed mode should leave space for settings, including at narrow widths.
- Hover and Panel should remain visible without overflowing their specimen viewport.
- Advanced must start collapsed and retain content/replay controls.
- Changing category must preserve selected mode/content and collapse state.
- Window resize and animated mode changes must keep the specimen height appropriate.

## Task 1: Compact presentation and delivery

- [x] Update SettingsWindow.cs and SettingsShell.cs: remove the side column and decorative edge; adapt stage height by mode; move auxiliary controls into Advanced.
- [x] Extend existing SettingsThemeDiagnostics.cs to check compact heights and hidden advanced controls, and save light/dark screenshots at normal and narrow widths.
- [x] Build version 2.26.1-ru.10, run isolated theme diagnostics and inspect screenshots.
- [x] Back up the installed EXE and profile; install and verify unchanged settings, then commit/push to our fork.

Low-impact reversible UI arrangement: use existing UI diagnostics rather than add logic tests that mirror the implementation.

Validation: published successfully; isolated theme diagnostics reported 0 failures, including compact heights, Advanced visibility, state persistence, both themes and narrow windows. Screenshots inspected. Read-only reviewer found no actionable issues. Installed executable matches the package and settings SHA256 is unchanged.
