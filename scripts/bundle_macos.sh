#!/bin/bash
set -e

PUBLISH_DIR=$1
VERSION=$2
APP_NAME="ProxyChecker"
BUNDLE_DIR="$PUBLISH_DIR/$APP_NAME.app"

echo "Bundling $APP_NAME for macOS..."
echo "Version: $VERSION"
echo "Source: $PUBLISH_DIR"

# Create directory structure
mkdir -p "$BUNDLE_DIR/Contents/MacOS"
mkdir -p "$BUNDLE_DIR/Contents/Resources"

# Copy binary and dependencies
cp "$PUBLISH_DIR/$APP_NAME" "$BUNDLE_DIR/Contents/MacOS/"
cp "$PUBLISH_DIR/"*.dylib "$BUNDLE_DIR/Contents/MacOS/" 2>/dev/null || true

# Copy and update Info.plist
cp Info.plist "$BUNDLE_DIR/Contents/"

# Update version in Info.plist (using sed for simplicity across environments)
sed -i.bak "s/1.0.0/$VERSION/g" "$BUNDLE_DIR/Contents/Info.plist"
rm "$BUNDLE_DIR/Contents/Info.plist.bak"

# Set permissions
chmod +x "$BUNDLE_DIR/Contents/MacOS/$APP_NAME"

echo "Created $BUNDLE_DIR"
