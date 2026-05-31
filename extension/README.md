# Ontological Studio Extension

VSCode / TRAE-compatible extension for the local Ontological Studio API.

## Commands

- `Ontological Studio: Health Check`
- `Ontological Studio: List Universes`
- `Ontological Studio: Solve Scenario`

## Local API

The extension expects the API to be available at:

- `http://127.0.0.1:53821`

Start it with:

```powershell
dotnet run --project OntologicalStudio.Api
```

## Package

```powershell
cd extension
npm install
npx vsce package
```

Then install the generated `.vsix` in VSCode or TRAE.