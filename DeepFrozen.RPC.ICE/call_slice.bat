SET PROJECT_DIR=%~dp0\
SET ICE_ROOT=%USERPROFILE%\.nuget\packages\zeroc.ice.net\3.7.10
SET SLICE_EXE=%ICE_ROOT%\tools\slice2cs -I%ICE_ROOT%\slice\ --underscore --output-dir %PROJECT_DIR%generated

ECHO ICE_ROOT=%ICE_ROOT%
IF EXIST %ICE_ROOT% (
ECHO ---------------------------------------------------------------------------
IF NOT EXIST %PROJECT_DIR%generated MD %PROJECT_DIR%generated

FOR %%i IN (%PROJECT_DIR%slice\*.ice) DO (
%SLICE_EXE% %%i
ECHO %%i CSC: %ERRORLEVEL%
)

ECHO ---------------------------------------------------------------------------
)