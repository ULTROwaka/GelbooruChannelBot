using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace GelbooruChannelBot
{
    class YanderePost : Post
    {
        [JsonProperty("id")]
        public long Id { get; set; }

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

        [JsonProperty("created_at")]
        public long CreatedAt { get; set; }
        [JsonProperty("updated_at")]
        public long UpdatedAt { get; set; }
        [JsonProperty("creator_id")]
        public long CreatorId { get; set; }
        [JsonProperty("approver_id")]
        public object ApproverId { get; set; }
        [JsonProperty("author")]
        public string Author { get; set; } 
        [JsonProperty("change")]
        public long Change { get; set; }
        [JsonProperty("source")]
        public string Source { get; set; }
        [JsonProperty("score")]
        public long Score { get; set; }
        [JsonProperty("md5")]
        public string Md5 { get; set; }
        [JsonProperty("file_size")]
        public long FileSize { get; set; }
        [JsonProperty("file_ext")]
        public string FileExt { get; set; }
        [JsonProperty("file_url")]
        public string FileUrl { get; set; }
        [JsonProperty("is_shown_in_index")]
        public bool IsShownInIndex { get; set; }
        [JsonProperty("preview_url")]
        public string PreviewUrl { get; set; }
        [JsonProperty("preview_width")]
        public long PreviewWidth { get; set; }
        [JsonProperty("preview_height")]
        public long PreviewHeight { get; set; }
        [JsonProperty("actual_preview_width")]
        public long ActualPreviewWidth { get; set; }
        [JsonProperty("actual_preview_height")]
        public long ActualPreviewHeight { get; set; }
        [JsonProperty("sample_url")]
        public string SampleUrl { get; set; }
        [JsonProperty("sample_width")]
        public long SampleWidth { get; set; }
        [JsonProperty("sample_height")]
        public long SampleHeight { get; set; }
        [JsonProperty("sample_file_size")]
        public long SampleFileSize { get; set; }
        [JsonProperty("jpeg_url")]
        public string JpegUrl { get; set; }
        [JsonProperty("jpeg_width")]
        public long JpegWidth { get; set; }
        [JsonProperty("jpeg_height")]
        public long JpegHeight { get; set; }
        [JsonProperty("jpeg_file_size")]
        public long JpegFileSize { get; set; }
        [JsonProperty("rating")]
        public string Rating { get; set; }
        [JsonProperty("is_rating_locked")]
        public bool IsRatingLocked { get; set; }
        [JsonProperty("has_children")]
        public bool HasChildren { get; set; }
        [JsonProperty("parent_id")]
        public object ParentId { get; set; }
        [JsonProperty("status")]
        public string Status { get; set; }
        [JsonProperty("is_pending")]
        public bool IsPending { get; set; }
        [JsonProperty("width")]
        public long Width { get; set; }
        [JsonProperty("height")]
        public long Height { get; set; }
        [JsonProperty("is_held")]
        public bool IsHeld { get; set; }
        [JsonProperty("frames_pending_string")]
        public string FramesPendingString { get; set; }
        [JsonProperty("frames_pending")]
        public object[] FramesPending { get; set; }
        [JsonProperty("frames_string")]
        public string FramesString { get; set; }
        [JsonProperty("frames")]
        public object[] Frames { get; set; }
        [JsonProperty("is_note_locked")]
        public bool IsNoteLocked { get; set; }
        [JsonProperty("last_noted_at")]
        public long LastNotedAt { get; set; }
        [JsonProperty("last_commented_at")]
        public long LastCommentedAt { get; set; }

        public override bool Equals(Post other)
        {
            return Md5.Equals(other.GetHash());
        }

        public override string GetFileUrl()
        {
            return FileUrl;
        }

        public override string GetHash()
        {
            return Md5;
        }

        public override string GetId()
        {
            return Id.ToString();
        }

        public override string GetPostLink()
        {
            return $"https://yande.re/post/show/{Id}";
        }

        public override string GetTags()
        {
            return Tags;
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
            return SampleUrl;
        }

        public override long GetFileSize()
        {
            return FileSize;
        }
    }
}
