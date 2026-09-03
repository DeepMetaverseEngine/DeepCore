
@SET PROJECT_DIR=%1
@SET SOLUTION_DIR=%2
@SET TARGET_DIR=%3
@SET SERIALIZER_NAME=%4
@SET APPEND_DLLS=%5

@IF EXIST %SOLUTION_DIR%Common\Library\Common (
@xcopy /Y %SOLUTION_DIR%Common\Library\Common\CommonLang.dll    %TARGET_DIR%
@xcopy /Y %SOLUTION_DIR%Common\Library\Common\CommonCodeGen.exe %TARGET_DIR%
)

@echo ---------------------------------------------------------------------------
@echo - GEN %SERIALIZER_NAME%
@echo ---------------------------------------------------------------------------
@IF EXIST %PROJECT_DIR%generated (
@del /Q %PROJECT_DIR%generated\*.cs
@%TARGET_DIR%CommonCodeGen -ns:%SERIALIZER_NAME% -id:%TARGET_DIR% -od:%PROJECT_DIR%generated
@%TARGET_DIR%CommonCodeGen -ns:%SERIALIZER_NAME% -id:%TARGET_DIR% -of:%PROJECT_DIR%generated\codec.cs -t:csharp-code-ids.xml
) ELSE (
@echo Not Exist %PROJECT_DIR%generated
)

@echo ---------------------------------------------------------------------------
@echo - Build : DDogProtocol.Serializer.dll
@echo ---------------------------------------------------------------------------
@IF EXIST %PROJECT_DIR%generated (
@set csc_dir="C:\Windows\Microsoft.NET\Framework64\v3.5"
@IF EXIST %csc_dir% (
@%csc_dir%\csc /target:library /out:%PROJECT_DIR%%SERIALIZER_NAME%.dll %PROJECT_DIR%generated\*.cs /r:System.dll;System.Core.dll;System.Data.dll;%TARGET_DIR%CommonLang.dll;%TARGET_DIR%CommonRPG.dll;%APPEND_DLLS%
) ELSE (
@echo Not Exist %csc_dir%
)
)