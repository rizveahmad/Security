#!/usr/bin/env bash
# scripts/migrate.sh
# Convenience wrapper to apply EF Core migrations against the default connection.
# Usage: ./scripts/migrate.sh [--connection "Server=..."]

set -euo pipefail

CONNECTION="${1:-}"

cd "$(dirname "$0")/.."

if [ -n "$CONNECTION" ]; then
  dotnet ef database update \
    --project src/Security.Infrastructure \
    --startup-project src/Security.Web \
    --connection "$CONNECTION"
else
  dotnet ef database update \
    --project src/Security.Infrastructure \
    --startup-project src/Security.Web
fi

echo "Migration complete."
