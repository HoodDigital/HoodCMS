﻿using System;
using System.Collections.Generic;
using Hood.Infrastructure;

namespace Hood.Models
{
    public class EditContentModel
    {
        public Content Content { get; set; }
        public ContentType Type { get; set; }
        public List<ContentCategory> Categories { get; set; }
        public List<string> Templates { get; set; }
        public IEnumerable<Subscription> Subscriptions { get; set; }
        public IList<ApplicationUser> Authors { get; internal set; }
        public OperationResult SaveResult { get; internal set; }
    }

    public class EditContentModelSend
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Excerpt { get; set; }
        public string Body { get; set; }
        public string Tags { get; set; }
        public string Slug { get; set; }
        public string AuthorId { get; set; }
        public int Status { get; set; }
        public int Views { get; set; }
        public bool Featured { get; set; }

        // Parent Content
        public int? ParentId { get; set; }

        public DateTime PublishDate { get; set; }
        public int PublishHour { get; set; }
        public int PublishMinute { get; set; }

    }
}