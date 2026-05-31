# Ontological Studio Bridge

Extensión para **VSCode** y **TRAE** que conecta la app de escritorio
*OntologicalStudio* con el motor de IA del editor (GitHub Copilot, Claude,
Gemini… cualquier modelo expuesto vía `vscode.lm`). Además incluye un panel
embebido que muestra Universes / Scenarios / Solutions de la base de datos
local sin salir del editor.

## ¿Qué hace?

1. **AI Bridge** — Levanta un pequeño servidor HTTP en
   `http://localhost:39217` (configurable). La app de escritorio envía sus
   prompts a este servidor y la extensión los reenvía al modelo que tengas
   activo en VSCode/TRAE. Así no necesitas configurar API keys en la app.
2. **Status View** — Vista en la activity bar con el estado del bridge,
   contador de peticiones servidas y último modelo usado.
3. **Ontological Studio Panel** — Webview con vista por columnas (Universes
   → Scenarios → Solutions/Artifacts) que consulta la API local del desktop
   (`http://127.0.0.1:53821` por defecto).

## Instalación rápida

```bash
cd OntologicalStudio.VSCodeBridge
yarn install
yarn compile
yarn package         # genera ontologicalstudio-bridge-0.1.0.vsix
```

Instala el `.vsix`:
- **VSCode**: `code --install-extension ontologicalstudio-bridge-0.1.0.vsix`
- **TRAE**: igual, sólo cambia `code` por `trae`, o usa la UI
  *Extensions → … → Install from VSIX*.

## Configuración de la app desktop

En **AI Settings** del desktop, selecciona:

- Provider: `VSCode / TRAE Bridge`
- Endpoint: `http://localhost:39217`
- Model: `(vacío para usar el primero disponible, o por ej.: `copilot/gpt-4o`)
- API Key: *(no aplica)*

## Comandos

| Comando | Atajo paleta |
|---|---|
| Iniciar bridge | `Ontological Studio: Start AI Bridge` |
| Detener bridge | `Ontological Studio: Stop AI Bridge` |
| Mostrar estado | `Ontological Studio: Show Bridge Status` |
| Abrir panel | `Ontological Studio: Open Panel` |

## Configuración

| Setting | Default | Descripción |
|---|---|---|
| `ontologicalstudio.bridge.port` | `39217` | Puerto local del bridge |
| `ontologicalstudio.bridge.autoStart` | `true` | Arrancar bridge al abrir el editor |
| `ontologicalstudio.bridge.preferredModel` | `""` | Modelo preferido (`vendor/family` o `family`) |
| `ontologicalstudio.desktop.apiBaseUrl` | `http://127.0.0.1:53821` | URL de la API del desktop |

## Endpoints HTTP expuestos por el bridge

- `GET  /health` — estado del bridge
- `GET  /models` — lista de chat models disponibles en VSCode/TRAE
- `POST /chat` — envía un chat al LM:
  ```json
  {
    "systemPrompt": "...",
    "userPrompt": "...",
    "model": "copilot/gpt-4o"   // opcional
  }
  ```
  Respuesta: `{ "content": "...", "model": "copilot/gpt-4o" }`

Soporta también el formato OpenAI-like:
```json
{ "messages": [{"role":"system","content":"..."},{"role":"user","content":"..."}] }
```

## Seguridad

El servidor escucha **solo en 127.0.0.1**, no es accesible desde la red.
La primera vez que se invoca un modelo, VSCode/TRAE pedirá tu consentimiento
para que la extensión use el LM (es un requisito del API `vscode.lm`).
