# Deployment no Azure App Service

Este guia explica como colocar a SmartGardenApi no Azure App Service.

## Pré-requisitos

- Conta Azure ativa
- Azure CLI instalado (opcional, mas recomendado)
- Git configurado

## Opção 1: Deployment via Azure Portal (Mais Simples)

### 1. Criar o App Service no Azure Portal

1. Acede ao [Azure Portal](https://portal.azure.com)
2. Clica em "Create a resource" → "Web App"
3. Preenche:
   - **Subscription**: A tua subscrição
   - **Resource Group**: Cria ou seleciona um existente
   - **Name**: `smartgardenapi` (ou outro nome único)
   - **Publish**: Code
   - **Runtime stack**: .NET 9
   - **Operating System**: Linux (recomendado) ou Windows
   - **Region**: Escolhe a região mais próxima
   - **App Service Plan**: Cria um novo ou usa existente (Free tier disponível para testes)

4. **Importante**: Se aparecer uma mensagem sobre GitHub Actions não ser suportado durante a criação, **ignora** e continua. Vamos configurar depois.

5. Clica em "Review + create" e depois "Create"

### 2. Configurar Connection String

1. No Azure Portal, vai ao teu App Service
2. Navega para **Configuration** → **Application settings**
3. Adiciona uma nova connection string:
   - **Name**: `ConnectionStrings:Garden`
   - **Value**: `Data Source=/home/data/garden.db`
   - **Type**: Custom
4. Clica em **Save** (no topo)

**Nota**: O Azure App Service tem um diretório persistente em `/home/data/` onde podes guardar ficheiros como a base de dados SQLite.

### 3. Configurar GitHub Actions (DEPOIS de criar o App Service)

**Nota**: Se não conseguiste configurar durante a criação, segue estes passos:

1. No Azure Portal, vai ao teu App Service
2. Navega para **Deployment Center**
3. No separador **Settings**, seleciona:
   - **Source**: GitHub
   - **Organization**: A tua organização GitHub
   - **Repository**: O teu repositório (ex: `ISI_ETL_FT2/SmartGardenApi`)
   - **Branch**: `main` (ou `master`)
   - **Runtime stack**: .NET
   - **Version**: 9.0
4. Clica em **Save**

O Azure vai criar automaticamente um workflow do GitHub Actions no teu repositório e fazer deploy.

**Alternativa - Configurar manualmente GitHub Actions:**

Se preferires configurar manualmente (mais controlo):

1. No Azure Portal, vai ao teu App Service
2. Clica em **Get publish profile** (botão no topo da página)
3. Guarda o ficheiro `.PublishSettings`
4. No GitHub, vai ao teu repositório → **Settings** → **Secrets and variables** → **Actions**
5. Clica em **New repository secret**:
   - **Name**: `AZURE_WEBAPP_PUBLISH_PROFILE`
   - **Value**: Abre o ficheiro `.PublishSettings` e copia TODO o conteúdo XML
6. Edita `.github/workflows/azure-deploy.yml` e altera `AZURE_WEBAPP_NAME` para o nome do teu App Service
7. Faz commit e push - o GitHub Actions vai fazer deploy automaticamente

## Opção 2: Deployment via GitHub Actions (CI/CD Automático) - Configuração Manual

Esta opção dá-te mais controlo sobre o processo de deployment.

### 1. Criar o App Service primeiro

Primeiro, cria o App Service no Azure Portal (ver Opção 1, passo 1). **Não configures deployment durante a criação.**

### 2. Obter Publish Profile

1. No Azure Portal, vai ao teu App Service
2. Clica em **Get publish profile** (botão no topo da página)
3. Guarda o ficheiro `.PublishSettings` (vai fazer download automaticamente)

### 3. Configurar GitHub Secret

1. No GitHub, vai ao teu repositório
2. **Settings** → **Secrets and variables** → **Actions**
3. Clica em **New repository secret**
4. **Name**: `AZURE_WEBAPP_PUBLISH_PROFILE`
5. **Value**: 
   - Abre o ficheiro `.PublishSettings` que descarregaste
   - Copia **TODO** o conteúdo XML (desde `<publishData>` até `</publishData>`)
   - Cola no campo Value
6. Clica em **Add secret**

### 4. Ajustar o Workflow

Edita `.github/workflows/azure-deploy.yml` e altera:
```yaml
env:
  AZURE_WEBAPP_NAME: smartgardenapi    # ← Altera para o nome do teu App Service
```

### 5. Fazer Push

```bash
git add .
git commit -m "Add Azure deployment workflow"
git push origin main
```

O GitHub Actions vai fazer build e deploy automaticamente. Podes ver o progresso em:
- GitHub → **Actions** tab → vê o workflow a correr

## Opção 3: Deployment Manual via Azure CLI

### 1. Instalar Azure CLI

```bash
# Linux/Mac
curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash

# Windows
# Download do instalador: https://aka.ms/installazurecliwindows
```

### 2. Login no Azure

```bash
az login
```

### 3. Criar Resource Group (se necessário)

```bash
az group create --name SmartGardenRG --location westeurope
```

### 4. Criar App Service Plan

```bash
az appservice plan create \
  --name SmartGardenPlan \
  --resource-group SmartGardenRG \
  --sku FREE \
  --is-linux
```

### 5. Criar Web App

```bash
az webapp create \
  --name smartgardenapi \
  --resource-group SmartGardenRG \
  --plan SmartGardenPlan \
  --runtime "DOTNET|9.0"
```

### 6. Configurar Connection String

```bash
az webapp config connection-string set \
  --resource-group SmartGardenRG \
  --name smartgardenapi \
  --connection-string-type Custom \
  --settings ConnectionStrings:Garden="Data Source=/home/data/garden.db"
```

### 7. Fazer Deploy

```bash
# Build e publish localmente
dotnet publish -c Release -o ./publish

# Deploy via ZIP
cd publish
zip -r ../deploy.zip .
cd ..
az webapp deployment source config-zip \
  --resource-group SmartGardenRG \
  --name smartgardenapi \
  --src deploy.zip
```

## Configurações Importantes

### Variáveis de Ambiente no Azure

No Azure Portal → App Service → Configuration → Application settings, podes configurar:

- `ConnectionStrings:Garden`: Caminho para a base de dados SQLite
- `ASPNETCORE_ENVIRONMENT`: `Production` (já está configurado por defeito)

### Limitações do SQLite no Azure

⚠️ **Atenção**: SQLite funciona no Azure App Service, mas:
- A base de dados fica no sistema de ficheiros efémero por defeito
- Usa `/home/data/` para persistência (diretório persistente)
- Para produção, considera migrar para Azure SQL Database ou PostgreSQL

### HTTPS

O Azure App Service fornece HTTPS automaticamente com certificado SSL gratuito.

### URLs

Após deployment, a tua API estará disponível em:
- `https://smartgardenapi.azurewebsites.net/api`

## Verificar Deployment

1. Vai a `https://smartgardenapi.azurewebsites.net/api/swagger` (se Swagger estiver ativo em produção)
2. Testa um endpoint: `https://smartgardenapi.azurewebsites.net/api/auth/register?username=test&password=test123`

## Troubleshooting

### Ver Logs

```bash
az webapp log tail --name smartgardenapi --resource-group SmartGardenRG
```

Ou no Azure Portal: **App Service** → **Log stream**

### Verificar se a App está a correr

```bash
az webapp show --name smartgardenapi --resource-group SmartGardenRG --query state
```

### Restart da App

```bash
az webapp restart --name smartgardenapi --resource-group SmartGardenRG
```

## Próximos Passos

- [ ] Configurar custom domain
- [ ] Configurar Application Insights para monitoring
- [ ] Considerar migrar para Azure SQL Database para produção
- [ ] Configurar backup automático da base de dados
- [ ] Configurar staging slots para testes
