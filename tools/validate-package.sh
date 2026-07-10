#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

SAMPLES=(
  "samples/JwtAuth.Direct"
  "samples/SecretKeyAuth.Direct"
  "samples/JwtAuth.DependencyInjection"
  "samples/SecretKeyAuth.DependencyInjection"
)

echo "==> Pack Xg3.Auth (Release)"
PACK_ARGS=(-c Release -o ./artifacts --property:ContinuousIntegrationBuild=true)
if [ -n "${VERSION:-}" ]; then
  echo "==> Package version: $VERSION (from tag)"
  PACK_ARGS+=(--property:Version="$VERSION")
else
  echo "==> Package version: from project file (no VERSION env set)"
fi
dotnet pack src/Xg3.Auth "${PACK_ARGS[@]}"

NUPKG="$(ls -1 artifacts/Xg3.Auth.*.nupkg | head -1)"
echo "==> Packed: $NUPKG"

echo "==> Nuspec dependency groups"
TMP_NUSPEC="$(mktemp -d)"
unzip -p "$NUPKG" '*.nuspec' > "$TMP_NUSPEC/Xg3.Auth.nuspec"
sed -n '/<dependencies>/,/<\/dependencies>/p' "$TMP_NUSPEC/Xg3.Auth.nuspec"
rm -rf "$TMP_NUSPEC"

for sample in "${SAMPLES[@]}"; do
  echo "==> Canary: $sample (net8.0)"
  dotnet restore "$sample" \
    --source "$ROOT/artifacts" \
    --source "https://api.nuget.org/v3/index.json"
  dotnet build "$sample" -c Release --no-restore
done

echo "==> Package validation complete"
