using Prism.Commands;
using Prism.Mvvm;
using RSlackCleaner.Areas.Token;
using RSlackCleaner.Services.Slack;
using RSlackCleaner.Services.Slack.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace RSlackCleaner.Areas.Main
{
    public class MainVM : BindableBase
    {
        public string UserToken;

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

        private long messagesDeleted;
        public long MessagesDeleted { get => messagesDeleted; set => SetProperty(ref messagesDeleted, value); }

        private long messagesCount;
        public long MessagesCount { get => messagesCount; set => SetProperty(ref messagesCount, value); }


        private Visibility progressBarVisibility = Visibility.Hidden;
        public Visibility ProgressBarVisibility { get => progressBarVisibility; set => SetProperty(ref progressBarVisibility, value); }

        public DelegateCommand SearchCmd { get; set; }
        public DelegateCommand DeleteMessagesCmd { get; set; }
        public DelegateCommand ReadUserTokenCmd { get; set; }

        public MainVM()
        {
            SearchCmd = new DelegateCommand(SearchMessagesOnChannels);
            DeleteMessagesCmd = new DelegateCommand(DeleteMessages);
            ReadUserTokenCmd = new DelegateCommand(ReadUserToken);
        }

        private void ReadUserToken()
        {
            TokenV tokenV = new TokenV();
            if (tokenV.ShowDialog() == true)
            {
                if (tokenV.DataContext is TokenVM tokenVm)
                {
                    UserToken = tokenVm.UserToken;
                }
            }
            else
            {
                App.Current.Shutdown();
            }
        }

        private async void DeleteMessages()
        {
            SetAppStatus(true);
            ProgressBarVisibility = Visibility.Visible;
            MessagesDeleted = 0;
            MessagesCount = Channels.Where(x => x.IsChecked).Sum(x => x.UserMessages);

            SlackService slackService = new SlackService(UserToken);
            await slackService.DeleteMessagesFromChannel(Channels.Where(x => x.IsChecked).ToArray(), SelectedDate);

            SetAppStatus(false);

            SearchMessagesOnChannels();
        }

        public async void SearchMessagesOnChannels()
        {
            SetAppStatus(true);

            SlackService slackService = new SlackService(UserToken);
            Channels = await slackService.GetChannels(SelectedDate);

            SetAppStatus(false);
        }



        public void SetAppStatus(bool isLoading)
        {
            IsSearchEnabled = !isLoading;
            IsDeleteMessagesEnabled = !isLoading;

            Mouse.OverrideCursor = isLoading ? Cursors.Wait : null;
        }

    }
}
