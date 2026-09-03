@echo off

SET TargetDir=%1
SET TargetSrc=%~dp0\Assets\Plugins\
SET AOT_LIB=%~dp0\Assets\Library\

rem rd /S/Q %AOT_LIB%
rem xcopy /Y/E /EXCLUDE:%~dp0\copydll.exclude.txt            %TargetDir%\DeepCore.???    %AOT_LIB%
rem xcopy /Y/E /EXCLUDE:%~dp0\copydll.exclude.txt            %TargetDir%\DeepCore.*.???  %AOT_LIB%
rem xcopy /Y/E /EXCLUDE:%~dp0\copydll.exclude.packages.txt   %TargetSrc%\*               %TargetDir%\Assets\Plugins\
