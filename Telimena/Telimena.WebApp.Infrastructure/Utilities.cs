﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telimena.Client;

namespace Telimena.WebApp.Infrastructure
{
    public static class Utilities
    {
        public static UpdateRequest ReadRequest(string escapedJsonString, ITelimenaSerializer serializer)
        {
            string json = serializer.UrlDecodeJson(escapedJsonString);
            UpdateRequest model = serializer.Deserialize<UpdateRequest>(json);
            return model;
        }
    }
}