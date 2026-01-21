#!/bin/bash

# Script de deployment rápido para Azure App Service
# Uso: ./deploy-azure.sh <resource-group> <app-name>

set -e

RESOURCE_GROUP=${1:-SmartGardenRG}
APP_NAME=${2:-smartgardenapi}

echo "🚀 Building SmartGardenApi..."
dotnet publish -c Release -o ./publish

echo "📦 Creating deployment package..."
cd publish
zip -r ../deploy.zip . > /dev/null
cd ..

echo "☁️  Deploying to Azure App Service..."
az webapp deployment source config-zip \
  --resource-group "$RESOURCE_GROUP" \
  --name "$APP_NAME" \
  --src deploy.zip

echo "✅ Deployment complete!"
echo "🌐 Your API is available at: https://$APP_NAME.azurewebsites.net/api"

# Cleanup
rm -rf publish deploy.zip

echo "🧹 Cleanup complete!"
