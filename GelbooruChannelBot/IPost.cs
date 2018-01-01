using System;
using System.Collections.Generic;
using System.Text;

namespace GelbooruChannelBot
{ 
    interface IPost : IEquatable<IPost>
    {
        string Id { get; }
        string Tags { get; }
        string FileUrl { get; }
        string Hash { get; }
        string GetTags(int count);
        string GetPostLink();
    }
}
