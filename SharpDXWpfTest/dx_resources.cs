using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.WIC;

namespace image_match
{
    public class dx_resources
    {
        public SharpDX.Direct3D11.Device d3dDevice;
        public SharpDX.Direct2D1.Factory d2dFactory;
        public SharpDX.WIC.ImagingFactory wicFactory;
        public SharpDX.Direct2D1.RenderTarget d2d_render_target;
        public SharpDX.Direct3D11.Texture2D RenderTarget;
        public SharpDX.Direct2D1.Bitmap[] descriptor_image_plus;
        public SharpDX.Direct2D1.Bitmap[] descriptor_image_minus;
        public SharpDX.Direct2D1.Bitmap point_image;

        public dx_resources()
        {
            d3dDevice = new SharpDX.Direct3D11.Device(SharpDX.Direct3D.DriverType.Hardware, SharpDX.Direct3D11.DeviceCreationFlags.BgraSupport);
            wicFactory = new SharpDX.WIC.ImagingFactory();
            d2dFactory = new SharpDX.Direct2D1.Factory();
            
            Texture2DDescription colordesc = new Texture2DDescription
            {
                BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                Format = Format.B8G8R8A8_UNorm,
                Width = 1,
                Height = 1,
                MipLevels = 1,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                OptionFlags = ResourceOptionFlags.Shared,
                CpuAccessFlags = CpuAccessFlags.None,
                ArraySize = 1
            };
            this.RenderTarget = new Texture2D(d3dDevice, colordesc);

            Surface surface = this.RenderTarget.QueryInterface<Surface>();

            RenderTargetProperties rtp = new RenderTargetProperties(new SharpDX.Direct2D1.PixelFormat(Format.Unknown, SharpDX.Direct2D1.AlphaMode.Premultiplied));

            d2d_render_target = new RenderTarget(d2dFactory, surface, rtp);
            //d2d_render_target.AntialiasMode = AntialiasMode.PerPrimitive;

            descriptor_image_plus = new SharpDX.Direct2D1.Bitmap[6];
            int bitmap_index=0;
            for (int width = 16; width <= 512; width*=2)
            {
                SharpDX.WIC.Bitmap point_image_wic = new SharpDX.WIC.Bitmap(wicFactory, (int)width, (int)width, SharpDX.WIC.PixelFormat.Format32bppPBGRA, BitmapCreateCacheOption.CacheOnLoad);

                RenderTargetProperties renderTargetProperties = new RenderTargetProperties(RenderTargetType.Default, new SharpDX.Direct2D1.PixelFormat(Format.Unknown, AlphaMode.Unknown), 0, 0, RenderTargetUsage.None, FeatureLevel.Level_DEFAULT);
                
                WicRenderTarget d2dRenderTarget = new WicRenderTarget(d2dFactory, point_image_wic, renderTargetProperties);

                SolidColorBrush solidColorBrush = new SolidColorBrush(d2dRenderTarget, Color.Black);

                d2dRenderTarget.BeginDraw();
                d2dRenderTarget.Clear(Color.Transparent);
                if (width >= 256)
                    d2dRenderTarget.DrawEllipse(new Ellipse(new Vector2(width / 2f, width / 2f), width / 2f - 2f, width / 2f - 2f), solidColorBrush, 2f);
                else
                    d2dRenderTarget.DrawEllipse(new Ellipse(new Vector2(width / 2f, width / 2f), width / 2f - 2f, width / 2f - 2f), solidColorBrush, 1f);
                d2dRenderTarget.EndDraw();
                BitmapProperties props = new BitmapProperties(new SharpDX.Direct2D1.PixelFormat(SharpDX.DXGI.Format.B8G8R8A8_UNorm, AlphaMode.Premultiplied));
                descriptor_image_plus[bitmap_index] = SharpDX.Direct2D1.Bitmap.FromWicBitmap(d2d_render_target, point_image_wic, props);

                solidColorBrush.Dispose();
                d2dRenderTarget.Dispose();
                bitmap_index++;
            }

            descriptor_image_minus = new SharpDX.Direct2D1.Bitmap[6];
            bitmap_index = 0;
            for (int width = 16; width <= 512; width *= 2)
            {
                SharpDX.WIC.Bitmap point_image_wic = new SharpDX.WIC.Bitmap(wicFactory, (int)width, (int)width, SharpDX.WIC.PixelFormat.Format32bppPBGRA, BitmapCreateCacheOption.CacheOnLoad);

                RenderTargetProperties renderTargetProperties = new RenderTargetProperties(RenderTargetType.Default, new SharpDX.Direct2D1.PixelFormat(Format.Unknown, AlphaMode.Unknown), 0, 0, RenderTargetUsage.None, FeatureLevel.Level_DEFAULT);

                WicRenderTarget d2dRenderTarget = new WicRenderTarget(d2dFactory, point_image_wic, renderTargetProperties);

                SolidColorBrush solidColorBrush = new SolidColorBrush(d2dRenderTarget, Color.White);

                d2dRenderTarget.BeginDraw();
                d2dRenderTarget.Clear(Color.Transparent);
                if(width>=256)
                    d2dRenderTarget.DrawEllipse(new Ellipse(new Vector2(width / 2f, width / 2f), width / 2f - 2f, width / 2f - 2f), solidColorBrush, 2f);
                else
                    d2dRenderTarget.DrawEllipse(new Ellipse(new Vector2(width / 2f, width / 2f), width / 2f - 2f, width / 2f - 2f), solidColorBrush, 1f);
                d2dRenderTarget.EndDraw();
                BitmapProperties props = new BitmapProperties(new SharpDX.Direct2D1.PixelFormat(SharpDX.DXGI.Format.B8G8R8A8_UNorm, AlphaMode.Premultiplied));
                descriptor_image_minus[bitmap_index] = SharpDX.Direct2D1.Bitmap.FromWicBitmap(d2d_render_target, point_image_wic, props);

                solidColorBrush.Dispose();
                d2dRenderTarget.Dispose();
                bitmap_index++;
            }

            float point_size=10;

            SharpDX.WIC.Bitmap wic_point_image = new SharpDX.WIC.Bitmap(wicFactory, (int)point_size, (int)point_size, SharpDX.WIC.PixelFormat.Format32bppPBGRA, BitmapCreateCacheOption.CacheOnLoad);

            RenderTargetProperties renderTargetProperties_point_image = new RenderTargetProperties(RenderTargetType.Default, new SharpDX.Direct2D1.PixelFormat(Format.Unknown, AlphaMode.Unknown), 0, 0, RenderTargetUsage.None, FeatureLevel.Level_DEFAULT);

            WicRenderTarget d2dRenderTarget_point = new WicRenderTarget(d2dFactory, wic_point_image, renderTargetProperties_point_image);

            SolidColorBrush solidColorBrush_point = new SolidColorBrush(d2dRenderTarget_point, new Color4(137f / 255f, 201f / 255f, 238f / 255f, 1f));
            SolidColorBrush solidColorBrush_point2 = new SolidColorBrush(d2dRenderTarget_point, Color.Black);

            d2dRenderTarget_point.BeginDraw();
            d2dRenderTarget_point.Clear(Color.Transparent);

            d2dRenderTarget_point.FillEllipse(new Ellipse(new Vector2(point_size / 2f, point_size / 2f), point_size / 2f - 2f, point_size / 2f - 2f), solidColorBrush_point);
            d2dRenderTarget_point.DrawEllipse(new Ellipse(new Vector2(point_size / 2f, point_size / 2f), point_size / 2f - 2f, point_size / 2f - 2f), solidColorBrush_point2);
            d2dRenderTarget_point.EndDraw();
            BitmapProperties props_point = new BitmapProperties(new SharpDX.Direct2D1.PixelFormat(SharpDX.DXGI.Format.B8G8R8A8_UNorm, AlphaMode.Premultiplied));
            point_image = SharpDX.Direct2D1.Bitmap.FromWicBitmap(d2d_render_target, wic_point_image, props_point);

            solidColorBrush_point2.Dispose();
            solidColorBrush_point.Dispose();
            d2dRenderTarget_point.Dispose();

            surface.Dispose();
        }
    }
}
