#!/bin/zsh
# Local packaging only; release publication is handled separately by CI.
set -euo pipefail
cd "${0:A:h}"
python3 ../scripts/package.py claude --platform macos "$@"
