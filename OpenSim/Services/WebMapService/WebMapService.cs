using System;
using OpenSim.Services.Interfaces;
using OpenMetaverse;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;
using log4net;
using Nini.Config;
using OpenSim.Server.Base;
using System.Collections.Generic;
using System.Reflection;
using System.Drawing;
using System.Net;
using System.IO;
using System.Text;
using OpenSim.Framework.Servers.HttpServer;

namespace OpenSim.Services.WebMapService
{
    public class WebMapService : IWebMapService
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        protected IConfigSource m_config;
        private IGridService m_GridService;

        public WebMapService(IConfigSource config)            
        {
            m_log.DebugFormat("[Web Map SERVICE]: Starting...");

            m_config = config;
            IConfig webMapConfig = config.Configs["WebMapService"];
            if (webMapConfig != null)
            {
                string gridService = webMapConfig.GetString("GridServiceModule", String.Empty);

                if (gridService != String.Empty)
                {
                    Object[] args = new Object[] { config };
                    m_GridService = ServerUtils.LoadPlugin<IGridService>(gridService, args);
                }
            }
        }

        #region IWebMapService Members

        public GridRegion GetRegion(float x, float y)
        {
            return m_GridService.GetRegionByPosition(UUID.Zero, (int)x, (int)y);
        }

        public Bitmap MergeImg(Dictionary<Bitmap, BBox> regionMaps, BBox picSize)
        {
            Bitmap newImg = new Bitmap(picSize.Width, picSize.Height);
            Graphics gfx = Graphics.FromImage(newImg);
            foreach (KeyValuePair<Bitmap, BBox> pair in regionMaps)
            {
                Bitmap regionMap = pair.Key;
                Rectangle srcRec = new Rectangle(0, 0, regionMap.Width, regionMap.Height);
                Rectangle desRec = pair.Value.ToRectangle();
                gfx.DrawImage(regionMap, desRec, srcRec, GraphicsUnit.Pixel);
            }
            gfx.Dispose();
            return newImg;
        }

        public Bitmap GetRegionMap(string path, GridRegion region, string[] layers, BBoxF range, BBox picSize, string format)
        {
            UriBuilder ub = new UriBuilder(region.ServerURI);
            ub.Port = (int)region.HttpPort;
            ub.Path = path;
            StringBuilder sb = new StringBuilder();
            string layerString = "";
            for (int i = 0; i < layers.Length; i++)
            {
                layerString += layers[i];
                if (i != layers.Length - 1)
                    layerString += ",";
            }
            sb.AppendFormat("SERVICE=WMS&REQUEST=GetMap&LAYERS={0}&BBOX={1}&HEIGHT={2}&WIDTH={3}&FORMAT={4}",
                layerString, range.ToString(), picSize.Height, picSize.Width, format);
            ub.Query = sb.ToString();
            Uri uri = ub.Uri;
            WebRequest webRequest = WebRequest.Create(uri);
            webRequest.Method = "GET";
            webRequest.Timeout = 5000;
            Bitmap regionMap = null;
            //Send request and get response, which should be an image
            try
            {
                using (WebResponse webResponse = webRequest.GetResponse())
                {
                    Stream stream = webResponse.GetResponseStream();
                    regionMap = new Bitmap(stream);
                }
            }
            //if failed, create a blank image.
            catch (Exception e)
            {
                m_log.Error("can't connect to " + uri + e.Message + e.StackTrace);
                regionMap = CreatePlainImage(picSize);
            }
            return regionMap;
        }

        public void GetWMSParams(OSHttpRequest request, out string[] layers, out string format, out BBoxF range, out BBox picSize)
        {
            layers = null;
            format = null;
            range = null;
            picSize = null;
            try
            {
                layers = request.QueryString["LAYERS"].Split(',');
                format = request.QueryString["FORMAT"];
                range = new BBoxF(request.QueryString["BBOX"]);
                int height = Int32.Parse(request.QueryString["HEIGHT"]);
                int width = Int32.Parse(request.QueryString["WIDTH"]);
                picSize = new BBox(0, 0, width, height);
            }
            catch (Exception e)
            {
                m_log.Error("[WebMap]: Request parameters invalid!");
            }
        }

        public Bitmap CreatePlainImage(BBox picSize)
        {
            Bitmap img = new Bitmap(picSize.Width, picSize.Height);
            Graphics gfx = Graphics.FromImage(img);
            gfx.Clear(Color.CadetBlue);
            gfx.Dispose();
            return img;
        }

        #endregion

        
    }
}
