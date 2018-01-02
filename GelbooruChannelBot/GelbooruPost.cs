using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace GelbooruChannelBot
{
    class GelbooruPost : Post
    {
        [JsonProperty("directory")]
        public string Directory { get; set; }
        [JsonProperty("hash")]
        public string Hash { get; set; }
        [JsonProperty("height")]
        public string Height { get; set; }
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("image")]
        public string Image { get; set; }
        [JsonProperty("change")]
        public string Change { get; set; }
        [JsonProperty("owner")]
        public string Owner { get; set; }
        [JsonProperty("parent_id")]
        public string Parent_id { get; set; }
        [JsonProperty("rating")]
        public string Rating { get; set; }
        [JsonProperty("sample")]
        public string Sample { get; set; }
        [JsonProperty("sample_height")]
        public string SampleHeight { get; set; }
        [JsonProperty("sample_width")]
        public string SampleWidth { get; set; }
        [JsonProperty("score")]
        public string Score { get; set; }

        private string _tags = "";
        [JsonProperty("tags")]
        public string Tags
        {
            get { return _tags; }
            set
            {
                _tags = FilterateTags(value);
            }
        }
        [JsonProperty("width")]
        public string Width { get; set; }
        [JsonProperty("file_url")]
        public string FileUrl { get; set; } 

        public override bool Equals(Post other)
        {
            return Hash.Equals(other.GetHash());
        }

        public override string GetPostLink()
        {
            return $"https://gelbooru.com/index.php?page=post&s=view&id={Id}";
        }

        public override string GetId()
        {
            return Id;
        }

        public override string GetTags()
        {
            return Tags;
        }

        public override string GetFileUrl()
        {
            return FileUrl;
        }

        public override string GetHash()
        {
            return Hash;
        }

        public override string GetTags(int count)
        {
            List<string> tags = GetImportantTags(_tags);
            if (tags.Count < count)
            {
                string[] tagsArray = _tags.Split(' ');
                foreach (string tag in tagsArray)
                {
                    if (!tags.Contains(tag)) tags.Add(tag);
                    if (tags.Count == count) break;
                }
            }
            return String.Join(' ', tags);
        }

    }
}
