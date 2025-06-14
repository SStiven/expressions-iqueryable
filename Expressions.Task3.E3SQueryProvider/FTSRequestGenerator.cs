﻿using Expressions.Task3.E3SQueryProvider.Attributes;
using Expressions.Task3.E3SQueryProvider.Models.Request;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Expressions.Task3.E3SQueryProvider
{
    public class FtsRequestGenerator
    {
        private readonly string _FTSSearchTemplate = @"/searchFts";
        private readonly string _baseAddress;

        #region Constructors

        public FtsRequestGenerator(string baseAddress)
        {
            _baseAddress = baseAddress;
        }

        #endregion

        #region public methods

        public Uri GenerateRequestUrl<T>(string query = "*", int start = 0, int limit = 10)
        {
            return GenerateRequestUrl(typeof(T), query, start, limit);
        }

        public Uri GenerateRequestUrl(Type type, string query = "*", int start = 0, int limit = 10)
        {
            string metaTypeName = GetMetaTypeName(type);

            string[] parts = SplitQueryUsingAndOperator(query);

            var ftsQueryRequest = new FtsQueryRequest
            {
                Statements = parts.Select(q => new Statement { Query = q }).ToList(),
                Start = start,
                Limit = limit
            };

            var ftsQueryRequestString = JsonConvert.SerializeObject(ftsQueryRequest);

            var uri = BindByName($"{_baseAddress}{_FTSSearchTemplate}",
                new Dictionary<string, string>()
                {
                    { "metaType", metaTypeName },
                    { "query", ftsQueryRequestString }
                });

            return uri;
        }

        private string[] SplitQueryUsingAndOperator(string query)
        {
            const string andOperator = " && ";
            if (!query.Contains(andOperator))
            {
                return new string[] { query };
            }

            return query.Split(andOperator);
        }

        #endregion

        private static Uri BindByName(string baseAddress, Dictionary<string, string> queryParams)
            => new Uri(QueryHelpers.AddQueryString(baseAddress, queryParams));

        private static string GetMetaTypeName(Type type)
        {
            var attributes = type.GetCustomAttributes(typeof(E3SMetaTypeAttribute), false);

            if (attributes.Length == 0)
                throw new Exception($"Entity {type.FullName} do not have attribute E3SMetaType");

            return ((E3SMetaTypeAttribute)attributes[0]).Name;
        }
    }
}
