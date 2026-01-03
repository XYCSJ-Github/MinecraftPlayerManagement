using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Windows;
using System;

namespace g_mpm
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // 全局异常处理
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            DispatcherUnhandledException += Application_DispatcherUnhandledException;

            base.OnStartup(e);
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception? ex = e.ExceptionObject as Exception;
#pragma warning disable CS8604 // 引用类型参数可能为 null。
            HandleException(ex);
#pragma warning restore CS8604 // 引用类型参数可能为 null。
        }

        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            HandleException(e.Exception);
            e.Handled = true;
        }

        private void HandleException(Exception ex)
        {
            if (ex != null)
            {
                string errorMessage = $"未处理的异常:\n{ex.Message}\n\n堆栈跟踪:\n{ex.StackTrace}";

                MessageBox.Show(errorMessage, "g_mpm 错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}