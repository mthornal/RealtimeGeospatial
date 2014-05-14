namespace RealtimeGeospatial.Web.SignalR
{    
    using Microsoft.AspNet.SignalR;
    using Microsoft.AspNet.SignalR.Hubs;

    [HubName("twitterHub")]
    public class TwitterHub : Hub
    {
        private readonly TwitterBroadcaster twitterBroadcaster;

        public TwitterHub() : this(TwitterBroadcaster.Instance) { }

        public TwitterHub(TwitterBroadcaster twitterBroadcaster)
        {
            this.twitterBroadcaster = twitterBroadcaster;
        }
    }
}