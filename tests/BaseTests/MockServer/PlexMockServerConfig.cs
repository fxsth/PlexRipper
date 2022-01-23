﻿using System;

namespace PlexRipper.BaseTests
{
    public class PlexMockServerConfig
    {
        public Uri DownloadUri { get; set; }

        public Uri ServerUri { get; set; }

        public int DownloadFileSizeInMb { get; init; }

        public long DownloadFileSizeInBytes => DownloadFileSizeInMb * 1024;

        public static string FileUrl => "/library/parts/65125/1193813456/file.mp4";
    }
}