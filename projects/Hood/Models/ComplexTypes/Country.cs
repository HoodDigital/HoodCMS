﻿namespace Hood.Models
{
    public class Country
    {
        public string Iso3 { get; set; }
        public string Iso2 { get; set; }
        public string IsoNumeric { get; set; }
        public string Name { get; set; }
        public string FullName { get; set; }
        public string CurrencySymbol { get; set; }
        public string CurrencyName { get; set; }

        public Country(string name, string fullName, string iso2, string iso3, string numeric, string currencySymbol, string currencyName)
        {
            Iso2 = iso2;
            Iso3 = iso2;
            IsoNumeric = numeric;
            Name = name;
            FullName = fullName;
            CurrencySymbol = currencySymbol;
            CurrencyName = currencyName;
        }
    }
}
