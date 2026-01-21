# 🔧 Correções Aplicadas ao Deployment

## Problemas Corrigidos

### 1. ✅ Workflow Melhorado test

**Antes**: O workflow não especificava o projeto explicitamente
**Agora**: 
- Especifica `SmartGardenApi.csproj` em todos os comandos
- Garante que o build e publish são feitos corretamente

### 2. ✅ Ficheiro de Troubleshooting Criado

Criado `TROUBLESHOOTING_DEPLOYMENT.md` com soluções para problemas comuns:
- Erros de executável não encontrado
- Problemas com base de dados SQLite
- Erros no GitHub Actions
- Problemas de runtime

### 3. ✅ Script de Verificação

Criado `check-deployment.sh` para verificar se tudo está configurado:
```bash
./check-deployment.sh
```

## Checklist Antes de Fazer Deploy

### No Azure Portal:

1. ✅ **App Service criado** com:
   - Runtime stack: `.NET 9` (ou `.NET 8` se 9 não estiver disponível)
   - Operating System: `Linux`

2. ✅ **Configuration** → **General settings**:
   - Stack: `.NET`
   - Version: `9.0` (ou mais recente disponível)
   - Startup Command: `dotnet SmartGardenApi.dll` (se necessário)

3. ✅ **Configuration** → **Application settings**:
   - `ConnectionStrings:Garden` = `Data Source=/home/data/garden.db`

### No GitHub:

1. ✅ **Secret configurado**:
   - Settings → Secrets → Actions
   - Nome: `AZURE_WEBAPP_PUBLISH_PROFILE`
   - Valor: Todo o conteúdo do ficheiro `.PublishSettings` do Azure

2. ✅ **Workflow configurado**:
   - `.github/workflows/azure-deploy.yml` existe
   - `AZURE_WEBAPP_NAME` corresponde ao nome do App Service

### Localmente:

1. ✅ **Projeto compila**:
   ```bash
   dotnet build -c Release
   ```

2. ✅ **Publish funciona**:
   ```bash
   dotnet publish -c Release -o ./publish
   ```

## Como Diagnosticar Problemas

### 1. Verificar Logs do GitHub Actions

1. GitHub → **Actions** tab
2. Clica no workflow que falhou
3. Expande cada step para ver o erro específico

### 2. Verificar Logs do Azure

**Via Portal**:
- App Service → **Log stream** (tempo real)

**Via CLI**:
```bash
az webapp log tail --name smartgardenapi --resource-group SmartGardenRG
```

### 3. Testar Localmente

```bash
# Build
dotnet build -c Release

# Publish
dotnet publish -c Release -o ./publish

# Testar
cd publish
dotnet SmartGardenApi.dll
```

Se funcionar localmente mas não no Azure, é problema de configuração do Azure.

## Erros Comuns e Soluções Rápidas

### ❌ "No executable found"
**Solução**: Azure Portal → Configuration → General settings → Startup Command: `dotnet SmartGardenApi.dll`

### ❌ "502 Bad Gateway"
**Solução**: 
1. Verifica logs: `az webapp log tail`
2. Verifica se o runtime está correto: `.NET 9`
3. Verifica connection string

### ❌ "Failed to deploy" no GitHub Actions
**Solução**:
1. Verifica se o secret `AZURE_WEBAPP_PUBLISH_PROFILE` está correto
2. Verifica se `AZURE_WEBAPP_NAME` corresponde ao Azure
3. Verifica os logs do GitHub Actions

## Próximos Passos

1. **Executa o script de verificação**:
   ```bash
   ./check-deployment.sh
   ```

2. **Faz push para o branch main**:
   ```bash
   git add .
   git commit -m "Fix deployment configuration"
   git push origin main
   ```

3. **Monitoriza o deployment**:
   - GitHub → Actions → vê o workflow a correr
   - Azure Portal → Deployment Center → vê o histórico

4. **Se ainda falhar**:
   - Lê `TROUBLESHOOTING_DEPLOYMENT.md` para soluções detalhadas
   - Verifica os logs específicos do erro
   - Testa deployment manual: `./deploy-azure.sh`
