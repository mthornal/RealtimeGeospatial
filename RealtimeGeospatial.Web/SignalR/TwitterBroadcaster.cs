namespace RealtimeGeospatial.Web.SignalR
{
    using System.Threading.Tasks;    

    using Microsoft.AspNet.SignalR;
    using Microsoft.AspNet.SignalR.Hubs;

    public class TwitterBroadcaster
    {
        // San Francisco
        //private const string BoundingBox = "-122.75,36.8,-121.75,37.8";

        // UK
        private const string BoundingBox = "-9.05,48.77,2.19,58.88";

        private static TwitterBroadcaster instance;

        private TwitterBroadcaster(IHubConnectionContext clients)
        {
            this.Clients = clients;                   

            Task.Run(() =>
                {
                    using (var tweetStreamer = new TweetStreamer(BoundingBox, this.BroadcastTweet))
                    {
                        tweetStreamer.StreamTweets();
                    }
                });
        }

        public static TwitterBroadcaster Instance
        {
            get
            {
                return instance ?? (instance = new TwitterBroadcaster(GlobalHost.ConnectionManager.GetHubContext<TwitterHub>().Clients));
            }
        }

        private IHubConnectionContext Clients { get; set; }

        private void BroadcastTweet(Tweet tweet)
        {
            this.Clients.All.broadcastTweet(tweet); 
        }
    }
}