using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
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
            catch (Exception e)
            {
                Console.WriteLine($"(!) {DateTime.UtcNow}: {e.Source}:::{e.Message}");
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

            if (AnounceBot != null && AnounceChatId != 0)
            {
                AnounceBot.SendTextMessageAsync(AnounceChatId, $"{Instance} Start \n" +
                    $"CHANNEL_BOT_TOKEN: {Environment.GetEnvironmentVariable("CHANNEL_BOT_TOKEN")}\n" +
                    $"CHANNEL_CHAT_ID: {Environment.GetEnvironmentVariable("CHANNEL_CHAT_ID")}\n" +
                    $"CHANNEL_REQUEST_URL: {Environment.GetEnvironmentVariable("CHANNEL_REQUEST_URL")}");
            }

            Bot.Timeout = new TimeSpan(0, 0, 15);
            var thread = new Thread(() =>
            {
                Console.WriteLine($"(!) {DateTime.UtcNow}: Thread Created");
                while (true)
                {
                    try
                    {
                        Console.WriteLine($"(!) {DateTime.UtcNow}:Try URL {Url}");
                        switch (Instance)
                        {
                            case "Gelbooru":
                                SendImagesToChannel(GetNewestPosts<GelbooruPost>(Url, OldPostIdList, PostsPerCheck));
                                break;
                            case "Yandere":
                                SendImagesToChannel(GetNewestPosts<YanderePost>(Url, OldPostIdList, PostsPerCheck));
                                break;
                            default: Console.WriteLine($"(!) {DateTime.UtcNow}: {Instance} can`t start"); break;
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"(!) {DateTime.UtcNow}: {e.Message}");
                    }

                    Thread.Sleep(WaitTime);
                }
            });
            thread.Start();
            Console.ReadLine();
            Console.WriteLine($"(!) {DateTime.UtcNow}: {Instance} Stop");
        }


        public static bool RemoteCertValidateCallback(object sender, X509Certificate cert, X509Chain chain, System.Net.Security.SslPolicyErrors error)
        {
            return true;
        }

        static List<Post> GetNewestPosts<T>(string url, List<string> storage, int count = 1) where T : Post
        {

            bool firstTry = false;
            #if RELEASE
            if (storage.Count == 0) firstTry = true;
            #endif

            List<Post> newPosts = new List<Post>();
            url = url.Replace("*limit*", $"limit={count}");
            Console.WriteLine($"{DateTime.UtcNow}: Request {url}");
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
                Console.WriteLine($"(!) {DateTime.UtcNow}: {e.Source}:::{e.Message}");
                return newPosts;
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

        static async void SendImagesToChannel(List<Post> storage)
        {
            if (storage == null) return;
            if (storage.Count == 0) return;

            Console.WriteLine($"{DateTime.UtcNow}:Sending to channel {ChatId}");

            foreach (var post in storage)
            {                
                var keyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(new[]
                                    {
                                    new InlineKeyboardUrlButton("Post", post.GetPostLink())
                                    });

                string tags = post.GetTags(15);
#region WebM send
                //webm отправляем как ссылку
                if (post.GetFileUrl().Contains(".webm"))
                {
                    try
                    {
                        Console.WriteLine($"{DateTime.UtcNow}:Send WebM {post.GetPostLink()}");
                        await Bot.SendTextMessageAsync(ChatId, $"💕<a href=\"{post.GetPostLink()}\">WebM Link</a>💕\n{tags}", parseMode: Telegram.Bot.Types.Enums.ParseMode.Html, replyMarkup: keyboard, disableNotification: true);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"(!) {DateTime.UtcNow}: {e.Source}:::{e.Message} (url: {post.GetFileUrl()})\n\t(sample_url: {post.GetSampleUrl()})");
                    }
                    continue;
                }
#endregion
#region Gif send
                //gif отправляем как документ
                if (post.GetFileUrl().Contains(".gif"))
                {
                    try
                    {
                        Console.WriteLine($"{DateTime.UtcNow}:Send Gif {post.GetFileUrl()}");
                        await Bot.SendDocumentAsync(ChatId, new Telegram.Bot.Types.FileToSend(post.GetFileUrl()), caption: tags, replyMarkup: keyboard, disableNotification: true);
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine($"(!) {DateTime.UtcNow}: {e.Source}:::{e.Message} (url: {post.GetFileUrl()})\n\t(sample_url: {post.GetSampleUrl()})");
                    }
                    continue;
                }
#endregion
#region Pic send
                //jpeg, png и все остальное отправляем как фото                  
                try
                {
                    Console.WriteLine($"{DateTime.UtcNow}:Send Pic {post.GetFileUrl()}");
                    await Bot.SendPhotoAsync(ChatId, new Telegram.Bot.Types.FileToSend(post.GetFileUrl()), caption: tags, replyMarkup: keyboard, disableNotification: true);
                }
                catch(Exception e) //Переотправляем файл с меньшим размером
                {
                    Console.WriteLine($"(!) {DateTime.UtcNow}: {e.Source}:::{e.Message} (url: {post.GetFileUrl()})\n\t(sample_url: {post.GetSampleUrl()})");
                    Console.WriteLine($"\tResend Pic {post.GetSampleUrl()}");
                    try
                    {
                        await Bot.SendPhotoAsync(ChatId, new Telegram.Bot.Types.FileToSend(post.GetSampleUrl()), caption: tags, replyMarkup: keyboard, disableNotification: true);
                    }
                    catch
                    {
                        Console.WriteLine($"\t(!) Fail resend Pic {post.GetSampleUrl()}");
                    }               
                }
#endregion
            }
        }
    }
}
