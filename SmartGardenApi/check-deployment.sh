#!/bin/bash

# Script para verificar se tudo está configurado corretamente para deployment

echo "🔍 Verificando configuração de deployment..."
echo ""

# Cores
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

ERRORS=0

# Verificar se o projeto compila
echo "1️⃣ Verificando se o projeto compila..."
if dotnet build -c Release > /dev/null 2>&1; then
    echo -e "${GREEN}✅ Projeto compila corretamente${NC}"
else
    echo -e "${RED}❌ Erro ao compilar o projeto${NC}"
    echo "   Executa: dotnet build -c Release"
    ERRORS=$((ERRORS + 1))
fi

# Verificar se o publish funciona
echo ""
echo "2️⃣ Verificando se o publish funciona..."
if dotnet publish -c Release -o ./test-publish > /dev/null 2>&1; then
    echo -e "${GREEN}✅ Publish funciona corretamente${NC}"
    rm -rf ./test-publish
else
    echo -e "${RED}❌ Erro ao fazer publish${NC}"
    echo "   Executa: dotnet publish -c Release"
    ERRORS=$((ERRORS + 1))
fi

# Verificar se o workflow existe
echo ""
echo "3️⃣ Verificando workflow do GitHub Actions..."
if [ -f ".github/workflows/azure-deploy.yml" ]; then
    echo -e "${GREEN}✅ Workflow encontrado${NC}"
    
    # Verificar se o nome do App Service está configurado
    if grep -q "AZURE_WEBAPP_NAME:" .github/workflows/azure-deploy.yml; then
        APP_NAME=$(grep "AZURE_WEBAPP_NAME:" .github/workflows/azure-deploy.yml | cut -d: -f2 | tr -d ' ')
        echo -e "${YELLOW}   ⚠️  Verifica se '$APP_NAME' corresponde ao nome do teu App Service no Azure${NC}"
    fi
else
    echo -e "${RED}❌ Workflow não encontrado${NC}"
    ERRORS=$((ERRORS + 1))
fi

# Verificar se há secrets configurados (não podemos verificar o valor, mas podemos avisar)
echo ""
echo "4️⃣ Verificando GitHub Secrets..."
echo -e "${YELLOW}   ⚠️  Verifica manualmente no GitHub:${NC}"
echo "      Settings → Secrets and variables → Actions"
echo "      Deve existir: AZURE_WEBAPP_PUBLISH_PROFILE"

# Verificar appsettings
echo ""
echo "5️⃣ Verificando configurações..."
if [ -f "appsettings.Production.json" ]; then
    echo -e "${GREEN}✅ appsettings.Production.json encontrado${NC}"
else
    echo -e "${YELLOW}⚠️  appsettings.Production.json não encontrado (opcional)${NC}"
fi

# Resumo
echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
if [ $ERRORS -eq 0 ]; then
    echo -e "${GREEN}✅ Verificação completa!${NC}"
    echo ""
    echo "Próximos passos:"
    echo "1. Verifica se o secret AZURE_WEBAPP_PUBLISH_PROFILE está configurado no GitHub"
    echo "2. Verifica se o nome do App Service no workflow corresponde ao Azure"
    echo "3. Faz push para o branch main/master"
    echo "4. Verifica o deployment no GitHub Actions"
else
    echo -e "${RED}❌ Encontrados $ERRORS erro(s)${NC}"
    echo ""
    echo "Corrige os erros acima antes de fazer deployment"
fi
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
