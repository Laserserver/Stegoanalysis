using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace Stegoanalysis
{
    /// <summary>
    /// Синглтон ImageHolder
    /// </summary>
    public class ImageHolder
    {
        /// <summary>
        /// Экземпляр синглтона ImageHolder
        /// </summary>
        private static ImageHolder _instance;

        private Bitmap _first;
        private Bitmap _second;

        private List<Bitmap> _firstChannels;
        private List<Bitmap> _secondChannels;


        /// <summary>
        /// Возвращает экземпляр синглтона класса ImageHolder.
        /// </summary>
        public static ImageHolder Instance => _instance ?? (_instance = new ImageHolder());

        /// <summary>
        /// Закрытый конструктор класса.
        /// </summary>
        private ImageHolder()
        {
            _firstChannels = new List<Bitmap>();
            _secondChannels = new List<Bitmap>();
        }

        public void BitmapImage2Bitmap(BitmapImage bitmapImage, bool first)
        {
            using (MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmapImage));
                enc.Save(outStream);
                if(first)
                {
                    _first = new Bitmap(outStream);
                }
                else
                {
                    _second = new Bitmap(outStream);
                }
            }
        }
        

        private BitmapImage Bitmap2BitmapImage(Bitmap bitmap)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                bitmap.Save(stream, ImageFormat.Bmp);

                stream.Position = 0;
                BitmapImage result = new BitmapImage();
                result.BeginInit();
                // According to MSDN, "The default OnDemand cache option retains access to the stream until the image is needed."
                // Force the bitmap to load right now so we can dispose the stream.
                result.CacheOption = BitmapCacheOption.OnLoad;
                result.StreamSource = stream;
                result.EndInit();
                result.Freeze();
                return result;
            }
            
        }

        public List<BitmapImage> GetChannels()
        {
            List<BitmapImage> images = new List<BitmapImage>();

            for (int i = 0; i < 3; i++)
            {
                images.Add(Bitmap2BitmapImage(_firstChannels[i]));
                images.Add(Bitmap2BitmapImage(_secondChannels[i]));
            }
            return images;
        }

        public void SetChannels(List<Bitmap> channels, bool first)
        {
            if (first)
                _firstChannels = channels;
            else
                _secondChannels = channels;
        }

        public Bitmap[] GetImages()
        {
            return new []{ _first, _second};
        }
    }
}
