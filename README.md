# ONTOLOGICAL STUDIO

**A Professional Problem-Solving Studio powered by ontology modeling and AI reasoning.**

---

## 📋 ESTADO ACTUAL

✅ **Arquitectura MVP Completada**  
✅ **Construcción Exitosa**  
⏳ **Listo para Desarrollo de Funcionalidades**

---

## 🏗️ ARQUITECTURA

### Estructura de Proyectos

```
OntologicalStudio.sln
├── OntologicalStudio.Core/              # Modelo de dominio
├── OntologicalStudio.Persistence/       # Acceso a datos (EF Core)
├── OntologicalStudio.Application/       # Servicios de aplicación
├── OntologicalStudio.Infrastructure/    # Infraestructura
├── OntologicalStudio.Desktop/           # UI Avalonia (MVVM)
├── OntologicalStudio.Api/               # API layer
├── OntologicalStudio.AIProviders/       # Proveedores AI
└── OntologicalStudio.Localization/      # Localización (en/es)
```

### Patrón de Arquitectura

- **Clean Architecture** (dependencias inward)
- **Domain-Driven Design**
- **Repository Pattern**
- **MVVM Pattern**

---

## 🚀 INSTRUCCIONES DE DESARROLLO

### Requisitos Previos

- .NET 8.0 SDK
- Visual Studio 2022 o VS Code
- Git

### Configuración Inicial

```bash
# Clonar el repositorio
git clone <repository-url>
cd OntogenesisPlus

# Restaurar dependencias
dotnet restore

# Construir la solución
dotnet build

# Ejecutar la aplicación
dotnet run --project OntologicalStudio.Desktop
```

### Comandos Útiles

```bash
# Construir todo
dotnet build

# Limpiar y reconstruir
dotnet clean && dotnet build

# Ejecutar pruebas
dotnet test

# Ejecutar aplicación de escritorio
dotnet run --project OntologicalStudio.Desktop
```

---

## 📁 ESTRUCTURA DE CÓDIGO

### Core (OntologicalStudio.Core)

**Models/** - Entidades de dominio
- Entity, EntityType, Universe, Scenario
- Relationship, RelationshipType, Tag
- Junction Entities (EntityScenario, EntityTag)

**Interfaces/** - Interfaces de dominio
- Repositories (IEntityRepository, IUniverseRepository, etc.)
- Services (IAIProvider, IAIHydrationService)
- DTOs (HydrationOptions, PromptContext)

### Persistence (OntologicalStudio.Persistence)

**Context/** - DbContext
- ApplicationDbContext (EF Core)

**Repositories/** - Implementaciones
- Repository pattern implementation

### Application (OntologicalStudio.Application)

**Services/** - Servicios de aplicación
- UniverseService, EntityService, ScenarioService
- RelationshipService, AIHydrationService

**DTOs/** - Objetos de Transferencia de Datos
- EntityDto, UniverseDto, ScenarioDto, etc.

### Infrastructure (OntologicalStudio.Infrastructure)

**DependencyInjection.cs** - Configuración DI
- AddInfrastructure()
- AddLocalization()

### Desktop (OntologicalStudio.Desktop)

**Views/** - Vistas Avalonia
- MainWindow.axaml

**ViewModels/** - ViewModels MVVM
- MainWindowViewModel, ViewModelBase

**Resources/** - Recursos
- Estilos, assets

### Localization (OntologicalStudio.Localization)

**Languages/** - Archivos de idioma
- en.json (inglés - por defecto)
- es.json (español)

**Services/** - Servicios de localización
- ILocalizationService
- LocalizationService

---

## 🌐 LOCALIZACIÓN

### Idiomas Soportados

- **English (en)** - Por defecto
- **Español (es)** - Español

### Agregar Nuevo Idioma

1. Crear archivo `Languages/[codigo].json`
2. Copiar estructura de `en.json`
3. Traducir todas las claves
4. El sistema detectará automáticamente el nuevo idioma

### Cambiar Idioma en Tiempo de Ejecución

```csharp
// En cualquier parte del código
localizationService.ChangeLanguage("es");
```

---

## 🤖 INTEGRACIÓN AI

### Proveedores AI

**IAIProvider Interface:**
- ProviderName
- HydrateEntityAsync()
- SuggestRelationshipsAsync()
- GeneratePromptAsync()
- ValidateConfigurationAsync()

### Implementar Nuevo Proveedor

1. Crear clase que implemente IAIProvider
2. Implementar métodos de hidratación
3. Registrar en DependencyInjection
4. Configurar API keys

### Proveedores Pendientes

- OpenAI
- Anthropic
- Ollama

---

## 📊 BASE DE DATOS

### Esquema (SQLite)

**Tablas Principales:**
- EntityType, Entity, RelationshipType, Relationship
- Universe, Scenario, Tag
- EntityScenario, EntityTag

### Migraciones

```bash
# Crear migración
dotnet ef migrations add [NombreMigracion]

# Actualizar base de datos
dotnet ef database update
```

---

## 🎨 UI FRAMEWORK

### Avalonia UI 12.0.0

**Características:**
- Multiplataforma (Windows, macOS, Linux)
- XAML-based
- ReactiveUI integration
- Fluent Theme

### MVVM Pattern

**ViewModels:**
- Heredan de ViewModelBase (ReactiveObject)
- Propiedades con RaiseAndSetIfChanged
- Commands con ReactiveCommand

**Views:**
- XAML files
- Data binding
- Commands binding

---

## 📝 PRÓXIMOS PASOS

### Fase 2: Espacio de Trabajo Visual
- Integrar NodeEditorAvalonia 12.0.0
- Lienzo visual con drag/drop
- Nodos de entidades
- Conexiones de relaciones
- Zoom y pan

### Fase 3: Biblioteca de Entidades
- Sistema de plantillas
- Importar/Exportar
- Componentes reutilizables

### Fase 4: Motor de Informes
- Integrar QuestPDF 2024.3.0
- Plantillas de informes
- Generación de PDF
- Exportación

### Fase 5: Proveedores AI
- OpenAI provider
- Anthropic provider
- Ollama provider

---

## 🐛 RESOLUCIÓN DE PROBLEMAS

### Errores de Construcción

```bash
# Limpiar y reconstruir
dotnet clean && dotnet build

# Restaurar dependencias
dotnet restore
```

### Errores de Referencia de Proyecto

Verificar que los proyectos estén en la solución:
```bash
dotnet sln list
```

### Errores de NuGet

```bash
# Limpiar cache de NuGet
dotnet nuget locals all --clear
dotnet restore
```

---

## 📚 DOCUMENTACIÓN

### Archivos de Documentación

- **PROJECT_STATUS_REPORT.md** - Reporte técnico en inglés
- **PROJECT_STATUS.json** - Estado en formato JSON
- **INFORME_ESTADO_PROYECTO.md** - Reporte técnico en español
- **README.md** - Este archivo

### Estructura de Archivos

```
OntologicalStudio/
├── PROJECT_STATUS_REPORT.md
├── PROJECT_STATUS.json
├── INFORME_ESTADO_PROYECTO.md
├── README.md
└── [proyectos...]
```

---

## 👥 COLABORACIÓN

### Convenciones de Código

- **C# 12.0** con referencias nulas habilitadas
- **Clean Architecture** - Dependencias inward
- **MVVM** - Separación clara de concerns
- **Repository Pattern** - Abstracción de datos

### Flujo de Trabajo

1. Crear rama feature/[nombre]
2. Implementar funcionalidad
3. Construir y probar
4. Crear pull request
5. Revisión y merge

---

## 📄 LICENCIA

[Por definir]

---

## 🙏 AGRADECIMIENTOS

- Avalonia UI Team
- Entity Framework Core Team
- ReactiveUI Team
- Comunidad .NET

---

**Versión:** 0.1.0  
**Estado:** MVP Architecture Complete  
**Última actualización:** 2026-05-29
