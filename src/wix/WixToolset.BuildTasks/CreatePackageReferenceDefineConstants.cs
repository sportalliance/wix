// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.BuildTasks
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    /// <summary>
    /// MSBuild task to create a list of preprocessor defines from resolved package references.
    /// </summary>
    public sealed class CreatePackageReferenceDefineConstants : Task
    {
        [Required]
        public ITaskItem[] ResolvedPackageReferences { get; set; }

        [Output]
        public ITaskItem[] DefineConstants { get; private set; }

        public override bool Execute()
        {
            var defineConstants = new SortedDictionary<string, string>();

            foreach (var packageReference in this.ResolvedPackageReferences)
            {
                this.AddDefineConstantsForResolvedReference(defineConstants, packageReference);
            }

            this.DefineConstants = defineConstants.Select(define => new TaskItem(define.Key + "=" + define.Value)).ToArray<ITaskItem>();

            return true;
        }

        private void AddDefineConstantsForResolvedReference(IDictionary<string, string> defineConstants, ITaskItem packageReference)
        {
            var packageDir = packageReference.GetMetadata("Path");

            // Define constants only if a "Path" property exists, i.e., the package must have the property
            // "GeneratePathProperty" set to true, or it contains a Tools folder.
            if (!String.IsNullOrWhiteSpace(packageDir))
            {
                var packageName = packageReference.GetMetadata("Identity");
                var packageFileName = Path.ChangeExtension(packageName, ".nupkg");
                var packageVersion = this.GetPackageVersion(packageDir, packageName);
                var referenceName = ToolsCommon.CreateIdentifierFromValue(ToolsCommon.GetMetadataOrDefault(packageReference, "Name", packageName));

                defineConstants[referenceName + ".PackageName"] = packageName;
                defineConstants[referenceName + ".PackageFileName"] = packageFileName;
                defineConstants[referenceName + ".Version"] = packageVersion;

                defineConstants[referenceName + ".PackageDir"] = packageDir;
            }
        }

        private string GetPackageVersion(string packageDir, string packageName)
        {
            var nuspecPath = Path.Combine(packageDir, Path.ChangeExtension(packageName, ".nuspec"));
            var nuspec = XDocument.Load(nuspecPath);
            
            if (nuspec.Root != null)
            {
                var ns = nuspec.Root.GetDefaultNamespace();

                return nuspec.Descendants(ns + "metadata").Elements(ns + "version").FirstOrDefault()?.Value;    
            }

            return String.Empty;
        }
    }
}
