# Ontological Studio — Roadmap

> Ontology-Augmented Reasoning System.
> Cross-platform desktop (.NET 8 + Avalonia MVVM) + future IDE extension layer.

---

## Estado actual (baseline)

- [x] Arquitectura modular: `Core`, `Application`, `Persistence`, `Infrastructure`, `AIProviders`, `Desktop`, `Api`, `Localization`.
- [x] Modelo de dominio MVP: `Universe`, `Entity`, `EntityType`, `Relationship`, `RelationshipType`, `Scenario`, `Tag`.
- [x] Persistencia EF Core + SQLite con migration inicial y seeder.
- [x] Repositorios con `SaveChangesAsync()` correcto.
- [x] Desktop Avalonia MVVM con tabs: Universes / Entities / Relationships / Scenarios / Prompt Preview.
- [x] `IAIProvider` con `ConfigurableAIProvider` (OpenAI / Anthropic / Ollama / fallback heurístico).

---

## Fase A — Solutions multimodal *(MVP completado)*

**Objetivo:** Cada `Scenario` puede ejecutarse contra un proveedor de IA y producir 0..N `Solution`. Cada `Solution` guarda el prompt enviado, el proveedor usado y 1..N artefactos (texto / imagen / archivo).

### Modelo de dominio (Core)
- [x] `Solution` *(EntityBase)*
  - `Title`, `ScenarioId`, `PromptSnapshot`, `ProviderUsed`, `ModelUsed`,
  - `Status` (`Draft` | `Final` | `Archived`), `Rating` (0–5), `Notes`.
- [x] `SolutionArtifact` *(EntityBase)*
  - `SolutionId`, `Kind` (`Text` | `Markdown` | `Image` | `File` | `Json`),
  - `MimeType`, `InlineContent` (TEXT, para texto/markdown/json),
  - `BlobPath` (para binarios), `SizeBytes`, `Order`.
- [x] Enums `SolutionStatus`, `ArtifactKind`.

### Persistencia
- [x] `DbSet<Solution>` + `DbSet<SolutionArtifact>` en `ApplicationDbContext`.
- [x] Configuraciones EF + relaciones `Scenario 1..N Solution 1..N Artifact`.
- [x] Migration `AddSolutions`.

### Almacenamiento de blobs
- [x] `IBlobStore` (Core).
- [x] `FileSystemBlobStore` (Infrastructure) en `%LocalAppData%/OntologicalStudio/blobs/{guid}`.

### Application
- [x] Extraer la lógica de construcción de prompt de `PromptPreviewViewModel` a `IPromptBuilder` / `PromptBuilder` reutilizable.
- [x] `ISolutionService` + `SolutionService`:
  - `Task<Solution> RunAsync(Guid scenarioId, string? extraInstructions, CancellationToken ct)`
  - `Task<IEnumerable<Solution>> GetByScenarioAsync(Guid scenarioId)`
  - `Task DeleteAsync(Guid id)`
  - `Task UpdateRatingAsync(Guid id, int rating)`
- [x] Repositorios: `ISolutionRepository` + `SolutionRepository` (con SaveChangesAsync).

### DI (Infrastructure)
- [x] Registrar `IBlobStore`, `ISolutionRepository`, `ISolutionService`, `IPromptBuilder`.

### UI (Desktop)
- [x] `SolutionsViewModel` enlazado al escenario seleccionado.
- [x] Sub-panel en `ScenariosView`: lista de soluciones + botón **Run** + visor de artefactos (texto y, si es imagen, preview).
- [ ] Acciones: borrar solución, calificar, abrir artefacto en explorador.

### Acceptance
- [x] Crear universo + entidades + escenario → click **Run** → se persiste 1 `Solution` con artefacto de texto.
- [x] La solución sobrevive al reinicio de la app.
- [x] Si no hay API key, se usa fallback heurístico y la solución se guarda igual.

---

## Fase B — Canvas gráfico del Universo *(MVP completado)*

**Objetivo:** Sustituir (o complementar) la lista de Entities/Relationships por un canvas visual.

- [x] `UniverseCanvasView` con canvas visual MVP. Zoom (Ctrl+wheel) + pan (middle-button drag).
- [ ] Persistir `x`,`y` en `Entity.Metadata` (JSON). No se modifica esquema.
- [x] Drag-create: doble click en vacío → diálogo/selector de tipo MVP desde panel lateral.
- [x] Drag-link: selección visual de origen/destino desde el canvas en *link mode* → crea `Relationship`.
- [x] Hit-test + selección. Borrado por botón **Delete selected node** y tecla DEL.
- [ ] Auto-layout opcional (Sugiyama / dagre-port / fuerza).
- [x] Canvas agregado como vista principal complementaria del universo en pestaña dedicada `Universe Canvas`.

---

## Fase C — Hidratación con preview/diff *(en curso)*

**Objetivo:** Hidratar entidades con resultado de IA, controlado por el usuario (apply selectivo).

- [x] Modelo `HydrationLog` (EntityId, PromptUsed, ProviderUsed, RawResponse, AppliedFields[], CreatedAt).
- [~] Diálogo de hidratación:
  - [x] Prompt editable, pre-rellenado por template (Person / Company / Belief / …).
  - [ ] Streaming de respuesta.
  - [x] Diff visual entre estado actual y propuesta de la IA.
  - [x] Apply por campo (checkboxes).
- [x] Historial por entidad.
- [ ] Refactor `IAIProvider`:
  - `IAsyncEnumerable<AIChunk> StreamAsync(AIRequest req, CancellationToken ct)`
  - Chunks: `TextChunk`, `ImageChunk`, `FileChunk`, `DoneChunk(InputTokens, OutputTokens)`.
  - `GeneratePromptAsync` queda como wrapper.

---

## Fase D — Local API + extensión VSCode

**Objetivo:** El núcleo expone una API local para que una extensión IDE pueda mandar prompts y recibir respuestas, usando el LLM del IDE (`vscode.lm`).

### Backend
- [ ] Activar `OntologicalStudio.Api` (Kestrel embebido en Desktop, puerto efímero `127.0.0.1:0`).
- [ ] Token de sesión escrito en `~/.ontological-studio/session.json`.
- [ ] Endpoints:
  - `GET  /universes`, `GET /universes/{id}` (con entidades+relaciones).
  - `POST /entities/{id}/hydrate` → SSE stream de `AIChunk`.
  - `POST /scenarios/{id}/solve` → SSE stream + persiste `Solution`.
  - `POST /bridge/responses` (callback de la extensión cuando usa `vscode.lm`).
- [ ] `IdeBridgeAIProvider`: implementación de `IAIProvider` que despacha a un cliente conectado vía SSE/WebSocket y espera respuesta.

### Extensión VSCode (TypeScript, repo separado o `extension/` en monorepo)
- [ ] Comando: **Ontological Studio: Open Universe** (panel webview).
- [ ] Comando: **Ontological Studio: Hydrate Selection With LLM** (usa `vscode.lm.selectChatModels` + `sendRequest`).
- [ ] Comando: **Ontological Studio: Solve Scenario**.
- [ ] Auth via token leído del archivo de sesión.
- [ ] Cliente HTTP/SSE → backend.

---

## Fase E — Forks y otros IDEs

- [ ] Validar que la extensión funciona en TRAE / Cursor / Windsurf (forks de VSCode). Caso a caso.
- [ ] En IDEs sin `vscode.lm`, fallback a API key externa (proveedor configurable que ya existe).
- [ ] Verdent: investigar API real cuando la documenten.

---

## Riesgos y decisiones abiertas

- **Streaming a la UI**: para Fase C/D necesitaremos SignalR o `IAsyncEnumerable` directo a VM. Decidir antes de Fase C.
- **Multimodal real**: la API de OpenAI/Anthropic devuelve imágenes via URL; descargar y guardar como blob requiere cuidar tamaño / mime.
- **Canvas performance**: si universos crecen >100 nodos, evaluar `NodeNetwork` o webview con Cytoscape.
- **Auth de extensión**: token simple con expiración por sesión; rotar si se reinicia el desktop.

---

## Convenciones del proyecto

- Repositorios: siempre `await _context.SaveChangesAsync()` al final de Add/Update/Delete.
- VMs en Desktop usan `ScopedRunner` para abrir un scope de DI por operación (DbContext es Scoped).
- Toda nueva entidad de dominio hereda `EntityBase` (Id, CreatedAt, UpdatedAt, IsDeleted).
- Avalonia 11.2: **no** usar `RowSpacing` / `ColumnSpacing` en `Grid` (no soportado). Usar `Margin`.
- Migrations: `dotnet ef migrations add <Name> -p OntologicalStudio.Persistence -s OntologicalStudio.Desktop`.
