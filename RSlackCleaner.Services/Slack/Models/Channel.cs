namespace RSlackCleaner.Services.Slack.Models
{
    public class Channel
    {
        public string Name { get; set; }
        public string ChannelId { get; set; }
        public ChannelType ChannelType { get; set; }
        public bool IsChecked { get; set; }
        public string MessagesCount { get; set; }
    }
}
