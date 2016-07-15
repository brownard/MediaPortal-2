﻿using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktListDetail : TraktList
  {
    [DataMember(Name = "updated_at")]
    public string UpdatedAt { get; set; }

    [DataMember(Name = "item_count")]
    public int ItemCount { get; set; }

    [DataMember(Name = "comment_count")]
    public int Comments { get; set; }

    [DataMember(Name = "likes")]
    public int Likes { get; set; }

    [DataMember(Name = "ids")]
    public TraktId Ids { get; set; }

    [DataMember(Name = "user")]
    public TraktUser User { get; set; }
  }
}