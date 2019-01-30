﻿using System;
using System.Collections.Generic;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;

namespace TelimenaClient.Telemetry
{
    /// <inheritdoc />
    public class TelemetryModule : ITelemetryModule
    {
        /// <summary>
        ///     Asynchronous Telimena methods
        /// </summary>
        public TelemetryModule(ITelimenaProperties properties)
        {
            this.properties = properties;
        }

        private readonly ITelimenaProperties properties;

        /// <summary>
        /// Gets the telemetry client.
        /// </summary>
        /// <value>The telemetry client.</value>
        public TelemetryClient TelemetryClient { get; private set; }


        /// <inheritdoc />
        public void View(string viewName, Dictionary<string, object> telemetryData = null)
        {
            this.TelemetryClient.TrackPageView(viewName);
        }

        /// <inheritdoc />

        public void Exception(Exception exception, Dictionary<string, object> telemetryData = null)
        {
            this.TelemetryClient.TrackException(exception);
        }

        /// <inheritdoc />

        public void SendAllDataNow()
        {
            this.TelemetryClient.Flush();
        }

        /// <summary>
        /// Initializes the telemetry client.
        /// </summary>
        public void InitializeTelemetryClient()
        {
            TelemetryClientBuilder builder = new TelemetryClientBuilder(this.properties);
            this.TelemetryClient = builder.GetClient();
        }

        /// <summary>
        /// Initializes the telemetry client.
        /// </summary>
        [Obsolete("For tests only")]
        internal void InitializeTelemetryClient(ITelemetryChannel channel)
        {
            TelemetryClientBuilder builder = new TelemetryClientBuilder(this.properties);
#pragma warning disable 618
            this.TelemetryClient = builder.GetClient(channel);
#pragma warning restore 618
        }

        /// <inheritdoc />
        public void Event(string eventName, Dictionary<string, string> telemetryData = null)
        {
            this.TelemetryClient.TrackEvent(eventName, telemetryData);

        }

     
    }
}