#!/bin/bash
# =============================================================================
# Lexichord macOS Code Signing Script
# =============================================================================
#
# Signs and optionally notarizes macOS application bundles using codesign
# and Apple's notarization service.
#
# Usage: ./sign-macos.sh <app-bundle> [--notarize]
#
# Arguments:
#   app-bundle    Path to .app bundle to sign
#   --notarize    Also notarize after signing (requires Apple credentials)
#
# Environment variables (required):
#   APPLE_DEVELOPER_ID        - Developer ID certificate identity
#                               e.g. "Developer ID Application: Company (TEAMID)"
#
# Environment variables (required for notarization):
#   APPLE_TEAM_ID             - 10-character Apple Team ID
#   APPLE_ID                  - Apple ID email for notarization
#   APPLE_APP_SPECIFIC_PASSWORD - App-specific password
#
# Version: v0.1.7b
# =============================================================================

set -e

# Parse arguments
APP_BUNDLE="${1:-}"
NOTARIZE=false
ENTITLEMENTS="./build/macos/entitlements.plist"

if [[ "$2" == "--notarize" ]]; then
    NOTARIZE=true
fi

# Validate arguments
if [[ -z "$APP_BUNDLE" ]]; then
    echo "Usage: $0 <app-bundle> [--notarize]"
    exit 1
fi

if [[ ! -d "$APP_BUNDLE" ]]; then
    echo "Error: App bundle not found: $APP_BUNDLE"
    exit 1
fi

if [[ ! -f "$ENTITLEMENTS" ]]; then
    echo "Error: Entitlements file not found: $ENTITLEMENTS"
    exit 1
fi

if [[ -z "$APPLE_DEVELOPER_ID" ]]; then
    echo "Error: APPLE_DEVELOPER_ID environment variable not set"
    exit 1
fi

echo "═══════════════════════════════════════════════════════════════"
echo "  Lexichord macOS Code Signing"
echo "═══════════════════════════════════════════════════════════════"
echo ""
echo "App Bundle:   $APP_BUNDLE"
echo "Identity:     $APPLE_DEVELOPER_ID"
echo "Entitlements: $ENTITLEMENTS"
echo "Notarize:     $NOTARIZE"
echo ""

# Step 1: Sign all nested Mach-O binaries
echo "[1/4] Signing nested binaries..."

find "$APP_BUNDLE" -type f -perm +111 -exec file {} \; 2>/dev/null | \
    grep "Mach-O" | cut -d: -f1 | while read -r binary; do
    echo "  Signing: $(basename "$binary")"
    codesign --force --options runtime \
        --entitlements "$ENTITLEMENTS" \
        --sign "$APPLE_DEVELOPER_ID" \
        "$binary"
done

# Step 2: Sign dylibs and frameworks
echo ""
echo "[2/4] Signing libraries..."

find "$APP_BUNDLE" -type f \( -name "*.dylib" -o -name "*.framework" \) 2>/dev/null | while read -r lib; do
    echo "  Signing: $(basename "$lib")"
    codesign --force --options runtime \
        --entitlements "$ENTITLEMENTS" \
        --sign "$APPLE_DEVELOPER_ID" \
        "$lib"
done

# Step 3: Sign the main app bundle
echo ""
echo "[3/4] Signing app bundle..."

codesign --force --deep --options runtime \
    --entitlements "$ENTITLEMENTS" \
    --sign "$APPLE_DEVELOPER_ID" \
    "$APP_BUNDLE"

echo "Bundle signed successfully."

# Step 4: Verify signature
echo ""
echo "[4/4] Verifying signature..."

codesign --verify --verbose=2 --strict "$APP_BUNDLE"

echo ""
echo "Signature verification passed!"

# Display signature details
echo ""
echo "Signature Details:"
codesign -dv --verbose=2 "$APP_BUNDLE" 2>&1 | grep -E "Authority|TeamIdentifier|Timestamp" | sed 's/^/  /'

# Notarization (if requested)
if [[ "$NOTARIZE" == "true" ]]; then
    echo ""
    echo "═══════════════════════════════════════════════════════════════"
    echo "  Notarizing Application"
    echo "═══════════════════════════════════════════════════════════════"
    echo ""

    # Validate notarization environment variables
    if [[ -z "$APPLE_ID" || -z "$APPLE_APP_SPECIFIC_PASSWORD" || -z "$APPLE_TEAM_ID" ]]; then
        echo "Error: Notarization requires APPLE_ID, APPLE_APP_SPECIFIC_PASSWORD, and APPLE_TEAM_ID"
        exit 1
    fi

    # Create a zip for notarization submission
    NOTARIZE_ZIP="${APP_BUNDLE%.app}.zip"
    echo "Creating zip for notarization: $NOTARIZE_ZIP"
    ditto -c -k --keepParent "$APP_BUNDLE" "$NOTARIZE_ZIP"

    # Submit for notarization
    echo "Submitting to Apple notary service..."
    xcrun notarytool submit "$NOTARIZE_ZIP" \
        --apple-id "$APPLE_ID" \
        --team-id "$APPLE_TEAM_ID" \
        --password "$APPLE_APP_SPECIFIC_PASSWORD" \
        --wait

    # Cleanup zip
    rm "$NOTARIZE_ZIP"

    # Staple the notarization ticket
    echo ""
    echo "Stapling notarization ticket..."
    xcrun stapler staple "$APP_BUNDLE"

    # Verify stapling
    echo "Verifying staple..."
    xcrun stapler validate "$APP_BUNDLE"

    echo ""
    echo "Notarization complete!"
fi

echo ""
echo "═══════════════════════════════════════════════════════════════"
echo "  Signing Complete!"
echo "═══════════════════════════════════════════════════════════════"
