:: Create junctions (symlinks) to binary references to account for different installation directories on different systems.
:: Use the dot (.) prefix so dotnet ignores the folder during build.

@echo off
set /p path="Please enter the folder location of your SpaceEngineers.exe or SpaceEngineersDedicated.exe: "

cd %~dp0
rmdir .GameBinaries > nul 2>&1
mklink /J .GameBinaries "%path%"
if errorlevel 1 goto Error
echo Done!

echo You can now open the plugin without issue.
goto EndFinal

:Error
echo An error occured creating the symlink.
goto EndFinal

:EndFinal
pause
