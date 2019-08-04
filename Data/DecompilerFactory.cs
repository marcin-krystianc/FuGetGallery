using Mono.Cecil;

namespace FuGetGallery
{
    public static class DecompilerFactory
    {
        public static ICSharpCode.Decompiler.CSharp.CSharpDecompiler GetDecompiler(AssemblyDefinition assemblyDefinition)
        {
            var format = ICSharpCode.Decompiler.CSharp.OutputVisitor.FormattingOptionsFactory.CreateMono();
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

            var decompilerSettings = new ICSharpCode.Decompiler.DecompilerSettings
            {
                ShowXmlDocumentation = false,
                ThrowOnAssemblyResolveErrors = false,
                AlwaysUseBraces = false,
                CSharpFormattingOptions = format,
                ExpandMemberDefinitions = true,
                DecompileMemberBodies = true,
                UseExpressionBodyForCalculatedGetterOnlyProperties = true,
            };

            var mainMonule = assemblyDefinition.MainModule;
            if (mainMonule == null) return null;
            return new ICSharpCode.Decompiler.CSharp.CSharpDecompiler(mainMonule, decompilerSettings);
        }
    }
}
