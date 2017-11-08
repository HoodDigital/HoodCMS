﻿using Hood.Extensions;
using Hood.Models;
using Hood.Models.Api;
using Hood.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace Hood.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Manager")]
    public class UsersController : Controller
    {
        private readonly UserManager<HoodIdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IAccountRepository _auth;
        private readonly IContentRepository _content;
        private readonly HoodDbContext<HoodIdentityUser> _db;
        private readonly IEmailSender _email;
        private readonly ISettingsRepository<HoodIdentityUser> _settings;
        private readonly IBillingService _billing;

        public UsersController(
            HoodDbContext<HoodIdentityUser> db,
            IAccountRepository auth,
            UserManager<HoodIdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IEmailSender email,
            ISettingsRepository<HoodIdentityUser> site,
            IBillingService billing,
            IContentRepository content)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _auth = auth;
            _db = db;
            _email = email;
            _settings = site;
            _content = content;
            _billing = billing;
        }

        [Route("admin/users/")]
        public IActionResult Index()
        {
            return View();
        }

        [Route("admin/users/edit/{id}/")]
        public async Task<IActionResult> Edit(string id)
        {
            EditUserModel<HoodIdentityUser> um = new EditUserModel<HoodIdentityUser>()
            {
                User = _auth.GetUserById(id)
            };
            um.Roles = await _userManager.GetRolesAsync(um.User);
            um.AllRoles = _auth.GetAllRoles();
            return View(um);
        }

        [Route("admin/users/edit/{id}/")]
        [HttpPost]
        public async Task<IActionResult> Edit(string id, SaveProfileModel model)
        {
            EditUserModel<HoodIdentityUser> um = new EditUserModel<HoodIdentityUser>()
            {
                User = _auth.GetUserById(id)
            };
            model.User.CopyProperties(um.User);
            _auth.UpdateUser(um.User);

            // reload model

            um.Roles = await _userManager.GetRolesAsync(um.User);
            um.AllRoles = _auth.GetAllRoles();
            return View(um);
        }

        [Route("admin/users/blade/{id}/")]
        public async Task<IActionResult> Blade(string id)
        {
            EditUserModel<HoodIdentityUser> um = new EditUserModel<HoodIdentityUser>()
            {
                User = _auth.GetUserById(id)
            };
            um.Roles = await _userManager.GetRolesAsync(um.User);
            um.AllRoles = _auth.GetAllRoles();
            return View(um);
        }

        [Route("admin/users/create/")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpGet]
        [Route("admin/users/roles/")]
        public async Task<IActionResult> Roles(string id)
        {
            EditUserModel<HoodIdentityUser> um = new EditUserModel<HoodIdentityUser>()
            {
                User = _auth.GetUserById(id)
            };
            um.Roles = await _userManager.GetRolesAsync(um.User);
            return View(um);
        }

        [Route("admin/users/reset/")]
        [HttpPost]
        public async Task<Response> ResetPassword(string id, string password)
        {
            try
            {
                HoodIdentityUser user = await _userManager.FindByIdAsync(id);
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var result = await _userManager.ResetPasswordAsync(user, token, password);
                if (result.Succeeded)
                {
                    return new Response(true);
                }
                else
                {
                    string error = "";
                    foreach (var err in result.Errors)
                    {
                        error += err.Description + Environment.NewLine;
                    }
                    throw new Exception(error);
                }
            }
            catch (Exception ex)
            {
                return new Response(ex.Message);
            }
        }

        [Route("admin/users/get/")]
        [HttpGet]
        public async Task<JsonResult> Get(ListFilters request, string search, string sort, string role)
        {
            IList<HoodIdentityUser> users = null;
            if (!string.IsNullOrEmpty(role))
            {
                users = await _userManager.GetUsersInRoleAsync(role);
            }
            else
            {
                users = await _userManager.Users.ToListAsync();
            }
            if (!string.IsNullOrEmpty(search))
            {
                string[] searchTerms = search.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                users = users.Where(n => searchTerms.Any(s => n.UserName.ToLower().Contains(s.ToLower()))).ToList();
            }
            switch (sort)
            {
                case "UserName":
                    users = users.OrderBy(n => n.UserName).ToList();
                    break;
                case "Email":
                    users = users.OrderBy(n => n.Email).ToList();
                    break;
                case "LastName":
                    users = users.OrderBy(n => n.LastName).ToList();
                    break;
                case "LastLogOn":
                    users = users.OrderByDescending(n => n.LastLogOn).ToList();
                    break;

                case "UserNameDesc":
                    users = users.OrderByDescending(n => n.UserName).ToList();
                    break;
                case "EmailDesc":
                    users = users.OrderByDescending(n => n.Email).ToList();
                    break;
                case "LastNameDesc":
                    users = users.OrderByDescending(n => n.LastName).ToList();
                    break;

                default:
                    users = users.OrderBy(n => n.UserName).ToList();
                    break;
            }
            var ps = users.Skip(request.skip).Take(request.take).Select(u => _settings.ToApplicationUserApi(u));
            Response response = new Response(ps.ToArray(), users.Count());
            JsonSerializerSettings settings = new JsonSerializerSettings()
            {
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc
            };
            return Json(response, settings);
        }

        [Route("admin/users/getaddresses/")]
        [HttpGet]
        public List<AddressApi<HoodIdentityUser>> GetAddresses()
        {
            var user = _auth.GetCurrentUser();
            return user.Addresses.Select(a => new AddressApi<HoodIdentityUser>(a, user)).ToList();
        }

        [Route("admin/users/getusernames/")]
        public List<string> GetUserNames(bool normalised = false)
        {
            if (normalised)
                return _db.Users.Select(u => u.NormalizedUserName).ToList();
            return _db.Users.Select(u => u.UserName).ToList();
        }

        [Route("admin/users/getavatar/")]
        [HttpGet]
        public MediaApi GetAvatar(string id)
        {
            try
            {
                var user = _auth.GetUserById(id);
                if (user != null)
                {
                    if (user.Avatar == null)
                        return MediaApi.Blank(_settings.GetMediaSettings());
                    return new MediaApi(user.Avatar, _settings);
                }
                else
                    throw new Exception("No avatar found");
            }
            catch (Exception)
            {
                return MediaApi.Blank(_settings.GetMediaSettings());
            }
        }

        [Route("admin/users/getroles/")]
        [HttpGet]
        public async Task<JsonResult> GetRoles(string id)
        {
            HoodIdentityUser user = await _userManager.FindByIdAsync(id);
            IList<string> roles = await _userManager.GetRolesAsync(user);
            return Json(new { success = true, roles = roles });
        }

        [Route("admin/users/addtorole/")]
        [HttpPost]
        public async Task<Response> AddToRole(string id, string role)
        {
            try
            {
                HoodIdentityUser user = await _userManager.FindByIdAsync(id);
                if (!await _roleManager.RoleExistsAsync(role))
                    await _roleManager.CreateAsync(new IdentityRole(role));

                IdentityResult result = await _userManager.AddToRoleAsync(user, role);
                if (result.Succeeded)
                {
                    return new Response(true);
                }
                else
                {
                    IdentityError error = result.Errors.FirstOrDefault();
                    if (error != null)
                    {
                        throw new Exception(error.Description);
                    }
                    throw new Exception("The database could not be updated, please try later.");
                }
            }
            catch (Exception ex)
            {
                return new Response(ex.Message);
            }
        }

        [Route("admin/users/removefromrole/")]
        [HttpPost]
        public async Task<Response> RemoveFromRole(string id, string role)
        {
            try
            {
                HoodIdentityUser user = await _userManager.FindByIdAsync(id);
                IdentityResult result = await _userManager.RemoveFromRoleAsync(user, role);
                if (result.Succeeded)
                {
                    return new Response(true);
                }
                else
                {
                    throw new Exception("The database could not be updated, please try later.");
                }
            }
            catch (Exception ex)
            {
                return new Response(ex.Message);
            }
        }

        [Route("admin/users/add/")]
        [HttpPost]
        public async Task<Response> Add(CreateUserModel model)
        {
            try
            {
                var user = new HoodIdentityUser
                {
                    UserName = model.cuUserName,
                    Email = model.cuUserName,
                    FirstName = model.cuFirstName,
                    LastName = model.cuLastName,
                    CreatedOn = DateTime.Now,
                    LastLogOn = DateTime.Now,
                    SystemNotes = string.Format("[{0}] User created via admin panel by {1}.", DateTime.Now, User.Identity.Name) + Environment.NewLine
                };

                var result = await _userManager.CreateAsync(user, model.cuPassword);
                if (!result.Succeeded)
                {
                    return new Response(result.Errors);
                }
                if (model.cuNotifyUser)
                {
                    // Send the email to the user, letting em know n' shit.
                    // Create the email content

                    try
                    {
                        MailObject message = new MailObject()
                        {
                            To = new SendGrid.Helpers.Mail.EmailAddress(user.Email),
                            PreHeader = "You access information for " + _settings.GetSiteTitle(),
                            Subject = "You account has been created."
                        };
                        message.AddH1("Account Created!");
                        message.AddParagraph("Your account has been set up on the " + _settings.GetSiteTitle() + " website.");
                        message.AddParagraph("Username: <strong>" + model.cuUserName + "</strong>");
                        message.AddParagraph("Password: <strong>" + model.cuPassword + "</strong>");
                        await _email.SendEmailAsync(message);
                    }
                    catch (Exception)
                    {
                        // roll back!
                        var deleteUser = await _userManager.FindByEmailAsync(model.cuUserName);
                        await _userManager.DeleteAsync(deleteUser);
                        throw new Exception("There was a problem sending the email, ensure the site's email address and SendGrid settings are set up correctly before sending.");
                    }
                }
                return new Response(true);

            }
            catch (Exception ex)
            {
                return new Response(ex.Message);
            }
        }

        [Route("admin/users/clearimage/")]
        [HttpGet]
        public Response ClearImage(string id)
        {
            try
            {
                var user = _auth.GetUserById(id);
                user.Avatar = null;
                _auth.UpdateUser(user);
                return new Response(true, "The image has been cleared!");
            }
            catch (Exception ex)
            {
                return new Response(ex.Message);
            }
        }

        [Route("admin/users/delete/")]
        [HttpPost()]
        public async Task<Response> Delete(string id)
        {
            try
            {
                // delete the avatar - if it exists.
                HoodIdentityUser user = await _userManager.FindByIdAsync(id);
                string container = typeof(HoodIdentityUser).Name;
                var logins = await _userManager.GetLoginsAsync(user);
                foreach (var li in logins)
                {
                    await _userManager.RemoveLoginAsync(user, li.LoginProvider, li.ProviderKey);
                }
                var roles = await _userManager.GetRolesAsync(user);
                foreach (var role in roles)
                {
                    await _userManager.RemoveFromRoleAsync(user, role);
                }
                var claims = await _userManager.GetClaimsAsync(user);
                foreach (var claim in claims)
                {
                    await _userManager.RemoveClaimAsync(user, claim);
                }


                var userSubs = _auth.GetUserById(user.Id);
                if (userSubs.Subscriptions != null)
                {
                    if (userSubs.Subscriptions.Count > 0)
                    {
                        foreach (var sub in userSubs.Subscriptions)
                        {
                            var res = await _billing.Subscriptions.CancelSubscriptionAsync(sub.CustomerId, sub.StripeId, false);
                        }
                        userSubs.Subscriptions.Clear();
                        _auth.UpdateUser(userSubs);
                    }
                }

                IdentityResult result = await _userManager.DeleteAsync(user);
                if (result.Succeeded)
                {
                    return new Response(true);
                }
                else
                {
                    return new Response("There was a problem updating the database");
                }

            }
            catch (Exception ex)
            {
                return new Response(ex.Message);
            }
        }

    }
}
