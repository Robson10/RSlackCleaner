﻿using RSlackCleaner.Resources.Configuration;
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
        private readonly SlackTaskClient SlackClient;
        private readonly string UserIdOfToken;

        public Action MessageDeleted;

        public SlackService(string token)
        {

            SlackClient = new SlackTaskClient(token);

            Task<string> task = Task.Run(GetUserIdBasedOnToken);
            task.Wait();
            UserIdOfToken = task.Result;
        }


        public static async Task<bool> VerifyToken(string userToken)
        {
            SlackTaskClient slackTaskClient = new SlackTaskClient(userToken);
            LoginResponse response = await slackTaskClient.ConnectAsync();
            return response.ok;
        }

        private async Task<string> GetUserIdBasedOnToken()
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
            LoginResponse res = await SlackClient.ConnectAsync();

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
                (long userMessages, long messageCount, List<Message> messages) = await GetMessages(item.id, Models.ChannelType.DirectMessage, messagesOlderThan);

                channels.Add(new Models.Channel(
                    name: slackUsers.FirstOrDefault(x => !string.IsNullOrEmpty(x.id) && x.id.Equals(item.user))?.profile?.real_name ?? item.user,// users.FirstOrDefault(y => item.user.Equals(y.Id))?.Name ?? item.user,
                    channelId: item.id,
                    channelType: Models.ChannelType.DirectMessage,
                    isChecked: false,
                    messages: messages,
                    userMessagesCount: userMessages,
                    messagesCount: messageCount
                    ));
            }

            return channels;
        }

        private async Task<IEnumerable<Models.Channel>> GetPublicChannels(Channel[] channels, DateTime messagesOlderThan)
        {
            List<Models.Channel> publicChannels = new List<Models.Channel>();
            foreach (Channel item in channels)
            {
                (long userMessages, long messageCount, List<Message> messages) = await GetMessages(item.id, Models.ChannelType.Public, messagesOlderThan);

                publicChannels.Add(new Models.Channel(
                    name: item.name,
                    channelId: item.id,
                    channelType: Models.ChannelType.Public,
                    isChecked: false,
                    messages: messages,
                    userMessagesCount: userMessages,
                    messagesCount: messageCount
                ));
            }
            return publicChannels;
        }

        private async Task<IEnumerable<Models.Channel>> GetPrivateChannels(Channel[] groups, DateTime messagesOlderThan)
        {
            List<Models.Channel> channels = new List<Models.Channel>();
            foreach (Channel item in groups)
            {
                (long userMessages, long messageCount, List<Message> messages) = await GetMessages(item.id, Models.ChannelType.Private, messagesOlderThan);

                channels.Add(new Models.Channel(
                    name: item.name,
                    channelId: item.id,
                    channelType: Models.ChannelType.Private,
                    isChecked: false,
                    messages: messages,
                    userMessagesCount: userMessages,
                    messagesCount: messageCount
                ));
            }
            return channels;
        }

        public async Task DeleteMessagesFromChannel(Models.Channel[] channels, DateTime messagesOlderThan)
        {
            foreach (Models.Channel channel in channels)
            {
                await DeleteMessages(channel.ChannelId, channel.Messages.Where(x => x.ts < messagesOlderThan).ToList());
            }
        }

        private async Task DeleteMessages(string channelId, List<Message> messageHistories)
        {
            if (messageHistories == null)
            {
                return;
            }
            int deleteInterval = 300;

            foreach (Message item in messageHistories)
            {
                if (string.IsNullOrEmpty(item.user) || item.user.Equals(UserIdOfToken))
                {
                    DeletedResponse response;
                    do
                    {
                        try
                        {
                            response = await SlackClient.DeleteMessageAsync(channelId, item.ts);
                        }
                        catch(NullReferenceException)
                        {
                            response = new DeletedResponse() { ok = true };
                        }
                        Thread.Sleep(deleteInterval);
                    } while ((response != null && !response.ok) && response.error.Equals(Config.SlackRateLimitError));

                    MessageDeleted?.Invoke();
                }
            }
        }


        private async Task<(long UserMessageCount, long AllMessageCount, List<Message> messages)> GetMessages(string channelId, Models.ChannelType channelType, DateTime messagesOlderThan)
        {
            long allMessageCount = 0;
            long userMessageCount = 0;
            bool isMoreMessages = true;
            List<Message> messages = new List<Message>();
            while (isMoreMessages)
            {
                MessageHistory slackMessages = null;
                switch (channelType)
                {
                    case Models.ChannelType.Public:
                        slackMessages = await SlackClient.GetConversationsHistoryAsync(new Channel() { id = channelId }, messagesOlderThan, new DateTime(2000, 01, 01), 1000);
                        break;
                    case Models.ChannelType.Private:
                        slackMessages = await SlackClient.GetConversationsHistoryAsync(new Channel() { id = channelId }, messagesOlderThan, new DateTime(2000, 01, 01), 1000);
                        break;
                    case Models.ChannelType.DirectMessage:
                        slackMessages = await SlackClient.GetConversationsHistoryAsync(new Channel() { id = channelId }, messagesOlderThan, new DateTime(2000, 01, 01), 1000);
                        break;
                }

                if (slackMessages?.messages != null)
                {
                    allMessageCount += slackMessages.messages.Count(x => string.IsNullOrEmpty(x.subtype) || !x.subtype.Equals(Config.SlackGroupJoinMessageType));
                    userMessageCount += slackMessages.messages
                        .Count(x => 
                            (string.IsNullOrEmpty(UserIdOfToken) || string.IsNullOrEmpty(x.user) || x.user.Equals(UserIdOfToken)) 
                            && (string.IsNullOrEmpty(x.subtype) || !x.subtype.Equals(Config.SlackGroupJoinMessageType))
                        );
                    messages.AddRange(slackMessages.messages);
                }

                if (slackMessages?.messages?.Count() >= 1000)
                {
                    messagesOlderThan = slackMessages.messages.Min(x => x.ts);
                }
                else
                {
                    isMoreMessages = false;
                }
            }

            return (userMessageCount, allMessageCount, messages);
        }
    }
}
