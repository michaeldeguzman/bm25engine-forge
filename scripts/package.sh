#!/usr/bin/env bash
# Builds src/BM25Engine in Release and packages the shipped assembly (dll + pdb) into
# publish/BM25Engine.zip for OutSystems ODC External Logic upload.
#
# Tests live in a separate project (tests/BM25Engine.Tests) with its own ProjectReference back to
# src/BM25Engine, so building the source project alone produces a dll with zero xunit/TestPlatform
# references in its manifest — confirmed via reflection (Assembly.GetReferencedAssemblies()).
set -euo pipefail

cd "$(dirname "$0")/.."

dotnet build src/BM25Engine/BM25Engine.csproj -c Release

BUILD_DIR="src/BM25Engine/bin/Release/net8.0"
OUT_DIR="publish"
ZIP_PATH="$OUT_DIR/BM25Engine.zip"

mkdir -p "$OUT_DIR"
rm -f "$ZIP_PATH"

zip -j "$ZIP_PATH" "$BUILD_DIR/BM25Engine.dll" "$BUILD_DIR/BM25Engine.pdb"

echo "Packaged: $ZIP_PATH"
unzip -l "$ZIP_PATH"
