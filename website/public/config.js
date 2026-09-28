// Runtime configuration. Overwritten in the Docker image by
// docker/40-config.sh from ATA_API_BASE_URL / ATA_HUB_URL / ATA_ONESIGNAL_APP_ID.
// Empty values fall back to the build-time VITE_* variables.
window.__ATA_CONFIG__ = window.__ATA_CONFIG__ || {}
