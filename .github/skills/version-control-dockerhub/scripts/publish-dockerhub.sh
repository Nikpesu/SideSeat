#!/usr/bin/env bash
set -euo pipefail

image=""
current_version=""
new_version=""
dockerfile="Dockerfile"
context="."
changelog_directory="changelogs"
dry_run="false"

usage() {
  echo "Usage: $0 --image owner/repo --current-version vX.Y [--new-version vX.Y] [--dockerfile path] [--context path] [--changelog-directory path] [--dry-run]"
}

normalize_version() {
  local version="${1,,}"
  version="${version//[[:space:]]/}"
  [[ "$version" == v* ]] || version="v$version"
  [[ "$version" =~ ^v[0-9]+\.[0-9]+(\.[0-9]+)?$ ]] || {
    echo "Version '$1' must use vX.Y or vX.Y.Z format." >&2
    exit 1
  }
  printf '%s' "$version"
}

next_version() {
  local version
  version="$(normalize_version "$1")"
  local raw="${version#v}"
  IFS='.' read -r -a parts <<< "$raw"
  parts[1]="$((10#${parts[1]} + 1))"
  local joined
  joined="$(IFS='.'; echo "${parts[*]}")"
  printf 'v%s' "$joined"
}

run_docker() {
  echo "docker $*"
  if [[ "$dry_run" != "true" ]]; then
    docker "$@"
  fi
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --image) image="$2"; shift 2 ;;
    --current-version) current_version="$2"; shift 2 ;;
    --new-version) new_version="$2"; shift 2 ;;
    --dockerfile) dockerfile="$2"; shift 2 ;;
    --context) context="$2"; shift 2 ;;
    --changelog-directory) changelog_directory="$2"; shift 2 ;;
    --dry-run) dry_run="true"; shift ;;
    -h|--help) usage; exit 0 ;;
    *) echo "Unknown argument: $1" >&2; usage; exit 1 ;;
  esac
done

[[ -n "$image" && -n "$current_version" ]] || {
  usage
  exit 1
}

[[ "$image" =~ ^[a-z0-9]+([._-][a-z0-9]+)*/[a-z0-9]+([._-][a-z0-9]+)*$ ]] || {
  echo "Image '$image' must use Docker Hub owner/repository format." >&2
  exit 1
}

current_version="$(normalize_version "$current_version")"
if [[ -z "$new_version" ]]; then
  selected_version="$(next_version "$current_version")"
else
  selected_version="$(normalize_version "$new_version")"
fi

[[ -f "$dockerfile" ]] || { echo "Dockerfile not found: $dockerfile" >&2; exit 1; }
[[ -e "$context" ]] || { echo "Build context not found: $context" >&2; exit 1; }
changelog_path="${changelog_directory}/${selected_version}.md"
[[ -f "$changelog_path" ]] || { echo "Missing changelog: $changelog_path" >&2; exit 1; }
! grep -Eq '\bTODO\b' "$changelog_path" || {
  echo "Changelog contains TODO placeholders: $changelog_path" >&2
  exit 1
}

run_docker info --format '{{.OSType}}/{{.Architecture}}'
run_docker build --pull -f "$dockerfile" \
  --build-arg "APP_VERSION=${selected_version}" \
  --label "org.opencontainers.image.version=${selected_version}" \
  --label "com.sideseat.container.version=${selected_version}" \
  -t "${image}:${selected_version}" \
  -t "${image}:latest" \
  "$context"
run_docker push "${image}:${selected_version}"
run_docker push "${image}:latest"
run_docker buildx imagetools inspect "${image}:${selected_version}"
run_docker buildx imagetools inspect "${image}:latest"

echo "Published ${image}:${selected_version} and ${image}:latest"
