﻿using Hood.Extensions;
using Hood.Interfaces;
using System;

namespace Hood.Models.Api
{
    public partial class MediaApi
    {
        public int Id { get; set; }
        public long FileSize { get; set; }
        public string FileType { get; set; }
        public string Filename { get; set; }
        public string Directory { get; set; }
        public string BlobReference { get; set; }
        public string Container { get; set; }
        public string DownloadUrl { get; set; }
        public string DownloadUrlHttps { get; set; }
        public int Status { get; set; }
        public string Title { get; set; }
        public System.DateTime CreatedOn { get; set; }
        public string UserVars { get; set; }
        public string Notes { get; set; }
        public string SystemNotes { get; set; }
        public string GeneralFileType { get; set; }
        public string UniqueId { get; set; }

        // Formatted Members
        public string Icon { get; set; }
        public string FormattedSize { get; set; }

        public string ThumbUrl { get; set; }
        public string SmallUrl { get; set; }
        public string MediumUrl { get; set; }
        public string LargeUrl { get; set; }

        public MediaApi(IMediaObject mi)
        {
            if (mi == null)
                return;
            if (mi != null)
            {
                mi.CopyProperties(this);
                this.FormattedSize = (mi.FileSize / 1024).ToString() + "Kb";
                this.DownloadUrl = mi.Url.Replace("https://", "http://");
                this.DownloadUrlHttps = mi.Url.Replace("http://", "https://");

                if (this.GeneralFileType == "Directory")
                {
                    this.DownloadUrl = this.DownloadUrl + this.Directory.ToSeoUrl();
                    this.DownloadUrlHttps = this.DownloadUrlHttps + this.Directory.ToSeoUrl();
                    this.Container = "N/A";
                    this.BlobReference = "N/A";
                }

            }
            // Formatted Members

            Hood.FileType type = Hood.FileType.Unknown;
            if (Enum.TryParse(GeneralFileType, out type))
            {
                switch (type)
                {
                    case Hood.FileType.Image:
                        this.Icon = mi.SmallUrl;
                        this.SmallUrl = mi.SmallUrl;
                        this.MediumUrl = mi.MediumUrl;
                        this.LargeUrl = mi.LargeUrl;
                        this.ThumbUrl = mi.ThumbUrl;
                        break;
                    case Hood.FileType.Excel:
                        this.Icon = "/lib/hood/images/icons/excel.png";
                        this.SmallUrl = Icon;
                        this.MediumUrl = Icon;
                        this.LargeUrl = Icon;
                        this.ThumbUrl = Icon;
                        break;
                    case Hood.FileType.PDF:
                        this.Icon = "/lib/hood/images/icons/pdf.png";
                        this.SmallUrl = Icon;
                        this.MediumUrl = Icon;
                        this.LargeUrl = Icon;
                        this.ThumbUrl = Icon;
                        break;
                    case Hood.FileType.PowerPoint:
                        this.Icon = "/lib/hood/images/icons/powerpoint.png";
                        this.SmallUrl = Icon;
                        this.MediumUrl = Icon;
                        this.LargeUrl = Icon;
                        this.ThumbUrl = Icon;
                        break;
                    case Hood.FileType.Word:
                        this.Icon = "/lib/hood/images/icons/word.png";
                        this.SmallUrl = Icon;
                        this.MediumUrl = Icon;
                        this.LargeUrl = Icon;
                        this.ThumbUrl = Icon;
                        break;
                    case Hood.FileType.Photoshop:
                        this.Icon = "/lib/hood/images/icons/photoshop.png";
                        this.SmallUrl = Icon;
                        this.MediumUrl = Icon;
                        this.LargeUrl = Icon;
                        this.ThumbUrl = Icon;
                        break;
                    case Hood.FileType.Unknown:
                        this.Icon = "/lib/hood/images/icons/file.png";
                        this.SmallUrl = Icon;
                        this.MediumUrl = Icon;
                        this.LargeUrl = Icon;
                        this.ThumbUrl = Icon;
                        break;
                }
            }

            if (string.IsNullOrEmpty(this.Icon))
            {
                this.Icon = "/lib/hood/images/no-image.jpg";
            }

        }

        public MediaApi()
        {
        }

        public static MediaApi Blank()
        {
            MediaApi ret = new MediaApi();

            // Formatted Members
            ret.FormattedSize = "0Kb";

            ret.Icon = "/lib/hood/images/no-image.jpg";
            ret.SmallUrl = "/lib/hood/images/no-image.jpg"; ;
            ret.MediumUrl = "/lib/hood/images/no-image.jpg";
            ret.LargeUrl = "/lib/hood/images/no-image.jpg";
            ret.ThumbUrl = "/lib/hood/images/no-image.jpg";
            ret.DownloadUrl = "";
            ret.DownloadUrlHttps = "";
            ret.Container = "N/A";
            ret.BlobReference = "N/A";

            return ret;
        }
    }
}
