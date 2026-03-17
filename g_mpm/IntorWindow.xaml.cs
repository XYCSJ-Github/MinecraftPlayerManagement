using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

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

            Storyboard sb = (Storyboard)this.Resources["AbortIntor"];

            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(0.1);
            timer.Tick += (s, args) =>
            {
                sb.Begin(this);
                timer.Stop();
            };
            timer.Start();
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            Back.IsEnabled = false;
            button.IsEnabled = true;

            Storyboard sb = (Storyboard)this.Resources["AbortIntorB"];

            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(0.1);
            timer.Tick += (s, args) =>
            {
                sb.Begin(this);
                timer.Stop();
            };
            timer.Start();
        }
    }
}
