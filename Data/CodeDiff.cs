using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using ListDiff;
using Mono.Cecil;
using System.Linq;
using System.Net.Http;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using ICSharpCode.Decompiler.TypeSystem;

namespace FuGetGallery
{
    public static class DiffPieceExt
    {
        public static string GetDiffText (this DiffPiece diffPiece)
        {
            switch (diffPiece.Type) {
                case ChangeType.Inserted:
                    return $"+ {diffPiece.Text}";
                case ChangeType.Deleted:
                    return $"- {diffPiece.Text}";
                default:
                    return $"  {diffPiece.Text}";
            }
        }
    }

    public class CodeDiff : DiffBase
    {
        readonly InlineDiffBuilder inlineDiffBuilder;

        static readonly CodeDiffCache cache = new CodeDiffCache ();

        readonly ICSharpCode.Decompiler.DecompilerSettings decompilerSettings;

        public List<NamespaceDiffInfo> Namespaces { get; } = new List<NamespaceDiffInfo> ();

        public class DiffInfo
        {
            public ListDiffActionType Action;
        }

        public class NamespaceDiffInfo : DiffInfo
        {
            public string Namespace;
            public List<TypeDiffInfo> Types = new List<TypeDiffInfo> ();
        }

        public class TypeDiffInfo : DiffInfo
        {
            public TypeDefinition Type;
            public PackageTargetFramework Framework;
            public IReadOnlyList<IReadOnlyCollection<DiffPiece>> DiffChunks;
        }

        public CodeDiff (PackageData package, PackageTargetFramework framework, PackageData otherPackage, PackageTargetFramework otherFramework)
        {
            var format = ICSharpCode.Decompiler.CSharp.OutputVisitor.FormattingOptionsFactory.CreateMono ();
            format.SpaceBeforeMethodCallParentheses = false;
            format.SpaceBeforeMethodDeclarationParentheses = false;
            format.SpaceBeforeConstructorDeclarationParentheses = false;
            format.PropertyBraceStyle = ICSharpCode.Decompiler.CSharp.OutputVisitor.BraceStyle.EndOfLine;
            format.PropertyGetBraceStyle = ICSharpCode.Decompiler.CSharp.OutputVisitor.BraceStyle.EndOfLine;
            format.PropertySetBraceStyle = ICSharpCode.Decompiler.CSharp.OutputVisitor.BraceStyle.EndOfLine;
            format.AutoPropertyFormatting = ICSharpCode.Decompiler.CSharp.OutputVisitor.PropertyFormatting.ForceOneLine;
            format.SimplePropertyFormatting = ICSharpCode.Decompiler.CSharp.OutputVisitor.PropertyFormatting.ForceOneLine;
            format.IndentPropertyBody = false;
            format.IndexerDeclarationClosingBracketOnNewLine = ICSharpCode.Decompiler.CSharp.OutputVisitor.NewLinePlacement.SameLine;
            format.IndexerClosingBracketOnNewLine = ICSharpCode.Decompiler.CSharp.OutputVisitor.NewLinePlacement.SameLine;
            format.NewLineAferIndexerDeclarationOpenBracket = ICSharpCode.Decompiler.CSharp.OutputVisitor.NewLinePlacement.SameLine;
            format.NewLineAferIndexerOpenBracket = ICSharpCode.Decompiler.CSharp.OutputVisitor.NewLinePlacement.SameLine;
            decompilerSettings = new ICSharpCode.Decompiler.DecompilerSettings {
                ShowXmlDocumentation = false,
                ThrowOnAssemblyResolveErrors = false,
                AlwaysUseBraces = false,
                CSharpFormattingOptions = format,
                ExpandMemberDefinitions = true,
                DecompileMemberBodies = true,
                UseExpressionBodyForCalculatedGetterOnlyProperties = true,
            };

            this.Package = package;
            this.Framework = framework;
            this.OtherPackage = otherPackage;
            this.OtherFramework = otherFramework;
            inlineDiffBuilder = new InlineDiffBuilder (new Differ ());
  
            if (otherFramework == null) {
                Error = $"Could not find framework matching \"{framework?.Moniker}\" in {otherPackage?.Id} {otherPackage?.Version}.";
                return;
            }

            var asmDiff = OtherFramework.PublicAssemblies.Diff (Framework.PublicAssemblies, (x, y) => x.Definition.Name.Name == y.Definition.Name.Name);

            var types = new List<TypeDiffInfo> ();
            foreach (var aa in asmDiff.Actions) {
                IEnumerable<Tuple<TypeDefinition, PackageTargetFramework>> srcTypes;
                IEnumerable<Tuple<TypeDefinition, PackageTargetFramework>> destTypes;
                switch (aa.ActionType) {
                    case ListDiffActionType.Add:
                        srcTypes = Enumerable.Empty<Tuple<TypeDefinition, PackageTargetFramework>> ();
                        destTypes = aa.DestinationItem.PublicTypes.Select (x => Tuple.Create (x, Framework));
                        break;
                    case ListDiffActionType.Remove:
                        srcTypes = aa.SourceItem.PublicTypes.Select (x => Tuple.Create (x, OtherFramework));
                        destTypes = Enumerable.Empty<Tuple<TypeDefinition, PackageTargetFramework>> ();
                        break;
                    default:
                        srcTypes = aa.SourceItem.PublicTypes.Select (x => Tuple.Create (x, OtherFramework));
                        destTypes = aa.DestinationItem.PublicTypes.Select (x => Tuple.Create (x, Framework));
                        break;
                }
                if (aa.ActionType == ListDiffActionType.Remove)
                    continue;

                var oldCodeDecompiler = new ICSharpCode.Decompiler.CSharp.CSharpDecompiler (aa.SourceItem.Definition.MainModule, decompilerSettings);
                var newCodeDecompiler = new ICSharpCode.Decompiler.CSharp.CSharpDecompiler (aa.DestinationItem.Definition.MainModule, decompilerSettings);

                var typeDiff = srcTypes.Diff (destTypes, (x, y) => x.Item1.FullName == y.Item1.FullName);
                foreach (var ta in typeDiff.Actions) {
                    var ti = new TypeDiffInfo { Action = ta.ActionType };

                    var oldImplementation = string.Empty;
                    var newImplementation = string.Empty;

                    switch (ta.ActionType) {
                        case ListDiffActionType.Add:
                            ti.Type = ta.DestinationItem.Item1;
                            ti.Framework = ta.DestinationItem.Item2;
                            newImplementation = newCodeDecompiler.DecompileTypeAsString (new FullTypeName (ta.DestinationItem.Item1.FullName));
                            break;
                        case ListDiffActionType.Remove:
                            ti.Type = ta.SourceItem.Item1;
                            ti.Framework = ta.SourceItem.Item2;
                            oldImplementation = oldCodeDecompiler.DecompileTypeAsString (new FullTypeName (ta.SourceItem.Item1.FullName));
                            break;
                        default:
                            ti.Type = ta.DestinationItem.Item1;
                            ti.Framework = ta.DestinationItem.Item2;
                            oldImplementation = oldCodeDecompiler.DecompileTypeAsString (new FullTypeName (ta.SourceItem.Item1.FullName));
                            newImplementation = newCodeDecompiler.DecompileTypeAsString (new FullTypeName (ta.DestinationItem.Item1.FullName));
                            break;
                    }

                    var difChunks = MakeDiffChunks (inlineDiffBuilder.BuildDiffModel (oldImplementation, newImplementation, true).Lines, 3)
                        .ToList ();

                    if (difChunks.Any()) {
                        ti.DiffChunks = difChunks;
                        types.Add (ti);
                    }              
                }
            }
            foreach (var ns in types.GroupBy (x => x.Type.Namespace)) {
                var ni = new NamespaceDiffInfo { Action = ListDiffActionType.Update };
                ni.Namespace = ns.Key;
                ni.Types.AddRange (ns);
                Namespaces.Add (ni);
            }
            Namespaces.Sort ((x, y) => string.Compare (x.Namespace, y.Namespace, StringComparison.Ordinal));
        }

        private static IEnumerable<IReadOnlyCollection<DiffPiece>> MakeDiffChunks(IEnumerable<DiffPiece> diffLines, int contextSize)
        {
            var queue = new Queue<DiffPiece> ();
            var diffSeen = false;
            var unmodifiedTail = 0;

            foreach (var diffLine in diffLines) 
            {
                queue.Enqueue (diffLine);

                if (diffLine.Type == ChangeType.Deleted || diffLine.Type == ChangeType.Inserted) {
                    diffSeen = true;
                    unmodifiedTail = 0;
                }
                else {
                    unmodifiedTail++;
                }

                if (!diffSeen && queue.Count > contextSize)
                {
                    queue.Dequeue ();
                }
                else if (diffSeen && unmodifiedTail == contextSize * 2) {
                    var resultSize = queue.Count - contextSize;
                    var result = new List<DiffPiece> (resultSize);
                    for (int i = 0; i < resultSize; i++) {
                        result.Add (queue.Dequeue ());
                    }
                    yield return result;
                    diffSeen = false;
                    unmodifiedTail = queue.Count;
                }
            }

            if (diffSeen) {
                if (unmodifiedTail > contextSize) {
                    yield return queue
                        .SkipLast (unmodifiedTail - contextSize)
                        .ToList ();
                }
                yield return queue;
            }
        }

        public static async Task<CodeDiff> GetAsync (
            object inputId,
            object inputVersion,
            object inputFramework,
            object inputOtherVersion,
            HttpClient httpClient,
            CancellationToken token)
        {
            var versions = await PackageVersions.GetAsync (inputId, httpClient, token).ConfigureAwait (false);
            var version = versions.GetVersion (inputVersion);
            var otherVersion = versions.GetVersion (inputOtherVersion);
            var framework = (inputFramework ?? "").ToString ().ToLowerInvariant ().Trim ();

            return await cache.GetAsync(
                    Tuple.Create (versions.LowerId, version.VersionString, framework),
                    otherVersion.VersionString,
                    httpClient,
                    token)
                .ConfigureAwait (false);
        }

        class CodeDiffCache : DataCache<Tuple<string, string, string>, string, CodeDiff>
        {
            public CodeDiffCache () : base (TimeSpan.FromDays (365))
            {
            }
            
            protected override async Task<CodeDiff> GetValueAsync (
                Tuple<string, string, string> packageSpec,
                string otherVersion,
                HttpClient httpClient,
                CancellationToken token)
            {
                var packageId = packageSpec.Item1;
                var version = packageSpec.Item2;
                var inputFramework = packageSpec.Item3;

                var package = await PackageData.GetAsync (packageId, version, httpClient, token).ConfigureAwait (false);
                var otherPackage = await PackageData.GetAsync (packageId, otherVersion, httpClient, token).ConfigureAwait (false);

                var framework = package.FindClosestTargetFramework (inputFramework);
                var otherFramework = otherPackage.FindClosestTargetFramework (inputFramework);

                return await Task.Run (() => new CodeDiff (package, framework, otherPackage, otherFramework)).ConfigureAwait (false);
            }
        }
    }
}
