using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace TweetProcesser
{
    class Program
    {
        static void Main()
        {
            // You need to set your own keys and screen name
            var oAuthConsumerKey = "jkz4NQ7nXjalDlPnygvGQrBVb";
            var oAuthConsumerSecret = "f6wkDBFDDRSobsAQOhmlkdQ9nhMVEMFQwCyraxs0XXyPfPt0SI";
            var oAuthUrl = "https://api.twitter.com/oauth2/token";
            var screenname = "RukiRDX";

            // Do the Authenticate
            var authHeaderFormat = "Basic {0}";

            var authHeader = string.Format(authHeaderFormat,
                Convert.ToBase64String(Encoding.UTF8.GetBytes(Uri.EscapeDataString(oAuthConsumerKey) + ":" +
                Uri.EscapeDataString((oAuthConsumerSecret)))
            ));

            var postBody = "grant_type=client_credentials";

            HttpWebRequest authRequest = (HttpWebRequest)WebRequest.Create(oAuthUrl);
            authRequest.Headers.Add("Authorization", authHeader);
            authRequest.Method = "POST";
            authRequest.ContentType = "application/x-www-form-urlencoded;charset=UTF-8";
            authRequest.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            using (Stream stream = authRequest.GetRequestStream())
            {
                byte[] content = Encoding.ASCII.GetBytes(postBody);
                stream.Write(content, 0, content.Length);
            }

            authRequest.Headers.Add("Accept-Encoding", "gzip");

            WebResponse authResponse = authRequest.GetResponse();
            // deserialize into an object
            TwitAuthenticateResponse twitAuthResponse;
            using (authResponse)
            {
                using (var reader = new StreamReader(authResponse.GetResponseStream()))
                {
                    var objectText = reader.ReadToEnd();
                    twitAuthResponse = JsonConvert.DeserializeObject<TwitAuthenticateResponse>(objectText);
                }
            }

            //images sequence
            //847385119866232832; //847423600785215488; //847433954386788353; //847486963082969088;

            //need to change
            var latestId = 847424967146840070;//847434089594470402;//847486963082969088;
            string url = string.Format("https://api.twitter.com/1.1/search/tweets.json?q=%23conquerxians&result_type=mixed&max_id={0}&count=100", latestId);


            WebResponse response = DoSearch(url, twitAuthResponse);

            var searchJson = string.Empty;
            using (response)
            {
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    searchJson = reader.ReadToEnd();
                    dynamic array = JsonConvert.DeserializeObject(searchJson);
                    int i = 1;
                    foreach (var item in array.statuses)
                    {

                        string tags = string.Empty;
                        foreach (var tag in item.entities.hashtags)
                        {
                            tags += tag.text;
                        }

                        var type = item.extended_entities?.media[0].type;

                        if (type != null)
                        {
                            var mediaUrl = "";

                            //download photos
                            //if (type == "photo")
                            //{
                            //    mediaUrl = item.extended_entities.media[0].media_url;

                            //    //Task.Factory.StartNew(() => { DownloadMedia(mediaUrl, item.id, tags); });

                            //    using (WebClient client = new WebClient())
                            //    {
                            //        client.DownloadFile(new Uri(mediaUrl), "Media/" + item.id + ".jpg");
                            //    }
                            //}

                            //download videos
                            if (type == "video")
                            {
                                List<object> bitRateList = new List<object>();
                                foreach (var variant in item.extended_entities.media[0].video_info.variants)
                                {
                                    if (variant.bitrate != null)
                                    {
                                        bitRateList.Add(variant.bitrate);
                                    }
                                }

                                var maxRate = bitRateList.Max();

                                foreach (var variant in item.extended_entities.media[0].video_info.variants)
                                {
                                    if (maxRate == variant.bitrate)
                                    {
                                        mediaUrl = variant.url;
                                    }
                                }

                                using (WebClient client = new WebClient())
                                {
                                    client.DownloadFile(new Uri(mediaUrl), "Media/" + item.id + ".mp4");
                                }
                            }
                        }

                        i++;
                        Console.WriteLine(i + " - " + item.id + " Processed : " + type);
                    }

                }
            }

            //using (var r = new StreamReader("conq_feed.json"))
            //{
            //    var json = r.ReadToEnd();
            //    dynamic array = JsonConvert.DeserializeObject(json);

            //    foreach (var item in array)
            //    {
            //        var tags = item.entities.hashtags;
            //    }
            //}
        }

        public static WebResponse DoSearch(string query, TwitAuthenticateResponse twitAuthResponse)
        {
            HttpWebRequest searchRequest = (HttpWebRequest)WebRequest.Create(query);
            var requestHeaderFormat = "{0} {1}";
            searchRequest.Headers.Add("Authorization", string.Format(requestHeaderFormat, twitAuthResponse.token_type, twitAuthResponse.access_token));
            searchRequest.Method = "Get";
            return searchRequest.GetResponse();
        }

        static void DownloadMedia(string url, string id, string tags)
        {
            using (WebClient client = new WebClient())
            {
                client.DownloadFile(new Uri(url), id + "_" + tags + ".jpg");
            }
        }
    }
    class TwitAuthenticateResponse
    {
        public string token_type { get; set; }
        public string access_token { get; set; }
    }
}
