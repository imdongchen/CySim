using System;
using System.Collections.Generic;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Region.Framework.Interfaces;
using MapRendererCL;
using OpenSim.Framework;
using System.Drawing;
using Nini.Config;
using System.IO;
using System.Reflection;
using log4net;

namespace OpenSim.ApplicationPlugins.WebMap.Layer
{
    public class PrimLayer 
    {
        private List<PrimitiveCL> m_primList;
        private Scene m_scene;
        private BBoxF m_range;
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private string m_texPath;

        public PrimLayer()
        {
        }

        public PrimLayer(Scene scene)
        {
            m_scene = scene;
            IConfigSource configSrc = new IniConfigSource("WebMap.ini");
            IConfig config = configSrc.Configs["WebMap"];
            try
            {
                m_texPath = config.GetString("TexturePath");
            }
            catch (Exception e)
            {
                m_log.Error("[WebMap]: Error with WebMap.ini. Do you miss TexturePath key? " + e.Message + e.StackTrace);
            }
        }

        public void Initialize(BBoxF range)
        {
            m_range = range;
            List<EntityBase> objs = m_scene.GetEntities();
            ITerrainChannel heightMap = m_scene.Heightmap;
            m_primList = new List<PrimitiveCL>();

            lock (objs)
            {
                try
                {
                    foreach (EntityBase obj in objs)
                    {
                        if (obj is SceneObjectGroup)
                        {
                            SceneObjectGroup mapdot = (SceneObjectGroup)obj;
                            foreach (SceneObjectPart part in mapdot.Children.Values)
                            {
                                if (part == null)
                                    continue;
                                OpenMetaverse.Vector3 pos = part.GroupPosition;
                                //Abandon primitives underground
                                if (pos.X < m_range.MinX || pos.Y < m_range.MinY || pos.X >= m_range.MaxX || pos.Y >= m_range.MaxY)
                                    continue;
                                if (pos.Z + part.Scale.Z / 2 < heightMap[(int)pos.X, (int)pos.Y])
                                    continue;

                                LLVector3CL         position      = Utility.toLLVector3(pos);
                                LLQuaternionCL      rotation      = Utility.toLLQuaternion(part.RotationOffset);
                                LLVector3CL         scale         = Utility.toLLVector3(part.Scale);
                                PrimitiveBaseShape  shape         = part.Shape;
                                LLPathParamsCL      pathParams    = new LLPathParamsCL(shape.PathCurve, shape.PathBegin, shape.PathEnd, shape.PathScaleX, shape.PathScaleY, shape.PathShearX, shape.PathShearY, shape.PathTwist, shape.PathTwistBegin, shape.PathRadiusOffset, shape.PathTaperX, shape.PathTaperY, shape.PathRevolutions, shape.PathSkew); 
                                LLProfileParamsCL   profileParams = new LLProfileParamsCL(shape.ProfileCurve, shape.ProfileBegin, shape.ProfileEnd, shape.ProfileHollow);
                                LLVolumeParamsCL    volumeParams  = new LLVolumeParamsCL(profileParams, pathParams);
                                
                                int facenum = part.GetNumberOfSides();
                                List<SimpleColorCL> colors = new List<SimpleColorCL>();
                                for (uint j = 0; j < facenum; j++)
                                {
                                    TextureColorModel data = Utility.GetDataFromFile(m_texPath, shape.Textures.GetFace(j).TextureID.ToString());
                                    colors.Add(new SimpleColorCL(255, data.R, data.G, data.B));
                                }

                                m_primList.Add(new PrimitiveCL(volumeParams, position, rotation, scale, colors.ToArray(), facenum));
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    m_log.ErrorFormat("[WebMapService]: Initialize object layer failed with {0} {1}", e.Message, e.StackTrace);
                }
            }                        
        }

        public void Render(BBoxF range, BBox picSize, string cachePath)
        {
            if (range.MinX < m_range.MinX || range.MinY < m_range.MinY || range.MaxX > m_range.MaxX || range.MaxY > m_range.MaxY)
                throw new Exception("Prim layer render range larger than expected!");

            MapRenderCL mr = new MapRenderCL();
            string regionID = m_scene.RegionInfo.RegionID.ToString();
            try
            {
                if (m_primList.Count == 0)
                {
                    Bitmap map = new Bitmap(picSize.Width, picSize.Height);
                    map.Save(cachePath);
                }
                else
                {
                    mr.mapRender(
                        m_range.MinX, m_range.MinY, 0, m_range.MaxX, m_range.MaxY, 512,
                        m_primList.ToArray(), m_primList.Count,
                        picSize.Width, picSize.Height,
                        cachePath);
                }
            }
            catch (Exception e)
            {
                m_log.Error("Render prim layer failed with " + e.Message + e.StackTrace);
            }
        }

        public void MakeCache(float scale, string cachePath)
        {
            uint locX = m_scene.RegionInfo.RegionLocX;
            uint locY = m_scene.RegionInfo.RegionLocY;
            BBoxF regionRange = new BBoxF(locX * 256, locY * 256, (locX + 1) * 256, (locY + 1) * 256);
            Initialize(regionRange);
            BBox picSize = new BBox(0, 0, (int)(256 * scale), (int)(256 * scale));
            Render(regionRange, picSize, cachePath);
        }
    }
}
