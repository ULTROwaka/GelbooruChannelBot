using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Resources;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;

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
        static readonly int MaxOldPostsCount = 80;
        static readonly int PostsPerCheck = 40;
        static readonly int WaitTime = 300000;

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
                LogWrite($"(!) {DateTime.UtcNow}: {e.Source}:::{e.Message}", ConsoleColor.Red);
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
                LogWrite($"(!) {DateTime.UtcNow}: {e.Source}:::{e.Message}", ConsoleColor.Red);
            }

            LogWrite($"{DateTime.UtcNow}: {Url}");
            if (Url.Contains("gelbooru"))
            {
                Instance = "Gelbooru";
            }
            else
            {
                if (Url.Contains("yande.re"))
                {
                    Instance = "Yandere";
                }
                else
                {
                    if (Url.Contains("danbooru.donmai.us"))
                    {
                        Instance = "Danbooru";
                    }
                }
            }        

            if (AnounceBot != null && AnounceChatId != 0)
            {
                AnounceBot.SendTextMessageAsync(AnounceChatId, $"{Instance} Start \n" +
                    $"CHANNEL_BOT_TOKEN: {Environment.GetEnvironmentVariable("CHANNEL_BOT_TOKEN")}\n" +
                    $"CHANNEL_CHAT_ID: {Environment.GetEnvironmentVariable("CHANNEL_CHAT_ID")}\n" +
                    $"CHANNEL_REQUEST_URL: {Environment.GetEnvironmentVariable("CHANNEL_REQUEST_URL")}");
            }

            Bot.Timeout = new TimeSpan(0, 1, 0);
            var thread = new Thread( () =>
            {
                LogWrite($"{DateTime.UtcNow}: Thread Created");
                while (true)
                {
                    try
                    {
                        LogWrite($"{DateTime.UtcNow}:Try URL {Url}");
                        switch (Instance)
                        {
                            case "Gelbooru":
                                 SendToChannel(GetNewestPosts<GelbooruPost>(Url, OldPostIdList, PostsPerCheck)).Wait();
                                break;
                            case "Yandere":
                                 SendToChannel(GetNewestPosts<YanderePost>(Url, OldPostIdList, PostsPerCheck)).Wait();
                                break;
                            case "Danbooru":
                                 SendToChannel(GetNewestPosts<DanbooruPost>(Url, OldPostIdList, PostsPerCheck)).Wait();
                                break;
                            default: Console.WriteLine($"(!) {DateTime.UtcNow}: {Instance} can`t start"); break;
                        }
                    }
                    catch (Exception e)
                    {                       
                        LogWrite($"(!) {DateTime.UtcNow}: {e.Source}:{e.InnerException}:{e.StackTrace}:{e.Message}", ConsoleColor.Red);
                    }
                    LogWrite($"Wait {WaitTime}");
                    Thread.Sleep(WaitTime);                    
                }
#pragma warning disable CS0162 // Обнаружен недостижимый код
                LogWrite($"{DateTime.UtcNow}: Thread Dead");
#pragma warning restore CS0162 // Обнаружен недостижимый код
            });
            thread.Start();
            thread.Join();
            Console.ReadLine();
        }


        public static bool RemoteCertValidateCallback(object sender, X509Certificate cert, X509Chain chain, System.Net.Security.SslPolicyErrors error)
        {
            return true;
        }

        static List<PostBase> GetNewestPosts<T>(string url, List<string> storage, int count = 1) where T : PostBase
        {

            bool firstTry = false;
#if RELEASE
            if (storage.Count == 0) firstTry = true;
#endif
            List<PostBase> newPosts = new List<PostBase>();
            url = url.Replace("*limit*", $"limit={count}");
            Console.WriteLine($"{DateTime.UtcNow}: Request {url}");
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Timeout = 25000;
            HttpWebResponse resp;

            //Делаем запрос на получение Json данных постов
            try
            {
                resp = (HttpWebResponse)request.GetResponse();
            }
            catch (Exception e)//На случай отсутствия подключения к интернету
            {
                LogWrite($"(!) {DateTime.UtcNow}: {e.Source}:::{e.Message}", ConsoleColor.Red);
                return newPosts;
            }

            //Сериализуем полученные данные
            using (var reader = new StreamReader(resp.GetResponseStream()))
            {
                var str = reader.ReadToEnd();
                var posts = JsonConvert.DeserializeObject<List<T>>(str);

                foreach (var post in posts)
                {
                    //Выбираем только новые посты, no homo
                    if (!storage.Contains(post.GetId()) && !post.GetTags().Contains("#yaoi") && !post.GetTags().Contains("#male_focus") && !post.GetTags().Contains("#fat") && !post.GetTags().Contains("#pokemon"))
                    {
                        storage.Add(post.GetId());
                        if (!firstTry)
                        {
                            newPosts.Add(post);
                        }
                    }
                }
                Console.WriteLine($"{DateTime.UtcNow}:New posts count {newPosts.Count}");
            }

            //Триммируем список старых постов (память не резиновая)
            if (storage.Count > MaxOldPostsCount) storage.RemoveRange(0, storage.Count - MaxOldPostsCount);

            newPosts.Reverse();
            return newPosts;
        }

        static async Task SendToChannel(List<PostBase> storage)
        {         
            foreach (var pack in AnotherCompilePacks(storage))
            {
                if(pack.Count > 1 && pack.Count <= 10)
                {
                    LogWrite($"{DateTime.UtcNow}:Send Album");
                    await SendAlbumAsync(pack);
                    continue;
                }
                foreach (var post in pack)
                {
                    //webm отправляем как ссылку
                    if (post.GetFileUrl().Contains(".webm"))
                    {
                        LogWrite($"{DateTime.UtcNow}:Send Webm");
                        await SendWebmAsync(new[] { post });
                        continue;
                    }

                    //gif отправляем как документ
                    if (post.GetFileUrl().Contains(".gif"))
                    {
                        LogWrite($"{DateTime.UtcNow}:Send Gif");
                        await SendGifAsync(new[] { post });
                        continue;
                    }

                    //mp4 отправляем как видео
                    if (post.GetFileUrl().Contains(".mp4"))
                    {
                        await SendMp4Async(new[] { post });
                        continue;
                    }
                    //jpeg, png и все остальное отправляем как фото
                    await SendPicAsync(new[] { post });
                }
            }
        }

        private static async Task SendWebmAsync(IEnumerable<PostBase> posts)
        {
            foreach(var post in posts)
            {
                PostInfoLog(post);
                if (post.GetFileUrl().Contains(".webm"))
                {
                    var keyboard = new InlineKeyboardMarkup(new[]
                                    {
                                    InlineKeyboardButton.WithUrl("Post", post.GetPostLink())
                                    });

                    try
                    {
                        LogWrite($"{DateTime.UtcNow}:Send WebM {post.GetId()}", ConsoleColor.Yellow);
                        await Bot.SendTextMessageAsync(ChatId, $"💕<a href=\"{post.GetPostLink()}\">WebM Link</a>💕\n{post.GetTags(15)}",
                            parseMode: Telegram.Bot.Types.Enums.ParseMode.Html, replyMarkup: keyboard, disableNotification: true);
                        LogWrite($"{DateTime.UtcNow}:WebM sended {post.GetId()}", ConsoleColor.Green);
                    }
                    catch (Exception e)
                    {
                        LogWrite($"(!) {DateTime.UtcNow}: [{e.GetType()}] {e.Source}:::{e.Message} " +
                            $"(url: {post.GetFileUrl()})\n\t (url: {post.GetSampleUrl()})",
                            ConsoleColor.Red, 1);
                    }
                }
            }
        }

        private static async Task SendGifAsync(IEnumerable<PostBase> posts)
        {
            foreach (var post in posts)
            {
                PostInfoLog(post);
                if (post.GetFileUrl().Contains(".gif"))
                {
                    var keyboard = new InlineKeyboardMarkup(new[]
                                    {
                                    InlineKeyboardButton.WithUrl("Post", post.GetPostLink())
                                    });
                    try
                    {
                        LogWrite($"{DateTime.UtcNow}:Send gif {post.GetId()}", ConsoleColor.Yellow);
                        await Bot.SendDocumentAsync(ChatId, new InputOnlineFile(post.GetFileUrl()), caption: post.GetTags(15), 
                            replyMarkup: keyboard, disableNotification: true);
                        LogWrite($"{DateTime.UtcNow}:Gif sended  {post.GetId()}", ConsoleColor.Green);
                    }
                    catch (Exception e)
                    {
                        LogWrite($"(!) {DateTime.UtcNow}: [{e.GetType()}] {e.Source}:::{e.Message}" +
                             $" (url: {post.GetFileUrl()})\n\t(url: {post.GetSampleUrl()})",
                             ConsoleColor.Red, 1);
                    }
                }
            }

        }

        private static async Task SendPicAsync(IEnumerable<PostBase> posts)
        {
            foreach(var post in posts)
            {
                PostInfoLog(post);
                var tags = post.GetTags(15);
                var keyboard = new InlineKeyboardMarkup(new[] { InlineKeyboardButton.WithUrl("Post", post.GetPostLink()) });
                try
                {
                    long fileSize = post.GetOriginalSize();
                    if (fileSize < 5000000) //5Mb
                    {

                        LogWrite($"{DateTime.UtcNow}:Send pic {post.GetId()}", ConsoleColor.Yellow);
                        await Bot.SendPhotoAsync(ChatId, new InputOnlineFile(post.GetFileUrl()), caption: tags, replyMarkup: keyboard, 
                            disableNotification: true);                       
                        LogWrite($"{DateTime.UtcNow}:Pic sended {post.GetId()}", ConsoleColor.Green);
                    }
                    else
                    {
                        LogWrite($"{DateTime.UtcNow}:Send pic (sample) {post.GetId()}", ConsoleColor.Yellow);
                        await Bot.SendPhotoAsync(ChatId, new InputOnlineFile(post.GetSampleUrl()), caption: tags, replyMarkup: keyboard, 
                            disableNotification: true);
                        LogWrite($"{DateTime.UtcNow}:Pic sended (sample) {post.GetId()}", ConsoleColor.Green);
                    }
                }
                catch (Exception e)
                {
                    LogWrite($"(!) {DateTime.UtcNow}: [{e.GetType()}] {e.Source}:::{e.Message}" +
                        $" (url: {post.GetFileUrl()})\n\t(sample_url: {post.GetSampleUrl()})",
                        ConsoleColor.Red, 1);
                    try
                    {
                        LogWrite($"Resend pic(sample) {post.GetId()}\nUrl: {post.GetFileUrl()}", ConsoleColor.DarkYellow, 1);
                        await Bot.SendPhotoAsync(ChatId, new InputOnlineFile(post.GetSampleUrl()), caption: tags, replyMarkup: keyboard, 
                            disableNotification: true);
                        LogWrite($"{DateTime.UtcNow}:Pic resended {post.GetId()}", ConsoleColor.DarkGreen, 1);
                    }
                    catch
                    {
                        LogWrite($"{DateTime.UtcNow}:Resend fail {post.GetId()}", ConsoleColor.DarkRed, 1);
                    }

                }
            }
        }

        private static async Task SendAlbumAsync(List<PostBase> album)
        {
            var mediaList = new List<InputMediaBase>();
            List<InlineKeyboardButton[]> allButtons = new List<InlineKeyboardButton[]>();
            List<InlineKeyboardButton> row = new List<InlineKeyboardButton>();
            string links = "";   
            foreach (var postInAlbum in album)
            {
                string fileUrl = "";
                if (postInAlbum.GetOriginalSize() > 5000000)
                {
                    if (postInAlbum.GetSampleSize() < 5000000)
                    {
                        fileUrl = postInAlbum.GetSampleUrl();
                    }
                }
                else
                {
                    fileUrl = postInAlbum.GetFileUrl();
                }
                if (fileUrl.Equals("") || fileUrl.Contains(".gif") || fileUrl.Contains(".webm")) continue;
                var media = new InputMediaPhoto
                {
                    Media = new InputMedia(fileUrl),
                    Caption = postInAlbum.GetTags(10)
                };
                mediaList.Add(media);
                links = string.Concat(links, $"\n<a href=\"{postInAlbum.GetPostLink()}\">Post {mediaList.Count}</a>");
                row.Add(InlineKeyboardButton.WithUrl($"Post {mediaList.Count}", postInAlbum.GetPostLink()));
                if(row.Count == 2)
                {
                    allButtons.Add(row.ToArray());
                    row = new List<InlineKeyboardButton>();
                }
            }

            if (row.Count > 0)
            {
                allButtons.Add(row.ToArray());
            }
            var keyboard = new InlineKeyboardMarkup(allButtons);

            try
            {
                await Bot.SendMediaGroupAsync(ChatId, mediaList, disableNotification: true);
                await Bot.SendTextMessageAsync(ChatId, "🔗Links", replyMarkup: keyboard, disableNotification: true);
            }
            catch (Exception e)
            {
                LogWrite($"(!) {DateTime.UtcNow}: {e.Source}:::{e.Message}", ConsoleColor.Red);
                foreach(var media in mediaList)
                {
                    LogWrite($"(!) {DateTime.UtcNow}:{media.Media.Url}", ConsoleColor.Red);
                }
            }
        }

        private static async Task SendMp4Async(IEnumerable<PostBase> posts)
        {
            foreach (var post in posts)
            {
                PostInfoLog(post);
                if (post.GetFileUrl().Contains(".mp4"))
                {
                    var keyboard = new InlineKeyboardMarkup(new[]
                                    {
                                    InlineKeyboardButton.WithUrl("Post", post.GetPostLink())
                                    });

                    try
                    {
                        LogWrite($"{DateTime.UtcNow}:Send Mp4 {post.GetId()}", ConsoleColor.Yellow);
                        await Bot.SendPhotoAsync(ChatId, new InputOnlineFile(post.GetFileUrl()), caption: post.GetTags(10), replyMarkup: keyboard,
                           disableNotification: true);
                        LogWrite($"{DateTime.UtcNow}:Mp4 sended {post.GetId()}", ConsoleColor.Green);
                    }
                    catch (Exception e)
                    {
                        LogWrite($"(!) {DateTime.UtcNow}: [{e.GetType()}] {e.Source}:::{e.Message} " +
                            $"(url: {post.GetFileUrl()})\n\t (url: {post.GetSampleUrl()})",
                            ConsoleColor.Red, 1);
                    }
                }
            }
        }

        delegate void Writer(string text);

        private static void LogWrite(string text, ConsoleColor color = ConsoleColor.Gray, int level = 0, Writer writer = null)
        {
            StringBuilder builder = new StringBuilder("");
            builder.Append("\t", 0, level);
            builder.Append(text);
            if (writer != null)
            {
                writer(builder.ToString());
            }
            else
            {
                Console.ForegroundColor = color;
                Console.WriteLine(builder.ToString());
                Console.ResetColor();
            }
        }

        private static void PostInfoLog(PostBase post)
        {
            LogWrite($"{DateTime.UtcNow}:Orginal {post.GetFileUrl()}");
            LogWrite($"Id: {post.GetId()}", level: 1);
            LogWrite($"Size: {post.GetOriginalSize()}Byte", level: 1);

            LogWrite($"{DateTime.UtcNow}:Sample {post.GetSampleUrl()}");
            LogWrite($"Id: {post.GetId()}", level: 1);
            LogWrite($"Size: {post.GetSampleSize()}Byte", level: 1);
        }

        private static List<List<PostBase>> CompilePacks(IEnumerable<PostBase> posts)
        {
            LogWrite($"{DateTime.UtcNow}:Compile Packs", ConsoleColor.Cyan);
            List<List<PostBase>> packs = new List<List<PostBase>>();

            foreach(var post in posts)
            {
                bool added = false;
                foreach(var pack in packs)
                {
                    if(pack.Count == 10)
                    {
                        continue;
                    }
                    var tempPack = new List<PostBase>(pack);                   
                    foreach(var tempPost in tempPack)
                    {
                        LogWrite($"{DateTime.UtcNow}: - checking simmilarity of {post.GetId()} and {tempPost.GetId()}", ConsoleColor.Cyan);
                        if (post.IsSimilar(tempPost))
                        {
                            pack.Add(post);
                            added = true;
                            LogWrite($"{DateTime.UtcNow}: - {post.GetId()} simmilar {tempPost.GetId()}", ConsoleColor.Cyan);
                            break;                           
                        }
                    }

                    if (added)
                    {
                        break;
                    }
                }

                if (!added)
                {
                    packs.Add(new List<PostBase>()
                    {
                        post
                    });
                }

            }
            return packs;
        }

        private static List<List<PostBase>> AnotherCompilePacks(IEnumerable<PostBase> posts)
        {
            LogWrite($"{DateTime.UtcNow}:Compile Packs", ConsoleColor.Cyan);
            List<List<PostBase>> packs = new List<List<PostBase>>();

            foreach (var post in posts)
            {
                int maxSimilarityScore = 0;
                List<PostBase> maxSimilaryPack = null;
                foreach (var pack in packs)
                {
                    if (pack.Count == 10)
                    {
                        continue;
                    }
                    var tempPack = new List<PostBase>(pack);
                    int packSimilarityScore = 0;
                    foreach (var tempPost in tempPack)
                    {
                        LogWrite($"{DateTime.UtcNow}: - checking simmilarity of {post.GetId()} and {tempPost.GetId()}", ConsoleColor.Cyan);
                        packSimilarityScore += post.SimilarityScore(tempPost);
                    }
                    if (packSimilarityScore > maxSimilarityScore)
                    {
                        maxSimilarityScore = packSimilarityScore;
                        maxSimilaryPack = pack;
                    }
                }

                if (maxSimilaryPack == null)
                {
                    LogWrite($"{DateTime.UtcNow}: - {post.GetId()} not find pack, and create new", ConsoleColor.Cyan);
                    packs.Add(new List<PostBase>()
                    {
                        post
                    });
                }
                else
                {
                    LogWrite($"{DateTime.UtcNow}: - {post.GetId()} find pack with similarity score {maxSimilarityScore}", ConsoleColor.Cyan);
                    maxSimilaryPack.Add(post);
                }

            }

            return packs;
        }

    }
}
