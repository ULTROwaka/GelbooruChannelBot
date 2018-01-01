using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace GelbooruChannelBot
{
    class GelbooruPost : IPost
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
                foreach (var tag in value.Split(' '))
                {
                    if (IsUnavaibleTag(tag)) continue;
                    _tags = String.Concat(_tags, String.Concat(" #", tag.Replace("-", "_").Replace("/", "_").Replace("(", "").Replace(")", "")));
                }
            }
        }
        [JsonProperty("width")]
        public string Width { get; set; }
        [JsonProperty("file_url")]
        public string FileUrl { get; set; }

        private List<string> ImportantTags = new List<string> {"#loli", "#futanari", "#3d", "#yuri", "#happy_sex", "#vaginal",
           "#cum_in_pussy", "#cervical_penetration", "#x_ray", "#ahegao", "#animated", "#animated_gif", "#cosplay", "#looking_at_viewer",
            "#looking_at_viewer", "#as109", "#feral_lemma", "#bestiality", "#torogao",  "#thigh_high", "#dildo", "#bondage", "#masturbation",
            "#solo", "#anus", "#huge_penis", "#anal", "#oral", "#gokkun", "#gs_mantis", "#wntame"};

        private bool IsUnavaibleTag(string tag)
        {
            Regex reg = new Regex(@"[a-zA-Z]*[\d!+\/&?=:><;^@]+[a-zA-Z]*");
            return reg.IsMatch(tag) || tag.Equals("") || tag.Equals(" ");
        }     

        private List<string> GetImportantTags()
        {
            string[] tagsArray = _tags.Split(' ');          
            List<string> newTagsString = new List<string>();
            foreach (string tag in tagsArray)
            {
                if (!ImportantTags.Contains(tag)) continue;
                newTagsString.Add(tag);
            }
            return newTagsString;          
        }

        public string GetTags(int count)
        {
            List<string> tags = GetImportantTags();
            if(tags.Count < count)
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

        public override string ToString()
        {
            return $"{FileUrl}\n*💗 WEBM 💗*\nTags:\n{GetTags(10)}";
        }

        public bool Equals(IPost other)
        {
            return Hash.Equals(other.GetHash());
        }

        public string GetPostLink()
        {
            return $"https://gelbooru.com/index.php?page=post&s=view&id={Id}";
        }

        public string GetId()
        {
            return Id;
        }

        public string GetTags()
        {
            return Tags;
        }

        public string GetFileUrl()
        {
            return FileUrl;
        }

        public string GetHash()
        {
            return Hash;
        }
    }
}
