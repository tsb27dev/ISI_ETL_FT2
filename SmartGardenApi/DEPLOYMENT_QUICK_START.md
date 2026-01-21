# 🚀 Quick Start - Deployment Azure App Service

## Passo a Passo Rápido test

### 1️⃣ Criar App Service (Azure Portal)

1. Vai a [portal.azure.com](https://portal.azure.com)
2. **Create a resource** → **Web App**
3. Preenche:
   - **Name**: `smartgardenapi` (ou outro nome único)
   - **Runtime stack**: `.NET 9`
   - **Operating System**: `Linux`
   - **App Service Plan**: Cria novo (Free tier OK para testes)
4. **⚠️ IGNORA** qualquer mensagem sobre GitHub Actions - vamos configurar depois
5. **Review + create** → **Create**

### 2️⃣ Configurar Connection String

1. No App Service criado → **Configuration** → **Application settings**
2. **+ New application setting**:
   - **Name**: `ConnectionStrings:Garden`
   - **Value**: `Data Source=/home/data/garden.db`
3. **Save** (no topo)

### 3️⃣ Configurar GitHub Actions (Escolhe UMA opção)

#### Opção A: Via Azure Portal (Mais Fácil)

1. No App Service → **Deployment Center**
2. **Settings** tab:
   - **Source**: `GitHub`
   - **Organization**: A tua org GitHub
   - **Repository**: O teu repo
   - **Branch**: `main`
   - **Runtime stack**: `.NET`
   - **Version**: `9.0`
3. **Save** → O Azure cria o workflow automaticamente

#### Opção B: Manual (Mais Controlo)

1. No App Service → **Get publish profile** (botão topo)
2. Guarda o ficheiro `.PublishSettings`
3. No GitHub → **Settings** → **Secrets** → **Actions**
4. **New repository secret**:
   - **Name**: `AZURE_WEBAPP_PUBLISH_PROFILE`
   - **Value**: Copia TODO o conteúdo do `.PublishSettings`
5. Edita `.github/workflows/azure-deploy.yml`:
   ```yaml
   AZURE_WEBAPP_NAME: smartgardenapi  # ← Altera para o teu nome
   ```
6. Commit e push:
   ```bash
   git add .github/workflows/azure-deploy.yml
   git commit -m "Configure Azure deployment"
   git push
   ```

### 4️⃣ Verificar Deployment

1. Vai a: `https://smartgardenapi.azurewebsites.net/api/swagger`
2. Ou testa: `https://smartgardenapi.azurewebsites.net/api/auth/register?username=test&password=test123`

## ✅ Pronto!

A tua API está no Azure. Cada push para `main` vai fazer deploy automático.

## 🔧 Troubleshooting

**Erro no deployment?**
- Verifica logs: Azure Portal → App Service → **Log stream**
- Ou: GitHub → **Actions** → vê os logs do workflow

**Base de dados não persiste?**
- Verifica se a connection string está configurada: `ConnectionStrings:Garden` = `Data Source=/home/data/garden.db`
- O diretório `/home/data/` é persistente no Azure

**App não inicia?**
- Verifica logs: `az webapp log tail --name smartgardenapi --resource-group SmartGardenRG`
- Verifica se o runtime está correto: `.NET 9` no App Service

## 📚 Mais Detalhes

Lê `AZURE_DEPLOYMENT.md` para instruções completas e outras opções de deployment.
