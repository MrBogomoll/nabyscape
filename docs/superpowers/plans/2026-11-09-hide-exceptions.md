# Application hiding exceptions

Goal: let the user exempt chosen executables from automatic hiding over both maximized and fullscreen windows.

Use the existing automatic-tuck policy and settings page. Match exact executable names, case-insensitively; do not match window titles or substrings. Default list empty; user manages entries. Manual hiding and Always tucked retain priority. Preserve monitor and foreground tracking.

Tasks: add normalized persisted HideAppExceptions with factory/reset support; write tests covering exact matching, normalization, persistence, and browser versus exempt apps; add a dedicated visible Exceptions section with running-app picker, EXE browse, manual entry and removal; apply exclusions to the sampled external foreground window; register section/search/preview and Russian strings; build, run self-tests and isolated settings diagnostics, review UI, install with backup, push own fork.
