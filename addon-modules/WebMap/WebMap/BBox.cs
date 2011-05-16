using System;
using System.Collections.Generic;
using System.Drawing;

namespace OpenSim.ApplicationPlugins.WebMap
{
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
    }
}
