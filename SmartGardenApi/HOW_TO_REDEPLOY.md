# 🔄 Como Fazer Redeploy

## Opção 1: Push Automático (Recomendado) 🚀

Se tens GitHub Actions configurado, o redeploy é **automático** quando fazes push:

```bash
# Faz as tuas alterações nos ficheiros
git add .
git commit -m "Descrição das alterações"
git push origin main
```

O GitHub Actions vai:
1. Detectar o push
2. Fazer build automaticamente
3. Fazer deploy para o Azure

**Verifica o progresso**:
- GitHub → **Actions** tab → vê o workflow a correr
- Azure Portal → **Deployment Center** → vê o histórico de deployments

## Opção 2: Trigger Manual do Workflow

Se quiseres forçar um redeploy sem fazer push:

1. GitHub → **Actions** tab
2. Seleciona o workflow "Deploy to Azure App Service"
3. Clica em **Run workflow** (botão no topo direito)
4. Seleciona o branch (ex: `main`)
5. Clica em **Run workflow**

Isto vai executar o workflow mesmo sem mudanças no código.

## Opção 3: Redeploy via Azure Portal

1. Azure Portal → App Service
2. **Deployment Center**
3. Clica em **Sync** ou **Redeploy**
4. Ou vai a **Deployment Center** → **Logs** → **Redeploy**

## Opção 4: Redeploy Manual via Azure CLI

```bash
# Se já tens o código publicado localmente
./deploy-azure.sh SmartGardenRG smartgarden

# Ou manualmente:
dotnet publish -c Release -o ./publish
cd publish
zip -r ../deploy.zip .
cd ..
az webapp deployment source config-zip \
  --resource-group SmartGardenRG \
  --name smartgarden \
  --src deploy.zip
rm deploy.zip
```

## Opção 5: Restart da App (Se só mudaste configurações)

Se só mudaste configurações no Azure Portal (connection strings, etc.):

```bash
az webapp restart --name smartgarden --resource-group SmartGardenRG
```

Ou no Azure Portal:
- App Service → **Overview** → **Restart**

## Verificar se o Redeploy Funcionou

### 1. Verificar Deployment Status

**GitHub Actions**:
- GitHub → **Actions** → vê se o último workflow passou (✅ verde)

**Azure Portal**:
- App Service → **Deployment Center** → **Logs**
- Deve mostrar "Active" ou "Succeeded"

### 2. Testar a API

```bash
# Testa um endpoint
curl https://smartgarden.azurewebsites.net/api/auth/register?username=test&password=test123

# Ou abre no browser
https://smartgarden.azurewebsites.net/api/swagger
```

### 3. Verificar Logs

```bash
# Logs em tempo real
az webapp log tail --name smartgarden --resource-group SmartGardenRG
```

Ou no Azure Portal:
- App Service → **Log stream**

## Troubleshooting do Redeploy

### ❌ Workflow não executa automaticamente

**Causa**: O workflow só executa em pushes para `main` ou `master`

**Solução**: 
- Verifica se estás a fazer push para o branch correto
- Ou usa "Run workflow" manualmente

### ❌ Deployment falha

**Solução**:
1. Verifica os logs do GitHub Actions
2. Verifica se o secret `AZURE_WEBAPP_PUBLISH_PROFILE` ainda está válido
3. Verifica se o nome do App Service está correto no workflow

### ❌ Mudanças não aparecem

**Solução**:
1. Verifica se o deployment foi bem-sucedido
2. Pode ser cache - faz restart da app:
   ```bash
   az webapp restart --name smartgarden --resource-group SmartGardenRG
   ```
3. Limpa o cache do browser se testares via browser

## Fluxo Recomendado

1. **Faz alterações localmente**
2. **Testa localmente**:
   ```bash
   dotnet run
   ```
3. **Commit e push**:
   ```bash
   git add .
   git commit -m "Descrição das alterações"
   git push origin main
   ```
4. **Monitoriza o deployment**:
   - GitHub → Actions → vê o workflow
   - Aguarda até estar completo (✅)
5. **Testa no Azure**:
   - Vai a `https://smartgarden.azurewebsites.net/api/swagger`

## Dica: Verificar Último Deployment

```bash
# Ver quando foi o último deployment
az webapp deployment list-publishing-profiles \
  --name smartgarden \
  --resource-group SmartGardenRG

# Ver histórico de deployments
az webapp deployment list \
  --name smartgarden \
  --resource-group SmartGardenRG
```

## Resumo Rápido

**Para redeploy automático** (mais comum):
```bash
git add .
git commit -m "Update"
git push origin main
```

**Para redeploy manual**:
- GitHub Actions → Run workflow
- Ou: `./deploy-azure.sh SmartGardenRG smartgarden`
