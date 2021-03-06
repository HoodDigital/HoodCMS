﻿using Hood.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Hood.Interfaces
{
    public interface IHoodDbContext : IDbContext
    {
        DbSet<UserAccessCode> AccessCodes { get; set; }
        DbSet<Address> Addresses { get; set; }
        DbSet<Content> Content { get; set; }
        DbSet<ContentCategory> ContentCategories { get; set; }
        DbSet<Log> Logs { get; set; }
        DbSet<MediaObject> Media { get; set; }
        DbSet<Option> Options { get; set; }
        DbSet<PropertyListing> Properties { get; set; }
        bool AllMigrationsApplied();
        void Seed(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager);
    }
}