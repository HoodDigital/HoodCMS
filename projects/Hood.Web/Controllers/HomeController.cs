﻿using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Hood.Web.Controllers
{
    public class HomeController : Hood.Controllers.HomeController
    {
        public HomeController()
            : base()
        {}

        public override async Task<IActionResult> Index() => await base.Index();
    }
}
