#!/usr/bin/env bash
# pack-macos.sh — Packages Lexichord for macOS using Velopack
#
# Usage:
#   ./pack-macos.sh --version 0.1.7 [--channel stable|insider] [--output ./releases]
#
# Requires:
#   - .NET 9 SDK
#   - Velopack CLI (vpk): Install with 'dotnet tool install -g vpk'
#
# Version: v0.1.7a

set -euo pipefail

# Default values
VERSION=""
CHANNEL="stable"
OUTPUT_DIR="./releases"
CONFIGURATION="Release"
RUNTIME="osx-x64"

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --version)
            VERSION="$2"
            shift 2
            ;;
        --channel)
            CHANNEL="$2"
            shift 2
            ;;
        --output)
            OUTPUT_DIR="$2"
            shift 2
            ;;
        --configuration)
            CONFIGURATION="$2"
            shift 2
            ;;
        --runtime)
            RUNTIME="$2"
            shift 2
            ;;
        --help)
            echo "Usage: $0 --version <version> [--channel stable|insider] [--output <dir>]"
            exit 0
            ;;
        *)
            echo "Unknown option: $1"
            exit 1
            ;;
    esac
done

# Validate required arguments
if [[ -z "$VERSION" ]]; then
    echo "Error: --version is required"
    echo "Usage: $0 --version <version> [--channel stable|insider] [--output <dir>]"
    exit 1
fi

if [[ "$CHANNEL" != "stable" && "$CHANNEL" != "insider" ]]; then
    echo "Error: --channel must be 'stable' or 'insider'"
    exit 1
fi

# Resolve paths
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
HOST_PROJECT="$REPO_ROOT/src/Lexichord.Host/Lexichord.Host.csproj"
PUBLISH_DIR="$REPO_ROOT/publish/$RUNTIME"
OUTPUT_PATH="$REPO_ROOT/$OUTPUT_DIR"

echo "=== Lexichord macOS Packaging ==="
echo "Version: $VERSION"
echo "Channel: $CHANNEL"
echo "Configuration: $CONFIGURATION"
echo "Runtime: $RUNTIME"
echo ""

# Step 1: Clean previous publish
echo "[1/4] Cleaning previous build..."
rm -rf "$PUBLISH_DIR"

# Step 2: Publish as self-contained
echo "[2/4] Publishing Lexichord..."
dotnet publish "$HOST_PROJECT" \
    --configuration "$CONFIGURATION" \
    --runtime "$RUNTIME" \
    --self-contained true \
    --output "$PUBLISH_DIR" \
    -p:Version="$VERSION" \
    -p:PublishSingleFile=false \
    -p:DebugType=None \
    -p:DebugSymbols=false

# Step 3: Verify vpk is installed
echo "[3/4] Checking Velopack CLI..."
if ! command -v vpk &> /dev/null; then
    echo "Error: Velopack CLI (vpk) is not installed."
    echo "Install with: dotnet tool install -g vpk"
    exit 1
fi
echo "Using Velopack: $(vpk --version)"

# Step 4: Package with Velopack
echo "[4/4] Packaging with Velopack..."
PACKAGE_ID="Lexichord"
if [[ "$CHANNEL" == "insider" ]]; then
    PACKAGE_ID="Lexichord-insider"
fi

# Create output directory
mkdir -p "$OUTPUT_PATH"

# Run vpk pack
vpk pack \
    --packId "$PACKAGE_ID" \
    --packVersion "$VERSION" \
    --packDir "$PUBLISH_DIR" \
    --mainExe "Lexichord.Host" \
    --outputDir "$OUTPUT_PATH" \
    --packTitle "Lexichord"

echo ""
echo "=== Packaging Complete ==="
echo "Output: $OUTPUT_PATH"
echo "Files:"
ls -la "$OUTPUT_PATH"/*.nupkg 2>/dev/null || true
ls -la "$OUTPUT_PATH"/*.dmg 2>/dev/null || true
