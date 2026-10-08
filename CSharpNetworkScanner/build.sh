#!/usr/bin/env bash
# Build the scanner.
set -e
cd "$(dirname "$0")"
dotnet build
