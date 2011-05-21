using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using MapRendererCL;
using FreeImageAPI;
using OpenMetaverse;
using System.Drawing;
using System.IO;
using System.Data.SQLite;

namespace OpenSim.ApplicationPlugins.WebMap
{
    public class Utility
    {
        private static MySqlConnection m_mysqlConnect;
        private static SQLiteConnection m_sqliteConnect;

        public static LLVector3CL toLLVector3(Vector3 vector)
        {
            return new LLVector3CL(vector.X, vector.Y, vector.Z);
        }
        public static LLQuaternionCL toLLQuaternion(Quaternion qua)
        {
            return new LLQuaternionCL(qua.X, qua.Y, qua.Z, qua.W);
        }

        internal static string ConvertToString(Bitmap bmp)
        {
            MemoryStream stream = new MemoryStream();
            bmp.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
            byte[] byteImage = stream.ToArray();
            stream.Close();
            stream.Dispose();
            return Convert.ToBase64String(byteImage);
        }

        internal static Bitmap CutImage(Bitmap img, BBox srcSize, BBox picSize)
        {
            Bitmap newImg = new Bitmap(picSize.Width, picSize.Height);
            Graphics gfx = Graphics.FromImage(newImg);
            gfx.DrawImage(img, picSize.ToRectangle(), srcSize.ToRectangle(), GraphicsUnit.Pixel);
            gfx.Dispose();
            return newImg;
        }

        public static long IntToLong(int a, int b)
        {
            return ((long)a << 32 | (long)b);
        }

        public static void LongToInt(long a, out int b, out int c)
        {
            b = (int)(a >> 32);
            c = (int)(a & 0x00000000FFFFFFFF);
        }

        public static PointF Projection(ref PointF agentPos, ref BBoxF bbox, BBox picSize)
        {
            PointF result = new PointF();
            result.X = (agentPos.X - bbox.MinX) * picSize.Width / bbox.Width;
            result.Y = picSize.Height - (agentPos.Y - bbox.MinY) * picSize.Height / bbox.Height;
            return result;
        }
        public static void StoreDataIntoFiles(List<TextureColorModel> data, string filePath)
        {
            if (!Directory.Exists(filePath))
                Directory.CreateDirectory(filePath);
            foreach (TextureColorModel model in data)
            {
                string file = filePath + model.ID;
                if (File.Exists(file))
                    continue;
                TextWriter tw = new StreamWriter(file, false);
                tw.WriteLine(model.A);
                tw.WriteLine(model.R);
                tw.WriteLine(model.G);
                tw.WriteLine(model.B);
                tw.Close();
            }
        }

        public static TextureColorModel GetDataFromFile(string path, string id)
        {
            TextureColorModel model;
            string file = path + id;
            if (!File.Exists(file))
                model = new TextureColorModel(null, 255, 120, 120, 120);
            else
            {
                TextReader tr = new StreamReader(file);
                byte a = Convert.ToByte(tr.ReadLine());
                byte r = Convert.ToByte(tr.ReadLine());
                byte g = Convert.ToByte(tr.ReadLine());
                byte b = Convert.ToByte(tr.ReadLine());
                model = new TextureColorModel(id, a, r, g, b);
                tr.Close();
            }
            return model;
        }

        public static void ConnectMysql(string connectionString)
        {
            m_mysqlConnect = new MySqlConnection(connectionString);
            m_mysqlConnect.Open();
        }

        public static void DisconnectMysql()
        {
            if (m_mysqlConnect != null)
            {
                m_mysqlConnect.Close();
                m_mysqlConnect = null;
            }
        }

        public static List<TextureColorModel> GetDataFromMysql()
        {
            List<TextureColorModel> models = new List<TextureColorModel>();
            lock (m_mysqlConnect)
            {
                using (MySqlCommand cmd = new MySqlCommand("SELECT id, data FROM assets", m_mysqlConnect))
                {
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string id = (string)reader["id"];
                            byte[] data = (byte[])reader["data"];
                            SimpleColorCL color = GetColorFromTexture(data);
                            if (color != null)
                            {
                                models.Add(new TextureColorModel(id, color.GetA(), color.GetR(), color.GetG(), color.GetB()));
                            }
                        }
                    }
                }
            }
            return models;
        }

        public static void ConnectSqlite(string connectionString)
        {
            m_sqliteConnect = new SQLiteConnection(connectionString);
            m_sqliteConnect.Open();
        }

        public static void DisconnectSqlite()
        {
            if (m_sqliteConnect != null)
            {
                m_sqliteConnect.Close();
                m_sqliteConnect = null;
            }
        }

        public static List<TextureColorModel> GetDataFromSqlite()
        {
            List<TextureColorModel> models = new List<TextureColorModel>();
            lock (m_sqliteConnect)
            {
                using (SQLiteCommand cmd = new SQLiteCommand("SELECT id, data FROM assets", m_sqliteConnect))
                {
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string id = (string)reader["id"];
                            byte[] data = (byte[])reader["data"];
                            SimpleColorCL color = GetColorFromTexture(data);
                            if (color != null)
                            {
                                models.Add(new TextureColorModel(id, color.GetA(), color.GetR(), color.GetG(), color.GetB()));
                            }
                        }
                    }
                }
            }
            return models;
        }

        private static SimpleColorCL GetColorFromTexture(byte[] data)
        {
            using (MemoryStream ms = new MemoryStream(data))
            {
                FIBITMAP dib = FreeImage.LoadFromStream(ms);
                if (dib.IsNull)
                {
                    return null;
                }
                uint width = FreeImage.GetWidth(dib);
                uint height = FreeImage.GetHeight(dib);
                int sum = (int)((width-4) * (height-4));
                int r = 0, g = 0, b = 0, a = 0;
                //get sample points from texture, leaving out edge
                for (uint x = 2; x < width-2; x++)
                    for (uint y = 2; y < height-2; y++)
                    {
                        RGBQUAD color;
                        FreeImage.GetPixelColor(dib, x, y, out color);
                        r += color.rgbRed;
                        g += color.rgbGreen;
                        b += color.rgbBlue;
                        a += color.rgbReserved;
                    }
                r = r / sum;
                g = g / sum;
                b = b / sum;
                a = a / sum;

                FreeImage.Unload(dib);
                return new SimpleColorCL((byte)a, (byte)r, (byte)g, (byte)b);
            }
        }
    }

    public class TextureColorModel
    {
        public string ID;
        public byte A;
        public byte R;
        public byte G;
        public byte B;
        public TextureColorModel(string id, byte a, byte r, byte g, byte b)
        {
            ID = id;
            A = a;
            R = r;
            G = g;
            B = b;
        }
        public TextureColorModel() { }
    }
}
