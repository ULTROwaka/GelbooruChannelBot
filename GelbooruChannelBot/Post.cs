using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace GelbooruChannelBot
{
    abstract class Post : IEquatable<Post>
    {
        protected List<string> ImportantTags = new List<string> {"#loli", "#futanari", "#3d", "#yuri", "#happy_sex", "#vaginal",
           "#cum_in_pussy", "#cervical_penetration", "#x_ray", "#ahegao", "#animated", "#animated_gif", "#cosplay", "#looking_at_viewer",
            "#looking_at_viewer", "#as109", "#feral_lemma", "#bestiality", "#torogao",  "#thigh_high", "#dildo", "#bondage", "#masturbation",
            "#solo", "#anus", "#huge_penis", "#anal", "#oral", "#gokkun", "#gs_mantis", "#wntame, #zhaoyebai"};

        protected bool IsUnavaibleTag(string tag)
        {
            Regex reg = new Regex(@"[a-zA-Z]*[\d!+\/&?=:><;^@]+[a-zA-Z]*");
            return reg.IsMatch(tag) || tag.Equals("") || tag.Equals(" ");
        }

        protected List<string> GetImportantTags(string tags)
        {
            string[] tagsArray = tags.Split(' ');
            List<string> newTagsString = new List<string>();
            foreach (string tag in tagsArray)
            {
                if (!ImportantTags.Contains(tag)) continue;
                newTagsString.Add(tag);
            }
            return newTagsString;
        }

        protected string FilterateTags(string tags)
        {
            string outTags = "";
            foreach (var tag in tags.Split(' '))
            {
                if (IsUnavaibleTag(tag)) continue;
                outTags = String.Concat(outTags, String.Concat(" #", tag.Replace("-", "_").Replace("/", "_").Replace("(", "").Replace(")", "")));
            }

            return outTags;
        }

        abstract public string GetId();
        abstract public string GetTags();
        abstract public string GetFileUrl();
        abstract public string GetSampleUrl();
        abstract public string GetHash();
        abstract public string GetTags(int count);
        abstract public string GetPostLink();
        abstract public long GetOriginalSize();
        abstract public long GetSampleSize();

        abstract public bool Equals(Post other);
    }
}
