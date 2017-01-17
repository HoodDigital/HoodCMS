﻿namespace Hood.Services
{
    public class BillingService : IBillingService
    {
        private IStripeService _stripe;
        private ISubscriptionPlanService _subscriptionPlans;
        private ISubscriptionService _subscriptions;
        private ICustomerService _customers;
        private ICardService _cards;
        private IInvoiceService _invoices;

        public BillingService(IStripeService stripe,
                              ISubscriptionService subscriptions,
                              ISubscriptionPlanService subscriptionPlans,
                              ICustomerService customers,
                              ICardService cards,
                              IInvoiceService invoices)
        {
            _stripe = stripe;
            _subscriptionPlans = subscriptionPlans;
            _subscriptions = subscriptions;
            _customers = customers;
            _cards = cards;
            _invoices = invoices;
        }

        public IStripeService Stripe
        {
            get
            {
                return _stripe;
            }
        }

        public ISubscriptionPlanService SubscriptionPlans
        {
            get
            {
                return _subscriptionPlans;
            }
        }

        public ISubscriptionService Subscriptions
        {
            get
            {
                return _subscriptions;
            }
        }

        public ICustomerService Customers
        {
            get
            {
                return _customers;
            }
        }

        public ICardService Cards
        {
            get
            {
                return _cards;
            }
        }

        public IInvoiceService Invoices
        {
            get
            {
                return _invoices;
            }
        }

    }
}