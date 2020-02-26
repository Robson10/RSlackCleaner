using SlackAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RSlackCleaner.Services.Slack
{
    public class SlackService
    {
        private readonly string UserToken;
        private SlackTaskClient SlackClient;

        public SlackService(string token)
        {
            UserToken = token;
            SlackClient = CreateSlackClient();
        }

        private SlackTaskClient CreateSlackClient()
        {
            SlackTaskClient slackClient = new SlackTaskClient(UserToken);
            return slackClient;
        }

        private async Task<string> GetTokenUserId()
        {
            AuthTestResponse auth = await SlackClient.TestAuthAsync();
            return auth.user_id;
        }


        private List<Models.User> GetUsers(User[] users)
        {

            return users.Select(x => new Models.User
            {
                Id = x.id,
                Name = x.profile.real_name,
            }).ToList();
        }

        public async Task<List<Models.Channel>> GetChannels(DateTime messagesOlderThan)
        {
            List<Models.Channel> ChannelList = new List<Models.Channel>();
            var res = await SlackClient.ConnectAsync();

            ChannelList.AddRange(await GetPrivateChannels(res.groups, messagesOlderThan));
            ChannelList.AddRange(await GetPublicChannels(res.channels, messagesOlderThan));
            ChannelList.AddRange(await GetDirectMessageChannels(res.ims, res.users, messagesOlderThan));
            return ChannelList;
        }

        private async Task<IEnumerable<Models.Channel>> GetDirectMessageChannels(DirectMessageConversation[] ims, User[] slackUsers, DateTime messagesOlderThan)
        {
            List<Models.User> users = GetUsers(slackUsers);

            List<Models.Channel> channels = new List<Models.Channel>();
            foreach (DirectMessageConversation item in ims)
            {
                string messageCount = await GetMessageCount(item.id, Models.ChannelType.DirectMessage, messagesOlderThan);

                channels.Add(new Models.Channel()
                {
                    IsChecked = false,
                    ChannelType = Models.ChannelType.DirectMessage,
                    ChannelId = item.id,
                    Name = users.FirstOrDefault(y => item.user.Equals(y.Id))?.Name ?? item.user,
                    MessagesCount = messageCount
                });
            }

            return channels;
        }

        private async Task<IEnumerable<Models.Channel>> GetPublicChannels(Channel[] channels, DateTime messagesOlderThan)
        {
            List<Models.Channel> publicChannels = new List<Models.Channel>();
            foreach (Channel item in channels)
            {
                string messageCount = await GetMessageCount(item.id, Models.ChannelType.Public, messagesOlderThan);

                publicChannels.Add(new Models.Channel()
                {
                    IsChecked = false,
                    ChannelType = Models.ChannelType.Public,
                    ChannelId = item.id,
                    Name = item.name,
                    MessagesCount = messageCount
                });
            }
            return publicChannels;
        }

        private async Task<IEnumerable<Models.Channel>> GetPrivateChannels(Channel[] groups, DateTime messagesOlderThan)
        {
            List<Models.Channel> channels = new List<Models.Channel>();
            foreach (Channel item in groups)
            {
                string messageCount =  await GetMessageCount(item.id, Models.ChannelType.Private, messagesOlderThan);

                channels.Add(new Models.Channel()
                {
                    IsChecked = false,
                    ChannelType = Models.ChannelType.Private,
                    ChannelId = item.id,
                    Name = item.name,
                    MessagesCount = messageCount
                });
            }
            return channels;
        }

        /// <summary>
        /// Return private channels
        /// </summary>
        /// <returns>private channels.</returns>
        private async Task<List<Models.Channel>> GetPrivateChannels(DateTime messagesOlderThan)
        {
            GroupListResponse slackChannels = await SlackClient.GetGroupsListAsync(false);

            List<Models.Channel> channels = new List<Models.Channel>();
            foreach (SlackAPI.Channel item in slackChannels.groups)
            {
                string messageCount = await GetMessageCount(item.id, Models.ChannelType.Private, messagesOlderThan);

                channels.Add(new Models.Channel()
                {
                    IsChecked = false,
                    ChannelType = Models.ChannelType.Private,
                    ChannelId = item.id,
                    Name = item.name,
                    MessagesCount = messageCount
                });
            }
            return channels;
        }

        /// <summary>
        /// Return public channels.
        /// </summary>
        /// <returns>Public channels.</returns>
        private async Task<List<Models.Channel>> GetPublicChannels(DateTime messagesOlderThan)
        {
            ChannelListResponse slackChannels = await SlackClient.GetChannelListAsync(false);

            List<Models.Channel> channels = new List<Models.Channel>();
            foreach (SlackAPI.Channel item in slackChannels.channels)
            {
                string messageCount = await GetMessageCount(item.id, Models.ChannelType.Public, messagesOlderThan);

                channels.Add(new Models.Channel()
                {
                    IsChecked = false,
                    ChannelType = Models.ChannelType.Public,
                    ChannelId = item.id,
                    Name = item.name,
                    MessagesCount = messageCount
                });
            }

            return channels;
        }

        /// <summary>
        /// Return direct message channels.
        /// </summary>
        /// <returns>Direct message channels.</returns>
        private async Task<List<Models.Channel>> GetDirectMessageChannels(DateTime messagesOlderThan)
        {
            DirectMessageConversationListResponse slackChannels = await SlackClient.GetDirectMessageListAsync();

            List<Models.User> users = null;// await GetUsers();

            List<Models.Channel> channels = new List<Models.Channel>();
            foreach (DirectMessageConversation item in slackChannels.ims)
            {
                string messageCount = await GetMessageCount(item.id, Models.ChannelType.DirectMessage, messagesOlderThan);

                channels.Add(new Models.Channel()
                {
                    IsChecked = false,
                    ChannelType = Models.ChannelType.DirectMessage,
                    ChannelId = item.id,
                    Name = users.FirstOrDefault(y => item.user.Equals(y.Id))?.Name ?? item.user,
                    MessagesCount = messageCount
                });
            }

            return channels;
        }

        public async Task DeleteMessagesFromChannel(Models.Channel channel, DateTime messagesOlderThan)
        {
            string tokenUserId = await GetTokenUserId();
            MessageHistory messages = null;
            bool isDone = false;

            while (!isDone)
            {
                if (channel.ChannelType == Models.ChannelType.Public)
                {
                    messages = await SlackClient.GetChannelHistoryAsync(new SlackAPI.Channel() { id = channel.ChannelId }, messagesOlderThan, new DateTime(2000, 01, 01), 1000);
                }
                else if (channel.ChannelType == Models.ChannelType.Private)
                {
                    messages = await SlackClient.GetGroupHistoryAsync(new SlackAPI.Channel() { id = channel.ChannelId }, messagesOlderThan, new DateTime(2000, 01, 01), 1000);
                }
                else if (channel.ChannelType == Models.ChannelType.DirectMessage)
                {
                    messages = await SlackClient.GetDirectMessageHistoryAsync(new DirectMessageConversation() { id = channel.ChannelId }, messagesOlderThan, new DateTime(2000, 01, 01), 1000);
                }

                List<Message> messageHistory = messages.messages.Where(x => !string.IsNullOrEmpty(x.user) && x.user.Equals(tokenUserId)).ToList();

                if (messageHistory != null && messageHistory.Count > 0)
                {
                    await DeleteMessages(channel.ChannelId, messageHistory);
                }
                else
                {
                    isDone = true;
                }
            }
        }

        private async Task DeleteMessages(string channelId, List<Message> messageHistories)
        {
            if (messageHistories == null)
            {
                return;
            }

            string tokenUserId = await GetTokenUserId();

            foreach (Message item in messageHistories)
            {
                if (string.IsNullOrEmpty(item.user) || item.user.Equals(tokenUserId))
                {
                    await SlackClient.DeleteMessageAsync(channelId, item.ts);
                    Thread.Sleep(500);
                }
            }
        }

        private async Task<string> GetMessageCount(string channelId, Models.ChannelType channelType, DateTime messagesOlderThan)
        {
            string tokenUserId = await GetTokenUserId();
            long allMessageCount = 0;
            long userMessageCount = 0;
            bool isMoreMessages = true;

            while (isMoreMessages)
            {
                MessageHistory slackMessages = null;
                switch (channelType)
                {
                    case Models.ChannelType.Public:
                        slackMessages = await SlackClient.GetChannelHistoryAsync(new Channel() { id = channelId }, messagesOlderThan, new DateTime(2000, 01, 01), 1000);
                        break;
                    case Models.ChannelType.Private:
                        slackMessages = await SlackClient.GetGroupHistoryAsync(new Channel() { id = channelId }, messagesOlderThan, new DateTime(2000, 01, 01), 1000);
                        break;
                    case Models.ChannelType.DirectMessage:
                        slackMessages = await SlackClient.GetDirectMessageHistoryAsync(new DirectMessageConversation() { id = channelId }, messagesOlderThan, new DateTime(2000, 01, 01), 1000);
                        break;
                }

                if (slackMessages != null && slackMessages.messages != null)
                {
                    allMessageCount += slackMessages.messages.Count();
                    userMessageCount += slackMessages.messages.Count(x => string.IsNullOrEmpty(tokenUserId) || string.IsNullOrEmpty(x.user) || x.user.Equals(tokenUserId));
                }

                if (slackMessages.messages.Count() >= 1000)
                {
                    messagesOlderThan = slackMessages.messages.Min(x => x.ts);
                }
                else
                {
                    isMoreMessages = false;
                }
            }

            return userMessageCount + "/" +allMessageCount;
        }
    }
}
