# Preserve the cumulative mod

This project contains interdependent VR features developed across multiple chats. Preserve the complete current feature set in `docs/FEATURE-INVENTORY.md` when making a scoped change.

- Start from the current cumulative source. Do not replace the installation, native engine or gameplay modules with an older release to fix a narrow defect.
- Launcher, documentation, packaging and security changes must retain unrelated gameplay scripts, control bindings, graphics settings, calibration and accepted rendering behavior. Inspect the actual changed files and delivery contents before publishing.
- Compiled binaries contain executable GPU shaders and structured resources outside native executable sections. Never treat arbitrary binary string replacement as harmless metadata cleanup. Preserve container validity, native instructions and GPU program chunks; preserving `.text` alone is insufficient.
- Keep player configuration and flat-game settings intact. Never reset or overwrite them as a speculative repair.
- Keep source, package, installation and active profile consistent. Do not silently ship an older backend, omit a module or asset, or replace a current cumulative patch with a historical candidate.
- Respect the user's instruction not to run tests. Do not launch the game, headset sessions or test suites under the guise of verification. A required build and static file inspection are distinct from gameplay acceptance.
- Describe evidence precisely. File comparisons, successful builds and automated checks do not establish headset visuals, controls, comfort or performance. Do not claim perfect operation without observing it.
- Keep repairs focused. Do not add speculative feature changes or wholesale renderer replacements while fixing a specific regression.
