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
dotnet pack src/Xg3.Auth -c Release -o ./artifacts --property:ContinuousIntegrationBuild=true

NUPKG="$(ls -1 artifacts/Xg3.Auth.*.nupkg | head -1)"
echo "==> Packed: $NUPKG"

echo "==> Nuspec dependency groups"
TMP_NUSPEC="$(mktemp -d)"
unzip -p "$NUPKG" '*.nuspec' > "$TMP_NUSPEC/Xg3.Auth.nuspec"
sed -n '/<dependencies>/,/<\/dependencies>/p' "$TMP_NUSPEC/Xg3.Auth.nuspec"
rm -rf "$TMP_NUSPEC"

for sample in "${SAMPLES[@]}"; do
  echo "==> Canary: $sample (net8.0)"
  dotnet restore "$sample"
  dotnet build "$sample" -c Release --no-restore
done

echo "==> Package validation complete"
