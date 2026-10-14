# Estado del proyecto y pendientes

Última actualización: 2026-10-14. Último commit al escribir este documento: `89a87c3` (rama `main`, sin remoto configurado).

## 1. Qué está hecho y verificado

| Área | Estado | Evidencia |
|---|---|---|
| Backend (ASP.NET Core, .NET 10) | Completo | `dotnet test backend` → 41 de 41 pasan; `dotnet format --verify-no-changes` limpio |
| Frontend (React 19 + Vite + Tailwind) | Completo | `npm --prefix frontend test` → 22 de 22 pasan; `npm run build` y `npm run lint` limpios |
| Flujo de punta a punta en local | Probado por el usuario con Gemini real | Link → "Obteniendo transcript…" → "Generando notas…" → Markdown en vivo |
| Reintentos ante 503 de Gemini + modelo de respaldo | Implementado con tests | `GeminiLlmProviderTests`: reintenta en 503, cambia a respaldo, no reintenta 429 |
| Revisión final independiente de la rama | Hecha; 1 crítico y 4 importantes corregidos | Corregidos: límite por IP detrás de proxy, errores inesperados a mitad del stream, cuota diaria gastada por links inválidos, notas vacías si el LLM no devuelve texto, README sin Docker |
| CI (GitHub Actions) | Escrito, **nunca ejecutado en GitHub** | `.github/workflows/ci.yml` |
| Deploy a Azure | Escrito, **nunca ejecutado** | `.github/workflows/deploy-*.yml`, `docs/deploy.md` |

## 2. Cómo correrlo y probarlo en local

Requisitos: .NET SDK 10, Node 24. Docker no está instalado en la máquina de desarrollo.

**Backend** (escucha en `http://localhost:5136`):
```powershell
$env:Gemini__ApiKey = "TU_API_KEY"          # https://aistudio.google.com/apikey
$env:Gemini__Model = "gemini-3.6-flash"      # opcional; es el valor por defecto
dotnet run --project backend/src/NotasNet.Api
```
**Frontend** (el proxy de Vite reenvía `/api` a `localhost:5136`):
```powershell
npm --prefix frontend run dev
```
**Tests:** `dotnet test backend` y `npm --prefix frontend test`.

Rutas del backend: `GET /health` y `POST /api/notes`. Cualquier otra ruta, incluida `/`, devuelve 404 y es normal.

**Gotcha:** si el backend está corriendo, `dotnet test` falla con `MSB3027` porque los archivos están bloqueados. Detené el backend o usá `dotnet test backend --artifacts-path <carpeta-temporal>`.

## 3. Configuración relevante

| Variable | Valor por defecto | Descripción |
|---|---|---|
| `Gemini__ApiKey` | vacío | Obligatoria. Sin ella toda petición termina en `llm_failure` |
| `Gemini__Model` | `gemini-3.6-flash` | Modelo principal |
| `Gemini__FallbackModel` | `gemini-3.5-flash-lite` | Se usa si el principal sigue en 503 tras 2 reintentos (esperas de 1 s y 3 s) |
| `Cors__AllowedOrigin` | `http://localhost:5173` | Único origen permitido por CORS |
| `RateLimit__PerMinute` | 5 | Peticiones por minuto por IP (usa `X-Forwarded-For` detrás de proxy) |
| `Notes__DailyLimit` | 200 | Tope diario global de peticiones al LLM, en memoria |
| `Notes__MaxTranscriptChars` / `Notes__ChunkChars` / `Notes__MinWords` | 400000 / 60000 / 30 | Límites del transcript |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | vacío | Si se define, activa Application Insights |

Sobre los modelos: según la documentación de precios de Gemini consultada el 2026-10-14, `gemini-3.6-flash` y `gemini-3.5-flash-lite` tienen tier gratuito. Esas condiciones cambian, así que conviene revisarlas en https://ai.google.dev/gemini-api/docs/pricing. El tier gratuito tiene límites bajos por proyecto (se ven en https://aistudio.google.com/rate-limit) y Google puede usar los datos enviados para mejorar sus productos.

Un error 503 de Gemini ("high demand") es temporal y no es un fallo del proyecto.

## 4. Pendiente para publicar (PUNTO DONDE QUEDAMOS)

El usuario pidió una guía para publicar el repo en GitHub. La guía se entregó, pero **ningún paso se ejecutó todavía**. No hay remoto (`git remote -v` vacío) y `gh` no está instalado, así que se publica desde la web.

Datos que faltan del usuario:
1. **Nombre y email de su cuenta de GitHub.** Los 12 commits están firmados como `dev <was1966@gmail.com>` porque no hay identidad de git configurada. Si se quiere que el portfolio muestre su nombre, hay que reescribir el historial antes de publicar (seguro mientras no haya remoto). Si no, queda como está.
2. **Usuario de GitHub**, para reemplazar `OWNER/REPO` en el badge de `README.md`.
3. **Decisión sobre `docs/superpowers/`** (la spec y el plan). Recomendación: dejarla, porque muestra el proceso de diseño.

Pasos, en orden:
1. `git config --global user.name "..."` y `git config --global user.email "..."`.
2. Crear en https://github.com/new un repo **público y vacío** (sin README, `.gitignore` ni licencia), por ejemplo `notas-net`.
3. En la raíz del proyecto:
   ```powershell
   git remote add origin https://github.com/TU_USUARIO/notas-net.git
   git push -u origin main
   ```
4. Reemplazar `OWNER/REPO` en `README.md` por `TU_USUARIO/notas-net` y hacer push.
5. En la pestaña Actions, confirmar que **CI** pase en verde. Los workflows de deploy deben aparecer **omitidos**, no en rojo: se saltan solos mientras no existan las variables `ACR_NAME` y `API_URL`.
6. Opcional: descripción y topics del repo (`dotnet`, `react`, `typescript`, `gemini`, `youtube`, `sse`).

Riesgo conocido: el CI nunca corrió en GitHub, así que puede revelar diferencias entre Windows y Linux (por ejemplo, el formato de `dotnet format`). Se evitó la causa más probable con `.editorconfig` y `.gitattributes` (`eol=lf`), pero no está comprobado.

## 5. Pendiente para el deploy en Azure

Todo está en `docs/deploy.md`: comandos `az` de aprovisionamiento, y la tabla de secretos y variables de GitHub. Requiere la suscripción de Azure del usuario. Puntos a tener en cuenta:
- Para Static Web Apps usar región `eastus2` o `westus2`; `docs/deploy.md` todavía dice `eastus`, que no es una región válida para ese servicio.
- Los Dockerfiles y el `docker-compose.yml` **nunca se construyeron** (no hay Docker en la máquina de desarrollo). Su primera prueba real será `az acr build`.
- Tras el deploy: completar en `README.md` el link de la demo, y agregar una captura o GIF.
- El límite de 240 s de la entrada de Container Apps puede cortar videos de varias horas, porque el resumen por partes es secuencial.

## 6. Pendientes menores (diferidos a propósito)

- Al cancelar, el Markdown parcial queda en pantalla sin botones de acción.
- Falta una validación al arrancar: sin `Gemini__ApiKey` la app inicia igual y todo falla con `llm_failure`.
- `Cors__AllowedOrigin` vacío (`""`) desactiva CORS en silencio; debería usar `IsNullOrWhiteSpace`.
- `URL.revokeObjectURL` inmediatamente después del click puede cancelar la descarga en Firefox.
- `frontend/README.md` es la plantilla sin tocar de Vite.
- Hay poco logging fuera del manejo de errores de Gemini y del caso inesperado en el endpoint.
- La cuota diaria vive en memoria: se reinicia con el contenedor y no se comparte entre réplicas.
- El deploy del frontend corre tests pero no lint.

## 7. Próximas mejoras posibles (fuera de la v1)

Historial de notas (persistencia con EF Core), cuentas de usuario, y fallback enviando el video directo a Gemini cuando no hay transcript.

## 8. Decisiones de diseño relevantes

- .NET 10 en lugar de 8: es el SDK instalado y la spec no fija versión.
- El prompt del sistema está en `backend/src/NotasNet.Api/Prompts/notes.system.md` y se carga como recurso embebido.
- El transcript se obtiene con la librería no oficial `YoutubeExplode`, aislada detrás de `ITranscriptProvider`. Es el punto más frágil del proyecto, porque puede romperse si YouTube cambia su formato.
- `GenerateNotesUseCase` consume la cuota diaria solo después de validar la URL y comprobar que el transcript tiene contenido, justo antes de llamar al LLM.

## 9. Mapa de archivos

| Ruta | Contenido |
|---|---|
| `backend/src/NotasNet.Api/Application/` | Caso de uso, parser de URL, interfaces, troceado de transcript |
| `backend/src/NotasNet.Api/Infrastructure/` | Proveedores de Gemini y YouTube, parser SSE, opciones |
| `backend/src/NotasNet.Api/Endpoints/` | Endpoint SSE y cuota diaria |
| `backend/tests/NotasNet.Tests/` | Tests del backend |
| `frontend/src/` | `App.tsx`, `components/`, `hooks/useNotesStream.ts`, `lib/` |
| `docs/` | `architecture.md`, `deploy.md`, spec y plan en `superpowers/` |
| `.github/workflows/` | `ci.yml`, `deploy-backend.yml`, `deploy-frontend.yml` |
