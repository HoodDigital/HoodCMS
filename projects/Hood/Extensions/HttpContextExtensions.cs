﻿using Hood.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;

namespace Hood.Extensions
{
    public static class HttpContextExtensions
    {
        public static AccountInfo<HoodIdentityUser> GetAccountInfo(this HttpContext context)
        {
            return context.Items["AccountInfo"] as AccountInfo<HoodIdentityUser>;
        }

        public static bool IsLockedOut(this HttpContext context, List<string> allowedCodes)
        {
            if (context.User.IsInRole("Admin") || context.User.IsInRole("SuperUser"))
                return false;

            if (!context.Session.TryGetValue("LockoutModeToken", out byte[] betaCodeBytes))
                return true;
            var betaCode = System.Text.Encoding.Default.GetString(betaCodeBytes);

            if (allowedCodes.Contains(betaCode))
            {
                return false;
            }
            return true;
        }

        public static bool MatchesAccessCode(this HttpContext context, string code)
        {
            if (context.User.IsInRole("Admin") || context.User.IsInRole("SuperUser"))
                return true;

            if (!context.Session.TryGetValue("LockoutModeToken", out byte[] betaCodeBytes))
                return false;
            var betaCode = System.Text.Encoding.Default.GetString(betaCodeBytes);

            if (betaCode.Equals(code, StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }
            return false;
        }

        public static string GetSiteUrl(this HttpContext context, bool includePath = false, bool includeQuery = false)
        {
            // Return variable declaration
            var appPath = string.Empty;

            // Checking the current context content
            if (context?.Request != null)
            {
                //Formatting the fully qualified website url/name
                appPath = string.Format("{0}://{1}{2}",
                                        context.Request.Scheme,
                                        context.Request.Host,
                                        context.Request.PathBase);
                if (!appPath.EndsWith("/"))
                    appPath += "/";

                if (includePath)
                {
                    if (appPath.EndsWith("/"))
                        appPath = appPath.TrimEnd('/');
                    appPath += context.Request.Path;
                    if (includeQuery)
                        appPath += context.Request.QueryString.ToUriComponent();
                }

            }
            return appPath;
        }
    }
}
