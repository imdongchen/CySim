using System;
using System.Collections.Generic;
using OpenSim.Server.Base;
using OpenSim.Framework.Servers.HttpServer;
using log4net;
using System.Net;
using System.IO;
using OpenSim.Services.Interfaces;
using OpenMetaverse;
using System.Drawing;
using System.Text;
using System.Reflection;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;

namespace OpenSim.Server.Handlers.Map
{
    class WebMapServerGetHandler : BaseStreamHandler
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private IWebMapService m_webMapService;

        public WebMapServerGetHandler(IWebMapService service)
            : base("GET", "/map")
        {
            m_webMapService = service;
        }

        public override byte[] Handle(string path, Stream request, OSHttpRequest httpRequest, OSHttpResponse httpResponse)
        {
            m_log.Info("[WebMapService]: Web map request received");
            string result = "";
            switch (httpRequest.QueryString["SERVICE"])
            {
                case "WMS":
                    if (httpRequest.QueryString["REQUEST"] == "GetMap")
                    {
                        string[] layers;
                        BBox picSize;
                        string format;
                        BBoxF range;
                        Dictionary<Bitmap, BBox> regionMapsDic = new Dictionary<Bitmap, BBox>();
                        m_webMapService.GetWMSParams(httpRequest, out layers, out format, out range, out picSize);
                        BBoxF cover = getCoverRange(range);
                        for (float y = cover.MaxY; y >= cover.MinY; y -= 256)
                            for (float x = cover.MinX; x <= cover.MaxX; x += 256)
                            {
                                Bitmap regionMap = null;
                                GridRegion region = m_webMapService.GetRegion(x, y);
                                BBoxF regionRange = getRegionRange(x, y, range);
                                BBox regionPicSize = getRegionPicSize(regionRange, range, picSize);
                                if (region == null)
                                    regionMap = m_webMapService.CreatePlainImage(regionPicSize);
                                else
                                {
                                    regionMap = m_webMapService.GetRegionMap(path, region, layers, regionRange, regionPicSize, format);
                                }
                                regionMapsDic.Add(regionMap, regionPicSize);
                            }
                        Bitmap map = m_webMapService.MergeImg(regionMapsDic, picSize);
                        result = convertToString(map);
                        httpResponse.ContentType = "image/png";
                        httpResponse.StatusCode = (int)HttpStatusCode.OK;
                    }
                    else if (httpRequest.QueryString["REQUEST"] == "GetCapabilities")
                    {
                        httpResponse.StatusCode = (int)HttpStatusCode.OK;
                        httpResponse.ContentType = "text/xml";
                        TextReader textReader = new StreamReader("WMS_Capabilities.xml");
                        result = textReader.ReadToEnd();
                        textReader.Close();
                    }
                    else
                    {
                        result = "unsupported query method";
                    }
                    break;
                case "WFS":
                    break;
                default:
                    httpResponse.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
                    result = "unsupported service";
                    break;
            }

            m_log.Info("[WebMapService]: Web map request responded");
            if (httpResponse.ContentType == "image/png")
            {
                return Convert.FromBase64String(result);
            }
            return Encoding.UTF8.GetBytes(result); 
        }

        private BBoxF getCoverRange(BBoxF range)
        {
            float minX = (int)range.MinX / 256 * 256;
            float maxX = (int)(range.MaxX - 1) / 256 * 256;
            float minY = (int)range.MinY / 256 * 256;
            float maxY = (int)(range.MaxY - 1)/ 256 * 256;
            return new BBoxF(minX, minY, maxX, maxY);
        }

        private string convertToString(Bitmap image)
        {
            MemoryStream ms = new MemoryStream();
            image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            byte[] byteImage = ms.ToArray();
            ms.Close();            
            return Convert.ToBase64String(byteImage);
        }

        private BBoxF getRegionRange(float x, float y, BBoxF range)
        {
            float difLeft = range.MinX - x;
            float minX = difLeft > 0 ? range.MinX : range.MinX - difLeft;
            float difRight = range.MaxX - x - 256;
            float maxX = difRight > 0 ? range.MaxX - difRight : range.MaxX;
            float difTop = range.MaxY - y - 256;
            float maxY = difTop > 0 ? range.MaxY - difTop : range.MaxY;
            float difBottom = range.MinY - y;
            float minY = difBottom > 0 ? range.MinY : range.MinY - difBottom;
            return new BBoxF(minX, minY, maxX, maxY);
        }

        private BBox getRegionPicSize(BBoxF regionRange, BBoxF range, BBox picSize)
        {
            int minX = (int)((regionRange.MinX - range.MinX) / range.Width * picSize.Width);
            int maxX = (int)((regionRange.MaxX - range.MinX) / range.Width * picSize.Width);
            int minY = (int)((range.MaxY - regionRange.MaxY) / range.Height * picSize.Height);
            int maxY = (int)((range.MaxY - regionRange.MinY) / range.Height * picSize.Height);
            return new BBox(minX, minY, maxX, maxY);
        }
    }
}
