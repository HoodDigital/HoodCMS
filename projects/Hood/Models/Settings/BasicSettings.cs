﻿using Hood.Interfaces;
using System;
using System.ComponentModel.DataAnnotations;

namespace Hood.Models
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    [Serializable]
    public class BasicSettings
    {
        // Owner Info
        [Display(Name = "First name")]
        public string OwnerFirstName { get; set; }
        [Display(Name = "Last name")]
        public string OwnerLastName { get; set; }
        [Display(Name = "Display name")]
        public string OwnerDisplayName { get; set; }
        public string OwnerPhone { get; set; }
        public string OwnerMobile { get; set; }
        [Display(Name = "Job Title")]
        public string JobTitle { get; set; }

        // Site Address
        [Display(Name = "Site Title")]
        public string SiteTitle { get; set; }

        [Display(Name = "Company Name")]
        public string CompanyName { get; set; }
        [Display(Name = "Site/Company Phone Number")]
        public string Phone { get; set; }
        [Display(Name = "Site/Company Email")]
        public string Email { get; set; }

        // Description
        public string Description { get; set; }

        // Notes
        public string Notes { get; set; }

        public SiteAddress Address { get; set; }

        public BasicSettings()
        {
            // Set Defaults
            SiteTitle = "Website.com";

        }
    }

    public partial class SiteAddress : IAddress
    {

        public string ContactName { get; set; }

        [Display(Name = "Building Name/Number")]
        public string Number { get; set; }
        [Display(Name = "Address 1")]
        public string Address1 { get; set; }
        [Display(Name = "Address 2")]
        public string Address2 { get; set; }
        public string City { get; set; }
        public string County { get; set; }
        public string Country { get; set; }
        public string Postcode { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

}