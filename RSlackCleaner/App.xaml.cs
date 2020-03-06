using RSlackCleaner.Resources.Localization;
using System.Globalization;
using System.Threading;
using System.Windows;

namespace RSlackCleaner
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            SetResourceLanguage();
        }

        private void SetResourceLanguage()
        {
            res.Culture = CultureInfo.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.CurrentCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.CurrentCulture;
        }
    }
}
