﻿using Hood.Extensions;
using Hood.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;


namespace Hood.Controllers
{
    public class InstallController : Controller
    {
        private readonly IApplicationLifetime _applicationLifetime;
        private readonly IConfiguration _config;
        public InstallController(IApplicationLifetime applicationLifetime, IConfiguration config)
        {
            _applicationLifetime = applicationLifetime;
            _config = config;
        }

        public IActionResult Install()
        {
            var provider = EmbeddedFiles.GetProvider();
            var files = provider.GetDirectoryContents("");     
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken()]
        public IActionResult Install(string reason)
        {
            if (!_config.CheckSetup("Installed"))
                _applicationLifetime.StopApplication();

            ViewData["Restarting"] = "App is restarting... please do not refresh the page.";
            return View();
        }
    }
}