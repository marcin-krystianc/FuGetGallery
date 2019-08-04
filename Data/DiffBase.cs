using System;

namespace FuGetGallery
{
    public abstract class DiffBase
    {
        public PackageData Package { get; }
        public PackageTargetFramework Framework { get; }
        public PackageData OtherPackage { get; }
        public PackageTargetFramework OtherFramework { get; }

        public DiffBase(PackageData package, PackageTargetFramework framework, PackageData otherPackage, PackageTargetFramework otherFramework)
        {
            this.Package = package;
            this.Framework = framework;
            this.OtherPackage = otherPackage;
            this.OtherFramework = otherFramework
                ?? throw new Exception($"Could not find framework matching \"{framework?.Moniker}\" in {otherPackage?.Id} {otherPackage?.Version}.");
        }
    }
}
