using System.Windows;
using System.Windows.Threading;

namespace MarkItDown.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            if (args.ExceptionObject is Exception ex)
            {
                MessageBox.Show($"Dasturda kutilmagan xatolik yuz berdi:\n\n{ex.Message}\n\n{ex.StackTrace}", "MarkItDown Studio Xatosi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        };

        DispatcherUnhandledException += (s, args) =>
        {
            MessageBox.Show($"Xatolik:\n\n{args.Exception.Message}", "MarkItDown Studio", MessageBoxButton.OK, MessageBoxImage.Warning);
            args.Handled = true;
        };
    }
}
