using System;
using System.Collections.Generic;
using log4net;
using System.Reflection;
using OpenSim.Region.Framework.Scenes;
using System.Drawing;

namespace OpenSim.ApplicationPlugins.WebMap.Layer
{
    public class AvatarLayer
    {
        private Scene m_scene;
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public AvatarLayer(Scene scene)
        {
            m_scene = scene;
        }

        public Bitmap Render(BBoxF range, BBox picSize)
        {
            Bitmap mapImg = new Bitmap(picSize.Width, picSize.Height);
            Graphics gfx = Graphics.FromImage(mapImg);

            gfx.Clear(Color.FromArgb(0, 0, 0, 0));

            Pen pen = new Pen(Color.Red);
            Brush brush = Brushes.Red;

            // draw agent position on the map
            try
            {
                m_scene.ForEachScenePresence(delegate(ScenePresence agent)
                {
                    if (!agent.IsChildAgent)
                    {
                        PointF agentPos = new PointF(agent.AbsolutePosition.X + m_scene.RegionInfo.RegionLocX * 256, agent.AbsolutePosition.Y + m_scene.RegionInfo.RegionLocY * 256);
                        PointF agentImgPos = Utility.Projection(ref agentPos, ref range, picSize);
                        RectangleF rect = new RectangleF(agentImgPos.X, agentImgPos.Y, 20, 20); //point width and height hard coded as 20, should be changed
                        gfx.FillEllipse(brush, rect);
                    }
                }
                );
            }
            catch (Exception)
            {
                throw new Exception("[WebMap]: Agent layer rendering failed");
            }
            gfx.Dispose();
            pen.Dispose();
            return mapImg;
        }
    }
}
