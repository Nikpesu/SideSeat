#!/usr/bin/env bash
set -euo pipefail

version=""
summary="TODO: Dodaj kratki sazetak izdanja."
output_directory="changelogs"
force="false"
declare -a added=()
declare -a changed=()
declare -a fixed=()
declare -a security=()
declare -a docker_items=()

usage() {
  echo "Usage: $0 --version vX.Y [--summary text] [--added item] [--changed item] [--fixed item] [--security item] [--docker item] [--output-directory path] [--force]"
}

normalize_version() {
  local value="${1,,}"
  value="${value//[[:space:]]/}"
  [[ "$value" == v* ]] || value="v$value"
  [[ "$value" =~ ^v[0-9]+\.[0-9]+(\.[0-9]+)?$ ]] || {
    echo "Version '$1' must use vX.Y or vX.Y.Z format." >&2
    exit 1
  }
  printf '%s' "$value"
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --version) version="$2"; shift 2 ;;
    --summary) summary="$2"; shift 2 ;;
    --added) added+=("$2"); shift 2 ;;
    --changed) changed+=("$2"); shift 2 ;;
    --fixed) fixed+=("$2"); shift 2 ;;
    --security) security+=("$2"); shift 2 ;;
    --docker) docker_items+=("$2"); shift 2 ;;
    --output-directory) output_directory="$2"; shift 2 ;;
    --force) force="true"; shift ;;
    -h|--help) usage; exit 0 ;;
    *) echo "Unknown argument: $1" >&2; usage; exit 1 ;;
  esac
done

[[ -n "$version" ]] || { usage; exit 1; }
version="$(normalize_version "$version")"

repository_root="$(pwd -P)"
mkdir -p "$output_directory"
output_root="$(cd "$output_directory" && pwd -P)"
[[ "$output_root" == "$repository_root"* ]] || {
  echo "Changelog output must stay inside the repository." >&2
  exit 1
}

output_path="${output_root}/${version}.md"
if [[ -e "$output_path" && "$force" != "true" ]]; then
  echo "Changelog already exists: $output_path. Use --force to overwrite it." >&2
  exit 1
fi

{
  echo "# SideSeat ${version}"
  echo
  echo "Datum izdanja: $(date +%F)"
  echo
  echo "$summary"

  print_section() {
    local title="$1"
    shift
    [[ $# -gt 0 ]] || return
    echo
    echo "## ${title}"
    echo
    local item
    for item in "$@"; do
      [[ -n "$item" ]] && echo "- $item"
    done
  }

  print_section "Added" "${added[@]}"
  print_section "Changed" "${changed[@]}"
  print_section "Fixed" "${fixed[@]}"
  print_section "Security" "${security[@]}"
  print_section "Docker" "${docker_items[@]}"
} > "$output_path"

echo "Created changelog: $output_path"
