using System;
using System.Collections.Generic;
using log4net;
using System.Reflection;
using Nini.Config;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Framework;
using System.Threading;
using OpenSim.Region.Framework.Scenes;
using OpenSim.ApplicationPlugins.WebMap.Layer;
using System.Drawing;
using System.IO;
using OpenSim.Region.CoreModules.Framework.InterfaceCommander;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Framework.Console;

namespace OpenSim.ApplicationPlugins.WebMap
{
    public class WebMap : IApplicationPlugin
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private string m_name = "WebMap";
        private string m_version = "0.0";
        protected OpenSimBase m_openSim;
        private BaseHttpServer m_server;
        private int m_texUpdateInterval;
        private List<Scene> m_sceneList;
        private PrimLayer m_primLayer;
        private TerrainLayer m_terrainLayer;
        private AvatarLayer m_avatarLayer;
        private int m_zoomLevel;
        private int m_minMapSize;
        private int m_mapUpdateInterval;
        private string m_remoteConnectionString;
        private string m_cachePath;
        private string m_texPath;
        private readonly Commander m_commander = new Commander("cysim_map");

        #region IPlugin Members

        public string Version
        {
            get { return m_version; }
        }

        public string Name
        {
            get { return m_name; }
        }

        public void Initialise()
        {
            m_log.Error("[APPPLUGIN]: " + Name + " cannot be default-initialized!");
            throw new PluginNotInitialisedException(Name);
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
        }

        #endregion

        #region IApplicationPlugin Members

        public void Initialise(OpenSimBase openSim)
        {
            m_openSim = openSim;
            m_server = openSim.HttpServer;
            m_sceneList = m_openSim.SceneManager.Scenes;
             
            IConfigSource configSrc = new IniConfigSource("WebMap.ini");
            IConfig config = configSrc.Configs["WebMap"];
            try
            {
                m_texUpdateInterval = config.GetInt("TextureUpdateInterval");
                m_zoomLevel = config.GetInt("ZoomLevel");
                m_minMapSize = config.GetInt("MinMapSize");
                m_mapUpdateInterval = config.GetInt("MapUpdateInterval");
                m_remoteConnectionString = config.GetString("RemoteConnectionString");
                m_cachePath = config.GetString("CachePath");
                m_texPath = config.GetString("TexturePath");
                if (!Directory.Exists(m_cachePath))
                    Directory.CreateDirectory(m_cachePath);
                if (!Directory.Exists(m_texPath))
                    Directory.CreateDirectory(m_texPath);
            }
            catch (Exception e)
            {
                m_log.Error("[WebMap]: Error with WebMap.ini, " + e.Message + e.StackTrace);
            }

            new Thread(getTextureDataThread).Start();

            Command mapCommand =
                new Command("cysim_map", CommandIntentions.COMMAND_HAZARDOUS, mapCommandPrompt, "map cache and texture update");
            m_commander.RegisterCommand("cysim_map", mapCommand);
            MainConsole.Instance.Commands.AddCommand("cysim_map",
                true,
                "cysim_map",
                "cysim_map",
                "map cache and texture update",
                HandleMapCommand);
            m_log.Info("[WebMap]: initialized!");
        }

        public void PostInitialise()
        {
            new Thread(makeCacheThread).Start();
            OWSStreamHandler h = new OWSStreamHandler("GET", "/map", owsHandler);
            m_server.AddStreamHandler(h);
        }

        #endregion

        private void mapCommandPrompt(object[] args)
        {
            m_log.InfoFormat("map command is called");
        }

        public void HandleMapCommand(string module, string[] cmd)
        {
            ProcessCommand(cmd);
        }
        private void ProcessCommand(string[] cmd)
        {
            if (cmd.Length < 3 || cmd[1] != "update")
            {
                MainConsole.Instance.Output("Syntax: cysim_map update texture|map");
                return;
            }
            switch (cmd[2])
            {
                case "texture":
                    getTextureData();
                    break;
                case "map":
                    makeCache();
                    break;
            }
        }

        public string owsHandler(string request, string path, string param,
                              OSHttpRequest httpRequest, OSHttpResponse httpResponse)
        {
            switch (httpRequest.QueryString["SERVICE"])
            {
                case "WMS":
                    if (httpRequest.QueryString["REQUEST"] == "GetMap")
                    { 
                        string[] layers;
                        BBox picSize;
                        string format;
                        BBoxF range;
                        getWMSParams(httpRequest, out layers, out format, out range, out picSize);
                        Bitmap map = getMap(layers, range, picSize);
                        httpResponse.ContentType = "image/png";
                        string rel = Utility.ConvertToString(map);
                        map.Dispose();
                        return rel;
                    }
                    else if (httpRequest.QueryString["REQUEST"] == "GetCapabilities")
                    {
                        httpResponse.ContentType = "text/xml";
                        string capDes = "";
                        TextReader textReader = new StreamReader("WMS_Capability.xml");
                        capDes = textReader.ReadToEnd();
                        textReader.Close();
                        return capDes;
                    }
                    else
                    {
                        m_log.Error("[WebMap]: Request parameter 'REQUEST' invalid");
                        return "Sorry, the request method is not supported by this service.";
                    }
                case "WFS":
                    return null;
                default:
                    m_log.Error("[WebMap]: Requested service invalid!");
                    return "Service not yet supported!";
            }
        }

        private void getWMSParams(OSHttpRequest request, out string[] layers, out string format, out BBoxF range, out BBox picSize)
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

        private Bitmap getMap(string[] layers, BBoxF range, BBox picSize)
        {
            List<Bitmap> layerImgs = new List<Bitmap>();
            Scene scene = getScene(range);
            if (scene == null)
            {
                m_log.Error("[WebMap]: Requested bounding box is not within existed regions!");
                return new Bitmap(picSize.Width, picSize.Height);
            }
            for (int i = 0, len = layers.Length; i < len; i++)
            {
                if (layers[i] == "terrain")
                {
                    m_terrainLayer = new TerrainLayer(scene);
                    layerImgs.Add(m_terrainLayer.Render(range, picSize));
                }
                if (layers[i] == "avatar")
                {
                    m_avatarLayer = new AvatarLayer(scene);
                    layerImgs.Add(m_avatarLayer.Render(range, picSize));
                }
                if (layers[i] == "prim")
                {
                    float scale = getScale(range, picSize);
                    string cacheName = m_cachePath + scene.RegionInfo.RegionID + "_" + scale;
                    Bitmap cache = null;
                    try
                    {
                        cache = new Bitmap(cacheName);
                        cache.MakeTransparent(Color.FromArgb(0, 0, 0, 0));
                    }
                    catch
                    {
                        cache = new Bitmap(picSize.Width, picSize.Height);
                        cache.MakeTransparent(Color.FromArgb(0, 0, 0, 0));
                        m_log.Error("[WebMap]: Cache not found!");
                    }
                    BBox srcSize = getCutSize(cache, scene, range);
                    layerImgs.Add(Utility.CutImage(cache, srcSize, picSize));
                    cache.Dispose();
                }
            }
            Bitmap map = null;
            switch (layerImgs.Count)
            {
                case 0:
                    throw new Exception("Requested layer(s) invalid!");
                case 1:
                    map = layerImgs[0];
                    break;
                default:
                    map = overlayImages(picSize, layerImgs);
                    break;
            }
            foreach (Bitmap bmp in layerImgs)
                bmp.Dispose();
            return map;
        }

        private BBox getCutSize(Bitmap cache, Scene scene, BBoxF range)
        {
            int minX = (int)((range.MinX - scene.RegionInfo.RegionLocX * 256) / 256 * cache.Width);
            int minY = (int)((256 - range.MaxY + scene.RegionInfo.RegionLocY * 256) / 256 * cache.Height);
            int maxX = (int)((range.MaxX - scene.RegionInfo.RegionLocX * 256) / 256 * cache.Width);
            int maxY = (int)((256 - range.MinY + scene.RegionInfo.RegionLocY * 256) / 256 * cache.Height);
            return new BBox(minX, minY, maxX, maxY);
        }

        private Scene getScene(BBoxF range)
        {
            int locX = (int)range.MinX / 256;
            int locY = (int)range.MinY / 256;
            foreach (Scene scene in m_sceneList)
            {
                if (scene.RegionInfo.RegionLocX == locX && scene.RegionInfo.RegionLocY == locY)
                    return scene;
            }
            return null;
        }

        private float getScale(BBoxF range, BBox picSize)
        {
            float scale = picSize.Width / range.Width;
            float tmpScale = scale;
            float diff = 10000.0f;
            for (int level = 0; level < m_zoomLevel; level++)
            {
                float s = (float)Math.Pow(2, level);
                float tmp = Math.Abs(scale - s);
                if (diff > tmp)
                {
                    diff = tmp;
                    tmpScale = s;
                }
            }
            scale = tmpScale;
            return scale;
        }

        private Bitmap overlayImages(BBox picSize, List<Bitmap> imgs)
        {
            Bitmap image = new Bitmap(picSize.Width, picSize.Height);
            try
            {
                Graphics gfx = Graphics.FromImage(image);
                gfx.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.GammaCorrected;
                gfx.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
                foreach (Bitmap img in imgs)
                {
                    gfx.DrawImage(img, new Rectangle(0, 0, picSize.Width, picSize.Height));
                }
                gfx.Dispose();
            }
            catch (Exception e)
            {
                m_log.Error("[WebMap]: Overlay layers failed " + e.Message + e.StackTrace);
            }
            return image;
        }

        private void getTextureDataThread()
        {
            while (true)
            {
                getTextureData();
                Thread.Sleep(m_texUpdateInterval);
            }
        }

        private void getTextureData()
        {
            m_log.Debug("[WebMap]: Start getting texture from remote database");
            try
            {
                Utility.ConnectMysql(m_remoteConnectionString);
                List<TextureColorModel> data = Utility.GetDataFromMysql();
                Utility.DisconnectMysql();
                Utility.StoreDataIntoFiles(data, m_texPath);
                m_log.Debug("[WebMap]: Successfully got all remote texture data");
            }
            catch (Exception e)
            {
                m_log.ErrorFormat("[WebMap]: Get texture data failed with {0} {1}", e.Message, e.StackTrace);
            }
        }

        private void makeCacheThread()
        {
            Thread.Sleep(8000);
            while (true)
            {
                makeCache();
                Thread.Sleep(m_mapUpdateInterval);
            }
        }

        private void makeCache()
        {
            m_log.Info("[WebMap]: Start generating map cache");
            try
            {
                foreach (Scene scene in m_sceneList)
                {
                    m_primLayer = new PrimLayer(scene);
                    uint locX = scene.RegionInfo.RegionLocX;
                    uint locY = scene.RegionInfo.RegionLocY;
                    BBoxF regionRange = new BBoxF(0, 0, 256, 256);
                    m_primLayer.Initialize(regionRange);

                    for (int level = 0; level < m_zoomLevel; level++)
                    {
                        int scale = (int)Math.Pow(2, level);
                        string cacheName = m_cachePath + scene.RegionInfo.RegionID + "_" + scale;
                        BBox picSize = new BBox(0, 0, (int)(256 * scale), (int)(256 * scale));
                        m_primLayer.Render(regionRange, picSize, cacheName);
                    }
                }
            }
            catch (Exception e)
            {
                m_log.Error("[WebMap]: Making cache failed, " + e.Message + e.StackTrace);
            }
            m_log.Info("[WebMap]: Map cache generated successfully");
        }
    }
}
