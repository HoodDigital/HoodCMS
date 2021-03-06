﻿using System.Collections.Generic;
using Hood.Models;
using Microsoft.Extensions.Configuration;

namespace Hood.Services
{
    public interface ISettingsRepository : IConfiguration
    {

        #region Get/Set/Delete
        T Get<T>(string key = null);
        void Set<T>(T value, string key = null);
        void Remove<T>(string key = null);
        #endregion

        #region Site Settings
        AccountSettings Account { get; }
        BasicSettings Basic { get; }
        ContactSettings Contact { get; }
        ContentSettings Content { get; }
        IntegrationSettings Integrations { get; }
        MailSettings Mail { get; }
        MediaSettings Media { get; }
        PropertySettings Property { get; }
        SeoSettings Seo { get; }
        #endregion
    }
}