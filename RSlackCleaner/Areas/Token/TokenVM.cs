using Prism.Commands;
using Prism.Mvvm;
using RSlackCleaner.Resources.Configuration;
using RSlackCleaner.Resources.Localization;
using RSlackCleaner.Services.Slack;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;

namespace RSlackCleaner.Areas.Token
{
    public class TokenVM : BindableBase
    {
        private string userToken;
        public string UserToken { get => userToken; set => SetProperty(ref userToken, value); }

        public DelegateCommand<Window> OkCmd { get; set; }
        public DelegateCommand<Window> CloseCmd { get; set; }
        public DelegateCommand GenerateTokenCmd { get; set; }
        public TokenVM()
        {
            OkCmd = new DelegateCommand<Window>(Ok);
            CloseCmd = new DelegateCommand<Window>(Close);
            GenerateTokenCmd = new DelegateCommand(GenerateToken);
        }

        private async void Ok(Window window)
        {
            if (string.IsNullOrEmpty(UserToken))
            {
                MessageBox.Show(res.msgEmptyToken, res.TitleInformation, MessageBoxButton.OK);
            }
            else if (!(await SlackService.VerifyToken(UserToken)))
            {
                MessageBox.Show(res.msgInvalidToken, res.TitleInformation, MessageBoxButton.OK);
            }
            else
            {
                CloseWindow(window, true);
            }
        }

        private void Close(Window window)
        {
            CloseWindow(window, false);
        }

        private void CloseWindow(Window window, bool dialogResult)
        {
            try
            {
                window.DialogResult = dialogResult;
                window.Close();
            }
            catch (InvalidOperationException) { }
        }

        private void GenerateToken()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start(new ProcessStartInfo(Config.TokenWebPage) { UseShellExecute = true });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start(Config.LinuxCmdOpen, Config.TokenWebPage);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start(Config.OSXCmdOpen, Config.TokenWebPage);
            }
            else
            {
                MessageBox.Show(string.Format(res.msgCannotOpenWebUrl, Config.TokenWebPage), res.TitleError, MessageBoxButton.OK);
            }
        }
    }
}
