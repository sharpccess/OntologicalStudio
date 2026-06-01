<div align="center">

# 🧠 Ontological Studio

**An ontology-aware desktop studio for AI-assisted reasoning, modeling & scenario solving.**

*Un estudio de escritorio orientado a ontologías para razonamiento, modelado y resolución de escenarios con IA.*

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Avalonia](https://img.shields.io/badge/Avalonia-11.2-883BCD?logo=avalonia&logoColor=white)](https://avaloniaui.net/)
[![SQLite](https://img.shields.io/badge/SQLite-Embedded-003B57?logo=sqlite&logoColor=white)](https://sqlite.org/)
[![License](https://img.shields.io/badge/License-TBD-lightgrey)](#-license--licencia)

[English](#-english) · [Español](#-español)

</div>

---

## 🇬🇧 English

### What is it?

**Ontological Studio** is a desktop application that helps you **model real-world systems as ontologies** (universes, entities, relationships, scenarios) and then use that structured knowledge as **rich context for Large Language Models** — so AI answers are grounded, traceable and reusable instead of generic.

You design a universe once, the system builds tight, context-aware prompts from it, and the AI returns artifacts (Markdown, JSON, plans, reports) that you can preview, refine, version and export.

### ✨ Key features

- 🌌 **Multi-universe workspace** — Each universe is an isolated knowledge space (one project, one domain, one client…).
- 🧩 **Visual canvas** — Drag-and-drop nodes for entities and connectors for relationships, with zoom and pan.
- 🔬 **Entity hydration** — Pick a node, click *Hydrate*, the AI enriches it with motivations, fears, incentives, behavioral patterns, web research, etc. Diff-preview before applying.
- 📜 **Scenario solving** — Define a scenario inside a universe. The app builds a complete prompt with all related entities and runs it through your configured AI to produce solutions and artifacts.
- 🎨 **Polished Markdown preview** — Artifacts are rendered as proper documents with headers, lists, scrollable view, and one-click export to TXT, Markdown, PDF or Word.
- 🛑 **Global AI operation overlay** — Every AI call shows a modal with the running provider, elapsed time and a **Cancel** button so you always know what's happening.
- 🌐 **Multi-provider AI** — Plug in **Ollama** (local or cloud), **OpenAI**, **Anthropic Claude**, **DeepSeek**, **Google Gemini**, or **OpenRouter** (one key, many models). Switch providers with two clicks; test the connection live from the UI.
- 🗂️ **Reusable library** — Save entities or full models and drop them into new universes.
- 🌍 **English & Spanish** — Full UI localization, switchable at runtime.
- 💾 **Local-first** — All your data stays in a local SQLite database under `%LOCALAPPDATA%\OntologicalStudio\`. No telemetry, no cloud lock-in.
- 📦 **Per-user installer (NSIS)** — Ships self-contained, no .NET runtime required on the target machine, no admin rights to install.

### 🏗️ Architecture

The solution is built with **Clean Architecture** so the inner layers know nothing about the outer ones:

```
┌──────────────────────────────────────────────┐
│  OntologicalStudio.Desktop  (Avalonia, MVVM) │
├──────────────────────────────────────────────┤
│  OntologicalStudio.Api   (Minimal API host)  │
├──────────────────────────────────────────────┤
│  OntologicalStudio.AIProviders  (HTTP calls) │
├──────────────────────────────────────────────┤
│  OntologicalStudio.Application  (use cases)  │
├──────────────────────────────────────────────┤
│  OntologicalStudio.Infrastructure  (DI, IO)  │
├──────────────────────────────────────────────┤
│  OntologicalStudio.Persistence  (EF Core)    │
├──────────────────────────────────────────────┤
│  OntologicalStudio.Core  (domain & models)   │
└──────────────────────────────────────────────┘
                 +  Localization
```

| Project | Responsibility |
|---|---|
| `Core` | Domain models, interfaces. No dependencies. |
| `Persistence` | EF Core + SQLite implementation of repositories. |
| `Application` | Services / use cases: hydration workflow, scenario solving, exports. |
| `AIProviders` | `ConfigurableAIProvider` dispatching to OpenAI / Anthropic / Ollama / DeepSeek / Gemini / OpenRouter. |
| `Infrastructure` | DI composition root, settings store, web research, blob store. |
| `Api` | Lightweight local HTTP API (`127.0.0.1:53821`) for power users. |
| `Desktop` | Avalonia 11 UI (MVVM + CommunityToolkit.Mvvm). |
| `Localization` | JSON-backed runtime translation service. |

### 🛠️ Tech stack

- **.NET 8** · **C# 12** · **Avalonia 11.2** · **CommunityToolkit.Mvvm** · **Entity Framework Core 8** · **SQLite** · **NSIS 3** (installer).

### 🚀 Getting started (developers)

```bash
# 1. Prerequisites
#    - .NET 8 SDK
#    - Windows / macOS / Linux

# 2. Clone & restore
git clone <repo-url>
cd OntologicalStudio
dotnet restore

# 3. Build everything
dotnet build

# 4. Run the desktop app
dotnet run --project OntologicalStudio.Desktop
```

The app auto-creates `%LOCALAPPDATA%\OntologicalStudio\ontology.db` on first launch.

### 🤖 Configuring an AI provider

Open the **AI** panel (top-right button) and fill in:

| Provider | Endpoint (auto if empty) | Model example | API key |
|---|---|---|---|
| **Ollama** (local) | `http://localhost:11434` | `qwen2.5-coder:32b` | optional |
| **Ollama Cloud** | `https://ollama.com` | `qwen3-coder-next` | required |
| **OpenAI / GPT** | `https://api.openai.com/v1/chat/completions` | `gpt-4o-mini` | required |
| **Anthropic / Claude** | `https://api.anthropic.com/v1/messages` | `claude-3-5-sonnet-latest` | required |
| **DeepSeek** | `https://api.deepseek.com/v1/chat/completions` | `deepseek-chat` | required |
| **Google Gemini** | `https://generativelanguage.googleapis.com` | `gemini-2.0-flash` | required |
| **OpenRouter** | `https://openrouter.ai/api/v1/chat/completions` | `anthropic/claude-3.5-sonnet` | required |

Click **Test connection** to validate before saving. The app calls each provider's `/models` (or `/api/tags` for Ollama) and reports the real status.

### 📦 Building the Windows installer

```cmd
cd installer
.\build_installer.bat                 :: default 0.1.0
.\build_installer.bat 0.4.0           :: custom version
```

Produces `OntologicalStudio-Setup-<version>.exe`:
- **Per-user**, no admin required.
- **Self-contained** — bundles the .NET 8 runtime.
- Adds Start Menu + Desktop shortcuts.
- Registers in **Add/Remove Programs**.
- Clean uninstaller (keeps user data unless you opt-in).

Full details in [`installer/README.md`](installer/README.md).

### 🗺️ Roadmap

- [ ] Streaming responses (token-by-token UI update)
- [ ] Operation history (last N AI calls, with replay)
- [ ] Multi-language artifact templates
- [ ] Live collaboration / sync layer (opt-in)
- [ ] macOS / Linux installer parity

### 🤝 Contributing

Branch from `main`, name your branch `feature/<short-name>` or `fix/<short-name>`, run `dotnet build` clean, open a PR with a brief rationale. Code style follows standard .NET conventions; **null reference types are enabled** project-wide.

### 📄 License · Licencia

Currently **TBD**. Until a license file is added, the code is "all rights reserved" by default. If you want to use it, open an issue and we'll talk.

---

## 🇪🇸 Español

### ¿Qué es?

**Ontological Studio** es una aplicación de escritorio para **modelar sistemas del mundo real como ontologías** (universos, entidades, relaciones, escenarios) y usar ese conocimiento estructurado como **contexto rico para modelos de lenguaje**. Así las respuestas de la IA son fundamentadas, trazables y reutilizables, no genéricas.

Diseñas el universo una vez, el sistema construye prompts precisos y la IA devuelve artefactos (Markdown, JSON, planes, informes) que puedes previsualizar, refinar, versionar y exportar.

### ✨ Características principales

- 🌌 **Workspace multi-universo** — Cada universo es un espacio de conocimiento aislado (un proyecto, un dominio, un cliente…).
- 🧩 **Canvas visual** — Arrastra entidades y conecta relaciones, con zoom y pan.
- 🔬 **Hidratación de entidades** — Selecciona un nodo, pulsa *Hydrate* y la IA lo enriquece con motivaciones, miedos, incentivos, patrones de conducta, investigación web, etc. Vista previa con diff antes de aplicar.
- 📜 **Resolución de escenarios** — Define un escenario dentro del universo. La app arma un prompt completo con las entidades relacionadas y lo ejecuta en la IA configurada para producir soluciones y artefactos.
- 🎨 **Preview de Markdown cuidado** — Los artefactos se renderizan como documentos legibles con jerarquía tipográfica, scroll y exportación de un clic a TXT, Markdown, PDF o Word.
- 🛑 **Overlay global de operación IA** — Toda llamada a la IA muestra un modal con el proveedor activo, tiempo transcurrido y botón **Cancelar**, para que siempre sepas qué está pasando.
- 🌐 **Multi-proveedor de IA** — Conecta con **Ollama** (local o cloud), **OpenAI**, **Anthropic Claude**, **DeepSeek**, **Google Gemini** o **OpenRouter** (una key, muchos modelos). Cambia de proveedor en dos clics; valida la conexión desde la UI.
- 🗂️ **Librería reutilizable** — Guarda entidades o modelos enteros y reúsalos en nuevos universos.
- 🌍 **Inglés y Español** — Localización completa, cambiable en caliente.
- 💾 **Local-first** — Tus datos viven en SQLite en `%LOCALAPPDATA%\OntologicalStudio\`. Sin telemetría ni lock-in en la nube.
- 📦 **Instalador por usuario (NSIS)** — Self-contained, no necesita .NET runtime instalado, no pide permisos de admin.

### 🏗️ Arquitectura

Construido con **Clean Architecture**: las capas internas no conocen las externas.

```
┌──────────────────────────────────────────────┐
│  OntologicalStudio.Desktop  (Avalonia, MVVM) │
├──────────────────────────────────────────────┤
│  OntologicalStudio.Api   (Minimal API local) │
├──────────────────────────────────────────────┤
│  OntologicalStudio.AIProviders  (HTTP)       │
├──────────────────────────────────────────────┤
│  OntologicalStudio.Application  (use cases)  │
├──────────────────────────────────────────────┤
│  OntologicalStudio.Infrastructure  (DI, IO)  │
├──────────────────────────────────────────────┤
│  OntologicalStudio.Persistence  (EF Core)    │
├──────────────────────────────────────────────┤
│  OntologicalStudio.Core  (dominio y modelos) │
└──────────────────────────────────────────────┘
                 +  Localization
```

| Proyecto | Responsabilidad |
|---|---|
| `Core` | Modelos de dominio e interfaces. Sin dependencias. |
| `Persistence` | EF Core + SQLite, implementación de repositorios. |
| `Application` | Servicios / casos de uso: hidratación, resolución, exportes. |
| `AIProviders` | `ConfigurableAIProvider` que despacha a OpenAI / Anthropic / Ollama / DeepSeek / Gemini / OpenRouter. |
| `Infrastructure` | Composition root de DI, settings, investigación web, blob store. |
| `Api` | API HTTP local (`127.0.0.1:53821`) para usos avanzados. |
| `Desktop` | UI Avalonia 11 (MVVM + CommunityToolkit.Mvvm). |
| `Localization` | Servicio de traducción en runtime basado en JSON. |

### 🛠️ Stack técnico

- **.NET 8** · **C# 12** · **Avalonia 11.2** · **CommunityToolkit.Mvvm** · **Entity Framework Core 8** · **SQLite** · **NSIS 3** (instalador).

### 🚀 Empezar (desarrolladores)

```bash
# 1. Requisitos
#    - .NET 8 SDK
#    - Windows / macOS / Linux

# 2. Clonar y restaurar
git clone <url-del-repo>
cd OntologicalStudio
dotnet restore

# 3. Compilar todo
dotnet build

# 4. Ejecutar la app de escritorio
dotnet run --project OntologicalStudio.Desktop
```

La app crea `%LOCALAPPDATA%\OntologicalStudio\ontology.db` al primer arranque.

### 🤖 Configurar un proveedor de IA

Abre el panel **AI** (botón arriba a la derecha) y rellena:

| Proveedor | Endpoint (auto si vacío) | Modelo de ejemplo | API key |
|---|---|---|---|
| **Ollama** (local) | `http://localhost:11434` | `qwen2.5-coder:32b` | opcional |
| **Ollama Cloud** | `https://ollama.com` | `qwen3-coder-next` | obligatoria |
| **OpenAI / GPT** | `https://api.openai.com/v1/chat/completions` | `gpt-4o-mini` | obligatoria |
| **Anthropic / Claude** | `https://api.anthropic.com/v1/messages` | `claude-3-5-sonnet-latest` | obligatoria |
| **DeepSeek** | `https://api.deepseek.com/v1/chat/completions` | `deepseek-chat` | obligatoria |
| **Google Gemini** | `https://generativelanguage.googleapis.com` | `gemini-2.0-flash` | obligatoria |
| **OpenRouter** | `https://openrouter.ai/api/v1/chat/completions` | `anthropic/claude-3.5-sonnet` | obligatoria |

Pulsa **Test connection** antes de guardar. La app llama al `/models` (o `/api/tags` en Ollama) y te dice el estado real.

### 📦 Generar el instalador Windows

```cmd
cd installer
.\build_installer.bat                 :: versión 0.1.0 por defecto
.\build_installer.bat 0.4.0           :: versión personalizada
```

Produce `OntologicalStudio-Setup-<versión>.exe`:
- **Per-user**, sin admin.
- **Self-contained** — incluye el runtime .NET 8.
- Accesos directos en menú inicio y escritorio.
- Aparece en **Agregar o quitar programas**.
- Desinstalador limpio (mantiene tus datos a menos que opt-in).

Detalles completos en [`installer/README.md`](installer/README.md).

### 🗺️ Roadmap

- [ ] Respuestas en streaming (UI actualizándose token a token)
- [ ] Histórico de operaciones (últimas N llamadas, con replay)
- [ ] Plantillas de artefacto multilingües
- [ ] Capa de colaboración / sincronización (opt-in)
- [ ] Paridad de instalador en macOS / Linux

### 🤝 Contribuir

Trabaja desde una rama `feature/<nombre>` o `fix/<nombre>` partiendo de `main`. `dotnet build` debe quedar limpio antes de abrir PR; describe brevemente el porqué del cambio. El proyecto tiene **referencias nullable habilitadas**.

### 📄 Licencia

Por definir. Hasta que se añada un archivo de licencia, el código es "todos los derechos reservados" por defecto. Si quieres usarlo, abre un issue y hablamos.

---

<div align="center">

Made with care · *Hecho con cuidado* 🧠
<br/>
**Ontological Studio**

</div>
