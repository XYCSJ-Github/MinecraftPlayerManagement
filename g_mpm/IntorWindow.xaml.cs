using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

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
    }
}
