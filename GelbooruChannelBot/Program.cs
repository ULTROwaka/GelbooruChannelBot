using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Resources;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.InlineKeyboardButtons;

namespace GelbooruChannelBot
{

    class Program
    {
        private static TelegramBotClient Bot;
        private static long ChatId;
        private static TelegramBotClient AnounceBot;
        private static long AnounceChatId;
        static string Url;
        static List<string> OldPostIdList = new List<string>();
        static readonly int MaxOldPostsCount = 40;
        static readonly int PostsPerCheck = 20;
        static readonly int WaitTime = 120000; //2 min

        static string Instance = "N/A";

        static void Main(string[] args)
        {
            ServicePointManager.ServerCertificateValidationCallback += new System.Net.Security.RemoteCertificateValidationCallback(RemoteCertValidateCallback);

            try
            {
                AnounceBot = new TelegramBotClient(Environment.GetEnvironmentVariable("ANNOUNCE_CHANNEL_BOT_TOKEN"));
                AnounceChatId = long.Parse(Environment.GetEnvironmentVariable("ANNOUNCE_CHANNEL_CHAT_ID"));
            }
            catch
            {
                AnounceBot = null;
                AnounceChatId = 0;
            }

            try
            {
                Bot = new TelegramBotClient(Environment.GetEnvironmentVariable("CHANNEL_BOT_TOKEN"));
                ChatId = long.Parse(Environment.GetEnvironmentVariable("CHANNEL_CHAT_ID"));
                Url = Environment.GetEnvironmentVariable("CHANNEL_REQUEST_URL");
            }
            catch (Exception e)
            {
                Console.WriteLine($"(!) {DateTime.UtcNow}: {e.Source}:::{e.Message}");
            }

            Console.WriteLine($"(!) {DateTime.UtcNow}: {Url}");
            if (Url.Contains("gelbooru")) Instance = "Gelbooru";
            else if (Url.Contains("yande.re")) Instance = "Yandere";

            if (AnounceBot!=null && AnounceChatId != 0)
            {
                AnounceBot.SendTextMessageAsync(AnounceChatId, $"{Instance} Start \n" +
                    $"CHANNEL_BOT_TOKEN: {Environment.GetEnvironmentVariable("CHANNEL_BOT_TOKEN")}" +
                    $"CHANNEL_CHAT_ID: {Environment.GetEnvironmentVariable("CHANNEL_CHAT_ID")}" +
                    $"CHANNEL_REQUEST_URL: {Environment.GetEnvironmentVariable("CHANNEL_REQUEST_URL")}");
            }
           
            Bot.Timeout = new TimeSpan(0, 0, 15);
            new Thread(() =>
            {
                Console.WriteLine($"(!) {DateTime.UtcNow}: Thread Created");
                while (true)
                {
                    try
                    {
                        Console.WriteLine($"(!) {DateTime.UtcNow}:Try URL {Url}");
                        if (Url.Contains("gelbooru"))
                        {                           
                            SendImagesToChannel(GetNewestPosts<GelbooruPost>(Url, OldPostIdList, PostsPerCheck));
                        }
                        else
                        {
                            if (Url.Contains("yande.re"))
                            {
                                SendImagesToChannel(GetNewestPosts<YanderePost>(Url, OldPostIdList, PostsPerCheck));
                            }
                        }
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine($"(!) {DateTime.UtcNow}: {e.Message}");
                    }
                    
                    Thread.Sleep(WaitTime);
                }

            }).Start();

            Console.ReadLine();
        }

        public static bool RemoteCertValidateCallback(object sender, X509Certificate cert, X509Chain chain, System.Net.Security.SslPolicyErrors error)
        {
            return true;
        }

        static List<Post> GetNewestPosts<T>(string url, List<string> storage, int count = 1) where T : Post
        {
            bool firstTry = false;
            if (storage.Count == 0) firstTry = true;

            List<Post> newPosts = new List<Post>();
            url = url.Replace("*limit*", $"limit={count}");

            Console.WriteLine($"(!) {DateTime.UtcNow}: Request {url}");
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Timeout = 15000;
            HttpWebResponse resp;

            //Делаем запрос на получение Json данных постов
            try
            {
                resp = (HttpWebResponse)request.GetResponse();
            }
            catch(Exception e)//На случай отсутствия подключения к интернету
            {
                Console.WriteLine($"{DateTime.UtcNow}: {e.Source}:::{e.Message}");
                return null;
            }

            //Сериализуем полученные данные
            using (var reader = new StreamReader(resp.GetResponseStream()))
            {
                var posts = JsonConvert.DeserializeObject<List<T>>(reader.ReadToEnd());

                foreach (var post in posts)
                {
                    //Выбираем только новые посты, no homo
                    if (!storage.Contains(post.GetId()) && !post.GetTags().Contains("#yaoi") && !post.GetTags().Contains("#male_focus"))
                    {
                        storage.Add(post.GetId());
                        if (!firstTry)
                        {
                            newPosts.Add(post);
                        }
                        
                    }
                }
                Console.WriteLine($"(!) {DateTime.UtcNow}:New posts count {newPosts.Count}");
            }

            //Триммируем список старых постов (память не резиновая)
            if (storage.Count > MaxOldPostsCount) storage.RemoveRange(0, storage.Count - MaxOldPostsCount);
        
            newPosts.Reverse();
            return newPosts;
        }

        static Dictionary<string, List<Post>> GetAlbums(List<Post> storage)
        {
            var albums = new Dictionary<string, List<Post>>();

            foreach (var post in storage)
            {                
                if (post.GetFileUrl().Contains(".webm") || post.GetFileUrl().Contains(".gif"))
                {
                    albums.Add(post.GetId(), new List<Post>(new[] { post }));
                    continue;
                }

                if (!albums.ContainsKey(post.GetPostAuthor()))
                {
                    albums.Add(post.GetPostAuthor(), new List<Post>(new[] { post }));
                }
                else
                { 
                    if(albums[post.GetPostAuthor()].Count < 10)
                    {
                        albums[post.GetPostAuthor()].Add(post);
                    }
                    else
                    {
                        string altAuthorname = string.Concat(post.GetPostAuthor(), "*");
                        if (!albums.ContainsKey(altAuthorname))
                        {
                            albums.Add(altAuthorname, new List<Post>(new[] { post }));
                        }
                        else
                        {
                                albums[altAuthorname].Add(post);
                        }
                    }                    
                }               
            }

            return albums;

        }

        static async void SendImagesToChannel(List<Post> storage)
        {
            if (storage.Count == 0 || storage == null) return;
            Console.WriteLine($"(!) {DateTime.UtcNow}:Sending to channel {ChatId}");
            List<Task<Telegram.Bot.Types.Message>> taskList = new List<Task<Telegram.Bot.Types.Message>>();
            
            var albums = GetAlbums(storage);
            foreach (var album in albums)
            {
                var mediaList = new List<Telegram.Bot.Types.InputMediaBase>();
                if (album.Value.Count > 1)
                {    
                    //Собираем альбом
                    foreach(var postInAlbum in album.Value)
                    {
                        var media = new Telegram.Bot.Types.InputMediaPhoto
                        {
                            Media = new Telegram.Bot.Types.InputMediaType(postInAlbum.GetFileUrl()),
                            Caption = postInAlbum.GetTags(10)
                        };
                        mediaList.Add(media);
                    }

                    //Отправляем альбом
                    try
                    {
                        await Bot.SendMediaGroupAsync(ChatId, mediaList, disableNotification: true);
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine($"(!!) {DateTime.UtcNow}: {e.Source}:::{e.Message}");
                    }                 
                    continue;
                }

                Post post = album.Value[0];
                var keyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(new[]
                                    {
                                    new InlineKeyboardUrlButton("Post", post.GetPostLink())
                                    });
                try
                {
                    string tags = post.GetTags(15);
                    //webm отправляем как ссылку
                    var str = post.GetFileUrl();
                    if (post.GetFileUrl().Contains(".webm"))
                    {
                        Console.WriteLine($"(!) {DateTime.UtcNow}:Send WebM {post.GetPostLink()}");
                        await Bot.SendTextMessageAsync(ChatId, $"💕<a href=\"{post.GetPostLink()}\">WebM Link</a>💕\n{post.GetTags(10)}",parseMode: Telegram.Bot.Types.Enums.ParseMode.Html, replyMarkup: keyboard, disableNotification: true);
                        continue;
                    }
                    //gif отправляем как документ
                    if (post.GetFileUrl().Contains(".gif"))
                    {
                        Console.WriteLine($"(!) {DateTime.UtcNow}:Send Gif {post.GetFileUrl()}");
                        await Bot.SendDocumentAsync(ChatId, new Telegram.Bot.Types.FileToSend(post.GetFileUrl()), caption: tags, replyMarkup: keyboard, disableNotification: true);
                        continue;
                    }
                    //jpeg, png и все остальное отправляем как фото
                    Console.WriteLine($"(!) {DateTime.UtcNow}:Send Pic {post.GetFileUrl()}");
                    await Bot.SendPhotoAsync(ChatId, new Telegram.Bot.Types.FileToSend(post.GetFileUrl()), caption: tags, replyMarkup: keyboard, disableNotification: true);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"{DateTime.UtcNow}: {e.Source}:::{e.Message} (url: {post.GetFileUrl()})");
                }
            }

        }
    }
}
