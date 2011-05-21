using System;
using System.Collections.Generic;
using OpenSim.Region.Framework.Scenes;
using System.Drawing;
using log4net;
using System.Reflection;
using OpenSim.Region.Framework.Interfaces;
using System.Drawing.Drawing2D;

namespace OpenSim.ApplicationPlugins.WebMap.Layer
{
    public class TerrainLayer
    {
        private Scene m_scene;
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public TerrainLayer(Scene scene)
        {
            m_scene = scene;
        }

        public Bitmap Render(BBoxF range, BBox picSize)
        {
            float minX = range.MinX - m_scene.RegionInfo.RegionLocX * 256;
            float maxY = range.MaxY - m_scene.RegionInfo.RegionLocY * 256 - 1;
            Color[] colors = getElevationColors();
            int pallete = colors.Length;
            ITerrainChannel heightMap = m_scene.Heightmap;
            Bitmap bmp = new Bitmap((int)range.Width, (int)range.Height);
            for (int y = 0; y < (int)range.Height; y++)
            {
                int regionY = (int)maxY - y;
                for (int x = 0; x < (int)range.Width; x++)
                {
                    int regionX = x + (int)minX;
                    // 512 is the largest possible height before colors clamp
                    int colorindex = (int)(Math.Max(Math.Min(1.0, heightMap[regionX, regionY] / 512.0), 0.0) * (pallete - 1));

                    // Handle error conditions
                    if (colorindex > pallete - 1 || colorindex < 0)
                        bmp.SetPixel(x, y, Color.Red);
                    else
                        bmp.SetPixel(x, y, colors[colorindex]);
                }
            }
            Bitmap layerPic = null;
            try
            {
                layerPic = new Bitmap(picSize.Width, picSize.Height);
                using (Graphics g = Graphics.FromImage(layerPic))
                {
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.CompositingQuality = CompositingQuality.HighQuality;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.DrawImage(bmp, new Rectangle(0, 0, picSize.Width, picSize.Height));
                }
            }
            catch
            {
                if (layerPic != null) layerPic.Dispose();
                throw;
            }
            return layerPic;
        }

        private Color[] getElevationColors()
        {
            Bitmap gradientmapLd = null;
            try
            {
                gradientmapLd = new Bitmap("defaultstripe.png");
            }
            catch
            {
                m_log.Error("[WebMap]: Render terrain layer failed: can't find defaultstripe.png!");
            }
            int pallete = gradientmapLd.Height;
            Color[] colors = new Color[pallete];
            for (int i = 0; i < pallete; i++)
            {
                colors[i] = gradientmapLd.GetPixel(0, i);
            }
            return colors;
        }
    }
}
