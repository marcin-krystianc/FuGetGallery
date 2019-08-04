using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using ListDiff;
using Mono.Cecil;
using System.Linq;
using System.Net.Http;

namespace FuGetGallery
{
    public abstract class DiffBase
    {
        public PackageData Package { get; protected set; }
        public PackageTargetFramework Framework { get; protected set; }
        public PackageData OtherPackage { get; protected set; }
        public PackageTargetFramework OtherFramework { get; protected set; }
        public string Error { get; protected set; } = "";
    }
}
