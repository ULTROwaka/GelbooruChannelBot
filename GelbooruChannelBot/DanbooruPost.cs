using System;
using System.Net;
using System.Collections.Generic;

using Newtonsoft.Json;
using System.Linq;

namespace GelbooruChannelBot
{
    class DanbooruPost : PostBase
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; }
        [JsonProperty("uploader_id")]
        public string UploaderId { get; set; }
        [JsonProperty("score")]
        public string Score { get; set; }
        [JsonProperty("source")]
        public string Source { get; set; }
        [JsonProperty("md5")]
        public string Md5 { get; set; }
        [JsonProperty("last_comment_bumped_at")]
        public string LastCommentBumpedAt { get; set; }
        [JsonProperty("rating")]
        public string Rating { get; set; }
        [JsonProperty("image_width")]
        public string ImageWidth { get; set; }
        [JsonProperty("image_height")]
        public string ImageHeight { get; set; }
        private string _tags = "";
        [JsonProperty("tag_string")]
        public string Tag
        {
            get { return _tags; }
            set
            {
                _tags = FilterateTags(value);
            }
        }
        [JsonProperty("is_note_locked")]
        public bool IsNoteLocked { get; set; }
        [JsonProperty("fav_count")]
        public string FavCount { get; set; }
        [JsonProperty("file_ext")]
        public string FileExt { get; set; }
        [JsonProperty("last_noted_at")]
        public object LastNotedAt { get; set; }
        [JsonProperty("is_rating_locked")]
        public bool IsRatingLocked { get; set; }
        [JsonProperty("parent_id")]
        public string ParentId { get; set; }
        [JsonProperty("has_children")]
        public bool HasChildren { get; set; }
        [JsonProperty("approver_id")]
        public string ApproverId { get; set; }
        [JsonProperty("tag_count_general")]
        public long TagCountGeneral { get; set; }
        [JsonProperty("tag_count_artist")]
        public long TagCountArtist { get; set; }
        [JsonProperty("tag_count_character")]
        public long TagCountCharacter { get; set; }
        [JsonProperty("tag_count_copyright")]
        public long TagCountCopyright { get; set; }
        [JsonProperty("file_size")]
        public long FileSize { get; set; }
        [JsonProperty("is_status_locked")]
        public bool IsStatusLocked { get; set; }
        [JsonProperty("fav_string")]
        public string FavString { get; set; }
        [JsonProperty("pool_string")]
        public string PoolString { get; set; }
        [JsonProperty("up_score")]
        public long UpScore { get; set; }
        [JsonProperty("down_score")]
        public long DownScore { get; set; }
        [JsonProperty("is_pending")]
        public bool IsPending { get; set; }
        [JsonProperty("is_flagged")]
        public bool IsFlagged { get; set; }
        [JsonProperty("is_deleted")]
        public bool IsDeleted { get; set; }
        [JsonProperty("tag_count")]
        public long TagCount { get; set; }
        [JsonProperty("updated_at")]
        public DateTime UpdatedAt { get; set; }
        [JsonProperty("is_banned")]
        public bool IsBanned { get; set; }
        [JsonProperty("pixiv_id")]
        public string PixivId { get; set; }
        [JsonProperty("last_commented_at")]
        public object LastCommentedAt { get; set; }
        [JsonProperty("has_active_children")]
        public bool HasActiveChildren { get; set; }
        [JsonProperty("bit_flags")]
        public long BitFlags { get; set; }
        [JsonProperty("tag_count_meta")]
        public long TagCountMeta { get; set; }
        [JsonProperty("uploader_name")]
        public string UploaderName { get; set; }
        [JsonProperty("has_large")]
        public bool HasLarge { get; set; }
        [JsonProperty("has_visible_children")]
        public bool HasVisibleChildren { get; set; }
        [JsonProperty("children_ids")]
        public object ChildrenIds { get; set; }
        [JsonProperty("tag_string_general")]
        public string TagStringGeneral { get; set; }
        [JsonProperty("tag_string_character")]
        public string TagStringCharacter { get; set; }
        [JsonProperty("tag_string_copyright")]
        public string TagStringCopyright { get; set; }
        [JsonProperty("tag_string_artist")]
        public string TagStringArtist { get; set; }
        [JsonProperty("tag_string_meta")]
        public string TagStringMeta { get; set; }
        [JsonProperty("file_url")]
        public string FileUrl { get; set; }
        [JsonProperty("large_file_url")]
        public string LargeFileUrl { get; set; }
        [JsonProperty("preview_file_url")]
        public string PreviewFileUrl { get; set; }

        public override bool Equals(PostBase other)
        {
            return Md5.Equals(other.GetHash());
        }

        public override string GetFileUrl()
        {
           return $"http://danbooru.donmai.us{FileUrl}";
        }

        public override string GetHash()
        {
            return Md5;
        }

        public override string GetId()
        {
            return Id;
        }

        public override long GetOriginalSize()
        {
            return FileSize;
        }

        public override string GetPostAuthor()
        {
            return UploaderName;
        }

        public override string GetPostLink()
        {
            return $" http://danbooru.donmai.us/posts/{Id}";
        }

        public override long GetSampleSize()
        {
            return 0;
        }

        public override string GetSampleUrl()
        {
            return $"http://danbooru.donmai.us{LargeFileUrl}";
        }

        public override string GetTags()
        {
            return Tag;
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

        public override bool IsSimilar(PostBase post, int trashold = 2)
        {
            return (SimilarityScore(post) >= trashold);
        }

        public override int SimilarityScore(PostBase post)
        {
            var otherPost = (DanbooruPost)post;
            int similarityScore = 0;

            if (FileUrl.Contains(".gif") || FileUrl.Contains(".webm") || FileUrl.Contains(".mp4"))
            {
                return -1000000;
            }

            List<string> thisTags = new List<string>(TagStringGeneral.Split(' ').Where(w => w != "#tagme" && w != "" && w != "#solo"));
            List<string> postTags = new List<string>(otherPost.TagStringGeneral.Split(' ').Where(w => w != "#tagme" && w != "" && w != "#solo"));
            var generalIntersect = thisTags.Intersect(postTags);

            thisTags = new List<string>(TagStringCharacter.Split(' '));
            postTags = new List<string>(otherPost.TagStringCharacter.Split(' '));
            var characterIntersect = thisTags.Intersect(postTags);

            thisTags = new List<string>(TagStringCopyright.Split(' '));
            postTags = new List<string>(otherPost.TagStringCopyright.Split(' '));
            var copyrightIntersect = thisTags.Intersect(postTags);

            thisTags = new List<string>(TagStringArtist.Split(' '));
            postTags = new List<string>(otherPost.TagStringArtist.Split(' '));
            var artistIntersect = thisTags.Intersect(postTags);

            similarityScore += generalIntersect.Count();
            similarityScore += characterIntersect.Count() * 10;
            similarityScore += generalIntersect.Count() * 100;
            similarityScore += generalIntersect.Count() * 1000;

            return similarityScore;
        }
    }

}
