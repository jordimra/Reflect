using System.Configuration;
using System.Data;
using System.Windows;
using System;

namespace Reflect.UI;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        AppDomain.CurrentDomain.UnhandledException += (s, ev) =>
        {
            MessageBox.Show($"Unhandled Exception: {ev.ExceptionObject}", "Error Fatal", MessageBoxButton.OK, MessageBoxImage.Error);
        };

        DispatcherUnhandledException += (s, ev) =>
        {
            MessageBox.Show($"Dispatcher Exception: {ev.Exception.Message}\n{ev.Exception.StackTrace}", "Error Fatal WPF", MessageBoxButton.OK, MessageBoxImage.Error);
            ev.Handled = true;
        };

        base.OnStartup(e);
    }
}

