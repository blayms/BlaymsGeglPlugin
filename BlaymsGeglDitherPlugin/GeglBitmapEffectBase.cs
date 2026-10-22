using System;
using PaintDotNet;
using PaintDotNet.Effects;
using PaintDotNet.Imaging;
using PaintDotNet.Rendering;

namespace BlaymsGeglPlugin
{
    internal abstract class GeglBitmapEffectBase<TToken> : BitmapEffect<TToken>
        where TToken : EffectConfigToken
    {
        private readonly object _renderLock = new object();
        private TToken _settings;
        private byte[] _processedPixels;
        private int _imageWidth;
        private int _imageHeight;

        protected GeglBitmapEffectBase(string name, System.Drawing.Image icon, string submenuName, TToken defaultToken)
            : base(name, icon, submenuName, BitmapEffectOptions.Create() with { IsConfigurable = true })
        {
            _settings = CloneToken(defaultToken);
        }

        protected abstract TToken CloneToken(TToken token);

        protected abstract byte[] ProcessPixels(byte[] pixels, int width, int height, TToken settings);

        protected abstract IEffectConfigForm CreateConfigForm(TToken settings);

        protected override IEffectConfigForm OnCreateConfigForm()
        {
            lock (_renderLock)
            {
                return CreateConfigForm(CloneToken(_settings));
            }
        }

        protected override void OnSetToken(TToken newToken)
        {
            base.OnSetToken(newToken);

            lock (_renderLock)
            {
                if (newToken != null)
                {
                    _settings = CloneToken(newToken);
                }

                _processedPixels = null;
                _imageWidth = 0;
                _imageHeight = 0;
            }
        }

        protected override void OnRender(IBitmapEffectOutput output)
        {
            if (output == null)
            {
                return;
            }

            byte[] pixels;
            int width;
            int height;

            lock (_renderLock)
            {
                if (_processedPixels == null)
                {
                    using (IEffectInputBitmap<ColorBgra32> sourceBitmap = Environment.GetSourceBitmapBgra32())
                    {
                        width = sourceBitmap.Size.Width;
                        height = sourceBitmap.Size.Height;
                        long byteCount = (long)width * height * 4L;

                        if (width <= 0 || height <= 0 || byteCount > Int32.MaxValue)
                        {
                            throw new InvalidOperationException("The source image is too large to process as an RGBA32 byte array.");
                        }

                        byte[] sourcePixels = new byte[(int)byteCount];

                        using (IBitmapLock<ColorBgra32> sourceLock = sourceBitmap.Lock(new RectInt32(0, 0, sourceBitmap.Size)))
                        {
                            RegionPtr<ColorBgra32> sourceRegion = sourceLock.AsRegionPtr();

                            for (int y = 0; y < height; y++)
                            {
                                int rowOffset = y * width * 4;

                                for (int x = 0; x < width; x++)
                                {
                                    ColorBgra32 pixel = sourceRegion[x, y];
                                    int offset = rowOffset + x * 4;

                                    sourcePixels[offset] = pixel.R;
                                    sourcePixels[offset + 1] = pixel.G;
                                    sourcePixels[offset + 2] = pixel.B;
                                    sourcePixels[offset + 3] = pixel.A;
                                }
                            }
                        }

                        _processedPixels = ProcessPixels(sourcePixels, width, height, CloneToken(_settings));
                        _imageWidth = width;
                        _imageHeight = height;
                    }
                }

                pixels = _processedPixels;
                width = _imageWidth;
                height = _imageHeight;
            }

            using (IBitmapLock<ColorBgra32> outputLock = output.LockBgra32())
            {
                RegionPtr<ColorBgra32> outputRegion = outputLock.AsRegionPtr();
                RectInt32 bounds = output.Bounds;

                for (int localY = 0; localY < bounds.Height; localY++)
                {
                    int imageY = bounds.Y + localY;

                    if (imageY < 0 || imageY >= height)
                    {
                        continue;
                    }

                    int rowOffset = imageY * width * 4;

                    for (int localX = 0; localX < bounds.Width; localX++)
                    {
                        int imageX = bounds.X + localX;

                        if (imageX < 0 || imageX >= width)
                        {
                            continue;
                        }

                        int offset = rowOffset + imageX * 4;

                        outputRegion[localX, localY] = ColorBgra32.FromBgra(
                            pixels[offset + 2],
                            pixels[offset + 1],
                            pixels[offset],
                            pixels[offset + 3]);
                    }
                }
            }
        }
    }
}
