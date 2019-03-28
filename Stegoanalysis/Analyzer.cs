using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Stegoanalysis
{
    /// <summary>
    /// Синглтон Analyzer
    /// </summary>
    public class Analyzer
    {
        /// <summary>
        /// Экземпляр синглтона Analyzer
        /// </summary>
        private static Analyzer _instance;

        private int _blockWidth, _blockHeight;
        private double _alpha;

        /// <summary>
        /// Возвращает экземпляр синглтона класса Analyzer.
        /// </summary>
        public static Analyzer Instance => _instance ?? (_instance = new Analyzer());

        /// <summary>
        /// Закрытый конструктор класса.
        /// </summary>
        private Analyzer()
        {
        }

        private void ParseValues(string width, string height, string alpha)
        {
            if (!int.TryParse(width, out _blockWidth))
                throw new Exception("ширины.");
            if (!int.TryParse(width, out _blockHeight))
                throw new Exception("высоты.");
            if (!double.TryParse(width, out _alpha))
                throw new Exception("уровня значимости.");
        }


        public Tuple<int, List<BitmapImage>> Analyze(string width, string height, string alpha)
        {
            try
            {
                ParseValues(width, height, alpha);
            }
            catch (Exception e)
            {
                throw new Exception("Неверный формат " + e.Message);
            }

            Bitmap[] btms = ImageHolder.Instance.GetImages();
            Bitmap first = ResizeImage(btms[0]);
            Bitmap second = ResizeImage(btms[1]);

            ImageHolder.Instance.SetChannels(SplitChannels(first), true);
            ImageHolder.Instance.SetChannels(SplitChannels(second), false);


            return Tuple.Create(1, ImageHolder.Instance.GetChannels());
        }



        private List<Bitmap> SplitChannels(Bitmap image)
        {
            List<Bitmap> channels = new List<Bitmap>();

            Bitmap red = new Bitmap(image);
            Bitmap green = new Bitmap(image);
            Bitmap blue = new Bitmap(image);

            for (int i = 0; i < image.Width; i++)
            {
                for (int j = 0; j < image.Height; j++)
                {
                    Color pixel = image.GetPixel(i, j);
                    red.SetPixel(i, j, Color.FromArgb(pixel.R, 0, 0));
                    green.SetPixel(i, j, Color.FromArgb(0, pixel.G, 0));
                    blue.SetPixel(i, j, Color.FromArgb(0, 0, pixel.B));
                }
            }

            channels.Add(red);
            channels.Add(green);
            channels.Add(blue);

            return channels;
        }

        private Bitmap ResizeImage(Bitmap image)
        {
            int width = image.Width;
            int height = image.Height;

            int boundaryX = width % _blockWidth;
            int boundaryY = height % _blockHeight;

            return new Bitmap(image, width + boundaryX, height + boundaryY);
        }
    }
}
