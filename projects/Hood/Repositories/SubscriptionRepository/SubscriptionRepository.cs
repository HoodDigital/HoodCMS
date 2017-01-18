﻿using Hood.Enums;
using Hood.Extensions;
using Hood.Infrastructure;
using Hood.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hood.Services
{
    public class SubscriptionRepository : ISubscriptionRepository
    {
        #region "Constructors"
        private readonly HoodDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuthenticationRepository _auth;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _config;
        private readonly ISiteConfiguration _site;
        private readonly IBillingService _billing;
        private readonly IMemoryCache _cache;

        public SubscriptionRepository(HoodDbContext db,
                                      UserManager<ApplicationUser> userManager,
                                      IAuthenticationRepository auth,
                                      IHttpContextAccessor httpContextAccessor,
                                      IMemoryCache cache,
                                      IConfiguration config,
                                      IBillingService billing,
                                      ISiteConfiguration site)
        {
            _db = db;
            _auth = auth;
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
            _config = config;
            _site = site;
            _cache = cache;
            _billing = billing;
        }
        #endregion

        #region "Subscriptions"
        public async Task<OperationResult> Add(Subscription subscription)
        {
            try
            {
                // try adding to stripe
                var myPlan = new StripePlanCreateOptions();
                myPlan.Id = subscription.StripeId;
                myPlan.Amount = subscription.Amount;                     // all amounts on Stripe are in cents, pence, etc
                myPlan.Currency = subscription.Currency;                 // "usd" only supported right now
                myPlan.Interval = subscription.Interval;                 // "month" or "year"
                myPlan.IntervalCount = subscription.IntervalCount;       // optional
                myPlan.Name = subscription.Name;
                myPlan.TrialPeriodDays = subscription.TrialPeriodDays;   // amount of time that will lapse before the customer is billed
                StripePlan response = await _billing.Stripe.PlanService.CreateAsync(myPlan);
                subscription.StripeId = response.Id;
                _db.Subscriptions.Add(subscription);
                var result = new OperationResult(_db.SaveChanges() == 1);
                return result;
            }
            catch (Exception ex)
            {
                return new OperationResult(ex.Message);
            }
        }
        public async Task<OperationResult> Delete(int id)
        {
            try
            {
                Subscription subscription = _db.Subscriptions.Where(p => p.Id == id).FirstOrDefault();
                _cache.Remove(typeof(Subscription).ToString() + "-" + subscription.Id);
                try
                {
                    await _billing.Stripe.PlanService.DeleteAsync(subscription.StripeId);
                }
                catch (StripeException)
                { }
                _db.SaveChanges();
                _db.Entry(subscription).State = EntityState.Deleted;
                await _db.SaveChangesAsync();
                return new OperationResult(true);
            }
            catch (Exception ex)
            {
                return new OperationResult(ex.Message);
            }
        }
        public async Task<Subscription> GetSubscriptionById(int id, bool nocache = false)
        {
            string cacheKey = typeof(Subscription).ToString() + "-" + id;
            Subscription subscription = _cache.Get(cacheKey) as Subscription;
            if (subscription != null && !nocache)
                return subscription;
            else
            {
                subscription = await _db.Subscriptions
                                    .Include(s => s.Users)
                                    .Include(s => s.Features)
                                    .FirstOrDefaultAsync(c => c.Id == id);

                _cache.Set(cacheKey, subscription);
                return subscription;
            }
        }
        public async Task<Subscription> GetSubscriptionByStripeId(string stripeId)
        {
            Subscription subscription = await _db.Subscriptions
                                .Include(s => s.Users)
                                .Include(s => s.Features)
                                .FirstOrDefaultAsync(c => c.StripeId == stripeId);

            return subscription;
        }
        private IQueryable<Subscription> GetSubscriptions(string search = "", string sort = "", bool includeUsers = false)
        {
            IQueryable<Subscription> subscriptions = _db.Subscriptions.Include(s => s.Features);

            if (includeUsers) 
                subscriptions = subscriptions.Include(s=> s.Users);

            // search the collection
            if (!string.IsNullOrEmpty(search))
            {

                string[] searchTerms = search.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                subscriptions = subscriptions.Where(n => searchTerms.Any(s => n.Name != null && n.Name.IndexOf(s, StringComparison.InvariantCultureIgnoreCase) >= 0)
                                      || searchTerms.Any(s => n.Description != null && n.Description.IndexOf(s, StringComparison.InvariantCultureIgnoreCase) >= 0)
                                      || searchTerms.Any(s => n.StripeId != null && n.StripeId.IndexOf(s, StringComparison.InvariantCultureIgnoreCase) >= 0)
                                      || searchTerms.Any(s => n.StatementDescriptor != null && n.StatementDescriptor.IndexOf(s, StringComparison.InvariantCultureIgnoreCase) >= 0));
            }


            // sort the collection and then output it.
            if (!string.IsNullOrEmpty(sort))
            {
                switch (sort)
                {
                    case "title":
                        subscriptions = subscriptions.OrderBy(n => n.Name);
                        break;
                    case "date":
                        subscriptions = subscriptions.OrderBy(n => n.Created);
                        break;
                    case "price":
                        subscriptions = subscriptions.OrderBy(n => n.Amount);
                        break;

                    case "title+desc":
                        subscriptions = subscriptions.OrderByDescending(n => n.Name);
                        break;
                    case "date+desc":
                        subscriptions = subscriptions.OrderByDescending(n => n.Created);
                        break;
                    case "price+desc":
                        subscriptions = subscriptions.OrderByDescending(n => n.Amount);
                        break;

                    default:
                        subscriptions = subscriptions.OrderByDescending(n => n.Created).ThenBy(n => n.Id);
                        break;
                }
            }
            return subscriptions;
        }
        public async Task<List<Subscription>> GetAllAsync()
        {
            return await GetSubscriptions().ToListAsync();
        }
        public async Task<List<Subscription>> GetLevels()
        {
            return await GetSubscriptions().Where(s => s.Public && !s.Addon).OrderBy(s => s.Level).ToListAsync();
        }
        public async Task<List<Subscription>> GetAddons()
        {
            return await GetSubscriptions().Where(s => s.Public && s.Addon).ToListAsync();
        }
        public async Task<PagedList<Subscription>> GetPagedSubscriptions(ListFilters filters, string search, string sort)
        {
            PagedList<Subscription> model = new PagedList<Subscription>();
            var subs = GetSubscriptions(search, sort, true);
            model.Items = await subs.Skip((filters.page - 1) * filters.pageSize).Take(filters.pageSize).ToListAsync();
            model.Count = subs.Count();
            model.Pages = model.Count / filters.pageSize;
            if (model.Pages < 1)
                model.Pages = 1;
            if ((model.Pages * filters.pageSize) < model.Count)
            {
                model.Pages++;
            }
            model.CurrentPage = filters.page;
            if (filters.pageSize != 5)
                model.PageSize = filters.pageSize;
            return model;
        }
        public async Task<OperationResult> UpdateSubscription(Subscription subscription)
        {
            try
            {
                _cache.Remove(typeof(Subscription).ToString() + "-" + subscription.Id);
                var myPlan = new StripePlanUpdateOptions();
                myPlan.Name = subscription.Name;
                StripePlan response = await _billing.Stripe.PlanService.UpdateAsync(subscription.StripeId, myPlan);
                _db.Update(subscription);
                await _db.SaveChangesAsync();
                return new OperationResult(true);
            }
            catch (DbUpdateException ex)
            {
                return new OperationResult(ex);
            }
        }
        #endregion

        #region "Stripe customer object"
        public async Task<StripeCustomer> GetCustomerObject(string stripeId, bool allowNullObject)
        {
            // Check if the user has a stripeId - if they do, we dont need to create them again, we can simply add a new card token to their account, or use an existing one maybe.
            if (stripeId.IsSet())
                try
                {
                    var customer = await _billing.Customers.FindByIdAsync(stripeId);
                    if (customer.Deleted.HasValue && customer.Deleted.Value)
                    {
                        _auth.ResetBillingInfo();
                        customer = null;
                    }
                    if (customer == null)
                        if (!allowNullObject)
                            throw new Exception(BillingMessage.NoCustomerObject.ToString());
                        else
                            _auth.ResetBillingInfo();
                    return customer;
                }
                catch (StripeException)
                {
                    if (allowNullObject)
                        return null;
                    else
                        throw new Exception(BillingMessage.NoCustomerObject.ToString());
                }
            else
                if (allowNullObject)
                return null;
            else
                throw new Exception(BillingMessage.NoStripeId.ToString());
        }
        #endregion

        #region "User Subscriptions"
        private IQueryable<ApplicationUser> GetSubscribers(int subscriptionId, string search = "", string sort = "")
        {
            IQueryable<ApplicationUser> users = _db.Users
                .Include(u => u.Avatar)
                .Include(u => u.Subscriptions)
                .ThenInclude(u => u.Subscription)
                .Where(u => u.Subscriptions.Any(s => s.SubscriptionId == subscriptionId && (s.Status == "trialing" || s.Status == "active")));

            // search the collection
            if (!string.IsNullOrEmpty(search))
            {

                string[] searchTerms = search.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                users = users.Where(n => searchTerms.Any(s => n.FirstName != null && n.FirstName.IndexOf(s, StringComparison.InvariantCultureIgnoreCase) >= 0)
                                      || searchTerms.Any(s => n.LastName != null && n.LastName.IndexOf(s, StringComparison.InvariantCultureIgnoreCase) >= 0)
                                      || searchTerms.Any(s => n.StripeId != null && n.StripeId.IndexOf(s, StringComparison.InvariantCultureIgnoreCase) >= 0)
                                      || searchTerms.Any(s => n.Email != null && n.Email.IndexOf(s, StringComparison.InvariantCultureIgnoreCase) >= 0));
            }


            // sort the collection and then output it.
            if (!string.IsNullOrEmpty(sort))
            {
                switch (sort)
                {
                    case "UserName":
                        users = users.OrderBy(n => n.UserName);
                        break;
                    case "Email":
                        users = users.OrderBy(n => n.Email);
                        break;
                    case "LastName":
                        users = users.OrderBy(n => n.LastName);
                        break;
                    case "LastLogOn":
                        users = users.OrderByDescending(n => n.LastLogOn);
                        break;

                    case "UserNameDesc":
                        users = users.OrderByDescending(n => n.UserName);
                        break;
                    case "EmailDesc":
                        users = users.OrderByDescending(n => n.Email);
                        break;
                    case "LastNameDesc":
                        users = users.OrderByDescending(n => n.LastName);
                        break;

                    default:
                        users = users.OrderByDescending(n => n.CreatedOn).ThenBy(n => n.Id);
                        break;
                }
            }
            return users;
        }
        public async Task<PagedList<ApplicationUser>> GetPagedSubscribers(ListFilters filters, int subscriptionId, string search, string sort)
        {
            PagedList<ApplicationUser> model = new PagedList<ApplicationUser>();
            var subs = GetSubscribers(subscriptionId, search, sort);
            model.Items = await subs.Skip((filters.page - 1) * filters.pageSize).Take(filters.pageSize).ToListAsync();
            model.Count = subs.Count();
            model.Pages = model.Count / filters.pageSize;
            if (model.Pages < 1)
                model.Pages = 1;
            if ((model.Pages * filters.pageSize) < model.Count)
            {
                model.Pages++;
            }
            model.CurrentPage = filters.page;
            if (filters.pageSize != 5)
                model.PageSize = filters.pageSize;
            return model;
        }
        public async Task CreateUserSubscription(int planId, string stripeToken, string cardId)
        {
            // Load user object and clear cache.
            ApplicationUser user = GetUserObject();

            // Load the subscription plan.
            Subscription subscription = await GetSubscriptionById(planId);

            // Get the stripe subscription plan object.
            StripePlan plan = await _billing.SubscriptionPlans.FindByIdAsync(subscription.StripeId);

            // Check for customer or throw, but allow it to be null.
            StripeCustomer customer = await GetCustomerObject(user.StripeId, true);

            // The object for the new user subscription to be recieved from stripe.
            StripeSubscription newSubscription = null;

            // if the user has provided a cardId to use, then let's try and use that!
            if (cardId.IsSet())
            {
                if (customer == null)
                    throw new Exception(BillingMessage.NoCustomerObject.ToString());

                // set the card as the default for the user, then subscribe the user.
                await _billing.Customers.SetDefaultCard(customer.Id, cardId);

                // check if the user is already on a subscription, if so, update that.
                var sub = (await _billing.Subscriptions.UserSubscriptionsAsync(user.StripeId)).FirstOrDefault(s => s.StripePlan.Id == plan.Id && s.Status != "canceled");
                if (sub != null)
                {
                    // there is an existing subscription, which is not cancelleed and matches the plan. BAIL OUT!                   
                    throw new Exception(BillingMessage.SubscriptionExists.ToString());
                }
                else
                {
                    // finally, add the user to the NEW subscription.
                    newSubscription = await _billing.Subscriptions.SubscribeUserAsync(customer.Id, subscription.StripeId);
                }
            }
            else
            {
                // if not, then the user must have supplied a token
                StripeToken stripeTokenObj = _billing.Stripe.TokenService.Get(stripeToken);
                if (stripeTokenObj == null)
                    throw new Exception(BillingMessage.InvalidToken.ToString());

                // Check if the customer object exists.
                if (customer == null)
                {
                    // if it does not, create it, add the card and subscribe the plan.
                    customer = await _billing.Customers.CreateCustomer(user, stripeToken);
                    user.StripeId = customer.Id;
                    var updateResult = _auth.UpdateUser(user);
                    newSubscription = await _billing.Subscriptions.SubscribeUserAsync(user.StripeId, plan.Id);
                }
                else
                {
                    // check if the user is already on a subscription, if so, update that.
                    var sub = (await _billing.Subscriptions.UserSubscriptionsAsync(user.StripeId)).FirstOrDefault(s => s.StripePlan.Id == plan.Id && s.Status != "canceled");
                    if (sub != null)
                    {
                        // there is an existing subscription, which is not cancelleed and matches the plan. BAIL OUT!                   
                        throw new Exception(BillingMessage.SubscriptionExists.ToString());
                    }
                    else
                    {
                        // finally, add the user to the NEW subscription, using the new card as the charge source.
                        newSubscription = await _billing.Subscriptions.SubscribeUserAsync(customer.Id, plan.Id, stripeToken);
                    }
                }
            }

            // We got this far, let's add the detail to the local DB.
            if (newSubscription == null)
                throw new Exception(BillingMessage.Error.ToString());

            UserSubscription newUserSub = new UserSubscription();
            newUserSub = UpdateUserSubscriptionFromStripe(newUserSub, newSubscription);
            newUserSub.StripeId = newSubscription.Id;
            newUserSub.CustomerId = user.StripeId;
            newUserSub.UserId = user.Id;
            newUserSub.SubscriptionId = subscription.Id;
            OperationResult result = _auth.AddUserSubscription(newUserSub);
        }
        public async Task UpgradeUserSubscription(int subscriptionId, int planId)
        {
            // Load user object and clear cache.
            ApplicationUser user = GetUserObject();

            Subscription subscription = await GetSubscriptionById(planId);
            UserSubscription userSub = GetUserSubscription(user, subscriptionId);

            // Check for customer or throw.
            var customer = await GetCustomerObject(user.StripeId, false);

            if (customer.DefaultSourceId.IsSet() || customer.Sources?.TotalCount > 0)
            {
                // there is a payment source - continue                  
                // Load the plan from stripe, then add to the user's subscription.
                StripePlan plan = await _billing.SubscriptionPlans.FindByIdAsync(subscription.StripeId);
                StripeSubscription sub = await _billing.Subscriptions.UpdateSubscriptionAsync(customer.Id, userSub.StripeId, plan);
                userSub = UpdateUserSubscriptionFromStripe(userSub, sub);
                userSub.Subscription = subscription;
                userSub.SubscriptionId = subscription.Id;
                _auth.UpdateUser(user);
            }
            else
                throw new Exception(BillingMessage.NoPaymentSource.ToString());
        }
        public async Task CancelUserSubscription(int userSubscriptionId)
        {
            // Load user object and clear cache.
            ApplicationUser user = GetUserObject();

            // Check for customer or throw.
            var customer = await GetCustomerObject(user.StripeId, false);

            // Check for subscription or throw.
            UserSubscription userSub = GetUserSubscription(user, userSubscriptionId);

            StripeSubscription sub = await _billing.Subscriptions.CancelSubscriptionAsync(customer.Id, userSub.StripeId, true);
            userSub = UpdateUserSubscriptionFromStripe(userSub, sub);
            _auth.UpdateUser(user);
        }
        public async Task RemoveUserSubscription(int userSubscriptionId)
        {
            // Load user object and clear cache.
            ApplicationUser user = GetUserObject();

            // Check for customer or throw.
            var customer = await GetCustomerObject(user.StripeId, false);

            // Check for subscription or throw.
            UserSubscription userSub = GetUserSubscription(user, userSubscriptionId);

            StripeSubscription sub = await _billing.Subscriptions.CancelSubscriptionAsync(customer.Id, userSub.StripeId, false);
            userSub = UpdateUserSubscriptionFromStripe(userSub, sub);
            _auth.UpdateUser(user);
        }
        public async Task ReactivateUserSubscription(int userSubscriptionId)
        {
            // Load user object and clear cache.
            ApplicationUser user = GetUserObject();

            // Check for customer or throw.
            var customer = await GetCustomerObject(user.StripeId, false);

            // Check for subscription or throw.
            UserSubscription userSub = GetUserSubscription(user, userSubscriptionId);

            StripeSubscription sub = await _billing.Subscriptions.FindById(user.StripeId, userSub.StripeId);
            StripeSubscriptionUpdateOptions options = new StripeSubscriptionUpdateOptions();

            sub = await _billing.Subscriptions.UpdateSubscriptionAsync(customer.Id, sub.Id, sub.StripePlan);
            userSub = UpdateUserSubscriptionFromStripe(userSub, sub);
            _auth.UpdateUser(user);
        }
        #endregion

        #region "User Subscription Helpers"
        private ApplicationUser GetUserObject()
        {
            var context = _httpContextAccessor.HttpContext;
            ApplicationUser user = _auth.GetUserById(_userManager.GetUserId(context.User), false, true);
            string cacheKey = typeof(AccountInfo).ToString() + "-" + user.Id;
            _cache.Remove(cacheKey);
            return user;
        }
        private UserSubscription GetUserSubscription(ApplicationUser user, int userSubscriptionId)
        {
            UserSubscription subscription = user.Subscriptions.Where(us => us.UserSubscriptionId == userSubscriptionId).FirstOrDefault();
            if (subscription == null)
                throw new Exception(BillingMessage.NoSubscription.ToString());
            return subscription;
        }
        private UserSubscription GetUserSubscriptionByStripeId(ApplicationUser user, string stripeId)
        {
            UserSubscription subscription = user.Subscriptions.Where(us => us.StripeId == stripeId).FirstOrDefault();
            if (subscription == null)
                throw new Exception(BillingMessage.NoSubscription.ToString());
            return subscription;
        }
        #endregion

        #region "WebHooks"
        public async Task ConfirmSubscriptionObject(StripeSubscription created, DateTime? eventTime)
        {
            try
            {
                ApplicationUser user = await _auth.GetUserByStripeId(created.CustomerId);
                UserSubscription userSub = GetUserSubscriptionByStripeId(user, created.Id);
                // Check the timestamp of the event, with the last update of the object
                // If this was updated last before the event, therefore the event is valid and should be applied.
                if (eventTime.HasValue && userSub.LastUpdated < eventTime)
                {
                    userSub = UpdateUserSubscriptionFromStripe(userSub, created);
                    userSub.Confirmed = true;
                    _auth.UpdateUser(user);
                }
            }
            catch (Exception)
            {

            }
        }
        public async Task UpdateSubscriptionObject(StripeSubscription updated, DateTime? eventTime)
        {
            try
            {
                ApplicationUser user = await _auth.GetUserByStripeId(updated.CustomerId);
                UserSubscription userSub = GetUserSubscriptionByStripeId(user, updated.Id);
                // Check the timestamp of the event, with the last update of the object
                // If this was updated last before the event, therefore the event is valid and should be applied.
                if (eventTime.HasValue && userSub.LastUpdated < eventTime)
                {
                    userSub = UpdateUserSubscriptionFromStripe(userSub, updated);
                    _auth.UpdateUser(user);
                }
            }
            catch (Exception)
            {

            }
        }
        public async Task RemoveUserSubscriptionObject(StripeSubscription deleted, DateTime? eventTime)
        {
            try
            {
                ApplicationUser user = await _auth.GetUserByStripeId(deleted.CustomerId);
                UserSubscription userSub = GetUserSubscriptionByStripeId(user, deleted.Id);
                // Check the timestamp of the event, with the last update of the object
                // If this was updated last before the event, therefore the event is valid and should be applied.
                if (eventTime.HasValue && userSub.LastUpdated < eventTime)
                {
                    userSub = UpdateUserSubscriptionFromStripe(userSub, deleted);
                    userSub.Deleted = true;
                    userSub.DeletedAt = DateTime.Now;
                    _auth.UpdateUser(user);
                }
            }
            catch (Exception)
            {

            }
        }
        private UserSubscription UpdateUserSubscriptionFromStripe(UserSubscription userSubscription, StripeSubscription stripeSubscription)
        {
            _auth.ClearUserFromCache(userSubscription.UserId);
            userSubscription.CancelAtPeriodEnd = stripeSubscription.CancelAtPeriodEnd;
            userSubscription.CanceledAt = stripeSubscription.CanceledAt;
            userSubscription.CurrentPeriodEnd = stripeSubscription.CurrentPeriodEnd;
            userSubscription.CurrentPeriodStart = stripeSubscription.CurrentPeriodStart;
            userSubscription.EndedAt = stripeSubscription.EndedAt;
            userSubscription.Quantity = stripeSubscription.Quantity;
            userSubscription.Start = stripeSubscription.Start;
            userSubscription.Status = stripeSubscription.Status;
            userSubscription.TaxPercent = stripeSubscription.TaxPercent;
            userSubscription.TrialEnd = stripeSubscription.TrialEnd;
            userSubscription.TrialStart = stripeSubscription.TrialStart;
            userSubscription.Notes += DateTime.Now.ToShortDateString() + " at " + DateTime.Now.ToShortTimeString() + " StripeEvent - Updated Subscription" + Environment.NewLine;
            userSubscription.LastUpdated = DateTime.Now;
            return userSubscription;
        }

        public async Task<UserSubscription> FindUserSubscriptionByStripeId(string id)
        {
            UserSubscription userSub = await _db.UserSubscriptions
                                .Include(s => s.User)
                                .Include(s => s.Subscription)
                                .FirstOrDefaultAsync(c => c.StripeId == id);
            return userSub;
        }
        #endregion
    }
}