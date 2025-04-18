#!/bin/sh
set -e

# Start Azurite with explicit host bindings
exec node /node_modules/azurite/dist/src/azurite.js \
  --blobHost "0.0.0.0" \
  --blobPort 10000 \
  --queueHost "0.0.0.0" \
  --queuePort 10001 \
  --tableHost "0.0.0.0" \
  --tablePort 10002 