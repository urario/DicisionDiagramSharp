using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DecisionDiagramSharp.Diagnostics.Tests;

[TestClass]
public sealed class TableModelTests
{
    [TestMethod]
    public void Constructors_RejectNullCollections()
    {
        Assert.Throws<ArgumentNullException>(() => new TableModel("T", null!, Array.Empty<TableRow>()));
        Assert.Throws<ArgumentNullException>(() => new TableModel("T", Array.Empty<string>(), null!));
        Assert.Throws<ArgumentNullException>(() => new TableRow(null!));
    }
}
