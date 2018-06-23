﻿namespace Telimena.WebApp.Core.Models
{
    using System;
    using System.Collections.Generic;

    public class ProgramUsageSummary : UsageSummary
    {
        public int ProgramId { get; set; }
        public virtual Program Program { get; set; }

        public virtual ICollection<ProgramUsageDetail> UsageDetails { get; set; } = new List<ProgramUsageDetail>();

        public override void UpdateUsageDetails(DateTime lastUsageDateTime, AssemblyVersion version)
        {
            var usage = new ProgramUsageDetail()
            {
                DateTime = lastUsageDateTime,
                UsageSummary = this,
                AssemblyVersion = version
            };
            this.UsageDetails.Add(usage);
        }

       
    }


}