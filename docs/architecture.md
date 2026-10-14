# Arquitectura

```mermaid
flowchart LR
    U([Usuario]) --> F[Frontend<br/>React + Vite]
    F -- "POST /api/notes {url}" --> B

    subgraph B[Backend · ASP.NET Core]
        direction TB
        E[Endpoint SSE<br/>rate limit + CORS] --> UC[GenerateNotesUseCase]
        UC --> TP[[ITranscriptProvider]]
        UC --> LP[[ILlmProvider]]
    end

    TP -. implementa .-> YT[YoutubeTranscriptProvider]
    LP -. implementa .-> GM[GeminiLlmProvider]
    YT -- transcript --> YTS[(YouTube)]
    GM -- "prompt + transcript" --> GEM[(Gemini API)]
    B -- "SSE: status · chunk · done · error" --> F
```

## Capas del backend

| Carpeta | Responsabilidad |
|---|---|
| `Endpoints/` | Endpoint HTTP, SSE, cuota diaria |
| `Application/` | Caso de uso, parser de URL, interfaces de proveedores |
| `Infrastructure/` | Implementaciones: YouTube (`YoutubeExplode`) y Gemini (REST) |
| `Prompts/` | Prompt del sistema versionado (recurso embebido) |

## Despliegue

- **Backend:** Azure Container Apps (escala a cero), imagen construida en Azure Container Registry.
- **Frontend:** Azure Static Web Apps.
- **Observabilidad:** Application Insights (se activa al definir `APPLICATIONINSIGHTS_CONNECTION_STRING`).
