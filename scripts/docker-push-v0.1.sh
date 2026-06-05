#!/usr/bin/env bash
set -euo pipefail

DOCKERHUB_USER="${1:?Usage: ./scripts/docker-push-v0.1.sh <dockerhub-user> [repository] [tag]}"
REPOSITORY="${2:-sideseat}"
TAG="${3:-v0.1}"
IMAGE="${DOCKERHUB_USER}/${REPOSITORY}:${TAG}"

docker build -f src/SideSeat/Dockerfile -t "${IMAGE}" .
docker push "${IMAGE}"

echo "Pushed ${IMAGE}"
