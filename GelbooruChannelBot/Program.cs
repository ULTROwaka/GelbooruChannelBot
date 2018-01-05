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
using Telegram.Bot.Types.InlineKeyboardButtons;
using FileToSend = Telegram.Bot.Types.FileToSend;
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
        static readonly int WaitTime = 600000; //2 min

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

            Console.WriteLine($"{DateTime.UtcNow}: {Url}");
            if (Url.Contains("gelbooru")) Instance = "Gelbooru";
            else if (Url.Contains("yande.re")) Instance = "Yandere";

            if (AnounceBot != null && AnounceChatId != 0)
            {
                AnounceBot.SendTextMessageAsync(AnounceChatId, $"{Instance} Start \n" +
                    $"CHANNEL_BOT_TOKEN: {Environment.GetEnvironmentVariable("CHANNEL_BOT_TOKEN")}\n" +
                    $"CHANNEL_CHAT_ID: {Environment.GetEnvironmentVariable("CHANNEL_CHAT_ID")}\n" +
                    $"CHANNEL_REQUEST_URL: {Environment.GetEnvironmentVariable("CHANNEL_REQUEST_URL")}");
            }

            Bot.Timeout = new TimeSpan(0, 1, 0);
            var thread = new Thread(() =>
            {
                Console.WriteLine($"{DateTime.UtcNow}: Thread Created");
                while (true)
                {
                    try
                    {
                        Console.WriteLine($"{DateTime.UtcNow}:Try URL {Url}");
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
                        LogWrite($"(!) {DateTime.UtcNow}: {e.Source}:::{e.Message}", ConsoleColor.Red);
                    }
                    LogWrite($"Wait {WaitTime}");
                    Thread.Sleep(WaitTime);
                    
                }
            });
            thread.Start();
            Console.ReadLine();
            Console.WriteLine($"{DateTime.UtcNow}: {Instance} Stop");
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
            url = url.Replace("*limit*", $"tags=webm&limit={count}");
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
                Console.WriteLine($"{DateTime.UtcNow}:New posts count {newPosts.Count}");
            }

            //Триммируем список старых постов (память не резиновая)
            if (storage.Count > MaxOldPostsCount) storage.RemoveRange(0, storage.Count - MaxOldPostsCount);

            newPosts.Reverse();
            return newPosts;
        }

        static Dictionary<string, List<PostBase>> GetAlbums(List<PostBase> storage)
        {
            var albums = new Dictionary<string, List<PostBase>>();

            foreach (var post in storage)
            {
                if (post.GetFileUrl().Contains(".webm") || post.GetFileUrl().Contains(".gif"))
                {
                    albums.Add(post.GetId(), new List<PostBase>(new[] { post }));
                    continue;
                }

                if (!albums.ContainsKey(post.GetPostAuthor()))
                {
                    albums.Add(post.GetPostAuthor(), new List<PostBase>(new[] { post }));
                }
                else
                {
                    if (albums[post.GetPostAuthor()].Count < 10)
                    {
                        albums[post.GetPostAuthor()].Add(post);
                    }
                    else
                    {
                        string altAuthorname = string.Concat(post.GetPostAuthor(), "*");
                        if (!albums.ContainsKey(altAuthorname))
                        {
                            albums.Add(altAuthorname, new List<PostBase>(new[] { post }));
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

        static async void SendImagesToChannel(List<PostBase> storage)
        {
            if (storage == null) return;
            if (storage.Count == 0) return;

            Console.WriteLine($"{DateTime.UtcNow}:Sending to channel {ChatId}");

            var albums = CompileAlbums(storage, 100, 4);
            List<PostBase> singlePosts = new List<PostBase>();
            foreach(var album in albums)
            {
                if (album.Value.Count > 1)
                {
                    LogWrite($"{album.Value.Count}", ConsoleColor.Yellow);
                    await SendAlbumAsync(album.Value);
                }
                else
                {
                    var post = album.Value[0];
                    string tags = post.GetTags(15);

                    //webm отправляем как ссылку
                    if (post.GetFileUrl().Contains(".webm"))
                    {
                        await SendWebmAsync(new[] { post });
                        continue;
                    }

                    //gif отправляем как документ
                    if (post.GetFileUrl().Contains(".gif"))
                    {
                        await SendGifAsync(new[] { post });
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
                    var keyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(new[]
                                    {
                                    new InlineKeyboardUrlButton("Post", post.GetPostLink())
                                    });

                    try
                    {
                        LogWrite($"{DateTime.UtcNow}:Send WebM {post.GetId()}", ConsoleColor.Yellow);
                        await Bot.SendTextMessageAsync(ChatId, $"💕<a href=\"{post.GetPostLink()}\">WebM Link</a>💕\n{post.GetTags(15)}", parseMode: Telegram.Bot.Types.Enums.ParseMode.Html, replyMarkup: keyboard, disableNotification: true);
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
                    var keyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(new[]
                                    {
                                    new InlineKeyboardUrlButton("Post", post.GetPostLink())
                                    });
                    try
                    {
                        LogWrite($"{DateTime.UtcNow}:Send gif {post.GetId()}", ConsoleColor.Yellow);
                        await Bot.SendDocumentAsync(ChatId, new FileToSend(new Uri(post.GetFileUrl())), caption: post.GetTags(15), replyMarkup: keyboard, disableNotification: true);
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
                var keyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(new[]
                                    {
                                    new InlineKeyboardUrlButton("Post", post.GetPostLink())
                                    });
                try
                {
                    long fileSize = post.GetOriginalSize();
                    if (fileSize < 5000000) //5Mb
                    {

                        LogWrite($"{DateTime.UtcNow}:Send pic {post.GetId()}", ConsoleColor.Yellow);
                        await Bot.SendPhotoAsync(ChatId, new FileToSend(new Uri(post.GetFileUrl())), caption: tags, replyMarkup: keyboard, disableNotification: true);
                        LogWrite($"{DateTime.UtcNow}:Pic sended {post.GetId()}", ConsoleColor.Green);
                    }
                    else
                    {
                        LogWrite($"{DateTime.UtcNow}:Send pic (sample) {post.GetId()}", ConsoleColor.Yellow);
                        await Bot.SendPhotoAsync(ChatId, new FileToSend(new Uri(post.GetSampleUrl())), caption: tags, replyMarkup: keyboard, disableNotification: true);
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
                        await Bot.SendPhotoAsync(ChatId, new Telegram.Bot.Types.FileToSend(new Uri(post.GetSampleUrl())), caption: tags, replyMarkup: keyboard, disableNotification: true);
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
            var mediaList = new List<Telegram.Bot.Types.InputMediaBase>();
            List<InlineKeyboardUrlButton[]> urlButtons = new List<InlineKeyboardUrlButton[]>();
            

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
                var media = new Telegram.Bot.Types.InputMediaPhoto
                {
                    Media = new Telegram.Bot.Types.InputMediaType(fileUrl),
                    Caption = postInAlbum.GetTags(10)
                };
                mediaList.Add(media);
                if(urlButtons.Count == 0)
                {
                    urlButtons.Add(new[] 
                    {
                        new InlineKeyboardUrlButton($"Post {mediaList.Count}", postInAlbum.GetPostLink()),
                        new InlineKeyboardUrlButton("This Channnel", "https://t.me/joinchat/AAAAAEJpUWYY8mRwJgUTtg")
                    });
                }
                else
                {
                    if (urlButtons[urlButtons.Count - 1][1].Text.Equals("This Channnel"))
                    {
                        try
                        {
                            urlButtons[urlButtons.Count - 1] [1] = new InlineKeyboardUrlButton($"Post {mediaList.Count}", postInAlbum.GetPostLink());
                        }
                        catch { }
                    }
                    else
                    {
                        try
                        {
                            urlButtons.Add(new[] 
                            {
                                new InlineKeyboardUrlButton($"Post {mediaList.Count}", postInAlbum.GetPostLink()),
                                new InlineKeyboardUrlButton("This Channnel", "https://t.me/joinchat/AAAAAEJpUWYY8mRwJgUTtg")
                            });

                        }
                        catch { }
                    }
                }
               
                
            }
            var keyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(urlButtons.ToArray());
            try
            {
                await Bot.SendMediaGroupAsync(ChatId, mediaList, disableNotification: true);
                await Bot.SendTextMessageAsync(ChatId, "🔺              🔺", replyMarkup: keyboard, disableNotification: true);
            }
            catch (Exception e)
            {
                Console.WriteLine($"(!) {DateTime.UtcNow}: {e.Source}:::{e.Message}");
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

        private static Dictionary<string, List<PostBase>> CompileAlbums(IEnumerable<PostBase> posts, int tagsCompareCount = 30, int equalityLevel = 2)
        {
            var albums = new Dictionary<string, Dictionary<string, List<PostBase>>>();
            foreach (PostBase post in posts)
            {
                var author = post.GetPostAuthor();
                var tags = post.GetTags(tagsCompareCount);

                if (!albums.ContainsKey(author))
                {
                    albums[author] = new Dictionary<string, List<PostBase>>(
                        new[]
                        {
                            new KeyValuePair<string, List<PostBase>>(tags, new List<PostBase>()
                            {
                                post
                            })
                        }
                        );
                }
                else
                {
                    var authorAlbums = albums[author];
                    bool added = false;
                    foreach (var key in authorAlbums.Keys)
                    {
                        if (authorAlbums[key].Count == 10) continue;
                        int equalTagsCount = 0;
                        var albumTags = key.Split(' ');
                        foreach (var albumTag in albumTags)
                        {
                            foreach (var postTag in tags.Split(' '))
                            {
                                if (albumTag.Equals(postTag))
                                {
                                    equalTagsCount++;
                                }
                            }
                        }
                        if (equalTagsCount  >= (key.Split(' ').Length /  equalityLevel) || equalTagsCount == tags.Split(' ').Length)
                        {
                            authorAlbums[key].Add(post);
                            added = true;
                            break;
                        }                       
                    }
                    if (!added)
                    {
                            authorAlbums[tags] = new List<PostBase>()
                            {
                                post
                            };
                    }
                }
            }
            var outDict = new Dictionary<string, List<PostBase>>();
            foreach (var authorAlbums in albums)
            {
                var author = authorAlbums.Key;
                foreach (var album in authorAlbums.Value)
                {
                    outDict.Add($"{author}*{album.Key}", album.Value);
                }
            }
            return outDict;
        }
     
    }
}
