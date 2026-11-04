# Security review — November 2026

The initial CodeQL scan reported 132 C# and 9 Python path-injection alerts. The reported C# data flows were traced to Windows special folders and temporary folders, paths explicitly selected in the desktop settings window, and a disposable shutdown-test fixture. The Python findings concern a publisher's explicit input/output arguments and the signed-in user's local controller-settings file.

These are same-user desktop and development tools, not a service accepting file paths from a network client or a less-privileged account. Their intended operations require access to the selected files. Runtime components are pinned by hash; downloaded archive and manifest paths are checked for containment. The existing reports were reviewed and dismissed as false positives with reasons recorded on the individual GitHub alerts. The local-source threat model and all four CodeQL languages remain enabled; no query or source directory was excluded to hide findings.

Additional hardening restricts update asset URLs to this repository's HTTPS GitHub release paths and sends an optional repository token only to this repository's GitHub API endpoints. A tracked-file publication audit runs on GitHub to detect common private-key, credential, personal Windows-profile-path and account-identifier patterns without printing matched values. Local credentials, machine state, private notes and recovery artifacts are ignored by Git.

Native release preparation now sanitizes embedded developer profile roots from diagnostic data using `scripts/Sanitize-RuntimePaths.py`. It accepts only the pinned source image, uses equal-length edits in read-only non-executable sections, and refuses changes to executable sections or image size. The resulting backend and delivery delta have new integrity pins.

Automated scans and pattern searches are limited evidence: they do not prove that all vulnerabilities or personal information have been found. Historical commits, published downloads, cached pages and existing third-party copies require separate review and cleanup; changing the current source alone does not remove those copies.
