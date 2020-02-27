using Prism.Commands;
using Prism.Mvvm;
using RSlackCleaner.Services.Slack;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;

namespace RSlackCleaner.Areas.Token
{
    public class TokenVM:BindableBase
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
                MessageBox.Show("Token nie został wprowadzony","", MessageBoxButton.OK);
            }
            else if (!(await SlackService.VerifyToken(UserToken)))
            {
                MessageBox.Show("Podany token jest nieprawidłowy.", "", MessageBoxButton.OK);
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

        private void CloseWindow(Window window,bool dialogResult)
        {
            window.DialogResult = dialogResult;
            window.Close();
        }

        private void GenerateToken()
        {
            string url = "https://api.slack.com/legacy/custom-integrations/legacy-tokens";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", url);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url); 
            }
            else
            {
                MessageBox.Show("Nie udało się uruchomić następującego adresu w przeglądarce:" + Environment.NewLine + url, "", MessageBoxButton.OK);
            }
        }
    }
}
