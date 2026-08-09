# Community crosshair catalog

The app fetches [`catalog.json`](catalog.json) directly from the `main` branch of this GitHub repository. GitHub is the hosting layer; no app server or database is required.

## Publishing a crosshair

Users can select **Community → Publish mine** in the app. That opens a prefilled GitHub issue containing the preset JSON. The `Publish community crosshair` GitHub Actions workflow validates the submitted data, sets the GitHub account as its author, adds it to `catalog.json`, and closes the issue automatically.

Before the first submission, enable **Settings → Actions → General → Workflow permissions → Read and write permissions** in the GitHub repository. GitHub Issues and GitHub Actions must also be enabled. No server, database, or app-held access token is used.

## Discovery shelves

The app provides three client-side views:

- **Top all-time** sorts by `totalDownloads`, then uses recorded on the current PC.
- **Popular this week** sorts by `weeklyDownloads`.
- **New releases** sorts by `publishedAt`.

`totalDownloads` and `weeklyDownloads` are catalog metadata that the repository maintainer updates when publishing a release. A static GitHub file cannot safely increment a shared per-preset counter whenever anonymous users click **Use**; the app therefore records each user's own use count locally in `%LOCALAPPDATA%\CrosshairY\community-usage.json`.
