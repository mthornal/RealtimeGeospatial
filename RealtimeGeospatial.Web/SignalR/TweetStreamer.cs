namespace RealtimeGeospatial.Web.SignalR
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;    
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading;    

    using Newtonsoft.Json.Linq;

    public class TweetStreamer : IDisposable
    {
        private const string OAuthConsumerKey = "OAuthConsumerKey";
        private const string OAuthConsumerSecret = "OAuthConsumerSecret";

        private const string OAuthToken = "OAuthToken";
        private const string OAuthTokenSecret = "OAuthTokenSecret";

        private const string OAuthVersion = "1.0";
        private const string OAuthSignatureMethod = "HMAC-SHA1";

        private const string BaseFormat = "oauth_consumer_key={0}&oauth_nonce={1}&oauth_signature_method={2}" +
                          "&oauth_timestamp={3}&oauth_token={4}&oauth_version={5}";

        private const string HeaderFormat = "oauth_consumer_key=\"{0}\", oauth_nonce=\"{1}\", oauth_signature=\"{2}\", oauth_signature_method=\"{3}\", oauth_timestamp=\"{4}\", oauth_token=\"{5}\", oauth_version=\"{6}\"";

        private const string ResourceUrl = "https://stream.twitter.com/1.1/statuses/filter.json";

        private readonly HttpClient httpClient;

        private readonly string boundingBox; // e.g. San Francisco: "-122.75,36.8,-121.75,37.8"

        private readonly Action<Tweet> callback;

        private Stream reponseStream;

        private StreamReader responseStreamReader;

        private bool disposed;

        public TweetStreamer(string boundingBox, Action<Tweet> callback)
        {
            this.boundingBox = boundingBox;
            this.callback = callback;
            this.httpClient = new HttpClient();
        }

        public void StreamTweets()
        {
            Debug.WriteLine("StreamTweets-Start");

            var oAuthNonce = Convert.ToBase64String(new ASCIIEncoding().GetBytes(DateTime.Now.Ticks.ToString()));
            var timeSpan = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            var oAuthTimestamp = Convert.ToInt64(timeSpan.TotalSeconds).ToString();            

            var baseString = string.Format(
                BaseFormat,
                Uri.EscapeDataString(OAuthConsumerKey),
                Uri.EscapeDataString(oAuthNonce),
                Uri.EscapeDataString(OAuthSignatureMethod),
                Uri.EscapeDataString(oAuthTimestamp),
                Uri.EscapeDataString(OAuthToken),
                Uri.EscapeDataString(OAuthVersion));

            var escapedBoundingBox = Uri.EscapeDataString(this.boundingBox);
            var locationsParameter = string.Format("locations={0}", escapedBoundingBox);           

            // remember alphabetical order
            baseString = string.Concat(locationsParameter, "&", baseString);

            baseString = string.Concat(
                "POST&",
                Uri.EscapeDataString(ResourceUrl),
                "&",
                Uri.EscapeDataString(baseString));

            var compositeKey = string.Concat(
                Uri.EscapeDataString(OAuthConsumerSecret),
                "&",
                Uri.EscapeDataString(OAuthTokenSecret));

            string oauthSignature;
            using (var hasher = new HMACSHA1(new ASCIIEncoding().GetBytes(compositeKey)))
            {
                oauthSignature = Convert.ToBase64String(hasher.ComputeHash(new ASCIIEncoding().GetBytes(baseString)));
            }            

            var authHeader = string.Format(
                HeaderFormat,
                Uri.EscapeDataString(OAuthConsumerKey),
                Uri.EscapeDataString(oAuthNonce),
                Uri.EscapeDataString(oauthSignature),
                Uri.EscapeDataString(OAuthSignatureMethod),
                Uri.EscapeDataString(oAuthTimestamp),
                Uri.EscapeDataString(OAuthToken),
                Uri.EscapeDataString(OAuthVersion)
            );                       
            
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("OAuth", authHeader);
            httpClient.Timeout = TimeSpan.FromMilliseconds(Timeout.Infinite);

            var fullUrl = string.Format("{0}?{1}", ResourceUrl, locationsParameter); 
            var formUrlEncodedContent = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>());                                               
            var request = new HttpRequestMessage(HttpMethod.Post, fullUrl) { Content = formUrlEncodedContent };

            var response = httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).Result;

            this.reponseStream = response.Content.ReadAsStreamAsync().Result;
            this.responseStreamReader = new StreamReader(this.reponseStream);

            while (!this.responseStreamReader.EndOfStream)
            {
                //We are ready to read the stream
                var currentLine = this.responseStreamReader.ReadLine();

                dynamic streamedTweet = JObject.Parse(currentLine);

                if (streamedTweet != null && streamedTweet.coordinates != null
                    && streamedTweet.coordinates.coordinates != null)
                {
                    var longitude = streamedTweet.coordinates.coordinates[0];
                    var latitude = streamedTweet.coordinates.coordinates[1];

                    var tweet = new Tweet
                                {
                                    Message = streamedTweet.text,
                                    User = streamedTweet.user.screen_name,
                                    Longitude = longitude,
                                    Latitude = latitude
                                };

                    callback(tweet);
                }

                Debug.WriteLine("StreamTweets-while");                
            }
        }        

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    if (this.responseStreamReader != null)
                    {
                        this.responseStreamReader.Dispose();
                    }

                    if (this.reponseStream != null)
                    {
                        this.reponseStream.Dispose();
                    }

                    if (this.httpClient != null)
                    {
                        this.httpClient.Dispose();
                    }
                }
                this.disposed = true;
            }            
        }

        ~TweetStreamer()
        {
            this.Dispose(false);
        }
    }
}