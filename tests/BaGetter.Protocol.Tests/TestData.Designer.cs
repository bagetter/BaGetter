﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace BaGetter.Protocol.Tests {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class TestData {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal TestData() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("BaGetter.Protocol.Tests.TestData", typeof(TestData).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to https://test.example/v3/autocomplete.
        /// </summary>
        internal static string AutocompleteUrl {
            get {
                return ResourceManager.GetString("AutocompleteUrl", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {
        ///  &quot;@id&quot;: &quot;https://api.nuget.org/v3/catalog0/index.json&quot;,
        ///  &quot;@type&quot;: [
        ///    &quot;CatalogRoot&quot;,
        ///    &quot;AppendOnlyCatalog&quot;,
        ///    &quot;Permalink&quot;
        ///  ],
        ///  &quot;commitId&quot;: &quot;6a3a3343-2d99-4a70-a1ef-9221008c8473&quot;,
        ///  &quot;commitTimeStamp&quot;: &quot;2010-01-15T00:00:00.000Z&quot;,
        ///  &quot;count&quot;: 2,
        ///  &quot;items&quot;: [
        ///    {
        ///      &quot;@id&quot;: &quot;https://test.example/v3/catalog/page0.json&quot;,
        ///      &quot;@type&quot;: &quot;CatalogPage&quot;,
        ///      &quot;commitId&quot;: &quot;c088ef83-7dd6-4d24-86e8-85e3089d2c3b&quot;,
        ///      &quot;commitTimeStamp&quot;: &quot;2010-01-05T00:00:00.000Z&quot;,
        ///      &quot;count&quot;: 2
        ///    },
        ///    {
        ///      &quot;@i [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string CatalogIndex {
            get {
                return ResourceManager.GetString("CatalogIndex", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to https://test.example/v3/catalog/index.json.
        /// </summary>
        internal static string CatalogIndexUrl {
            get {
                return ResourceManager.GetString("CatalogIndexUrl", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to .
        /// </summary>
        internal static string CatalogLeafInvalidDependencyVersionRange {
            get {
                return ResourceManager.GetString("CatalogLeafInvalidDependencyVersionRange", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to .
        /// </summary>
        internal static string CatalogLeafInvalidDependencyVersionRangeUrl {
            get {
                return ResourceManager.GetString("CatalogLeafInvalidDependencyVersionRangeUrl", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {
        ///  &quot;@id&quot;: &quot;https://test.example/v3/catalog/page0.json&quot;,
        ///  &quot;@type&quot;: &quot;CatalogPage&quot;,
        ///  &quot;commitId&quot;: &quot;c088ef83-7dd6-4d24-86e8-85e3089d2c3b&quot;,
        ///  &quot;commitTimeStamp&quot;: &quot;2010-01-05T00:00:00.000Z&quot;,
        ///  &quot;count&quot;: 2,
        ///  &quot;parent&quot;: &quot;https://test.example/v3/catalog/index.json&quot;,
        ///  &quot;items&quot;: [
        ///    {
        ///      &quot;@id&quot;: &quot;https://test.example/v3/catalog/2010.01.05.00.00.00/test.package.1.0.0.json&quot;,
        ///      &quot;@type&quot;: &quot;nuget:PackageDetails&quot;,
        ///      &quot;commitId&quot;: &quot;c088ef83-7dd6-4d24-86e8-85e3089d2c3b&quot;,
        ///      &quot;commitTimeStamp&quot;: &quot;2010-01-05T00:00:00. [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string CatalogPage {
            get {
                return ResourceManager.GetString("CatalogPage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to https://test.example/v3/catalog/page0.json.
        /// </summary>
        internal static string CatalogPageUrl {
            get {
                return ResourceManager.GetString("CatalogPageUrl", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {
        ///  &quot;@context&quot;: {
        ///    &quot;@vocab&quot;: &quot;http://schema.nuget.org/schema#&quot;
        ///  },
        ///  &quot;totalHits&quot;: 1,
        ///  &quot;data&quot;: [
        ///    &quot;Test.Package&quot;
        ///  ]
        ///}.
        /// </summary>
        internal static string DefaultAutocomplete {
            get {
                return ResourceManager.GetString("DefaultAutocomplete", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to https://test.example/v3/autocomplete?take=20&amp;prerelease=True&amp;semVerLevel=2.0.0.
        /// </summary>
        internal static string DefaultAutocompleteUrl {
            get {
                return ResourceManager.GetString("DefaultAutocompleteUrl", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {
        ///  &quot;@context&quot;: {
        ///    &quot;@vocab&quot;: &quot;http://schema.nuget.org/schema#&quot;,
        ///    &quot;@base&quot;: &quot;https://test.example/v3/metadata/&quot;
        ///  },
        ///  &quot;totalHits&quot;: 1,
        ///  &quot;data&quot;: [
        ///    {
        ///      &quot;@id&quot;: &quot;https://test.example/v3/metadata/test.package/index.json&quot;,
        ///      &quot;@type&quot;: &quot;Package&quot;,
        ///      &quot;registration&quot;: &quot;https://test.example/v3/metadata/test.package/index.json&quot;,
        ///      &quot;id&quot;: &quot;Test.Package&quot;,
        ///      &quot;version&quot;: &quot;3.0.0&quot;,
        ///      &quot;description&quot;: &quot;Package description&quot;,
        ///      &quot;summary&quot;: &quot;Package summary&quot;,
        ///      &quot;title&quot;: &quot;Test.Package&quot;,
        ///      &quot;ic [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string DefaultSearch {
            get {
                return ResourceManager.GetString("DefaultSearch", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to https://test.example/v3/search?take=20&amp;prerelease=True&amp;semVerLevel=2.0.0.
        /// </summary>
        internal static string DefaultSearchUrl {
            get {
                return ResourceManager.GetString("DefaultSearchUrl", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to https://test.example/v3/content.
        /// </summary>
        internal static string PackageContentUrl {
            get {
                return ResourceManager.GetString("PackageContentUrl", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {
        ///  &quot;versions&quot;: [
        ///    &quot;1.0.0&quot;,
        ///    &quot;2.0.0&quot;
        ///  ]
        ///}.
        /// </summary>
        internal static string PackageContentVersionList {
            get {
                return ResourceManager.GetString("PackageContentVersionList", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to https://test.example/v3/content/test.package/index.json.
        /// </summary>
        internal static string PackageContentVersionListUrl {
            get {
                return ResourceManager.GetString("PackageContentVersionListUrl", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {
        ///  &quot;@id&quot;: &quot;https://test.example/v3/catalog/2010.01.05.00.00.00/deleted.package.1.0.0.json&quot;,
        ///  &quot;@type&quot;: [
        ///    &quot;PackageDelete&quot;,
        ///    &quot;catalog:Permalink&quot;
        ///  ],
        ///  &quot;catalog:commitId&quot;: &quot;c088ef83-7dd6-4d24-86e8-85e3089d2c3b&quot;,
        ///  &quot;catalog:commitTimeStamp&quot;: &quot;2010-01-05T00:00:00.000Z&quot;,
        ///  &quot;id&quot;: &quot;Deleted.Package&quot;,
        ///  &quot;originalId&quot;: &quot;Deleted.Package&quot;,
        ///  &quot;published&quot;: &quot;2010-01-05T00:00:00.000Z&quot;,
        ///  &quot;version&quot;: &quot;1.0.0&quot;,
        ///  &quot;@context&quot;: {
        ///    &quot;@vocab&quot;: &quot;http://schema.nuget.org/schema#&quot;,
        ///    &quot;catalog&quot;: &quot;http://schema.nuget.org/catal [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string PackageDeleteCatalogLeaf {
            get {
                return ResourceManager.GetString("PackageDeleteCatalogLeaf", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to https://test.example/v3/catalog/2010.01.05.00.00.00/deleted.package.1.0.0.json.
        /// </summary>
        internal static string PackageDeleteCatalogLeafUrl {
            get {
                return ResourceManager.GetString("PackageDeleteCatalogLeafUrl", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {
        ///  &quot;@id&quot;: &quot;https://test.example/v3/catalog/2010.01.05.00.00.00/test.package.1.0.0.json&quot;,
        ///  &quot;@type&quot;: [
        ///    &quot;PackageDetails&quot;,
        ///    &quot;catalog:Permalink&quot;
        ///  ],
        ///  &quot;authors&quot;: &quot;Package Author&quot;,
        ///  &quot;catalog:commitId&quot;: &quot;c088ef83-7dd6-4d24-86e8-85e3089d2c3b&quot;,
        ///  &quot;catalog:commitTimeStamp&quot;: &quot;2010-01-05T00:00:00.000Z&quot;,
        ///  &quot;created&quot;: &quot;2010-01-05T00:00:00.000Z&quot;,
        ///  &quot;description&quot;: &quot;Package description&quot;,
        ///  &quot;iconUrl&quot;: &quot;http://test.example/icon.png&quot;,
        ///  &quot;id&quot;: &quot;Test.Package&quot;,
        ///  &quot;isPrerelease&quot;: false,
        ///  &quot;lastEdited&quot;: &quot;2010-01-05T00:00 [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string PackageDetailsCatalogLeaf {
            get {
                return ResourceManager.GetString("PackageDetailsCatalogLeaf", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to https://test.example/v3/catalog/2010.01.05.00.00.00/test.package.1.0.0.json.
        /// </summary>
        internal static string PackageDetailsCatalogLeafUrl {
            get {
                return ResourceManager.GetString("PackageDetailsCatalogLeafUrl", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to https://test.example/v3/metadata.
        /// </summary>
        internal static string PackageMetadataUrl {
            get {
                return ResourceManager.GetString("PackageMetadataUrl", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {
        ///  &quot;@id&quot;: &quot;https://api.nuget.org/v3/registration3-gz-semver2/newtonsoft.json/index.json&quot;,
        ///  &quot;@type&quot;: [
        ///    &quot;catalog:CatalogRoot&quot;,
        ///    &quot;PackageRegistration&quot;,
        ///    &quot;catalog:Permalink&quot;
        ///  ],
        ///  &quot;commitId&quot;: &quot;c088ef83-7dd6-4d24-86e8-85e3089d2c3b&quot;,
        ///  &quot;commitTimeStamp&quot;: &quot;2010-01-05T00:00:00.000Z&quot;,
        ///  &quot;count&quot;: 2,
        ///  &quot;items&quot;: [
        ///    {
        ///      &quot;@id&quot;: &quot;https://test.example/v3/metadata/test.package/index.json#page/1.0.0/1.0.0&quot;,
        ///      &quot;@type&quot;: &quot;catalog:CatalogPage&quot;,
        ///      &quot;commitId&quot;: &quot;c088ef83-7dd6-4d24-86e8-85e3089d2c3b&quot;,
        ///    [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string RegistrationIndexInlinedItems {
            get {
                return ResourceManager.GetString("RegistrationIndexInlinedItems", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to https://test.example/v3/metadata/test.package/index.json.
        /// </summary>
        internal static string RegistrationIndexInlinedItemsUrl {
            get {
                return ResourceManager.GetString("RegistrationIndexInlinedItemsUrl", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {
        ///    &quot;count&quot;: 1,
        ///    &quot;items&quot;: [
        ///        {
        ///            &quot;@id&quot;: &quot;https://nuget.pkg.github.com/metadata/my.github.pkg/index.json&quot;,
        ///            &quot;lower&quot;: &quot;2.1.5&quot;,
        ///            &quot;upper&quot;: &quot;2.1.6&quot;,
        ///            &quot;count&quot;: 2,
        ///            &quot;items&quot;: [
        ///                {
        ///                    &quot;@id&quot;: &quot;https://nuget.pkg.github.com/metadata/my.github.pkg/2.1.6.json&quot;,
        ///                    &quot;packageContent&quot;: &quot;https://nuget.pkg.github.com/metadata/download/my.github.pkg/2.1.6/my.github.pkg.2.1.6.nupkg&quot;,
        ///                     [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string RegistrationIndexLikeGithubPackages {
            get {
                return ResourceManager.GetString("RegistrationIndexLikeGithubPackages", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to https://test.example/v3/metadata/my.github.pkg/index.json.
        /// </summary>
        internal static string RegistrationIndexLikeGithubPackagesUrl {
            get {
                return ResourceManager.GetString("RegistrationIndexLikeGithubPackagesUrl", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {
        ///  &quot;@id&quot;: &quot;https://test.example/v3/metadata/paged.package/index.json&quot;,
        ///  &quot;@type&quot;: [
        ///    &quot;catalog:CatalogRoot&quot;,
        ///    &quot;PackageRegistration&quot;,
        ///    &quot;catalog:Permalink&quot;
        ///  ],
        ///  &quot;commitId&quot;: &quot;c088ef83-7dd6-4d24-86e8-85e3089d2c3b&quot;,
        ///  &quot;commitTimeStamp&quot;: &quot;2010-01-05T00:00:00.000Z&quot;,
        ///  &quot;count&quot;: 2,
        ///  &quot;items&quot;: [
        ///    {
        ///      &quot;@id&quot;: &quot;https://test.example/v3/metadata/paged.package/page/1.0.0/1.0.0.json&quot;,
        ///      &quot;@type&quot;: &quot;catalog:CatalogPage&quot;,
        ///      &quot;commitId&quot;: &quot;c088ef83-7dd6-4d24-86e8-85e3089d2c3b&quot;,
        ///      &quot;commitTimeStamp&quot;: &quot;2 [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string RegistrationIndexPagedItems {
            get {
                return ResourceManager.GetString("RegistrationIndexPagedItems", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to https://test.example/v3/metadata/paged.package/index.json.
        /// </summary>
        internal static string RegistrationIndexPagedItemsUrl {
            get {
                return ResourceManager.GetString("RegistrationIndexPagedItemsUrl", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {
        ///  &quot;@id&quot;: &quot;https://test.example/v3/metadata/test.package/1.0.0.json&quot;,
        ///  &quot;@type&quot;: [
        ///    &quot;Package&quot;,
        ///    &quot;http://schema.nuget.org/catalog#Permalink&quot;
        ///  ],
        ///  &quot;catalogEntry&quot;: &quot;https://test.example/v3/catalog/2010.01.05.00.00.00/test.package.1.0.0.json&quot;,
        ///  &quot;listed&quot;: true,
        ///  &quot;packageContent&quot;: &quot;https://test.example/v3/content/test.package/1.0.0/test.package.1.0.0.nupkg&quot;,
        ///  &quot;published&quot;: &quot;2010-01-05T00:00:00.000Z&quot;,
        ///  &quot;registration&quot;: &quot;https://test.example/v3/metadata/test.package/index.json&quot;,
        ///  &quot;@context&quot;: {
        ///    &quot;@voc [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string RegistrationLeafListed {
            get {
                return ResourceManager.GetString("RegistrationLeafListed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to https://test.example/v3/metadata/test.package/1.0.0.json.
        /// </summary>
        internal static string RegistrationLeafListedUrl {
            get {
                return ResourceManager.GetString("RegistrationLeafListedUrl", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {
        ///  &quot;@id&quot;: &quot;https://test.example/v3/metadata/paged.package/2.0.0.json&quot;,
        ///  &quot;@type&quot;: [
        ///    &quot;Package&quot;,
        ///    &quot;http://schema.nuget.org/catalog#Permalink&quot;
        ///  ],
        ///  &quot;catalogEntry&quot;: &quot;https://test.example/v3/catalog/2010.01.05.00.00.00/paged.package.2.0.0.json&quot;,
        ///  &quot;listed&quot;: false,
        ///  &quot;packageContent&quot;: &quot;https://test.example/v3/content/paged.package/2.0.0/paged.package.2.0.0.nupkg&quot;,
        ///  &quot;published&quot;: &quot;2010-01-05T00:00:00.000Z&quot;,
        ///  &quot;registration&quot;: &quot;https://test.example/v3/metadata/paged.package/index.json&quot;,
        ///  &quot;@context&quot;: {
        ///    [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string RegistrationLeafUnlisted {
            get {
                return ResourceManager.GetString("RegistrationLeafUnlisted", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to https://test.example/v3/metadata/paged.package/2.0.0.json.
        /// </summary>
        internal static string RegistrationLeafUnlistedUrl {
            get {
                return ResourceManager.GetString("RegistrationLeafUnlistedUrl", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {
        ///  &quot;@id&quot;: &quot;https://test.example/v3/metadata/paged.package/page/2.0.0/3.0.0.json&quot;,
        ///  &quot;@type&quot;: &quot;catalog:CatalogPage&quot;,
        ///  &quot;commitId&quot;: &quot;c088ef83-7dd6-4d24-86e8-85e3089d2c3b&quot;,
        ///  &quot;commitTimeStamp&quot;: &quot;2010-01-05T00:00:00.000Z&quot;,
        ///  &quot;count&quot;: 2,
        ///  &quot;lower&quot;: &quot;2.0.0&quot;,
        ///  &quot;parent&quot;: &quot;https://test.example/v3/metadata/paged.package/index.json&quot;,
        ///  &quot;upper&quot;: &quot;3.0.0&quot;,
        ///  &quot;items&quot;: [
        ///    {
        ///      &quot;@id&quot;: &quot;https://test.example/v3/metadata/paged.package/2.0.0.json&quot;,
        ///      &quot;@type&quot;: &quot;Package&quot;,
        ///      &quot;commitId&quot;: &quot;c088ef83-7dd6-4d24-86e8-85e [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string RegistrationPage {
            get {
                return ResourceManager.GetString("RegistrationPage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to https://test.example/v3/metadata/paged.package/page/2.0.0/3.0.0.json.
        /// </summary>
        internal static string RegistrationPageUrl {
            get {
                return ResourceManager.GetString("RegistrationPageUrl", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to https://test.example/v3/search.
        /// </summary>
        internal static string SearchUrl {
            get {
                return ResourceManager.GetString("SearchUrl", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {
        ///  &quot;version&quot;: &quot;3.0.0&quot;,
        ///  &quot;resources&quot;: [
        ///    {
        ///      &quot;@id&quot;: &quot;https://test.example/v3/catalog/index.json&quot;,
        ///      &quot;@type&quot;: &quot;Catalog/3.0.0&quot;,
        ///      &quot;comment&quot;: &quot;&quot;
        ///    },
        ///    {
        ///      &quot;@id&quot;: &quot;https://test.example/v3/metadata/&quot;,
        ///      &quot;@type&quot;: &quot;RegistrationsBaseUrl/3.6.0&quot;,
        ///      &quot;comment&quot;: &quot;&quot;
        ///    },
        ///    {
        ///      &quot;@id&quot;: &quot;https://test.example/v3/content/&quot;,
        ///      &quot;@type&quot;: &quot;PackageBaseAddress/3.0.0&quot;,
        ///      &quot;comment&quot;: &quot;&quot;
        ///    },
        ///    {
        ///      &quot;@id&quot;: &quot;https://test.example/v3/search&quot;,
        ///      &quot;@type&quot;: &quot;SearchQueryService/3.4.0&quot; [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string ServiceIndex {
            get {
                return ResourceManager.GetString("ServiceIndex", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to https://test.example/v3/index.json.
        /// </summary>
        internal static string ServiceIndexUrl {
            get {
                return ResourceManager.GetString("ServiceIndexUrl", resourceCulture);
            }
        }
    }
}
