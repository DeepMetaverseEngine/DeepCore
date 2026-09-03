@echo off

cd %~dp0

echo Current Path: %~dp0
for %%I in (.) do set "DIR_NAME=%%~nxI"
echo Current Project Name: %DIR_NAME%


SET PROJECT_NAME=%DIR_NAME%

if not exist .git (

echo ----------------------------------------------------------------------
echo ### Make Solution folder ### 
git init
if not exist %PROJECT_NAME%SLN (
    md %PROJECT_NAME%SLN
)
cd %PROJECT_NAME%SLN
echo Clone DeepMeta
git submodule add git@github.com:DeepMetaverseEngine/DeepMeta.git DeepMeta
cd ..
echo Init Project
dotnet %~dp0\%DIR_NAME%SLN\DeepMeta\.outputs\net10.0\gamecli.dll init %~dp0
)

echo ----------------------------------------------------------------------
pause