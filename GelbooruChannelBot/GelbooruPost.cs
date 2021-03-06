﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GelbooruChannelBot
{
    class GelbooruPost : PostBase
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
        public string ParentId { get; set; }
        [JsonProperty("rating")]
        public string Rating { get; set; }
        [JsonProperty("sample")]
        public bool Sample { get; set; }
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

        public override bool Equals(PostBase other)
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

        public override string GetSampleUrl()
        {
            var tmp = FileUrl.Replace("images", "samples");
            tmp = tmp.Insert(tmp.LastIndexOf('/') + 1, "sample_");
            tmp = tmp.Remove(tmp.LastIndexOf('.')) + ".jpg";
            return tmp;
        }

        public override long GetOriginalSize()
        {
            if (Sample)
            {
                return 6000000;
            }
            return 0;
        }

        public override long GetSampleSize()
        {
            return 0;
        }
        public override string GetPostAuthor()
        {
            return Owner;
        }

        public override bool IsSimilar(PostBase post, int trashold = 11)
        {
            return (SimilarityScore(post) >= trashold);
        }

        public override int SimilarityScore(PostBase post)
        {
            var otherPost = (GelbooruPost)post;
            int similarityScore = 0;

            if (FileUrl.Contains(".gif") || FileUrl.Contains(".webm"))
            {
                return -10;
            }

            List<string> thisTags = new List<string>(GetTags().Split(' ').Where(w => w != "#tagme" && w != "" && w != "#solo"));
            List<string> postTags = new List<string>(otherPost.GetTags().Split(' ').Where(w => w != "#tagme" && w != "" && w != "#solo"));

            foreach(var tag in thisTags.Intersect(postTags))
            {
                if (ImportantTags.Contains(tag))
                {
                    similarityScore += 100;
                }
                else
                {
                    similarityScore += 1;
                }
            }
            //similarityScore += thisTags.Intersect(postTags).Count();

            if (!GetPostAuthor().Equals("danbooru") && GetPostAuthor().Equals(otherPost.GetPostAuthor()))
            {              
                similarityScore += 100;
            }

            if (ParentId != null && otherPost.ParentId != null)
            {
                if (ParentId.Equals(otherPost.ParentId))
                {
                    similarityScore += 100;
                }
            }

            return similarityScore;
        }
    }
}
