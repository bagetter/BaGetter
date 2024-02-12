using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using NuGet.Versioning;
using Xunit;

namespace BaGetter.Core.Tests.Metadata
{
    public class RegistrationBuilderTests
    {
        private readonly Mock<IUrlGenerator> _urlGenerator;
        private readonly RegistrationBuilder _registrationBuilder;

        public RegistrationBuilderTests()
        {
            _urlGenerator = new Mock<IUrlGenerator>();
            _registrationBuilder = new RegistrationBuilder(_urlGenerator.Object);
        }

        #region helper methods

        private PackageRegistration GetPackageRegistration()
        {
            var packageId = "BaGetter.Test";
            var packages = new List<Package>
            {
                GetTestPackage(packageId, "3.1.0"),
                GetTestPackage(packageId, "10.0.5"),
                GetTestPackage(packageId, "3.2.0"),
                GetTestPackage(packageId, "3.1.0-pre"),
                GetTestPackage(packageId, "1.0.0-beta1"),
                GetTestPackage(packageId, "1.0.0"),
            };

            return new PackageRegistration(packageId, packages);
        }

        /// <summary>
        /// Create a fake <see cref="Package"></see> with the minimum metadata needed by the <see cref="RegistrationBuilder"></see>.
        /// </summary>
        private static Package GetTestPackage(string packageId, string version)
        {
            return new Package
            {
                Id = packageId,
                Authors = new string[] { "test" },
                PackageTypes = new List<PackageType> { new PackageType { Name = "test" } },
                Dependencies = new List<PackageDependency> { },
                Version = new NuGetVersion(version),
                //Use current date for each packages publish date, because later a date offset will be
                //calculated and leads to an overflow error of the offset because the default is year 0001.
                Published = DateTime.UtcNow,
                Downloads = 1
            };
        }

        #endregion

        [Fact]
        public void Ctor_UrlGeneratorIsNull_ShouldThrow()
        {
            // Act/Assert
            var ex = Assert.Throws<ArgumentNullException>(() => new RegistrationBuilder(null));
        }

        [Fact]
        public void BuildIndex_PackageRegistrationIsNull_ShouldThrow()
        {
            // Arrange
            var registrationBuilder = new RegistrationBuilder(_urlGenerator.Object);

            // Act/Assert
            var ex = Assert.Throws<ArgumentNullException>(() => registrationBuilder.BuildIndex(null));
        }

        [Fact]
        public void BuildIndex_RegistrationIndexResponse_ShouldBeSortedByVersion()
        {
            // Arrange
            var registration = GetPackageRegistration();

            // Act
            var response = _registrationBuilder.BuildIndex(registration);

            // Assert
            Assert.Equal(registration.Packages.Count, response.Pages[0].ItemsOrNull.Count);

            var index = 0;
            foreach (var package in registration.Packages.OrderBy(p => p.Version))
            {
                Assert.Equal(package.Version.ToFullString(), response.Pages[0].ItemsOrNull[index++].PackageMetadata.Version);
            }
        }

        [Fact]
        public void BuildIndex_RegistrationIndexResponse_ShouldHaveCorrectTotalDownloads()
        {
            // Arrange
            var registration = GetPackageRegistration();

            // Act
            var response = _registrationBuilder.BuildIndex(registration);

            // Assert
            Assert.Equal(registration.Packages.Count, response.TotalDownloads);
        }

        [Fact]
        public void BuildLeaf_RegistrationLeafResponse_ShouldHaveCorrectProperties()
        {
            // Arrange
            var packageId = "dummy";
            var packageVersion = "1.0.42";
            var isPackageListed = true;
            var publishDate = DateTime.UtcNow;

            var package = GetTestPackage(packageId, packageVersion);
            package.Listed = isPackageListed;
            package.Published = publishDate;

            // Act
            var response = _registrationBuilder.BuildLeaf(package);

            // Assert
            Assert.Equal(isPackageListed, response.Listed);
            Assert.Equal(publishDate, response.Published);
        }
    }
}
