using System;
using System.IO;
using System.Windows.Media.Imaging;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using OpenSURFcs;
namespace image_match
{
    public class image_sol
    {
        public int[] connection_id;
        public float[] points;
        public float[] scale;
        public float[] orientation;
        public bool[] sign;
        public int points_count;
        public int width;
        public int height;
        public string path;
        public float[] desctriptor;
        Random rand;
        public SharpDX.Direct2D1.Bitmap bitmap;
        public bool selected;
        public bool selected_preview;
        int descriptor_length;
        public image_sol(string filename, SharpDX.Direct2D1.RenderTarget d2d_render_target)
        {
            path = filename;
            
            System.Security.Cryptography.RandomNumberGenerator rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            byte[] arr = new byte[16];
            rng.GetNonZeroBytes(arr);

            rand = new Random(BitConverter.ToInt32(arr, 0));
            rng.Dispose();
            points_count = 0;


            try
            {
                using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    BitmapFrame bmpFrame = BitmapFrame.Create(fs);
                    width = bmpFrame.PixelWidth;
                    height = bmpFrame.PixelHeight;
                }
            }
            catch
            {
                width = 200;
                height = 100;
                path = "invalid";
            }
            bitmap = LoadFromFile(d2d_render_target, path);
            ipts= new List<IPoint>();
        }
        public void generate_false_points()
        {
            points_count = 500;

            scale = new float[points_count];
            orientation = new float[points_count];
            sign = new bool[points_count];
            //desctriptor = new float[points_count * 64];
            points = new float[points_count * 2];
            connection_id = new int[points_count];
            for (int i = 0; i < points_count; i++)
            {
                connection_id[i] = -1;
            }
            for (int i = 0; i < points_count; i++)
            {
                points[i * 2] = (float)rand.NextDouble() * width;
                points[i * 2 + 1] = (float)rand.NextDouble() * height;
                scale[i] = (float)Math.Pow((double)2, (double)rand.Next(1, 5));
                orientation[i] = (float)Math.Sqrt(rand.NextDouble() * Math.PI * 2);
                sign[i] = Convert.ToBoolean((rand.Next() % 2));
            }
        }
        bool integral_image=false;
        bool detected=false;
        bool described=false;
        bool surf_ready = false;
        List<IPoint> ipts;
        IntegralImage iimg;
        public void build_integral_image()
        {
            System.Drawing.Bitmap img = new System.Drawing.Bitmap(path);
            iimg = IntegralImage.FromImage(img);
            img.Dispose();
            integral_image = true;
        }
        public void detect_points()
        {
            if (!integral_image)
                return;
            //List<IPoint> ipts = new List<IPoint>();
            // Extract the interest points
            ipts.Clear();
            
            ipts = FastHessian.getIpoints(0.0002f, 5, 1, iimg);
            detected = true;
        }
        public void describe_points()
        {
            if (!detected)
                return;
            SurfDescriptor.DecribeInterestPoints(ipts, false, false, iimg);
            descriptor_length = 64;
            described = true;
            int length = ipts.Count;

            connection_id = new int[length];
            for (int i = 0; i < length; i++)
            {
                connection_id[i] = -1;
            }
        }
        public void get_render_data()
        {
            if (!described)
                return;
            points_count = ipts.Count;
            points = new float[points_count * 2];

            scale = new float[points_count];
            orientation = new float[points_count];
            sign = new bool[points_count];
            desctriptor = new float[points_count * descriptor_length];


            for (int i = 0; i < points_count; i++)
            {
                /*points[i * 2] = (float)rand.NextDouble() * width;
                points[i * 2 + 1] = (float)rand.NextDouble() * height;
                scale[i] = (float)Math.Pow((double)2, (double)rand.Next(1, 5));
                orientation[i] = (float)Math.Sqrt(rand.NextDouble() * Math.PI * 2);
                sign[i] = Convert.ToBoolean((rand.Next() % 2));*/

                points[i * 2] = ipts[i].x;
                points[i * 2 + 1] = ipts[i].y;
                scale[i] = ipts[i].scale;
                orientation[i] = ipts[i].orientation;
                sign[i] = Convert.ToBoolean(ipts[i].laplacian);
                for (int j = 0; j < descriptor_length; j++)
                {
                    desctriptor[i * descriptor_length + j] = ipts[i].descriptor[j];
                }
            }
            ipts.Clear();
            surf_ready = true;
        }
        public static Bitmap LoadFromFile(RenderTarget renderTarget, string file)
        {
            // Loads from file using System.Drawing.Image
            if (file != "invalid")
            {
                using (var bitmap = (System.Drawing.Bitmap)System.Drawing.Image.FromFile(file))
                {
                    var sourceArea = new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height);
                    var bitmapProperties = new BitmapProperties(new PixelFormat(Format.R8G8B8A8_UNorm, AlphaMode.Premultiplied));
                    var size = new Size2(bitmap.Width, bitmap.Height);

                    // Transform pixels from BGRA to RGBA
                    var bitmapData = bitmap.LockBits(sourceArea, System.Drawing.Imaging.ImageLockMode.ReadWrite, bitmap.PixelFormat);
                    int stride = bitmapData.Stride;
                    int d2d_strie = bitmap.Height * bitmap.Width * sizeof(int);
                    using (var tempStream = new DataStream(d2d_strie, true, true))
                    {
                        // Lock System.Drawing.Bitmap
                        long length = bitmap.Height * stride;
                        if (bitmap.PixelFormat == System.Drawing.Imaging.PixelFormat.Format32bppArgb)
                        {
                            unsafe
                            {
                                byte* rgbValues = (byte*)bitmapData.Scan0.ToPointer();
                                int rgba;
                                for (int i = 0; i < length; i += 4)
                                {
                                    rgba = rgbValues[i + 2] | (rgbValues[i + 1] << 8) | (rgbValues[i] << 16) | (rgbValues[i + 3] << 24);
                                    tempStream.Write(rgba);
                                }

                            }
                        }
                        if (bitmap.PixelFormat == System.Drawing.Imaging.PixelFormat.Format24bppRgb)
                        {
                            unsafe
                            {
                                byte full = 255;
                                byte* rgbValues = (byte*)bitmapData.Scan0.ToPointer();
                                int rgba;
                                for (int i = 0; i < length; i += 3)
                                {
                                    rgba = rgbValues[i + 2] | (rgbValues[i + 1] << 8) | (rgbValues[i] << 16) | (full << 24);
                                    //rgba = (full) | (rgbValues[i] << 8) | (rgbValues[i + 1] << 16) | (rgbValues[i + 2] << 24);
                                    tempStream.Write(rgba);
                                }

                            }
                        }

                        bitmap.UnlockBits(bitmapData);
                        tempStream.Position = 0;
                        Bitmap bitmap_d2d = new Bitmap(renderTarget, size, tempStream, d2d_strie / bitmap.Height, bitmapProperties);
                        bitmap.Dispose();
                        tempStream.Dispose();
                        return bitmap_d2d;
                    }
                }
            }
            else
            {

                System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(200, 100, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(bitmap);

                var pen = new System.Drawing.Pen(System.Drawing.Brushes.Red, 10f);
                graphics.Clear(System.Drawing.Color.White);
                graphics.DrawLine(pen, 0, 0, 200, 100);
                graphics.DrawLine(pen, 0, 100, 200, 0);
                graphics.Dispose();

                var sourceArea = new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height);
                var bitmapProperties = new BitmapProperties(new PixelFormat(Format.R8G8B8A8_UNorm, AlphaMode.Premultiplied));
                var size = new Size2(bitmap.Width, bitmap.Height);

                // Transform pixels from BGRA to RGBA
                var bitmapData = bitmap.LockBits(sourceArea, System.Drawing.Imaging.ImageLockMode.ReadWrite, bitmap.PixelFormat);
                int stride = bitmapData.Stride;
                int d2d_strie = bitmap.Height * bitmap.Width * sizeof(int);
                using (var tempStream = new DataStream(d2d_strie, true, true))
                {
                    // Lock System.Drawing.Bitmap
                    long length = bitmap.Height * stride;
                    if (bitmap.PixelFormat == System.Drawing.Imaging.PixelFormat.Format32bppArgb)
                    {
                        unsafe
                        {
                            byte* rgbValues = (byte*)bitmapData.Scan0.ToPointer();
                            int rgba;
                            for (int i = 0; i < length; i += 4)
                            {
                                rgba = rgbValues[i + 2] | (rgbValues[i + 1] << 8) | (rgbValues[i] << 16) | (rgbValues[i + 3] << 24);
                                tempStream.Write(rgba);
                            }

                        }
                    }
                    if (bitmap.PixelFormat == System.Drawing.Imaging.PixelFormat.Format24bppRgb)
                    {
                        unsafe
                        {
                            byte full = 255;
                            byte* rgbValues = (byte*)bitmapData.Scan0.ToPointer();
                            int rgba;
                            for (int i = 0; i < length; i += 3)
                            {
                                rgba = rgbValues[i + 2] | (rgbValues[i + 1] << 8) | (rgbValues[i] << 16) | (full << 24);
                                //rgba = (full) | (rgbValues[i] << 8) | (rgbValues[i + 1] << 16) | (rgbValues[i + 2] << 24);
                                tempStream.Write(rgba);
                            }

                        }
                    }

                    bitmap.UnlockBits(bitmapData);
                    tempStream.Position = 0;
                    Bitmap bitmap_d2d = new Bitmap(renderTarget, size, tempStream, d2d_strie / bitmap.Height, bitmapProperties);
                    bitmap.Dispose();
                    tempStream.Dispose();
                    pen.Dispose();
                    return bitmap_d2d;
                }
            }
        }
        public void dispose()
        {
            bitmap.Dispose();
        }
        
    }
}
