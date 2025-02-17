#!/bin/bash

# execution command: bash MacBuild.sh - ideally on Mac with VS Code

PROJECT_NAME="$1"
OUTPUT_DIR="./bin/Release/net8.0"

# Function to build for a specific architecture
build_for_arch() {
    local arch=$1
    echo "Building for $arch..."
    dotnet publish -r osx-$arch -c Release
}

# Build for both architectures
build_for_arch "x64"
build_for_arch "arm64"