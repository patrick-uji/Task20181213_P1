﻿using System;
using System.Net;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
namespace Task20181213.Common
{
    public static class Fixer
    {
        const string API_KEY = "..."; //Ideally keep this in an environment variable or secrets file
        const string API_URL = "http://data.fixer.io/api/";
        const string ACCESS_KEY_SUFFIX = "?access_key=" + API_KEY;
        const string LATEST_URL = API_URL + "latest" + ACCESS_KEY_SUFFIX;
        public static decimal GetExchangeRate(string sourceCurrency, string targetCurrency)
        {
            return QueryExchangeRate(LATEST_URL, sourceCurrency, targetCurrency);
        }
        public static decimal GetExchangeRate(string sourceCurrency, string targetCurrency, DateTime date)
        {
            string baseURL = API_URL + date.ToString("yyyy-MM-dd") + ACCESS_KEY_SUFFIX;
            return QueryExchangeRate(baseURL, sourceCurrency, targetCurrency);
        }
        private static decimal QueryExchangeRate(string baseURL, string sourceCurrency, string targetCurrency)
        {
            string url = baseURL + "&base=" + sourceCurrency + "&symbols=" + targetCurrency;
            JObject exchangeRates = QueryExchangeRates(url);
            return exchangeRates.Value<decimal>(targetCurrency);
        }
        public static IEnumerable<FixerExchangeRate> GetAllExchangeRates()
        {
            List<string> currencyCodes = new List<string>();
            foreach (var currExchangeRate in GetAllExchangeRates("EUR"))
            {
                yield return currExchangeRate;
                currencyCodes.Add(currExchangeRate.TargetCurrency);
            }
            foreach (var currCurrencyCode in currencyCodes)
            {
                foreach (var currExchangeRate in GetAllExchangeRates(currCurrencyCode))
                {
                    yield return currExchangeRate;
                }
            }
        }
        public static IEnumerable<FixerExchangeRate> GetAllExchangeRates(string sourceCurrency)
        {
            JObject exchangeRates = QueryExchangeRates(LATEST_URL + "&base=" + sourceCurrency);
            foreach (var currExchangeRate in exchangeRates.Properties())
            {
                yield return new FixerExchangeRate(sourceCurrency, currExchangeRate.Name, (decimal)currExchangeRate.Value);
            }
        }
        private static JObject QueryExchangeRates(string url)
        {
            using (WebClient webClient = new WebClient())
            {
                JObject jsonResponse = JObject.Parse(webClient.DownloadString(url));
                ErrorCodeCheck(jsonResponse);
                return jsonResponse.Value<JObject>("rates");
            }
        }
        private static void ErrorCodeCheck(JObject jsonResponse)
        {
            if (!jsonResponse.Value<bool>("success"))
            {
                JObject errorNode = jsonResponse.Value<JObject>("error");
                int errorCode = errorNode.Value<int>("code");
                switch (errorCode)
                {
                    case 101: throw new FixerException("API key not supplied. (Check Fixer.cs)", errorCode);
                    case 105: throw new FixerException("API key has restricted access.", errorCode);
                    case 202: throw new FixerException("Invalid currency code supplied.", errorCode);
                    default: throw new FixerException("Unknown Fixer.io error code (" + errorCode + "): " + errorNode.Value<string>("info"), errorCode);
                }
            }
        }
    }
}