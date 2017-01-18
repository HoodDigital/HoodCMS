﻿using Hood.Infrastructure;
using Hood.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hood.Services
{
    public interface IPropertyRepository
    {
        OperationResult<PropertyListing> Add(PropertyListing property);
        Task<PagedList<PropertyListing>> GetPagedProperties(PropertyFilters propertyFilters, bool published = true);
        PropertyListing GetPropertyById(int id, bool nocache = false);
        void ClearField(int id, string field);
        OperationResult Delete(int id);
        OperationResult<PropertyListing> UpdateProperty(PropertyListing model);
        OperationResult<PropertyListing> SetStatus(int id, Status status);
        Task<OperationResult> DeleteAll();
        Task<OperationResult<PropertyListing>> AddImage(PropertyListing property, PropertyMedia media);
        Task<OperationResult<PropertyListing>> AddFloorplan(PropertyListing property, PropertyFloorplan media);
        Task<List<PropertyListing>> GetFeatured();
        Task<List<PropertyListing>> GetRecent();
    }
}