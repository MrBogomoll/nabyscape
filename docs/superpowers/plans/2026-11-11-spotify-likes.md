# Spotify saved-track button

User wants a button that saves the current Spotify song to Liked Songs. Premium confirmed; user has no experience with Spotify Developer. Provide a clear one-time connection guide and never request a password or Client Secret. Official API uses PKCE and a public Client ID; Spotify currently requires Premium for the owner of a personal Development Mode application. A Client ID and the user's browser authorization are still needed for a real account check.

Design: an outlined/filled neutral heart in the music footer, only for a selected Spotify media session. Unconnected click opens Music settings. Once connected, resolve current track via Spotify's currently-playing endpoint and match title, artist and duration against the selected Windows media session; never guess by search. Show saved status, toggle the exact captured URI, disable during an operation and do not apply stale responses after track/account changes. Library endpoints are /me/library and /me/library/contains, not deprecated /me/tracks.

OAuth: authorization code PKCE with random state, SHA-256 challenge and exact loopback callback http://127.0.0.1:43821/callback. Fixed three-minute timeout and cancel support, bounded request bodies, scopes user-library-read/user-library-modify/user-read-currently-playing. Access token only in memory; refresh token in Windows Credential Manager using the existing credential abstraction. Public Client ID stored separately. HTTP failures never log tokens or response bodies. Refresh once on 401, respect Retry-After, no automatic retry of an ambiguous mutation. Poll only with visible music controls and a connected account.

Files: Features/Spotify/{SpotifyProtocol,SpotifyOAuth,SpotifyService}.cs; UI/Settings/SettingsSpotify.cs; Features/Spotify/SpotifyLikeButton.cs; App lifecycle; MediaControls footer slot; Russian.Spotify.json; mocked SpotifyTests and targeted UI checks.

- [x] Failing protocol/identity tests, then PKCE and exact-match helpers.
- [x] OAuth and library service with mocked HTTP, secure persistence, cancellation/race/failure checks.
- [x] Russian connection settings, heart button, lifecycle and visible-only demand.
- [x] Build and run full checks; visually verify both themes; prepare one-time Developer setup. Real authorization is user-controlled; the UI checks use mocks.
- [x] Install 1.1.1 with backup and matching profile hashes; synchronize source without changing the published 1.0 assets.

## UI follow-up (2026-11-11)

User requested a visible settings exit button, heart-only hover/loading states, the same heart in the middle hover panel without opening or resizing, and Client ID masked by default with an eye toggle. Added a custom transparent button template, consumed routed heart clicks, stopped the second-stage hover timer when interacting with that heart, and added a masked editor that hides again when the window loses activation.

Validation: 631 self-tests passed. Targeted UI checks cover exact save/remove, both theme contrast, middle-panel geometry/click routing, hidden ID, eye/edit synchronization and visible settings exit. Mock Spotify requests never change the real user's library. Real account authentication is user-controlled; no account token or personal Client ID belongs in the repository.

Final UI verification: 18 Spotify UI checks and 124 theme checks passed. Installed executable hash matches the tested publish. All five existing profile files, including Spotify connection data, retained their hashes during installation.
