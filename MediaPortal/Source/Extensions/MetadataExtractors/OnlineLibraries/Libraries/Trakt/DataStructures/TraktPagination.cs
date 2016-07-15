﻿namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  public class TraktPagination
  {
    public int TotalPages { get; set; }
    public int TotalItemsPerPage { get; set; }
    public int TotalItems { get; set; }
    public int CurrentPage { get; set; }
  }
}