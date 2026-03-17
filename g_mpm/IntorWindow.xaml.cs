using System.Windows;
using System.Windows.Media;

namespace g_mpm
{
    /// <summary>
    /// IntorWindow.xaml 的交互逻辑
    /// </summary>
    public partial class IntorWindow : Window
    {
        private readonly (TranslateTransform transform, double toX, double toY)[] _rectangles = new (TranslateTransform, double, double)[4];

        public IntorWindow()
        {
            InitializeComponent();
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            button.IsEnabled = false;
            Back.IsEnabled = true;
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            Back.IsEnabled = false;
            button.IsEnabled = true;
        }
    }
}
