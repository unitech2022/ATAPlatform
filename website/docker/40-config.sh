#!/bin/sh
# Writes the runtime config read by src/lib/config.ts (docs/12 §F21.10).
set -eu

escape() {
  printf '%s' "$1" | sed -e 's/\\/\\\\/g' -e 's/"/\\"/g'
}

cat > /usr/share/nginx/html/config.js <<CONFIG
window.__ATA_CONFIG__ = {
  apiBaseUrl: "$(escape "${ATA_API_BASE_URL:-}")",
  hubUrl: "$(escape "${ATA_HUB_URL:-}")",
  oneSignalAppId: "$(escape "${ATA_ONESIGNAL_APP_ID:-}")"
}
CONFIG
