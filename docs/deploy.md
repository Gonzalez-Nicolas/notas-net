# Despliegue en Azure

Aprovisionamiento único (reemplazá los valores entre `<>`). Requiere `az login` y la extensión `containerapp`.

```bash
RG=notasnet-rg
LOC=eastus2
ACR=<nombre-unico-acr>
ENV=notasnet-env
APP=notasnet-api

az group create -n $RG -l $LOC
az acr create -n $ACR -g $RG --sku Basic --admin-enabled true
az monitor app-insights component create --app notasnet-ai -g $RG -l $LOC
az containerapp env create -n $ENV -g $RG -l $LOC

# Primera imagen (después la actualiza el workflow)
az acr build --registry $ACR --image notasnet-api:init backend

az containerapp create -n $APP -g $RG --environment $ENV \
  --image $ACR.azurecr.io/notasnet-api:init \
  --registry-server $ACR.azurecr.io \
  --target-port 8080 --ingress external \
  --min-replicas 0 --max-replicas 2 \
  --secrets gemini-key=<GEMINI_API_KEY> \
  --env-vars Gemini__ApiKey=secretref:gemini-key \
             Gemini__Model=gemini-3.6-flash \
             Gemini__FallbackModel=gemini-3.5-flash-lite \
             Cors__AllowedOrigin=https://<tu-static-web-app>.azurestaticapps.net \
             APPLICATIONINSIGHTS_CONNECTION_STRING=<connection-string>

az staticwebapp create -n notasnet-web -g $RG -l $LOC
```

## Secretos y variables de GitHub

| Tipo | Nombre | Valor |
|---|---|---|
| Secret | `AZURE_CREDENTIALS` | JSON de un service principal con rol Contributor sobre el resource group (`az ad sp create-for-rbac --sdk-auth`) |
| Secret | `AZURE_STATIC_WEB_APPS_API_TOKEN` | Token de deploy de la Static Web App |
| Variable | `ACR_NAME` | Nombre del Container Registry |
| Variable | `CONTAINER_APP_NAME` | `notasnet-api` |
| Variable | `AZURE_RESOURCE_GROUP` | `notasnet-rg` |
| Variable | `API_URL` | URL pública del Container App (`https://…azurecontainerapps.io`) |

Con esto, cada push a `main` despliega backend (tras pasar el CI) y frontend.
