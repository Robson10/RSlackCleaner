using SlackAPI;
using System.Collections.Generic;
using System.Linq;

namespace RSlackCleaner.Services.Slack.Models
{
    public class Channel
    {
        public string Name { get; set; }
        public string ChannelId { get; set; }
        public ChannelType ChannelType { get; set; }
        public bool IsChecked { get; set; }
        public List<Message> Messages { get; set; }
        public long UserMessagesCount { get; set; }
        public long MessagesCount { get; set; }

        public Channel(string name, string channelId, ChannelType channelType, bool isChecked, List<Message> messages, long userMessagesCount, long messagesCount)
        {
            Name = name;
            ChannelId = channelId;
            ChannelType = channelType;
            IsChecked = isChecked;
            Messages = messages;
            UserMessagesCount = userMessagesCount;
            MessagesCount = messagesCount;
        }

    }
}
