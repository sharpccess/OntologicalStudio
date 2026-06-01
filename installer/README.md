# Installer

Carpeta para generar el instalador Windows de **Ontological Studio**.

## Requisitos previos (una sola vez)

1. **.NET 8 SDK** — https://dotnet.microsoft.com/download/dotnet/8.0
2. **NSIS 3.x** — https://nsis.sourceforge.io/Download
   - Por defecto se instala en `C:\Program Files (x86)\NSIS\` y el `.bat` lo encuentra automáticamente.

## Cómo generar el instalador

Doble clic en **`build_installer.bat`**, o desde una terminal en esta carpeta:

```cmd
.\build_installer.bat            REM versión por defecto 0.1.0
.\build_installer.bat 0.4.0      REM versión personalizada
```

El proceso hace:

1. `dotnet publish` self-contained para `win-x64` Release → carpeta `publish/`.
2. `makensis OntologicalStudio.nsi` → produce `OntologicalStudio-Setup-<version>.exe`.

Si todo va bien, al final tendrás:

```
installer/
  publish/                              (output del publish, intermedio)
  OntologicalStudio-Setup-0.1.0.exe     <-- el instalador final
```

## Qué hace el instalador

- Es **per-user**, no requiere permisos de administrador.
- Instala en `%LOCALAPPDATA%\Programs\OntologicalStudio\`.
- Crea accesos directos en **Escritorio** y **Menú Inicio**.
- Aparece en **"Agregar o quitar programas"** con icono, versión y editor.
- Ofrece desinstalación limpia (no toca `%LOCALAPPDATA%\OntologicalStudio\` con tus datos de usuario; eso se borra manualmente si quieres).

## Personalización rápida

Edita `OntologicalStudio.nsi` y cambia las constantes de la cabecera:

```nsi
!define APPNAME           "Ontological Studio"
!define COMPANYNAME       "OntologicalStudio"
!define VERSION           "0.1.0"
!define EXECUTABLE        "OntologicalStudio.Desktop.exe"
```

> Para cambiar la versión, prefiere pasarla por argumento al `.bat`; eso la propaga tanto al ensamblado como al instalador.

## Limpieza

Si quieres dejar la carpeta limpia entre builds:

```cmd
rmdir /s /q publish
del /q OntologicalStudio-Setup-*.exe
```

El `.bat` ya hace esto al principio de cada ejecución.
