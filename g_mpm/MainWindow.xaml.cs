using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace g_mpm
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Process p;
        public MainWindow()
        {
            InitializeComponent();

            ProcessStartInfo psi = new ProcessStartInfo()
            {
                FileName = "mpm.exe",
                UseShellExecute = false,
                CreateNoWindow = true
            };

            p = new Process();
            p.StartInfo = psi;
            p.Start();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var open_dir = new Microsoft.Win32.OpenFolderDialog();
            open_dir.Title = "打开文件夹";
            open_dir.ValidateNames = false;

            if(open_dir.ShowDialog() == true)
            {
                TB_path.Text = open_dir.FolderName;
            }
        }
    }
}