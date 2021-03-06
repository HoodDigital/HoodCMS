﻿using Hood.BaseTypes;
using Hood.Caching;
using Hood.Core;
using Hood.Enums;
using Hood.Extensions;
using Hood.Interfaces;
using Hood.Models;
using Hood.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace Hood.Controllers
{
    [Authorize]
    public abstract class ManageController : ManageController<HoodDbContext>
    {
        public ManageController() : base() { }
    }

    [Authorize]
    public abstract class ManageController<TContext> : BaseController<TContext, ApplicationUser, IdentityRole>
         where TContext : HoodDbContext
    {
        public ManageController()
            : base()
        {
        }

        [HttpGet]
        [Route("account/manage")]
        public virtual async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                throw new ApplicationException($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }
            var model = new UserViewModel
            {
                UserId = user.Id,
                Username = user.UserName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                IsEmailConfirmed = user.EmailConfirmed,
                StatusMessage = SaveMessage,
                Avatar = user.Avatar,
                Profile = User.GetUserProfile(),
                Roles = await _userManager.GetRolesAsync(user)
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("account/manage")]
        public virtual async Task<IActionResult> Index(UserViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            try
            {

                if (user == null)
                {
                    throw new Exception($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
                }


                if (!ModelState.IsValid)
                {
                    model.Roles = await _userManager.GetRolesAsync(user);
                    return View(model);
                }

                var email = user.Email;
                if (model.Email != email)
                {
                    var setEmailResult = await _userManager.SetEmailAsync(user, model.Email);
                    if (!setEmailResult.Succeeded)
                    {
                        throw new Exception(setEmailResult.Errors.FirstOrDefault().Description);
                    }
                }

                var phoneNumber = user.PhoneNumber;
                if (model.PhoneNumber != phoneNumber)
                {
                    var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, model.PhoneNumber);
                    if (!setPhoneResult.Succeeded)
                    {
                        model.Email = phoneNumber;
                        throw new Exception(setPhoneResult.Errors.FirstOrDefault().Description);
                    }
                }

                model.Profile.Id = user.Id;
                model.Profile.Notes = user.Notes;
                await _account.UpdateProfileAsync(model.Profile);

                SaveMessage = "Your profile has been updated.";
                MessageType = AlertType.Success;
            }
            catch (Exception ex)
            {
                if (user == null)
                {
                    throw new ApplicationException($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
                }
                SaveMessage = "Something went wrong: " + ex.Message;
                MessageType = AlertType.Danger;
            }

            return RedirectToAction(nameof(Index));
        }

        [Route("account/manage/avatar")]
        public virtual async Task<Response> UploadAvatar(IFormFile file, string userId)
        {
            // User must have an organisation.

            try
            {
                var user = await _account.GetUserByIdAsync(userId);
                if (user == null)
                    throw new Exception("Could not locate the user to add an avatar to.");

                IMediaObject mediaResult = null;
                if (file != null)
                {
                    // If the club already has an avatar, delete it from the system.
                    if (user.Avatar != null)
                    {
                        var mediaItem = await _db.Media.SingleOrDefaultAsync(m => m.UniqueId == user.Avatar.UniqueId);
                        if (mediaItem != null)
                            _db.Entry(mediaItem).State = EntityState.Deleted;
                        await _media.DeleteStoredMedia((MediaObject)user.Avatar);
                    }
                    var directory = await Engine.AccountManager.GetDirectoryAsync(User.GetUserId());
                    mediaResult = await _media.ProcessUpload(file, _directoryManager.GetPath(directory.Id));                    
                    user.Avatar = mediaResult;
                    await _account.UpdateUserAsync(user);
                    _db.Media.Add(new MediaObject(mediaResult, directory.Id));
                    await _db.SaveChangesAsync();
                }
                return new Response(true, mediaResult, $"The media has been set for attached successfully.");
            }
            catch (Exception ex)
            {
                return await ErrorResponseAsync<ManageController>($"There was an error setting the avatar.", ex);
            }
        }

        [Route("account/manage/verify-email")]
        public virtual async Task<IActionResult> SendVerificationEmail()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                throw new ApplicationException($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, code }, protocol: HttpContext.Request.Scheme);
            var verifyModel = new VerifyEmailModel(user, callbackUrl)
            {
                SendToRecipient = true
            };

            await _mailService.ProcessAndSend(verifyModel);

            SaveMessage = "Verification email sent. Please check your email.";

            if (user.Active)
                return RedirectToAction(nameof(Index));
            else
                return RedirectToAction(nameof(AccountController.ConfirmRequired), "Account");
        }

        [HttpGet]
        [Route("account/manage/change-password")]
        public virtual async Task<IActionResult> ChangePassword()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                throw new ApplicationException($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var model = new ChangePasswordViewModel { StatusMessage = SaveMessage };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("account/manage/change-password")]
        public virtual async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                throw new ApplicationException($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var changePasswordResult = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
            if (!changePasswordResult.Succeeded)
            {
                AddErrors(changePasswordResult);
                return View(model);
            }

            await _signInManager.SignInAsync(user, isPersistent: false);
            await _logService.AddLogAsync<ManageController>($"User ({user.UserName}) changed their password successfully.");
            SaveMessage = "Your password has been changed.";

            return RedirectToAction(nameof(ChangePassword));
        }

        [HttpGet]
        [Route("account/delete")]
        public virtual IActionResult Delete()
        {
            return View(nameof(Delete), new SaveableModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("account/delete/confirm")]
        public virtual async Task<IActionResult> ConfirmDelete()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                    throw new Exception("User not found.");

                await _account.DeleteUserAsync(user.Id, User);

                await _signInManager.SignOutAsync();

                await _logService.AddLogAsync<ManageController>($"User with Id {user.Id} has deleted their account.");
                return RedirectToAction(nameof(Deleted));
            }
            catch (Exception ex)
            {
                SaveMessage = $"Error deleting your account: {ex.Message}";
                MessageType = AlertType.Danger;
                await _logService.AddExceptionAsync<ManageController>($"Error when user attemted to delete their account.", ex);
            }
            return RedirectToAction(nameof(Delete));
        }

        [AllowAnonymous]
        [Route("account/deleted")]
        public virtual IActionResult Deleted()
        {
            return View(nameof(Deleted));
        }

        #region Helpers

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        #endregion
    }
}
