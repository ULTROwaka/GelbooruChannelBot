using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Resources;
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
        static List<string> OldPostIdList = new List<string>();
        static readonly int MaxOldPostsCount = 40;
        static readonly int PostsPerCheck = 20;
        static readonly int WaitTime = 120000; //2 min
        static readonly string _Url = $"https://gelbooru.com/index.php?page=dapi&s=post&q=index&json=1";

        static void Main(string[] args)
        {
            ResourceManager resManager = new ResourceManager("GelbooruChannelBot.Properties.Resources", Assembly.GetExecutingAssembly());
            Bot = new TelegramBotClient(resManager.GetString("TelegramToken"));
            ChatId = long.Parse(resManager.GetString("ChatId"));
            Bot.Timeout = new TimeSpan(0, 0, 15);
            new Thread(() =>
            {
                while (true)
                {
                    try
                    {
                        SendImagesToChannel(GetNewestPosts<GelbooruPost>(_Url, OldPostIdList, PostsPerCheck));
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

        static List<IPost> GetNewestPosts<T>(string url, List<string> storage, int count = 1) where T : IPost
        {
            List<IPost> newPosts = new List<IPost>();
            url = String.Concat(url, $"&limit={count}");

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
                Console.WriteLine($"{DateTime.UtcNow}: {e.Message}");
                return null;
            }

            //Сериализуем полученные данные
            using (var reader = new StreamReader(resp.GetResponseStream()))
            {
                var posts = JsonConvert.DeserializeObject<List<T>>(reader.ReadToEnd());

                foreach (var post in posts)
                {
                    //Выбираем только новые посты
                    if (!storage.Contains(post.GetId()))
                    {
                        storage.Add(post.GetId());
                        newPosts.Add(post);
                    }
                }
            }

            //Триммируем список старых постов (память не резиновая)
            if (storage.Count > MaxOldPostsCount) storage.RemoveRange(0, storage.Count - MaxOldPostsCount);
        
            newPosts.Reverse();
            return newPosts;
        }

        static async void SendImagesToChannel(List<IPost> storage)
        {
            if (storage.Count == 0 || storage == null) return;

            List<Task<Telegram.Bot.Types.Message>> taskList = new List<Task<Telegram.Bot.Types.Message>>();
            foreach (var post in storage)
            {
                if (post.GetTags().Contains("#yaoi") || post.GetTags().Contains("#male_focus")) continue; //Yaoi for gays, oh wait...

                var keyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(new[]
                                    {
                                    new InlineKeyboardUrlButton("Post", post.GetPostLink())
                                    });
                try
                {
                    string tags = post.GetTags(15);
                    //webm отправляем как ссылку
                    if (post.GetFileUrl().Contains(".webm"))
                    {
                        await Bot.SendTextMessageAsync(ChatId, $"WebM\n <a href={"\""}{post.GetFileUrl()}{"\""} />\n{post.GetTags(10)}",parseMode: Telegram.Bot.Types.Enums.ParseMode.Html, replyMarkup: keyboard, disableNotification: true);
                        continue;
                    }
                    //gif отправляем как документ
                    if (post.GetFileUrl().Contains(".gif"))
                    {
                        await Bot.SendDocumentAsync(ChatId, new Telegram.Bot.Types.FileToSend(post.GetFileUrl()), caption: tags, replyMarkup: keyboard, disableNotification: true);
                        continue;
                    }
                    //jpeg, png и все остальное отправляем как фото
                    await Bot.SendPhotoAsync(ChatId, new Telegram.Bot.Types.FileToSend(post.GetFileUrl()), caption: tags, replyMarkup: keyboard, disableNotification: true);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"{DateTime.UtcNow}: {e.Message} (url: {post.GetFileUrl()})");
                }
            }

        }
    }
}
