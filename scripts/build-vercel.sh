#!/usr/bin/env bash
set -euo pipefail
export DOTNET_ROOT="${PWD}/.dotnet"
export PATH="${DOTNET_ROOT}:${PATH}"
if ! command -v dotnet >/dev/null 2>&1; then
  echo "Installing .NET 9 SDK for Vercel build..."
  curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/tripplanner-dotnet-install.sh
  bash /tmp/tripplanner-dotnet-install.sh --channel 9.0 --install-dir "$DOTNET_ROOT" --no-path
fi
dotnet publish TripPlanner.Client/TripPlanner.Client.csproj -c Release -o /tmp/tripplanner-publish
mkdir -p vercel-dist
cp -a /tmp/tripplanner-publish/wwwroot/. vercel-dist/
printf '{"ApiBaseUrl":"/"}\n' > vercel-dist/appsettings.json
echo "Blazor WASM built successfully for Vercel."
