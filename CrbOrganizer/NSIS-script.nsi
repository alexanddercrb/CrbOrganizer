; Define installer information
Name "Crb Organizer Installer"
OutFile "$DESKTOP\CrbOrganizerInstaller.exe"
InstallDir "$PROGRAMFILES\CrbOrganizer"
RequestExecutionLevel admin

; Define sections
Section "Install Crb Organizer" SecInstall

    ; Create the installation directory in Program Files
    SetOutPath "$INSTDIR"

    ; Copy all files from the win-x64 folder
    File /r "C:\Users\Administrator\Desktop\win-x64\*"
    DetailPrint "All files from win-x64 copied to $INSTDIR."

    ; Create a desktop shortcut
    CreateShortcut "$DESKTOP\Crb Organizer.lnk" "$INSTDIR\CrbOrganizer.exe"
    DetailPrint "Desktop shortcut created (optional)."

SectionEnd

; Optional cleanup or finalization
Section -PostInstall
    DetailPrint "Installation completed successfully."
SectionEnd
