using System;
using System.Collections.Generic;

namespace DecisionDiagramSharp;

/// <summary>
/// Maps variable names to stable <see cref="VariableId"/> values.
/// </summary>
public sealed class VariableTable
{
    private readonly Dictionary<string, VariableId> _nameToId = new Dictionary<string, VariableId>(StringComparer.Ordinal);
    private readonly List<string> _idToName = new List<string>();

    /// <summary>
    /// Gets the number of registered variables.
    /// </summary>
    public int Count => _idToName.Count;

    /// <summary>
    /// Returns an existing identifier for <paramref name="name"/>, or registers a new one.
    /// </summary>
    /// <param name="name">Variable display name.</param>
    /// <returns>A stable variable identifier.</returns>
    public VariableId GetOrAdd(string name)
    {
        if (name == null)
        {
            throw new ArgumentNullException(nameof(name));
        }

        if (name.Length == 0)
        {
            throw new ArgumentException("Variable name must not be empty.", nameof(name));
        }

        VariableId existing;
        if (_nameToId.TryGetValue(name, out existing))
        {
            return existing;
        }

        var id = new VariableId(_idToName.Count);
        _nameToId.Add(name, id);
        _idToName.Add(name);
        return id;
    }

    /// <summary>
    /// Gets the display name for an identifier.
    /// </summary>
    /// <param name="id">Variable identifier.</param>
    /// <returns>Registered variable name.</returns>
    public string GetName(VariableId id)
    {
        if (id.Value < 0 || id.Value >= _idToName.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "VariableId is not registered in this VariableTable.");
        }

        return _idToName[id.Value];
    }
}
