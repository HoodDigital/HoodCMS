﻿using Hood.Core;
using Hood.Entities;
using Hood.Enums;
using Hood.Extensions;
using Hood.Interfaces;
using Hood.Services;
using Newtonsoft.Json;
using System;

namespace Hood.Models
{
    public partial class MediaObject :  BaseEntity, IMediaObject
    {
        public MediaObject()
        {
        }

        public MediaObject(IMediaObject mediaResult)
        {
            mediaResult.CopyProperties(this);
        }

        public long FileSize { get; set; }
        public string FileType { get; set; }
        public string Filename { get; set; }
        public string Directory { get; set; }
        public string BlobReference { get; set; }
        public string Url { get; set; }
        public DateTime CreatedOn { get; set; }
        public string GeneralFileType { get; set; }
        public string ThumbUrl { get; set; }
        public string SmallUrl { get; set; }
        public string MediumUrl { get; set; }
        public string LargeUrl { get; set; }
        public string UniqueId { get; set; }

        public virtual string Container
        {
            get
            {
                if (GeneralFileType == "Directory")
                    return "N/A";
                return Directory;
            }
        }
        public virtual string DownloadUrl => Url.Replace("https://", "http://");
        public virtual string DownloadUrlHttps => Url.Replace("http://", "https://");
        public virtual string Icon
        {
            get
            {
                GenericFileType type = GenericFileType.Unknown;
                string output = "";
                if (Enum.TryParse(GeneralFileType, out type))
                {
                    switch (type)
                    {
                        case GenericFileType.Image:
                            output = SmallUrl;
                            break;
                        case GenericFileType.Excel:
                            return "/lib/hood/images/icons/excel.png";
                        case GenericFileType.PDF:
                            return "/lib/hood/images/icons/pdf.png";
                        case GenericFileType.PowerPoint:
                            return "/lib/hood/images/icons/powerpoint.png";
                        case GenericFileType.Word:
                            return "/lib/hood/images/icons/word.png";
                        case GenericFileType.Photoshop:
                            return "/lib/hood/images/icons/photoshop.png";
                        case GenericFileType.Unknown:
                        default:
                            return "/lib/hood/images/icons/file.png";
                    }
                }
                if (!output.IsSet())
                {
                    output = NoImageUrl;
                }
                return output;
            }
        }
        public static string NoImageUrl
        {
            get
            {
                var output = "";
                var siteSettings = EngineContext.Current.Resolve<ISettingsRepository>();
                if (siteSettings != null)
                {
                    var mediaSettings = siteSettings.GetMediaSettings();
                    output = mediaSettings.NoImage.IsSet() ? mediaSettings.NoImage : "/lib/hood/images/no-image.jpg";
                }
                return output;
            }
        }
        public virtual string FormattedSize => (FileSize / 1024).ToString() + "Kb";
        public static IMediaObject Blank
        {
            get
            {
                var noImg = NoImageUrl;
                MediaObject ret = new MediaObject
                {
                    FileSize = 0,
                    Filename = "no-image.jpg",
                    SmallUrl = noImg,
                    MediumUrl = noImg,
                    LargeUrl = noImg,
                    ThumbUrl = noImg,
                    Url = noImg,
                    BlobReference = "N/A"
                };
                return ret;
            }
        }

        public string GetJson()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}