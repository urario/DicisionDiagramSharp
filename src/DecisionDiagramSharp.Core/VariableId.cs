using System;

namespace DecisionDiagramSharp;

/// <summary>
/// Identifies a decision diagram variable.
/// </summary>
public readonly struct VariableId : IEquatable<VariableId>, IComparable<VariableId>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VariableId"/> struct.
    /// </summary>
    /// <param name="value">Underlying numeric identifier.</param>
    public VariableId(int value)
    {
        Value = value;
    }

    /// <summary>
    /// Gets the underlying numeric identifier.
    /// </summary>
    public int Value { get; }

    /// <inheritdoc />
    public bool Equals(VariableId other)
    {
        return Value == other.Value;
    }

    /// <inheritdoc />
    public override bool Equals(object obj)
    {
        return obj is VariableId other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return Value;
    }

    /// <inheritdoc />
    public int CompareTo(VariableId other)
    {
        return Value.CompareTo(other.Value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString();
    }

    /// <summary>
    /// Compares two variable identifiers for equality.
    /// </summary>
    public static bool operator ==(VariableId left, VariableId right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Compares two variable identifiers for inequality.
    /// </summary>
    public static bool operator !=(VariableId left, VariableId right)
    {
        return !left.Equals(right);
    }
}
