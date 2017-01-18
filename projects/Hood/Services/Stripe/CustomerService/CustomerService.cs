﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Stripe;
using Hood.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;

namespace Hood.Services
{
    public class CustomerService : ICustomerService
    {
        private IStripeService _stripe;
        private UserManager<ApplicationUser> _userManager;
        private IMemoryCache _cache;

        public CustomerService(IStripeService stripe,
                               IMemoryCache cache,
                               UserManager<ApplicationUser> userManager)
        {
            _cache = cache;
            _stripe = stripe;
            _userManager = userManager;
        }

        public async Task<StripeCustomer> CreateCustomer(ApplicationUser user, string token, string planId = null)
        {
            var customer = new StripeCustomerCreateOptions();
            customer.Email = user.Email;
            customer.Description = string.Format("{0} {1} ({2})", user.FirstName, user.LastName, user.Email);
            customer.SourceToken = token;
            customer.PlanId = planId;
            StripeCustomer stripeCustomer = await _stripe.CustomerService.CreateAsync(customer);
            return stripeCustomer;
        }


        public void DeleteCustomer(string customerId)
        {
            _stripe.CustomerService.Delete(customerId);
        }

        public async Task<StripeCustomer> FindByIdAsync(string customerId)
        {
            StripeCustomer stripeCustomer = await _stripe.CustomerService.GetAsync(customerId);
            return stripeCustomer;
        }

        public async Task<IEnumerable<StripeCustomer>> GetAllAsync()
        {
            return await _stripe.CustomerService.ListAsync(new StripeCustomerListOptions()
            {
                
            }, new StripeRequestOptions() {

            });
        }

        public async Task SetDefaultCard(string customerId, string cardId)
        {
            var customer = new StripeCustomerUpdateOptions();
            customer.DefaultSource = cardId;
            StripeCustomer stripeCustomer = await _stripe.CustomerService.UpdateAsync(customerId, customer);
        }

    }
}