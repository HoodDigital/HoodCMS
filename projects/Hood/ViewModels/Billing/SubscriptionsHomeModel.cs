﻿using Hood.BaseTypes;
using Hood.Enums;
using Hood.Models;
using System.Collections.Generic;

namespace Hood.ViewModels
{
    public partial class BillingHomeModel : SaveableModel
    {
        public Stripe.Customer Customer { get; set; }
        public IEnumerable<Stripe.Invoice> Invoices { get; set; }
        public Stripe.Invoice NextInvoice { get; set; }
        public List<UserSubscription> Subscriptions { get; set; }
    }
}
