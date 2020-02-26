using Prism.Commands;
using Prism.Mvvm;
using RSlackCleaner.Services.Slack;
using RSlackCleaner.Services.Slack.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;

namespace RSlackCleaner.Areas.Main
{
    public class MainVM : BindableBase
    {
        private string userToken;
        public string UserToken { get => userToken; set => SetProperty(ref userToken, value); }

        private DateTime selectedDate = DateTime.Now.AddDays(-7);
        public DateTime SelectedDate { get => selectedDate; set => SetProperty(ref selectedDate, value); }

        private bool isSearchEnabled = true;
        public bool IsSearchEnabled { get => isSearchEnabled; set => SetProperty(ref isSearchEnabled, value); }

        private bool isDeleteMessagesEnabled = false;
        public bool IsDeleteMessagesEnabled { get => isDeleteMessagesEnabled; set => SetProperty(ref isDeleteMessagesEnabled, value); }

        private List<Channel> channels;
        public List<Channel> Channels
        {
            get => channels;
            set
            {
                SetProperty(ref channels, value);
                RaisePropertyChanged(nameof(PrivateChannels));
                RaisePropertyChanged(nameof(PublicChannels));
                RaisePropertyChanged(nameof(DirectMessageChannels));
            }
        }

        public List<Channel> PrivateChannels { get => channels?.Where(x => x.ChannelType == ChannelType.Private).ToList(); set => SetProperty(ref channels, value); }
        public List<Channel> PublicChannels { get => channels?.Where(x => x.ChannelType == ChannelType.Public).ToList(); set => SetProperty(ref channels, value); }
        public List<Channel> DirectMessageChannels { get => channels?.Where(x => x.ChannelType == ChannelType.DirectMessage).ToList(); set => SetProperty(ref channels, value); }

        public DelegateCommand SearchCmd { get; set; }
        public DelegateCommand DeleteMessagesCmd { get; set; }
        public DelegateCommand GenerateTokenCmd { get; set; }

        public MainVM()
        {
            SearchCmd = new DelegateCommand(SearchMessagesOnChannels);
            DeleteMessagesCmd = new DelegateCommand(DeleteMessages);
            GenerateTokenCmd = new DelegateCommand(RedirectToGenerateToken);
        }

        private async void DeleteMessages()
        {
            SetAppStatus(true, IsSearchEnabled, IsDeleteMessagesEnabled);

            SlackService slackService = new SlackService(userToken);
            for (int i = 0; i < Channels.Count; i++)
            {
                if (Channels[i].IsChecked)
                {
                    await slackService.DeleteMessagesFromChannel(Channels[i], SelectedDate);
                }
            }

            SetAppStatus(false, IsSearchEnabled, IsDeleteMessagesEnabled);

            SearchMessagesOnChannels();
        }

        public async void SearchMessagesOnChannels()
        {
            SetAppStatus(true, IsSearchEnabled, IsDeleteMessagesEnabled);

            SlackService slackService = new SlackService(UserToken);
            Channels = await slackService.GetChannels(SelectedDate);

            SetAppStatus(false, IsSearchEnabled, IsDeleteMessagesEnabled);
        }

        private void RedirectToGenerateToken()
        {
            string url = "https://api.slack.com/legacy/custom-integrations/legacy-tokens";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); // Works ok on windows
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", url);  // Works ok on linux
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url); // Not tested
            }
            else
            {
                MessageBox.Show("Nie udało się uruchomić następującego adresu w przeglądarce:" + Environment.NewLine + url, "", MessageBoxButton.OK);
            }
        }

        public void SetAppStatus(bool isLoading, params bool[] buttonsEnabledProperty)
        {
            for (int i = 0; i < buttonsEnabledProperty.Count(); i++)
            {
                buttonsEnabledProperty[i] = !isLoading;
            }

            Mouse.OverrideCursor = isLoading ? Cursors.Wait : null;
        }

    }
}
