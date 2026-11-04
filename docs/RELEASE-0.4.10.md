# Far Far VR v0.4.10

## Privacy and security changes

- Remove developer-local profile roots from the native backend's diagnostic strings. Equal-length edits are restricted to read-only, non-executable data; executable sections and image size are preserved.
- Update the backend and customization-delta integrity pins for the sanitized build.
- Restrict updater download links to this repository's HTTPS GitHub release paths. Optional repository credentials are sent only to this repository's GitHub API endpoints.
- Add automated publication checks for common private-data and credential patterns, and ignore local credentials, private notes and recovery material.
- Review the initial path-injection reports and document their same-user desktop/development context in `docs/SECURITY-REVIEW.md`. CodeQL remains enabled for all four configured languages.

## Install or update

Download **FarFarVR-v0.4.10.zip**, extract the complete archive, and run **Far Far VR.exe**. Existing players can use **Updates > Check for updates > Download update**, close Far Far West, and select **Install & restart**.

Keep the full package together. First setup or the next launch reconstructs the sanitized runtime from the pinned upstream archive and the new bundled delta.

## Validation

The native sanitization build step checked that all executable sections and the image size were unchanged and refused any edit outside read-only non-executable data. The launcher was rebuilt. No local regression tests or game/headset tests were run for this release; automated checks run on GitHub.
