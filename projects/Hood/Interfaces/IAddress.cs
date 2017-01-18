﻿using System;
using System.Net;
using Newtonsoft.Json;
using Hood.Extensions;
using Hood.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Html;
using Hood.Infrastructure;

namespace Hood.Interfaces
{
    public interface IAddress
    {
        string ContactName { get; set; }

        string Number { get; set; }
        string Address1 { get; set; }
        string Address2 { get; set; }
        string City { get; set; }
        string County { get; set; }
        string Country { get; set; }
        string Postcode { get; set; }
        double Latitude { get; set; }
        double Longitude { get; set; }
    }

    public enum AddressFormat
    {
        SingleLine,
        MultiLine
    }

    public static class IAddressExtensions
    {
        public static void CopyTo(this IAddress from, IAddress to)
        {
            to.ContactName = from.ContactName;
            to.Address1 = from.Address1;
            to.Address2 = from.Address2;
            to.City = from.City;
            to.County = from.County;
            to.Postcode = from.Postcode;
            to.Country = from.Country;
            to.Latitude = from.Latitude;
            to.Longitude = from.Longitude;
        }

        public static TAddress CloneTo<TAddress>(this IAddress from)
            where TAddress : IAddress, new()
        {
            var to = new TAddress();
            from.CopyTo(to);
            return to;
        }

        /// <summary>
        /// Validates the address part of the IAddress object. This will use postcodes.io in order to validate the address, 
        /// while doing so it will update the latitude and longitude of the address object.
        /// </summary>
        /// <param name="address">The IAddresvs object.</param>
        /// <returns>Valid</returns>
        public static OperationResult GetLatLongFromUKPostcode(this IAddress address)
        {
            try
            {
                if (!string.IsNullOrEmpty(address.Postcode))
                {
                    address.Postcode = address.Postcode.ToUpperInvariant();
                }
                PostcodeValidation json;
                using (WebClient wc = new WebClient())
                {
                    try
                    {
                        json = JsonConvert.DeserializeObject<PostcodeValidation>(wc.DownloadString("https://api.postcodes.io/postcodes/" + address.Postcode));
                    }
                    catch (Exception)
                    {
                        throw new Exception("Could not validate your postcode, the postcode does not exist.");
                    }
                }
                if (json == null)
                    throw new Exception("Could not validate your postcode, an error occurred, please try again.");
                if (json.status != 200)
                    throw new Exception("Could not validate your postcode, the postcode does not exist.");

                address.Latitude = json.result.latitude;
                address.Longitude = json.result.longitude;
                return new OperationResult(true);
            }
            catch (Exception ex)
            {
                return new OperationResult(ex);
            }
        }

        public static string ToFormat(this IAddress from, AddressFormat format, bool showPostcode = true, bool showCountry = false)
        {
            string ret = "";
            if (from == null || !from.Address1.IsSet())
                return ret;
            switch (format)
            {
                case AddressFormat.SingleLine:
                    ret = "";
                    ret += from.Number.IsSet() ? from.Number + (from.Address1.IsSet() ? ", " : "") : "";
                    ret += from.Address1.IsSet() ? from.Address1 : "";
                    ret += from.Address2.IsSet() ? ", " + from.Address2 : "";
                    ret += from.City.IsSet() ? ", " + from.City : "";
                    ret += from.County.IsSet() ? ", " + from.County : "";
                    ret += from.Postcode.IsSet() && showPostcode ? ", " + from.Postcode : "";
                    ret += from.Country.IsSet() && showCountry ? ", " + from.Country : "";
                    return ret;
                case AddressFormat.MultiLine:
                    ret = "";
                    ret += from.Number.IsSet() ? from.Number + (from.Address1.IsSet() ? ", " : "") : "";
                    ret += from.Address1.IsSet() ? from.Address1 : "";
                    ret += from.Address2.IsSet() ? Environment.NewLine + from.Address2 : "";
                    ret += from.City.IsSet() ? Environment.NewLine + from.City : "";
                    ret += from.County.IsSet() ? Environment.NewLine + from.County : "";
                    ret += from.Postcode.IsSet() && showPostcode ? Environment.NewLine + from.Postcode : "";
                    ret += from.Country.IsSet() && showCountry ? Environment.NewLine + from.Country : "";
                    return ret;
            }
            return ret;
        }

        public static IHtmlContent FormatAddress(this IHtmlHelper html, IAddress address, AddressFormat format, bool showPostcode = true, bool showCountry = false)
        {
            string ret = "";
            if (address == null || !address.Address1.IsSet())
                return html.Raw(ret);
            switch (format)
            {
                case AddressFormat.SingleLine:
                    ret = address.Address1;
                    ret += address.Address2.IsSet() ? ", " + address.Address2 : "";
                    ret += address.City.IsSet() ? ", " + address.City : "";
                    ret += address.County.IsSet() ? ", " + address.County : "";
                    ret += address.Postcode.IsSet() && showPostcode ? ", " + address.Postcode : "";
                    ret += address.Country.IsSet() && showCountry ? ", " + address.Country : "";
                    break;
                case AddressFormat.MultiLine:
                    ret = address.Address1;
                    ret += address.Address2.IsSet() ? "<br />" + address.Address2 : "";
                    ret += address.City.IsSet() ? "<br />" + address.City : "";
                    ret += address.County.IsSet() ? "<br />" + address.County : "";
                    ret += address.Postcode.IsSet() && showPostcode ? "<br />" + address.Postcode : "";
                    ret += address.Country.IsSet() && showCountry ? "<br />" + address.Country : "";
                    break;
            }
            return html.Raw(ret);
        }

    }
}
