using DeepCore;
using DeepCore.Game3D.Host;
using DeepCore.Game3D.Host.ZoneRuntime;
using DeepCore.Game3D.Slave;
using DeepMetaGame.Data.ZoneEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeepMetaGame.Tools.BattleEmulator
{
    public static class HostEmulatorLauncher
    {
        public static void MainLoop(ZoneHostFactory host, ZoneSlaveFactory slave, DirectoryInfo dataDir, params string[] args)
        {
            var pargs = Properties.ParseArgs(args);
            var dataroot = Data.ZoneDataFactory.Factory.CreateEditorTemplates(dataDir.FullName);
            dataroot.LoadAllTemplates();
            if (pargs.TryGetAsInt("-scene", out var _sceneID))
            {
                var sceneData = dataroot.LoadScene(_sceneID, false, false, true);
                var emu = new HostEmulator(host, slave, dataroot, sceneData, pargs);
                emu.MainLoop();
            }
            else
            {
                Console.WriteLine("需要参数 -scene <场景ID>");
            }
        }
        public static void MainLoop(ZoneHostFactory host, ZoneSlaveFactory slave, DirectoryInfo dataDir, InstanceBattle battle, params string[] args)
        {
            var pargs = Properties.ParseArgs(args);
            var dataroot = Data.ZoneDataFactory.Factory.CreateEditorTemplates(dataDir.FullName);
            dataroot.LoadAllTemplates();
            var emu = new HostEmulator(host, slave, dataroot, pargs, battle);
            emu.MainLoop();
        }
    }
}
