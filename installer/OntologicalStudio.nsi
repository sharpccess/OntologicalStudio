; ============================================================
;  Ontological Studio - NSIS installer script
;  Builds a per-user installer (no admin rights needed)
;  Produces: OntologicalStudio-Setup-<version>.exe
; ============================================================

Unicode true

; ---------- Configurable metadata ----------
!define APPNAME           "Ontological Studio"
!define COMPANYNAME       "OntologicalStudio"
!define VERSION           "0.1.0"
!define EXECUTABLE        "OntologicalStudio.Desktop.exe"
!define PUBLISHDIR        "publish"
!define UNINSTREGKEY      "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}"
!define COMPANYREGKEY     "Software\${COMPANYNAME}"

; The /DVERSION argument from the .bat overrides VERSION above when present
!ifdef OVERRIDE_VERSION
  !undef  VERSION
  !define VERSION "${OVERRIDE_VERSION}"
!endif

; ---------- Compiler settings ----------
Name                "${APPNAME}"
OutFile             "OntologicalStudio-Setup-${VERSION}.exe"
InstallDir          "$LOCALAPPDATA\Programs\${COMPANYNAME}"
InstallDirRegKey    HKCU "${COMPANYREGKEY}" "InstallDir"
RequestExecutionLevel user
SetCompressor       /SOLID lzma
ShowInstDetails     show
ShowUnInstDetails   show
BrandingText        "${APPNAME} v${VERSION}"

VIProductVersion        "${VERSION}.0"
VIAddVersionKey         "ProductName"     "${APPNAME}"
VIAddVersionKey         "CompanyName"     "${COMPANYNAME}"
VIAddVersionKey         "FileDescription" "${APPNAME} installer"
VIAddVersionKey         "FileVersion"     "${VERSION}.0"
VIAddVersionKey         "ProductVersion"  "${VERSION}.0"

; ---------- Modern UI 2 ----------
!include "MUI2.nsh"
!include "FileFunc.nsh"

!define MUI_ABORTWARNING
!define MUI_ICON   "..\OntologicalStudio.Desktop\Assets\app-icon.ico"
!define MUI_UNICON "..\OntologicalStudio.Desktop\Assets\app-icon.ico"

; Pages
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES

!define MUI_FINISHPAGE_RUN "$INSTDIR\${EXECUTABLE}"
!define MUI_FINISHPAGE_RUN_TEXT "Launch ${APPNAME} now"
!define MUI_FINISHPAGE_LINK "Project homepage"
!define MUI_FINISHPAGE_LINK_LOCATION "https://github.com/"
!insertmacro MUI_PAGE_FINISH

; Uninstall pages
!insertmacro MUI_UNPAGE_WELCOME
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_UNPAGE_FINISH

; Languages
!insertmacro MUI_LANGUAGE "English"
!insertmacro MUI_LANGUAGE "Spanish"

; ============================================================
;  Install
; ============================================================
Section "Install"
    SetOutPath "$INSTDIR"

    ; If a previous install exists, stop the running executable so we can overwrite.
    nsExec::ExecToLog 'taskkill /F /IM "${EXECUTABLE}" /T'

    ; Copy everything from the published folder
    File /r "${PUBLISHDIR}\*"

    ; --- Shortcuts ---
    CreateDirectory "$SMPROGRAMS\${APPNAME}"
    CreateShortCut  "$SMPROGRAMS\${APPNAME}\${APPNAME}.lnk"  "$INSTDIR\${EXECUTABLE}"  ""  "$INSTDIR\${EXECUTABLE}"  0
    CreateShortCut  "$SMPROGRAMS\${APPNAME}\Uninstall.lnk"   "$INSTDIR\Uninstall.exe"
    CreateShortCut  "$DESKTOP\${APPNAME}.lnk"                "$INSTDIR\${EXECUTABLE}"  ""  "$INSTDIR\${EXECUTABLE}"  0

    ; --- Registry entries for Add/Remove Programs (per-user) ---
    WriteRegStr   HKCU "${COMPANYREGKEY}"  "InstallDir"           "$INSTDIR"
    WriteRegStr   HKCU "${COMPANYREGKEY}"  "Version"              "${VERSION}"

    WriteRegStr   HKCU "${UNINSTREGKEY}"   "DisplayName"          "${APPNAME}"
    WriteRegStr   HKCU "${UNINSTREGKEY}"   "DisplayVersion"       "${VERSION}"
    WriteRegStr   HKCU "${UNINSTREGKEY}"   "Publisher"            "${COMPANYNAME}"
    WriteRegStr   HKCU "${UNINSTREGKEY}"   "InstallLocation"      "$INSTDIR"
    WriteRegStr   HKCU "${UNINSTREGKEY}"   "DisplayIcon"          "$INSTDIR\${EXECUTABLE}"
    WriteRegStr   HKCU "${UNINSTREGKEY}"   "UninstallString"      "$\"$INSTDIR\Uninstall.exe$\""
    WriteRegStr   HKCU "${UNINSTREGKEY}"   "QuietUninstallString" "$\"$INSTDIR\Uninstall.exe$\" /S"
    WriteRegDWORD HKCU "${UNINSTREGKEY}"   "NoModify"             1
    WriteRegDWORD HKCU "${UNINSTREGKEY}"   "NoRepair"             1

    ; Estimated size (in KB) computed at install time from the actual folder
    ${GetSize} "$INSTDIR" "/S=0K" $0 $1 $2
    IntFmt $0 "0x%08X" $0
    WriteRegDWORD HKCU "${UNINSTREGKEY}" "EstimatedSize" "$0"

    WriteUninstaller "$INSTDIR\Uninstall.exe"
SectionEnd

; ============================================================
;  Uninstall
; ============================================================
Section "Uninstall"
    ; Stop the app if running
    nsExec::ExecToLog 'taskkill /F /IM "${EXECUTABLE}" /T'

    ; Remove shortcuts
    Delete "$SMPROGRAMS\${APPNAME}\${APPNAME}.lnk"
    Delete "$SMPROGRAMS\${APPNAME}\Uninstall.lnk"
    RMDir  "$SMPROGRAMS\${APPNAME}"
    Delete "$DESKTOP\${APPNAME}.lnk"

    ; Remove install dir (everything we placed)
    RMDir /r "$INSTDIR"

    ; Remove registry entries
    DeleteRegKey HKCU "${COMPANYREGKEY}"
    DeleteRegKey HKCU "${UNINSTREGKEY}"

    ; NOTE: user data at %LOCALAPPDATA%\OntologicalStudio is NOT deleted on purpose.
    ; If you want to wipe it too, uncomment:
    ; RMDir /r "$LOCALAPPDATA\OntologicalStudio"
SectionEnd

