﻿namespace Telimena.WebApp.Core.Models
{
    using System;
    using System.Collections.Generic;

    public class FunctionUsageSummary : UsageSummary
    {
        public int FunctionId { get; set; }
        public virtual Function Function { get; set; }
        public virtual ICollection<FunctionUsageDetail> UsageDetails { get; set; } = new List<FunctionUsageDetail>();

        #region Overrides of UsageSummary
        public override void UpdateUsageDetails(DateTime lastUsageDateTime, AssemblyVersion version)
        {
            var usage = new FunctionUsageDetail()
            {
                DateTime = lastUsageDateTime,
                UsageSummary = this,
                AssemblyVersion = version
            };
            this.UsageDetails.Add(usage);
            this.SummaryCount = this.UsageDetails.Count;
        }

        private int _summaryCount;
        public override int SummaryCount
        {
            get
            {
                this._summaryCount = this.UsageDetails?.Count ?? 0;
                return this._summaryCount;
            }
            set => this._summaryCount = value;
        }
        #endregion
    }
}