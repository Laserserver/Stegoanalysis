using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Accord.Statistics.Distributions.Univariate;

namespace Stegoanalysis
{
    /// <summary>
    /// Синглтон Analyzer
    /// </summary>
    public class Analyzer
    {
        private static Analyzer _instance;
        private int _blockWidth, _blockHeight;
        private double _alpha;
        private List<double> chis = new List<double>();

        private List<int> layers = new List<int>();
        
        public static Analyzer Instance => _instance ?? (_instance = new Analyzer());

        private Analyzer()
        {
            ReadChis();
        }

        private void ParseValues(string width, string height, string alpha)
        {
            if (!int.TryParse(width, out _blockWidth))
                throw new Exception("ширины.");
            if (!int.TryParse(height, out _blockHeight))
                throw new Exception("высоты.");
            if (!double.TryParse(alpha, out _alpha))
                throw new Exception("уровня значимости.");
        }


        public (int, List<BitmapImage>) Analyze(string width, string height, string alpha)
        {
            try
            {
                ParseValues(width, height, alpha);
            }
            catch (Exception e)
            {
                throw new Exception("Неверный формат " + e.Message);
            }

            var btms = ImageHolder.Instance.GetImages();
            if (btms[0].Width != btms[1].Width)
                throw new Exception("Разные ширины у изображений.");
            if (btms[0].Height != btms[1].Height)
                throw new Exception("Разные высоты у изображений.");
            
            int countX = btms[0].Width / _blockWidth;
            int countY = btms[0].Height / _blockHeight;
            Bitmap first = ResizeImage(btms[0]);
            Bitmap second = ResizeImage(btms[1]);



            var firstT = Wtf(first, countX, countY);
            var secondT = Wtf(second, countX, countY);

            for (int i = 0; i < 3; i++)
            {
                firstT[i] = ResizeImage(firstT[i], true);
                secondT[i] = ResizeImage(secondT[i], true);
            }

            ImageHolder.Instance.SetChannels(firstT, true);
            ImageHolder.Instance.SetChannels(secondT, false);

           /* ImageHolder.Instance.SetChannels(SplitChannels(first), true);
            ImageHolder.Instance.SetChannels(SplitChannels(second), false);

            return Tuple.Create(1, ImageHolder.Instance.GetChannels());*/

            return (layers[0] > layers[1] ? 1 : 2, ImageHolder.Instance.GetChannels());
        }


        private void ReadChis()
        {
            using (StreamReader sr = new StreamReader("exp.txt"))
            {
                while (!sr.EndOfStream)
                {
                    chis.Add(double.Parse(sr.ReadLine() ?? throw new InvalidOperationException()));
                }
            }
        }



        private List<Bitmap> Wtf(Bitmap image, int countX, int countY)
        {
            double[,] arrayReal = new double[countY, countX];
            double[,] arrayTheor = new double[countY, countX];
            var channels = SplitChannels(image);

            List<byte[,]> norms = new List<byte[,]>();
            int sum = 0;

            //Color cd = channels[0].GetPixel(0, 0);

            for (int i = 0; i < 3; i++)
            {
                Bitmap current = channels[i];

                for (int y = 0; y < countY; y++)
                {
                    for (int x = 0; x < countX; x++)
                    {
                        Bitmap img = GetBlock(current, x, y);

                        Color c = img.GetPixel(0, 0);
                        Color cc = current.GetPixel(0, 0);

                        List<double> histTheor = new List<double>(new double[256]);

                        var histEmpir = GetHistogram(img, i);

                        for (int j = 0; j < 256; j+= 2)
                        {
                            histTheor[j] = (histEmpir[j] + histEmpir[j + 1]) / 2;
                            histTheor[j + 1] = histTheor[j];
                        }

                        var non_zeros = new List<int>();
                        for (int j = 0; j < 256; j++)
                        {
                            if(histTheor[j] > 19)
                                non_zeros.Add(j);
                        }

                        var newHistEmpir = new List<double>();
                        var newHistTheor = new List<double>();
                        
                        foreach (int t in non_zeros)
                        {
                            newHistEmpir.Add(histEmpir[t]);
                            newHistTheor.Add(histTheor[t]);
                        }

                        histEmpir = newHistEmpir;
                        histTheor = newHistTheor;
                        

                        if(histEmpir.Count != histTheor.Count)
                            throw new Exception("Проблема с гистограммами в методе Wtf.");

                        List<double> xi2 = histEmpir.Select((t, j) => (t - histTheor[j]) * (t - histTheor[j]) / histTheor[j]).ToList();

                        double realXi2 = xi2.Sum();
                        
                        double theorXi2 = chis[xi2.Count - 1];
                        arrayReal[y, x] = realXi2;
                        arrayTheor[y, x] = theorXi2;
                    }
                }

                var tarr = arrayReal;
                if (i == 2)
                {
                    for (int j = 0; j < countY; j++)
                    {
                        for (int k = 0; k < countX; k++)
                        {
                            tarr[j, k] /= arrayTheor[j, k];
                        }
                    }
                }
                
                var bytearr = new byte[countY, countX];
                for (int j = 0; j < countY; j++)
                {
                    for (int k = 0; k < countX; k++)
                    {
                        if (tarr[j, k] < 0)
                        {
                            bytearr[j, k] = (byte)(256 + tarr[j, k] % 256);
                        }
                        else
                        {
                            if (tarr[j, k] > 255)
                                bytearr[j, k] = (byte) (tarr[j, k] % 256);
                            else
                            {
                                bytearr[j, k] = (byte) Math.Round(tarr[j, k]);
                            }
                        }

                    }
                }
                norms.Add(bytearr);

                //sum += bytearr.Cast<byte>().Aggregate(sum, (current1, b) => current1 + b);
            }


            for (int y = 0; y < countY; y++)
            {
                for (int x = 0; x < countX; x++)
                {

                    for (int i = 0; i < _blockWidth + 1; i++)
                    {
                        for (int j = 0; j < _blockHeight + 1; j++)
                        {
                            Color R = channels[0].GetPixel(x * _blockWidth + i, y * _blockHeight + 1 + j);
                            Color G = channels[1].GetPixel(x * _blockWidth + i, y * _blockHeight + 1 + j);
                            Color B = channels[2].GetPixel(x * _blockWidth + i, y * _blockHeight + 1 + j);

                            byte Rr = R.R;

                            int val = R.R * norms[0][y, x];
                            val = val > 255 ? 255 : val;
                            byte Rg = (byte)(val);
                            byte Rb = (byte)(val);

                            if (Math.Abs(Rr - Rb) < 5 || Math.Abs(Rg - Rr) < 5)
                                sum++;
                            val = G.G * norms[1][y, x];
                            val = val > 255 ? 255 : val;
                            byte Gr = (byte)(val);
                            byte Gg = G.G;
                            byte Gb = (byte)(val);

                            if (Math.Abs(Gr - Gg) < 5 || Math.Abs(Gg - Gb) < 5)
                                sum++;

                            val = B.B * norms[2][y, x];
                            val = val > 255 ? 255 : val;
                            byte Br = (byte)(val);
                            byte Bg = (byte)(val);
                            byte Bb = B.B;

                            if (Math.Abs(Br - Bb) < 5 || Math.Abs(Bg - Bb) < 5)
                                sum++;
                            
                            channels[0].SetPixel(x * _blockWidth + i, y * _blockHeight + j, Color.FromArgb(Rr, Rg, Rb));
                            channels[1].SetPixel(x * _blockWidth + i, y * _blockHeight + j, Color.FromArgb(Gr, Gg, Gb));
                            channels[2].SetPixel(x * _blockWidth + i, y * _blockHeight + j, Color.FromArgb(Br, Bg, Bb));
                        }
                    }
                }
            }

            layers.Add(sum);
            return channels;
        }

        private Bitmap GetBlock(Bitmap image, int xblock, int yblock)
        {
            Bitmap img = new Bitmap(_blockWidth + 1, _blockHeight + 1);

            for (int x = 0; x < _blockWidth + 1; x++)
            {
                for (int y = 0; y < _blockHeight + 1; y++)
                {
                    Color i = image.GetPixel(xblock * _blockWidth + x, yblock * _blockHeight + y);
                    img.SetPixel(x, y, i);
                }
            }

            return img;
        }


        private List<double> GetHistogram(Bitmap image, int channel)
        {
            var hist = new List<double>(new double[256]);
            
            for (int i = 0; i < image.Width; i++)
            {
                for (int j = 0; j < image.Height; j++)
                {
                    Color c = image.GetPixel(i, j);
                    byte value = 0;
                    switch (channel)
                    {
                        case 0:
                            value = c.R;
                            break;
                        case 1:
                            value = c.G;
                            break;
                        case 2:
                            value = c.B;
                            break;
                    }
                    hist[value]++;
                }
            }

            return hist;
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
                    red.SetPixel(i, j, Color.FromArgb(pixel.R, 255, 255));
                    green.SetPixel(i, j, Color.FromArgb(255, pixel.G, 255));
                    blue.SetPixel(i, j, Color.FromArgb(255, 255, pixel.B));
                }
            }

            channels.Add(red);
            channels.Add(green);
            channels.Add(blue);

            return channels;
        }

        private Bitmap ResizeImage(Bitmap image, bool shrink = false)
        {
            int width = image.Width;
            int height = image.Height;



            if(shrink)
                return new Bitmap(image, width - _blockWidth, height - _blockHeight);
            Bitmap btm = new Bitmap(image, width + _blockWidth, height + _blockHeight);
            
            // copy down

            int yu = height - 1;
            int yd = height;

            int wl = width - 1;
            int wr = width;

            for (int i = 0; i < 64; i++, yu--, yd++)
            {
                for (int w = 0; w < width; w++)
                {
                    btm.SetPixel(w, yd, btm.GetPixel(w, yu));
                }
            }

            for (int i = 0; i < 64; i++, wl--, wr++)
            {
                for (int h = 0; h < height; h++)
                {
                    btm.SetPixel(wr, i, btm.GetPixel(wl, i));
                }
            }


            return btm;
        }
    }
}
