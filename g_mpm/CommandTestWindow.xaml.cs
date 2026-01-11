using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace g_mpm
{
    /// <summary>
    /// CommandTestWindow.xaml 的交互逻辑
    /// </summary>
    public partial class CommandTestWindow : Window
    {
        SMC sharedMemoryCreator = new SMC();
        public CommandTestWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFolderDialog dialog = new OpenFolderDialog();
            dialog.ShowDialog();
            string path;

            if (dialog.FolderName != null)
            {
                DirName.Text = dialog.FolderName;
                path = dialog.FolderName;
                sharedMemoryCreator.SendCommand("", DefCommand.PCOMMAND_SER_PATH, dialog.FolderName);
            }
        }
    }
}
