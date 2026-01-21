# 🔧 Troubleshooting - Deployment Azure App Service

## Problemas Comuns e Soluções

### ❌ Erro: "No executable found matching command 'dotnet-SmartGardenApi.dll'"

**Causa**: O Azure não encontra o executável da aplicação.

**Solução**:
1. No Azure Portal → App Service → **Configuration** → **General settings**
2. Verifica se **Stack** está definido como `.NET` e **Version** como `9.0`
3. Adiciona **Startup Command**: `dotnet SmartGardenApi.dll`
4. **Save** e reinicia a app

### ❌ Erro: "Application Error" ou "502 Bad Gateway"

**Causa**: A aplicação não está a iniciar corretamente.

**Soluções**:

1. **Verificar Logs**:
   ```bash
   az webapp log tail --name smartgardenapi --resource-group SmartGardenRG
   ```
   Ou no Azure Portal: **App Service** → **Log stream**

2. **Verificar se o diretório `/home/data/` existe**:
   - A connection string deve apontar para `/home/data/garden.db`
   - Este diretório é persistente no Azure

3. **Verificar Runtime**:
   - Azure Portal → **Configuration** → **General settings**
   - **Stack**: `.NET`
   - **Version**: `9.0`

### ❌ Erro no GitHub Actions: "Failed to deploy"

**Causas comuns**:

1. **Publish Profile incorreto**:
   - Verifica se o secret `AZURE_WEBAPP_PUBLISH_PROFILE` está correto
   - Deve conter TODO o XML do ficheiro `.PublishSettings`

2. **Nome do App Service errado**:
   - Verifica se `AZURE_WEBAPP_NAME` no workflow corresponde ao nome real no Azure

3. **Build falha**:
   - Verifica os logs do GitHub Actions
   - Pode ser problema de dependências ou versão do .NET

**Solução**:
```yaml
# No .github/workflows/azure-deploy.yml, verifica:
env:
  AZURE_WEBAPP_NAME: smartgardenapi  # ← Deve ser exatamente o nome no Azure
```

### ❌ Erro: "SQLite database locked" ou problemas com base de dados

**Causa**: SQLite pode ter problemas de concorrência no Azure.

**Soluções**:

1. **Usar diretório persistente**:
   - Connection string: `Data Source=/home/data/garden.db`

2. **Verificar permissões**:
   - O diretório `/home/data/` deve ter permissões de escrita

3. **Considerar migrar para Azure SQL Database** (para produção)

### ❌ Erro: "Package restore failed"

**Causa**: Problemas com NuGet packages.

**Solução**:
1. Verifica se todos os packages estão atualizados
2. Tenta fazer `dotnet restore` localmente
3. Verifica se há conflitos de versão

### ❌ Erro: "The specified framework 'Microsoft.NETCore.App', version '9.0.0' was not found"

**Causa**: O Azure não tem o runtime .NET 9 instalado.

**Solução**:
1. Azure Portal → App Service → **Configuration** → **General settings**
2. **Stack**: `.NET`
3. **Version**: `9.0` (ou a versão mais recente disponível)
4. Se não houver 9.0, pode ser necessário usar `.NET 8` e ajustar o projeto

### ❌ Erro: "CoreWCF" ou "SOAP service not working"

**Causa**: Problemas com configuração do CoreWCF.

**Solução**:
1. Verifica se o CoreWCF está configurado corretamente no `Program.cs`
2. Verifica os logs para erros específicos do CoreWCF
3. Pode ser necessário ajustar as configurações de binding

## Checklist de Verificação

Antes de reportar um problema, verifica:

- [ ] O App Service está criado e em execução
- [ ] O runtime está configurado como `.NET 9` (ou versão compatível)
- [ ] A connection string está configurada: `ConnectionStrings:Garden`
- [ ] O secret `AZURE_WEBAPP_PUBLISH_PROFILE` está configurado no GitHub
- [ ] O nome do App Service no workflow corresponde ao Azure
- [ ] Os logs foram verificados (GitHub Actions e Azure Log Stream)
- [ ] A aplicação compila localmente sem erros

## Como Obter Logs Detalhados

### Logs do GitHub Actions
1. GitHub → **Actions** tab
2. Clica no workflow que falhou
3. Expande cada step para ver detalhes

### Logs do Azure
1. Azure Portal → App Service → **Log stream** (tempo real)
2. Ou: **Logs** → **App Service Logs** (histórico)
3. Ou via CLI:
   ```bash
   az webapp log tail --name smartgardenapi --resource-group SmartGardenRG
   ```

### Logs da Aplicação
Adiciona logging no `appsettings.Production.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

## Comandos Úteis

```bash
# Ver estado da app
az webapp show --name smartgardenapi --resource-group SmartGardenRG --query state

# Restart da app
az webapp restart --name smartgardenapi --resource-group SmartGardenRG

# Ver configurações
az webapp config show --name smartgardenapi --resource-group SmartGardenRG

# Ver connection strings
az webapp config connection-string list --name smartgardenapi --resource-group SmartGardenRG

# Testar localmente antes de deploy
dotnet publish -c Release -o ./publish
cd publish
dotnet SmartGardenApi.dll
```

## Se Nada Funcionar

1. **Testa localmente primeiro**:
   ```bash
   dotnet publish -c Release
   cd bin/Release/net9.0/publish
   dotnet SmartGardenApi.dll
   ```

2. **Verifica se o problema é específico do Azure**:
   - Se funciona localmente mas não no Azure, é problema de configuração
   - Se não funciona localmente, corrige primeiro

3. **Considera usar Azure CLI para deploy manual**:
   ```bash
   ./deploy-azure.sh SmartGardenRG smartgardenapi
   ```

4. **Verifica a documentação oficial**:
   - [Azure App Service .NET Deployment](https://learn.microsoft.com/en-us/azure/app-service/quickstart-dotnetcore)
