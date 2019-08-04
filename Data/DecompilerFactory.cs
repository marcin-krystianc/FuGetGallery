using Mono.Cecil;

namespace FuGetGallery
{
    public static class DecompilerFactory
    {
        public static ICSharpCode.Decompiler.CSharp.OutputVisitor.CSharpFormattingOptions Format {get;}

        static DecompilerFactory()
        {
            ICSharpCode.Decompiler.CSharp.OutputVisitor.FormattingOptionsFactory.CreateMono();
            Format.SpaceBeforeMethodCallParentheses = false;
            Format.SpaceBeforeMethodDeclarationParentheses = false;
            Format.SpaceBeforeConstructorDeclarationParentheses = false;
            Format.PropertyBraceStyle = ICSharpCode.Decompiler.CSharp.OutputVisitor.BraceStyle.EndOfLine;
            Format.PropertyGetBraceStyle = ICSharpCode.Decompiler.CSharp.OutputVisitor.BraceStyle.EndOfLine;
            Format.PropertySetBraceStyle = ICSharpCode.Decompiler.CSharp.OutputVisitor.BraceStyle.EndOfLine;
            Format.AutoPropertyFormatting = ICSharpCode.Decompiler.CSharp.OutputVisitor.PropertyFormatting.ForceOneLine;
            Format.SimplePropertyFormatting = ICSharpCode.Decompiler.CSharp.OutputVisitor.PropertyFormatting.ForceOneLine;
            Format.IndentPropertyBody = false;
            Format.IndexerDeclarationClosingBracketOnNewLine = ICSharpCode.Decompiler.CSharp.OutputVisitor.NewLinePlacement.SameLine;
            Format.IndexerClosingBracketOnNewLine = ICSharpCode.Decompiler.CSharp.OutputVisitor.NewLinePlacement.SameLine;
            Format.NewLineAferIndexerDeclarationOpenBracket = ICSharpCode.Decompiler.CSharp.OutputVisitor.NewLinePlacement.SameLine;
            Format.NewLineAferIndexerOpenBracket = ICSharpCode.Decompiler.CSharp.OutputVisitor.NewLinePlacement.SameLine;
        }

        public static ICSharpCode.Decompiler.CSharp.CSharpDecompiler GetDecompiler(AssemblyDefinition assemblyDefinition)
        {
            var decompilerSettings = new ICSharpCode.Decompiler.DecompilerSettings
            {
                ShowXmlDocumentation = false,
                ThrowOnAssemblyResolveErrors = false,
                AlwaysUseBraces = false,
                CSharpFormattingOptions = Format,
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
