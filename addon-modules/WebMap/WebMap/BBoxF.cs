using System;
using System.Collections.Generic;

namespace OpenSim.ApplicationPlugins.WebMap
{
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
            string[] boxParams = new string[4];
            boxParams[0] = MinX.ToString();
            boxParams[1] = MinY.ToString();
            boxParams[2] = MaxX.ToString();
            boxParams[3] = MaxY.ToString();
            return string.Format("{0}, {1},{2},{3}", boxParams);
        }
    }
}
