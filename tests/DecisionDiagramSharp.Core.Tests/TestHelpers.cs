using System.Collections.Generic;
using System.Reflection;

namespace DecisionDiagramSharp.Core.Tests;

internal static class TestHelpers
{
    public static object GetPrivateField(object target, string fieldName)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!;
        return field.GetValue(target)!;
    }

    public static Dictionary<VariableId, bool> BuildBoolAssignment(IReadOnlyList<VariableId> variables, int mask)
    {
        var assignment = new Dictionary<VariableId, bool>();
        for (var i = 0; i < variables.Count; i++)
        {
            assignment[variables[i]] = (mask & (1 << i)) != 0;
        }

        return assignment;
    }
}
