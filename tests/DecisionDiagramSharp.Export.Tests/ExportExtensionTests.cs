using DecisionDiagramSharp;

namespace DecisionDiagramSharp.Export.Tests;

[TestClass]
public sealed class ExportExtensionTests
{
    /// <summary>
    /// Verifies that BDD export extension methods produce correctly formatted Markdown, CSV, and AsciiDoc outputs.
    /// </summary>
    /// <remarks>
    /// Confirms that high-level BDD export extensions produce meaningful tables from a handle-owned diagram
    /// without requiring the caller to pass the manager explicitly.
    /// </remarks>
    [TestMethod]
    public void BddExportExtensions_FormatTruthTableAndModelsAsMarkdown()
    {
        // Arrange
        var manager = new BddManager();
        var a = manager.GetOrAddVariable("A");
        var expression = manager.Var(a);

        // Act
        var truthTable = expression.ToMarkdownTruthTable(new TruthTableOptions { MaxVariables = 1, MaxRows = 2 });
        var truthTableCsv = expression.ToCsvTruthTable(new TruthTableOptions { MaxVariables = 1, MaxRows = 2 });
        var truthTableAsciiDoc = expression.ToAsciiDocTruthTable(new TruthTableOptions { MaxVariables = 1, MaxRows = 2 });
        var models = expression.ToMarkdownModels(new ModelEnumerationOptions { MaxModels = 2 });
        var modelsCsv = expression.ToCsvModels(new ModelEnumerationOptions { MaxModels = 2 });
        var modelsAsciiDoc = expression.ToAsciiDocModels(new ModelEnumerationOptions { MaxModels = 2 });

        // Assert
        StringAssert.Contains(truthTable, "## BDD Truth Table");
        StringAssert.Contains(truthTable, "| A | Result |");
        StringAssert.Contains(truthTableCsv, "A,Result");
        StringAssert.Contains(truthTableAsciiDoc, "|A |Result");
        StringAssert.Contains(models, "## BDD Models");
        StringAssert.Contains(models, "| Index | A |");
        StringAssert.Contains(modelsCsv, "Index,A");
        StringAssert.Contains(modelsAsciiDoc, "|Index |A");
    }

    /// <summary>
    /// Verifies that ZDD export extension methods produce correctly formatted Markdown, CSV, and AsciiDoc outputs.
    /// </summary>
    /// <remarks>
    /// Confirms that high-level ZDD export extensions preserve set-family semantics in formatted output.
    /// </remarks>
    [TestMethod]
    public void ZddExportExtensions_FormatSetFamiliesAsMarkdown()
    {
        // Arrange
        var manager = new ZddManager();
        var a = manager.GetOrAddVariable("A");
        var family = manager.MakeSet(new[] { a });

        // Act
        var markdown = family.ToMarkdownSetFamily(new SetEnumerationOptions { MaxSets = 2 });
        var csv = family.ToCsvSetFamily(new SetEnumerationOptions { MaxSets = 2 });
        var asciiDoc = family.ToAsciiDocSetFamily(new SetEnumerationOptions { MaxSets = 2 });

        // Assert
        StringAssert.Contains(markdown, "## ZDD Set Family");
        StringAssert.Contains(markdown, "{A}");
        StringAssert.Contains(csv, "Index,Set");
        StringAssert.Contains(asciiDoc, "|Index |Set");
    }
}
