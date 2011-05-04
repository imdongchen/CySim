using System;
using System.Collections.Generic;
using System.Reflection;
using log4net;
using Mono.Addins;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Framework.Console;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Services.Interfaces;
using OpenSim.Region.CoreModules.Framework.InterfaceCommander;

[assembly: Addin("CySimRegionModuleTree", "0.1")]
[assembly: AddinDependency("OpenSim", "0.5")]
 
namespace CySim.RegionModules.TreeManager
{
    [Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule", Id = "CySimRegionModuleTree")]
    public class TreeManager : ISharedRegionModule
    {
        private string m_name = "tree manager module";
        private List<Scene> m_SceneList = new List<Scene>();
        private readonly Commander m_commander = new Commander("cysim_tree");
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #region ISharedRegionModule interface
        public string Name
        {
            get { return m_name;}
        }

        public Type ReplaceableInterface
        {
            get { return null;}
        }

        public void Initialise(IConfigSource source)
        {
            Command treeCleanCommand =
                new Command("clean", CommandIntentions.COMMAND_HAZARDOUS, CleanAllTrees, "remove all trees on the selected region");

            m_commander.RegisterCommand("clean", treeCleanCommand);
            m_log.Info("Tree Manager module is initialised.");
            MainConsole.Instance.Commands.AddCommand("cysim_tree",
                true,
                "cysim_tree",
                "cysim_tree",
                "remove all tress from the region",
                HandleTreeManageCommand);
        }

        public void Close()
        {
        }

        public void AddRegion(Scene scene)
        {
            if (!m_SceneList.Contains(scene))
                m_SceneList.Add(scene);
        }

        public void RemoveRegion(Scene scene)
        {
            m_SceneList.Remove(scene);
        }

        public void RegionLoaded(Scene scene)
        {
        }

        public void PostInitialise()
        {
        }

        #endregion

        public void CleanAllTrees(Object[] args)
        {
            m_log.InfoFormat("Clean all trees command is called");
        }

        public void HandleTreeManageCommand(string module, string[] cmd)
        {
            if ((Scene)MainConsole.Instance.ConsoleScene != null)
            {
                foreach (Scene s in m_SceneList)
                    ProcessCommand(s, cmd);
            }
            else
                ProcessCommand((Scene)MainConsole.Instance.ConsoleScene, cmd);
        }

        bool ProcessCommand(Scene scene, string[] cmd)
        {
            if (cmd.Length < 2)
            {
                MainConsole.Instance.Output("Syntax: login enable|disable|status");
                return false;
            }

            switch (cmd[1])
            {
                case "clean":
                    RemoveTrees(scene);
                    break;
                default:
                    return false;
            }

            return true;
        }

        void RemoveTrees(Scene scene)
        {
            foreach (ISceneEntity entity in scene.Entities) 
            {
                if (entity is SceneObjectGroup)
                {
                    PrimitiveBaseShape  shape = ((SceneObjectGroup)entity).RootPart.Shape;
                    
                    if (shape.PCode == (byte)PCode.Tree || shape.PCode == (byte)PCode.NewTree)
                    {
                        scene.DeleteSceneObject((SceneObjectGroup)entity, false);
                    }
                }
            }
        }
    }
}