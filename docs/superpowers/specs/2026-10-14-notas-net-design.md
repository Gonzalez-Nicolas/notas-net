# Notas Net — Spec de diseño

Fecha: 2026-10-14

## 1. Objetivo y contexto

Proyecto de portfolio, atractivo para reclutadores y con contenido técnico interesante para contar.

**Flujo:** el usuario pega un link de YouTube → el backend obtiene el transcript → lo envía a un LLM (Gemini) con un prompt especializado → devuelve notas en Markdown (`notes.md`) → el frontend las muestra en vivo.

**Criterios de éxito:**
- Demo pública funcionando, con ejemplos de un clic.
- Código que muestre criterio de diseño (abstracciones, tests, CI) sin sobreingeniería.
- README que cuente las decisiones, límites y próximos pasos.

## 2. Decisiones tomadas

| Tema | Decisión |
|---|---|
| Stack | ASP.NET Core (C#, Minimal API) + React + TypeScript |
| LLM | Gemini, detrás de `ILlmProvider` |
| Transcript | Librería no oficial, detrás de `ITranscriptProvider` |
| Alcance | MVP limpio: sin cuentas, sin base de datos, sin historial |
| Despliegue | Azure (Container Apps + Static Web Apps) |
| Arquitectura | Clean Architecture ligera (un proyecto API con carpetas + proyecto de tests) |

## 3. Backend

**Estructura:**
- `Api`: endpoints (Minimal API), rate limiting, SSE, CORS.
- `Application`: caso de uso `GenerateNotes`, parser/validador de URL, interfaces `ILlmProvider` e `ITranscriptProvider`.
- `Infrastructure`: `GeminiLlmProvider`, `YoutubeTranscriptProvider`.
- `Tests`: proyecto aparte.

**Endpoint:** `POST /api/notes` con `{ "url": "..." }`. Responde por SSE con eventos:
- `status`: progreso ("Obteniendo transcript…", "Generando notas…").
- `chunk`: fragmento de Markdown.
- `done`: fin.
- `error`: código de error.

**Flujo interno:**
1. Valida y extrae `videoId` (soporta `youtube.com/watch`, `youtu.be`, `shorts`).
2. `ITranscriptProvider.GetAsync(videoId)` devuelve el texto.
3. Si el transcript excede un umbral, se divide y resume por partes (map-reduce); solo aplica a videos muy largos.
4. `ILlmProvider.StreamNotesAsync(transcript, prompt)` devuelve `IAsyncEnumerable<string>`.
5. El prompt vive en un archivo de recursos versionado, no en código.

**Errores (códigos claros):** URL inválida, video sin subtítulos, video privado o inexistente, límite de uso excedido, falla del LLM.

**Transversal:**
- Rate limiting por IP (middleware nativo).
- Tope de tamaño de transcript y límite diario global de requests al LLM.
- API key de Gemini en variables de entorno (Key Vault opcional).
- CORS limitado al dominio del frontend.

**Tests:** unitarios del parser de URL y del caso de uso (proveedores falsos); test de integración del endpoint.

## 4. Frontend

**Stack:** React + TypeScript + Vite, Tailwind, `react-markdown` con `remark-gfm`.

**Pantalla única, tres estados:**
1. **Inicio:** campo para el link, botón "Generar notas", ejemplos de videos con un clic.
2. **Generando:** estado actual y Markdown apareciendo en vivo vía SSE.
3. **Resultado:** notas renderizadas, título y miniatura, botones Copiar Markdown y Descargar `.md`, botón para generar otras.

**Detalles:** modo claro/oscuro según el sistema, responsive, errores amigables mapeados desde los códigos del backend, validación de link en cliente, botón de cancelar (aborta el stream).

**Código:**
- Hook `useNotesStream(url)`: consume SSE con `fetch` y lectura de stream (el endpoint es POST, por lo que no se usa `EventSource`).
- Componentes: `UrlForm`, `NotesViewer`, `ErrorBanner`, `StatusIndicator`.

**Tests:** Vitest + Testing Library para el hook (stream simulado) y la validación de URL.

## 5. Despliegue y CI/CD

**Azure:**
- Backend: Azure Container Apps (escala a cero).
- Frontend: Azure Static Web Apps (tier gratuito).
- Observabilidad: Application Insights.

**GitHub Actions:**
- En PR: build, `dotnet test`, Vitest, lint y formato.
- En push a `main`: build de imagen Docker, push a Azure Container Registry, deploy del backend; deploy del frontend con su propia acción.
- `Dockerfile` y `docker-compose.yml` para levantar todo localmente con un comando.

## 6. README y presentación

- Captura o GIF de la demo y link a la demo en vivo.
- Diagrama de arquitectura (basado en el original, mejorado).
- Stack y badges de CI y tests.
- Sección "Decisiones de diseño": por qué `ILlmProvider` e `ITranscriptProvider`, por qué SSE, por qué Clean Architecture ligera.
- "Limitaciones conocidas": librería no oficial de transcripts, videos sin subtítulos.
- "Próximos pasos": historial, cuentas, fallback con video directo a Gemini.
- Cómo correrlo local en 3 comandos.

## 7. Fuera de alcance (v1)

Cuentas de usuario, historial/persistencia, selección de múltiples proveedores desde la UI, fallback de video directo a Gemini.

## 8. Riesgos

- La librería no oficial de transcripts puede romperse si YouTube cambia su formato y está en zona gris respecto a los términos de servicio. Mitigación: aislada detrás de `ITranscriptProvider`.
- Costo/abuso de la demo pública. Mitigación: rate limit por IP, tope de transcript, límite diario global.
