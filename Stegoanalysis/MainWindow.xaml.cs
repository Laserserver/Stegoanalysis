using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;

namespace Stegoanalysis
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly string[] _types = {"в 1", "во 2"};
        private bool _first = false;
        private bool _second = false;

        private void SetAnsName(int image)
        {
            if (SelectFirstLink.Content.ToString().Contains(".bmp"))
                if(image == 1)
                    image++;
                else
                    image--;
                
            AnsTB.Text = $"Информация зашита {_types[image - 1]} изображении - {(image == 1 ? SelectFirstLink.Content.ToString() : SelectSecondLink.Content.ToString())}.";
        }

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            (int number, var images) = Analyzer.Instance.Analyze(WidthTB.Text, HeightTB.Text, AlphaTB.Text);
            SetAnsName(number);
            FirstImageR.Source = images[0];
            SecondImageR.Source = images[1];
            R.Visibility = Visibility.Visible;
            FirstImageG.Source = images[2];
            SecondImageG.Source = images[3];
            G.Visibility = Visibility.Visible;
            FirstImageB.Source = images[4];
            SecondImageB.Source = images[5];
            B.Visibility = Visibility.Visible;
        }

        private Tuple<BitmapImage, string> GetImage()
        {
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() != true)
            {
                return null;
            }

            BitmapImage bi3 = new BitmapImage();
            try
            {
                bi3.BeginInit();
                bi3.UriSource = new Uri(ofd.FileName);
                bi3.EndInit();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex);
            }

            return Tuple.Create(bi3, ofd.FileName.Split('\\').Last());
        }

        private void SelectFirstLink_Click(object sender, RoutedEventArgs e)
        {
            
            var bi3 = GetImage();
            if (bi3 == null)
                return;
            FirstImage.Source = bi3.Item1;
            ImageHolder.Instance.BitmapImage2Bitmap(bi3.Item1, true);

            SelectFirstLink.Content = bi3.Item2;

            if (_second)
                Go.IsEnabled = true;
            _first = true;
        }

        private void SelectSecondLink_Click(object sender, RoutedEventArgs e)
        {
            var bi3 = GetImage();
            if (bi3 == null)
                return;
            SecondImage.Source = bi3.Item1;
            ImageHolder.Instance.BitmapImage2Bitmap(bi3.Item1, false);

            SelectSecondLink.Content = bi3.Item2;
            if (_first)
                Go.IsEnabled = true;
            _second = true;
        }
    }
}
