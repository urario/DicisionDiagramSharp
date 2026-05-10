using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DecisionDiagramSharp.Diagnostics.Tests;

[TestClass]
public sealed class TableModelTests
{
    /// <summary>
    /// Verifies that TableModel and TableRow constructors reject null collections.
    /// </summary>
    /// <remarks>
    /// Guards the null-argument API contract for the immutable table model types;
    /// callers must supply non-null column and row collections.
    /// </remarks>
    [TestMethod]
    public void Constructors_RejectNullCollections()
    {
        // Arrange / Act / Assert
        Assert.Throws<ArgumentNullException>(() => new TableModel("T", null!, Array.Empty<TableRow>()));
        Assert.Throws<ArgumentNullException>(() => new TableModel("T", Array.Empty<string>(), null!));
        Assert.Throws<ArgumentNullException>(() => new TableRow(null!));
    }
}
