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
using SharpMap.Geometries;

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
             ProcessCommand(cmd);
        }

        bool ProcessCommand(string[] cmd)
        {
            if (cmd.Length < 2)
            {
                MainConsole.Instance.Output("Syntax: login enable|disable|status");
                return false;
            }

            switch (cmd[1])
            {
                case "clean":
                    if (MainConsole.Instance.ConsoleScene == null)
                    {
                        foreach (Scene scene in m_SceneList)
                            RemoveTrees(scene);
                    }
                    else
                        RemoveTrees((Scene)MainConsole.Instance.ConsoleScene);
                    break;
                case "plant":
                    if (cmd.Length < 3)
                    {
                        MainConsole.Instance.Output("Syntax: cysim plant <shapefile>");
                        return false;
                    }
                    
                    try
                    {
                        PlantTrees(cmd[2]);
                    }
                    catch (Exception e)
                    {
                        m_log.ErrorFormat("Plant trees failed with {0} {1}", e.Message, e.StackTrace);
                    }
                    break;
                default:
                    return false;
            }
            return true;
        }

        private void RemoveTrees(Scene scene)
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

        private void PlantTrees(string shapefile)
        {
            List<Geometry> features;
            List<int> types;
            List<double> heights;
            List<double> intervals;
            List<Point> rasterizedPoints = new List<Point>();
            List<Vector2> treePoints = new List<Vector2>();
            
            int flag = Utility.ReadShapefile(shapefile, out features, out intervals, out types, out heights);
            switch (flag)
            {
                case 0:
                    for (int i = 0, len = features.Count; i < len; i++)
                    {
                        Point p = features[i] as Point;
                        Vector2 newPoint = new Vector2((float)p.X, (float)p.Y);
                        heights[i] += Util.RandomClass.Next((int)(heights[i] * 0.2));
                        AddTree(heights[i], newPoint, types[i]);
                    }
                    break;
                case 1:
                    for (int i = 0, len = features.Count; i < len; i++)
                    {
                        LineString line = features[i] as LineString;
                        List<Point> pointList = Utility.RasterizeLine(line, intervals[i]);
                        foreach (Point p in pointList)
                        {
                            Vector2 newPoint = new Vector2();
                            newPoint.X = (float)(p.X + Util.RandomClass.Next(1));
                            newPoint.Y = (float)(p.Y + Util.RandomClass.Next(1));
                            heights[i] += Util.RandomClass.Next((int)(heights[i] * 0.2));
                            AddTree(heights[i], newPoint, types[i]);
                        }
                    }
                    break;
                case 2:
                    for (int i = 0, len = features.Count; i < len; i++)
                    {
                        Polygon polygon = features[i] as Polygon;
                        List<Point> pointList = Utility.RasterizeArea(polygon, intervals[i]);
                        foreach (Point p in pointList)
                        {
                            Vector2 newPoint = new Vector2();
                            newPoint.X = (float)(p.X + Util.RandomClass.Next(1));
                            newPoint.Y = (float)(p.Y + Util.RandomClass.Next(1));
                            heights[i] += Util.RandomClass.Next((int)(heights[i] * 0.2));
                            AddTree(heights[i], newPoint, types[i]);
                        }
                    }
                    break;
                default:
                    throw new Exception("Shape type not supported!");
            }
        }

        private void AddTree(double height, Vector2 pos, int treeType)
        {
            int x = (int)pos.X / 256;
            int y = (int)pos.Y / 256;
            foreach (Scene scene in m_SceneList)
            {
                uint locX = scene.RegionInfo.RegionLocX;
                uint locY = scene.RegionInfo.RegionLocY;
                if (x == locX && y == locY)
                {
                    float groupX = pos.X - 256 * x;
                    float groupY = pos.Y - 256 * y;
                    float groupZ = (float)scene.Heightmap[(int)groupX, (int)groupY];
                    Vector3 position = new Vector3(groupX, groupY, groupZ);
                    Vector3 scale = new Vector3((float)height, (float)height, (float)height);
                    Quaternion rotation = new Quaternion(0, 0, (float)Util.RandomClass.NextDouble(), (float)Util.RandomClass.NextDouble());
                    PrimitiveBaseShape treeShape = new PrimitiveBaseShape();
                    treeShape.PathCurve = 16;
                    treeShape.PathEnd = 49900;
                    treeShape.PCode = (byte)PCode.Tree; //newTree flag needed or not?
                    treeShape.Scale = scale;
                    treeShape.State = (byte)treeType;
                    scene.AddNewPrim(UUID.Random(), UUID.Random(), position, rotation, treeShape);
                }
            }
        }

    }
}