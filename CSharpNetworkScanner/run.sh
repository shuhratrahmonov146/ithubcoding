#!/usr/bin/env bash
# Run the scanner against a network you own or are allowed to test.
# Example: ./run.sh 192.168.1.0/24
set -e
cd "$(dirname "$0")"
dotnet run -- "$@"
