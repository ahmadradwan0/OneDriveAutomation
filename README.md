🧠 OneDrive Automation Project
Overview

This project provides a fully automated framework for detecting, downloading, and validating new Microsoft OneDrive releases.
It is designed to streamline the release testing process by automatically discovering new installer versions, collecting associated metadata, and verifying file integrity using cryptographic hashes.

Key Features

🔍 Automated Release Discovery
Periodically checks Microsoft’s distribution channels and known CDN endpoints to identify newly published OneDrive versions (including hidden or phased rollout builds).

📦 Version Collection and Archiving
Automatically downloads OneDrive installers (EXE, MSI, or setup variants) and maintains a structured local repository of all discovered builds.

🧾 File Integrity & Hash Verification
Computes and stores SHA256 and SHA1 hashes for each downloaded file, ensuring authenticity and traceability of every build.

🪣 Metadata Logging and History Tracking
Captures version numbers, download URLs, timestamps, and file sizes in a centralized JSON or CSV log, supporting both audit and analytics.

⚙️ Silent Installation & Automated Testing (optional)
Supports automated OneDrive installation in test environments (via PowerShell or C#) to validate setup consistency and deployment compatibility.

🔔 Notification Support
Can be extended to send email or API alerts when new versions are detected (integrations tested with SendGrid, Gmail API, and Resend).

Technology Stack

Language: C# (.NET 8.0) & PowerShell hybrid

Core Components:

HTTP scraping and CDN endpoint resolution

JSON-based version logging

SHA256/MD5 hashing

Local file indexing

Optional Integrations:

GitHub API (release tracking and commit logging)

SMTP / SendGrid for email alerts

Intended Use

This project was developed for internal testing, QA automation, and controlled release validation.
It allows developers, IT engineers, and security analysts to:

Track Microsoft OneDrive version changes,

Verify release authenticity through hash matching,

Automate repetitive installation and verification workflows.

Example Workflow

Run the script or service.

It automatically queries the OneDrive version endpoints.

Detects new releases, downloads installers, and saves metadata.

Hashes are computed and logged in a structured file (JSON or CSV).

(Optional) Notifications are sent if a new version is found.

Future Enhancements

Integration with GitHub Actions for scheduled checks.

Automatic release notes parsing and comparison.

Enhanced web dashboard for version analytics.
