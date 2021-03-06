﻿using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace TelimenaClient
{
    internal class TelimenaHttpClient : ITelimenaHttpClient
    {
        public TelimenaHttpClient(HttpClient client)
        {
            this.client = client;
        }

        public Uri BaseUri => this.client.BaseAddress;

        private readonly HttpClient client;

        public Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent httpContent)
        {
            return this.client.PostAsync(requestUri, httpContent);
        }

        public Task<HttpResponseMessage> GetAsync(string requestUri)
        {
            return this.client.GetAsync(requestUri);
        }
    }
}