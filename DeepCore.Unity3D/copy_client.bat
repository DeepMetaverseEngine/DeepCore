@SET UnityAssets=%1

@xcopy /Y /S %~dp0\Src\*.cs            %UnityAssets%Import\DeepCore.Unity3D\Src\
@xcopy /Y /S %~dp0\Platform\*.*        %UnityAssets%Plugins\DeepCore.Unity3D\Platform\
@xcopy /Y /S %~dp0\Shaders\*.shader        %UnityAssets%Resources\Shaders\