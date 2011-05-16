using System;
using System.Collections.Generic;
using OpenMetaverse;
using System.Drawing;
using OpenSim.Framework.Servers.HttpServer;

namespace OpenSim.Services.Interfaces
{
    public interface IWebMapService
    {
        GridRegion GetRegion(float x, float y);
        Bitmap MergeImg(Dictionary<Bitmap, BBox> regionMaps, BBox picSize);
        Bitmap GetRegionMap(string path, GridRegion region, string[] layers, BBoxF range, BBox picSize, string format);
        void GetWMSParams(OSHttpRequest httpRequest, out string[] layers, out string format, out BBoxF range, out BBox picSize);
        Bitmap CreatePlainImage(BBox picSize);
    }

    public class BBoxF
    {
        public float MinX, MinY, MaxX, MaxY;
        public BBoxF()
        {
            MinX = 0;
            MinY = 0;
            MaxX = 0;
            MaxY = 0;
        }

        public BBoxF(float minX, float minY, float maxX, float maxY)
        {
            MinX = minX;
            MinY = minY;
            MaxX = maxX;
            MaxY = maxY;
        }

        public BBoxF(string boxStr)
        {
            string[] boxParams = boxStr.Split(',');
            MinX = float.Parse(boxParams[0]);
            MinY = float.Parse(boxParams[1]);
            MaxX = float.Parse(boxParams[2]);
            MaxY = float.Parse(boxParams[3]);
        }

        public float Width
        {
            get { return MaxX - MinX; }
        }

        public float Height
        {
            get { return MaxY - MinY; }
        }

        public override string ToString()
        {
            return (int)MinX + "," + (int)MinY + "," + (int)MaxX + "," + (int)MaxY;
        }
    }

    public class BBox
    {
        public int MinX, MinY, MaxX, MaxY;
        public BBox()
        {
            MinX = 0;
            MinY = 0;
            MaxX = 0;
            MaxY = 0;
        }

        public BBox(int minX, int minY, int maxX, int maxY)
        {
            MinX = minX;
            MinY = minY;
            MaxX = maxX;
            MaxY = maxY;
        }

        public BBox(string boxStr)
        {
            string[] boxParams = boxStr.Split(',');
            MinX = (int)float.Parse(boxParams[0]);
            MinY = (int)float.Parse(boxParams[1]);
            MaxX = (int)float.Parse(boxParams[2]);
            MaxY = (int)float.Parse(boxParams[3]);
        }

        public int Width
        {
            get { return MaxX - MinX; }
        }

        public int Height
        {
            get { return MaxY - MinY; }
        }

        public Rectangle ToRectangle()
        {
            Rectangle rect = new Rectangle(MinX, MinY, Width, Height);
            return rect;
        }
        public override string ToString()
        {
            return MinX + "," + MinY + "," + MaxX + "," + MaxY;
        }
    }
}

