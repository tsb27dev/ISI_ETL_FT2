# 🔧 Como Corrigir o Workflow Gerado pelo Azure

## Problema

O Azure Portal (Deployment Center) pode criar um workflow automático que não especifica o ficheiro do projeto, causando o erro:
```
MSBUILD : error MSB1003: Specify a project or solution file.
```

## Solução

### Opção 1: Usar o Nosso Workflow (Recomendado)

O ficheiro `.github/workflows/azure-deploy.yml` já está corrigido e especifica o projeto em todos os comandos.

**Se o Azure criou um workflow diferente**, faz o seguinte:

1. **No GitHub**, vai a **Actions** → vê qual workflow está a correr
2. **Se for um workflow gerado pelo Azure** (ex: `azure-webapps-dotnet-core.yml`):
   - Edita esse ficheiro
   - Ou apaga-o e usa apenas o nosso `azure-deploy.yml`

### Opção 2: Corrigir o Workflow do Azure

Se preferires manter o workflow gerado pelo Azure, edita-o e garante que todos os comandos especificam o projeto:

```yaml
- name: Build with dotnet
  run: dotnet build SmartGardenApi.csproj --configuration Release

- name: Publish with dotnet  
  run: dotnet publish SmartGardenApi.csproj --configuration Release --output ./publish
```

### Opção 3: Desativar Deployment Automático do Azure

1. Azure Portal → App Service → **Deployment Center**
2. **Disconnect** da fonte atual
3. Usa apenas o nosso workflow manual via GitHub Actions

## Verificar Qual Workflow Está a Correr

1. GitHub → **Actions** tab
2. Vê qual workflow está a executar quando fazes push
3. Se for um workflow gerado pelo Azure, edita-o ou apaga-o

## Workflow Correto

O nosso workflow (`azure-deploy.yml`) já está correto e deve funcionar. Se ainda vês erros:

1. Verifica se há múltiplos workflows a correr
2. Desativa o workflow gerado pelo Azure
3. Usa apenas o nosso workflow manual

## Comandos Corretos

Todos os comandos devem especificar o ficheiro do projeto:

```bash
# ❌ ERRADO
dotnet build --configuration Release

# ✅ CORRETO  
dotnet build SmartGardenApi.csproj --configuration Release
```
