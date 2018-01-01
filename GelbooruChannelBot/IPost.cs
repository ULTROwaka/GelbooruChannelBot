using System;
using System.Collections.Generic;
using System.Text;

namespace GelbooruChannelBot
{ 
    interface IPost : IEquatable<IPost>
    {
        string GetId();
        string GetTags();
        string GetFileUrl();
        string GetHash();
        string GetTags(int count);
        string GetPostLink();
    }
}
