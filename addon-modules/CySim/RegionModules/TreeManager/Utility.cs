using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;
using SharpMap.Geometries;
using SharpMap.Data.Providers;
using SharpMap.Data;
using OpenSim.Framework.Console;

namespace CySim.RegionModules.TreeManager
{
    public class Utility
    {
        public static int ReadShapefile(string file, out List<Geometry> features, out List<double> intervals, out List<int> types, out List<double> heights)
        {
            int flag = 0;
            features = new List<Geometry>();
            types = new List<int>();
            heights = new List<double>();
            intervals = new List<double>();
            ShapeFile shp = null;

            try
            {
                shp = new ShapeFile(file);
            }
            catch (Exception e)
            {
                MainConsole.Instance.Output("Shapefile open failed!");
                throw new Exception("ShapeFile not exists! " + e.Message + e.StackTrace);
            }
            shp.Open();
            switch (shp.ShapeType)
            {
                case ShapeType.Point:
                    flag = 0;
                    break;
                case ShapeType.PolyLine:
                    flag = 1;
                    break;
                case ShapeType.Polygon:
                    flag = 2;
                    break;
                default:
                    flag = -1;
                    return flag;
            }

            FeatureDataSet ds = new FeatureDataSet();
            BoundingBox bbox = shp.GetExtents();
            shp.ExecuteIntersectionQuery(bbox, ds);
            FeatureDataTable table = ds.Tables[0] as FeatureDataTable;
            try
            {
                foreach (FeatureDataRow row in table.Rows)
                {
                    features.Add(row.Geometry);
                    heights.Add(Convert.ToDouble(row["Height"]));
                    types.Add(Convert.ToInt32(row["Type"]));
                    if (flag != 0)
                        intervals.Add(Convert.ToDouble(row["Interval"]));
                }
            }
            catch (Exception e)
            {
                throw new Exception("Attribute in shapefile incorrect with " + e.Message + e.StackTrace);
            }
            
            shp.Close();
            return flag;
        }

        public static double NormalDistribute(double mu, double sigma)
        {
            Random rnd = new Random();
            double a = rnd.NextDouble();
            double b = rnd.NextDouble();
            return Math.Sqrt(-2 * Math.Log10(a)) * Math.Cos(2 * Math.PI * b) * sigma + mu;
        }

        
        public static List<Point> RasterizeLine(LineString line, double interval)
        {
            if (interval <= 0)
                throw new Exception("Interval should be positive!");
            List<Point> result = new List<Point>();
            //distances[i] stores the distance from nodes[0] to nodes[i] along the line
            List<double> distances = new List<double>();
            IList<Point> nodes = line.Vertices;
            
            distances.Add(0);
            for (int i = 1, len = nodes.Count; i < len; i++)
            {
                double distance = GetDistance(nodes[i - 1], nodes[i]);
                distances.Add(distance + distances[i-1]);
            }

            result.Add(nodes[0]);
            int index = 0;
            for (double sum = interval, length = distances[distances.Count - 1]; sum <= length; sum += interval)
            {
                for (int i = index, len = distances.Count; i < len; i++)
                {
                    if (sum <= distances[i])
                    {
                        index = i;
                        break;
                    }
                }
                Point node = Interpolate(nodes[index-1], nodes[index], (sum - distances[index-1]) / (distances[index] - distances[index-1]));
                result.Add(node);
            }
            return result;
        }

        public static List<Point> RasterizeArea(Polygon polygon, double interval)
        {
            if (interval <= 0)
                throw new Exception("Interval should be positive!");
            List<Point> result = new List<Point>();
            BoundingBox bbox = polygon.GetBoundingBox();
            LinearRing exRing = polygon.ExteriorRing;
            IList<LinearRing> inRings = polygon.InteriorRings;
            int inRingsNum = polygon.NumInteriorRing;

            for (double y = bbox.Bottom; y <= bbox.Top; y += interval)
                for (double x = bbox.Left; x <= bbox.Right; x += interval)
                {
                    Point p = new Point(x, y);
                    if (exRing.IsPointWithin(p))
                    {
                        if (inRingsNum == 0)
                            result.Add(p);
                        else
                        {
                            int index = 0;
                            for ( ; index < inRingsNum; index++)
                                if (inRings[index].IsPointWithin(p))
                                    break;
                            if (index == inRingsNum)
                                result.Add(p);
                        }
                    }
                }
            return result;   
        }

        public static double GetDistance(Point node1, Point node2)
        {
            double difx = node1.X - node2.X;
            double dify = node1.Y - node2.Y;
            return Math.Sqrt(difx * difx + dify * dify);
        }

        public static Point Interpolate(Point node1, Point node2, double ratio)
        {
            double x = node1.X + (node2.X - node1.X) * ratio;
            double y = node1.Y + (node2.Y - node1.Y) * ratio;
            return new Point(x, y);
        }
    }
}
