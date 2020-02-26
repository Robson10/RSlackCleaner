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
        public SlackService(string token)
        {

        }
        private readonly string UserToken = string.Empty;

        private  SlackTaskClient CreateSlackClient()
        {
            SlackTaskClient slackClient = new SlackTaskClient(UserToken);
            return slackClient;
        }

        private async Task<string> GetTokenUserId()
        {
            SlackTaskClient slackClient = CreateSlackClient();
            AuthTestResponse auth = await slackClient.TestAuthAsync();
            return auth.user_id;
        }


        private async Task<List<Models.User>> GetUsers()
        {
            SlackTaskClient slackClient = CreateSlackClient();
            UserListResponse x = await slackClient.GetUserListAsync();

            return x.members.Select(x => new Models.User
            {
                Id = x.id,
                Name = x.profile.real_name,
            }).ToList();
        }

        public async Task<List<Models.Channel>> GetChannels(DateTime messagesOlderThan)
        {
            List<Models.Channel> ChannelList = new List<Models.Channel>();
            ChannelList.AddRange(await GetPrivateChannels(messagesOlderThan));
            ChannelList.AddRange(await GetPublicChannels(messagesOlderThan));
            ChannelList.AddRange(await GetDirectMessageChannels(messagesOlderThan));
            return ChannelList;
        }

        /// <summary>
        /// Return private channels
        /// </summary>
        /// <returns>private channels.</returns>
        private async Task<List<Models.Channel>> GetPrivateChannels(DateTime messagesOlderThan)
        {
            SlackTaskClient slackClient = CreateSlackClient();
            GroupListResponse slackChannels = await slackClient.GetGroupsListAsync(false);

            List<Models.Channel> channels = new List<Models.Channel>();
            foreach (Channel item in slackChannels.groups)
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
            SlackTaskClient slackClient = CreateSlackClient();
            ChannelListResponse slackChannels = await slackClient.GetChannelListAsync(false);

            List<Models.Channel> channels = new List<Models.Channel>();
            foreach (Channel item in slackChannels.channels)
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
            SlackTaskClient slackClient = CreateSlackClient();
            DirectMessageConversationListResponse slackChannels = await slackClient.GetDirectMessageListAsync();

            List<Models.User> users = await GetUsers();

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
            SlackTaskClient slackClient = CreateSlackClient();
            string tokenUserId = await GetTokenUserId();
            MessageHistory messages = null;
            bool isDone = false;

            while (!isDone)
            {
                if (channel.ChannelType == Models.ChannelType.Public)
                {
                    messages = await slackClient.GetChannelHistoryAsync(new Channel() { id = channel.ChannelId }, messagesOlderThan, new DateTime(2000, 01, 01), 1000);
                }
                else if (channel.ChannelType == Models.ChannelType.Private)
                {
                    messages = await slackClient.GetGroupHistoryAsync(new Channel() { id = channel.ChannelId }, messagesOlderThan, new DateTime(2000, 01, 01), 1000);
                }
                else if (channel.ChannelType == Models.ChannelType.DirectMessage)
                {
                    messages = await slackClient.GetDirectMessageHistoryAsync(new DirectMessageConversation() { id = channel.ChannelId }, messagesOlderThan, new DateTime(2000, 01, 01), 1000);
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
            SlackTaskClient slackClient = CreateSlackClient();

            foreach (Message item in messageHistories)
            {
                if (string.IsNullOrEmpty(item.user) || item.user.Equals(tokenUserId))
                {
                    await slackClient.DeleteMessageAsync(channelId, item.ts);
                    Thread.Sleep(500);
                }
            }
        }

        private async Task<string> GetMessageCount(string channelId, Models.ChannelType channelType, DateTime messagesOlderThan)
        {
            SlackTaskClient slackClient = CreateSlackClient();

            MessageHistory slackMessages = null;
            switch (channelType)
            {
                case Models.ChannelType.Public:
                    slackMessages = await slackClient.GetChannelHistoryAsync(new Channel() { id = channelId }, messagesOlderThan, new DateTime(2000, 01, 01), 1000);
                    break;
                case Models.ChannelType.Private:
                    slackMessages = await slackClient.GetGroupHistoryAsync(new Channel() { id = channelId }, messagesOlderThan, new DateTime(2000, 01, 01), 1000);
                    break;
                case Models.ChannelType.DirectMessage:
                    slackMessages = await slackClient.GetDirectMessageHistoryAsync(new DirectMessageConversation() { id = channelId }, messagesOlderThan, new DateTime(2000, 01, 01), 1000);
                    break;
            }

            if (slackMessages == null || slackMessages.messages == null)
            {
                return 0.ToString();
            }
            else
            {
                string tokenUserId = await GetTokenUserId();
                int count = slackMessages.messages.Count(x => string.IsNullOrEmpty(tokenUserId) || string.IsNullOrEmpty(x.user) || x.user.Equals(tokenUserId));

                if (count >= 1000)
                {
                    return "1000+";
                }
                else
                {
                    return count.ToString();
                }
            }
        }
    }
}
