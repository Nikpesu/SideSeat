---
name: version-control-dockerhub
description: Version, document, and publish a project Docker image to Docker Hub. Use when the user asks to prepare a new version, increment the current Docker version by 0.1, choose a custom version, create a per-version changelog, update Docker image references, build a release image, or upload versioned and latest tags to Docker Hub.
---

# Version Control and Docker Hub

## Workflow

1. Inspect the repository for the current image and version:
   - Check `docker-compose.hub.yml`, `docker-compose.yml`, publish scripts, and Git tags.
   - Prefer an explicit Docker Hub image such as `owner/repository:v0.1`.
   - Do not infer a version from build dates or commit counts.

2. Always ask the user before changing files or publishing:
   - State the detected current version.
   - Offer `current + 0.1` as the recommended choice.
   - Offer manual version entry.
   - Example: `Trenutna verzija je v0.1. Želiš v0.2 (preporučeno) ili želiš unijeti drugu verziju?`
   - Stop and wait for the answer. Never choose or publish automatically.

3. Normalize versions:
   - Store tags with a lowercase `v` prefix.
   - Treat `+0.1` as incrementing the second numeric component: `v0.1 -> v0.2`, `v1.9 -> v1.10`.
   - Accept manual `vX.Y`, `X.Y`, `vX.Y.Z`, or `X.Y.Z`.
   - Reject spaces, Docker tag separators, and nonnumeric version components.

4. Confirm the release values:
   - Docker Hub repository, for example `nikolica/sideseat`.
   - Selected version.
   - Dockerfile path.
   - Build context.
   - State that both `<image>:<version>` and `<image>:latest` will be overwritten/published.

5. Update repository version references when present:
   - Update the default app image in `docker-compose.hub.yml`.
   - Update default `SIDESEAT_VERSION` values and Dockerfile `APP_VERSION` defaults.
   - Update version-specific publish script names or commands only when they are actively used.
   - Do not alter unrelated dependency versions.

6. Create the version changelog before publishing:
   - Create `changelogs/<selected-version>.md` in the repository root.
   - Review `git status`, `git diff --stat`, relevant diffs, and the previous changelog.
   - Describe user-visible changes under `Added`, `Changed`, `Fixed`, `Security`, and `Docker` where applicable.
   - Omit empty sections.
   - Include the release date in `YYYY-MM-DD` format.
   - Never invent changes that are not present in the repository.
   - Use the bundled changelog maker when useful:
     - Windows:
       `powershell -ExecutionPolicy Bypass -File .github/skills/version-control-dockerhub/scripts/new-changelog.ps1 -Version <selected> -Summary "<summary>" -Added "<item>" -Changed "<item>" -Fixed "<item>" -Docker "<item>"`
     - Linux/macOS:
       `bash .github/skills/version-control-dockerhub/scripts/new-changelog.sh --version <selected> --summary "<summary>" --added "<item>" --changed "<item>" --fixed "<item>" --docker "<item>"`
   - Review the generated Markdown and replace all `TODO` placeholders before publishing.

7. Validate before publishing:
   - Ensure Docker Engine is running in Linux mode.
   - Ensure the user is logged in to Docker Hub. If push reports authentication failure, ask the user to run `docker login`.
   - Run the project build/tests when practical, or report why they were skipped.
   - Confirm `changelogs/<selected-version>.md` exists and contains no `TODO`.

8. Publish using the bundled script:
   - Windows:
     `powershell -ExecutionPolicy Bypass -File .github/skills/version-control-dockerhub/scripts/publish-dockerhub.ps1 -Image <owner/repo> -CurrentVersion <current> -NewVersion <selected> -Dockerfile <path> -Context <path>`
   - Linux/macOS:
     `bash .github/skills/version-control-dockerhub/scripts/publish-dockerhub.sh --image <owner/repo> --current-version <current> --new-version <selected> --dockerfile <path> --context <path>`
   - Omit the new version only when the user explicitly selected `+0.1`.

9. Verify the remote image:
   - Inspect both tags with `docker buildx imagetools inspect`.
   - Append the final remote digest to the `Docker` section of the changelog when it is known.
   - Report the selected version, platforms, and remote digest.

## Safety

- Never request or store Docker Hub passwords or access tokens in repository files.
- Never run `docker login` with credentials embedded in a command.
- Never publish until the user has explicitly selected the version.
- Never publish without a completed `changelogs/<version>.md`.
- Stop on failed build, failed push, or mismatched remote digest.
