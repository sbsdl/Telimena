﻿using System.IO;
using System.Web.Script.Serialization;
using System.Xml.Serialization;

namespace Telimena.Client
{
    using System.Collections.Generic;

    /// <summary>
    /// A data about a program
    /// </summary>
    public class ProgramInfo
    {
        /// <summary>
        /// A typical program has a primary assembly or an 'entry point'.
        /// This is where it's info should be defined
        /// </summary>
        public AssemblyInfo PrimaryAssembly { get;  set; }

        /// <summary>
        /// The name of the application.
        /// </summary>
        public string Name { get;  set; }

        /// <summary>
        /// The ID of the developer-owner of the application. Only provide if you have an ID.
        /// </summary>
        public int? DeveloperId { get; set; }

        /// <summary>
        /// An optional collection of helper assemblies data
        /// </summary>
        public List<AssemblyInfo> HelperAssemblies { get; set; }

        public string PrimaryAssemblyPath => this.PrimaryAssembly?.Location;
    }
}