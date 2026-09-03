// See https://aka.ms/new-console-template for more information
using DeepCore;
using DeepCore.IO;
using DeepCore.Reflection;
using Gate.Server.Launcher;
using static Gate.Server.GateServerManager;

ReflectionUtil.LoadDlls();
// new Test.Codec.TestBattleCodec();
// new Test.Win32.Battle.TestZoneDataFactory();
// new Test.Win32.Battle.TestZoneHostFactory();
// new Test.Win32.Battle.TestZoneSlaveFactory();
var pargs = Properties.ParseArgs(args);
if (CFiles.TryFindParentDirectory(Environment.CurrentDirectory, Path.Combine("Gate.Sample.Win32Editor", "GameEditor"), out var dir))
{
    pargs.Put("GateServerConfig.BattleEditorDir", dir);
}
//pargs.Put($"{nameof(GateServerSingleLauncher.ServiceMapping)}.{ServerNameManager.LogicServiceType}", typeof(TestLogicService).FullName);
var gate = new GateMainLoop(new GateSingleNodeLauncher());
gate.MainLoopGateTest(pargs);