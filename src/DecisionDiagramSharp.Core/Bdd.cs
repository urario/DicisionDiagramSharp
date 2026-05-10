using System;

namespace DecisionDiagramSharp;

/// <summary>
/// Typed handle to a BDD value owned by a specific <see cref="BddManager"/>.
/// </summary>
public readonly struct Bdd : IEquatable<Bdd>
{
    internal Bdd(BddManager manager, int nodeId)
    {
        Manager = manager;
        NodeId = nodeId;
    }

    /// <summary>
    /// Gets the manager that owns this BDD value.
    /// </summary>
    public BddManager Manager { get; }

    internal int NodeId { get; }

    /// <summary>
    /// Gets a value indicating whether this handle points to the False terminal.
    /// </summary>
    public bool IsFalse => NodeId == BddManager.FalseNodeId;

    /// <summary>
    /// Gets a value indicating whether this handle points to the True terminal.
    /// </summary>
    public bool IsTrue => NodeId == BddManager.TrueNodeId;

    /// <inheritdoc />
    public bool Equals(Bdd other)
    {
        return ReferenceEquals(Manager, other.Manager) && NodeId == other.NodeId;
    }

    /// <inheritdoc />
    public override bool Equals(object obj)
    {
        return obj is Bdd other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            var managerHash = Manager == null ? 0 : Manager.GetHashCode();
            return (managerHash * 397) ^ NodeId;
        }
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return "Bdd(" + NodeId + ")";
    }

    /// <summary>
    /// Compares two BDD handles for equality.
    /// </summary>
    public static bool operator ==(Bdd left, Bdd right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Compares two BDD handles for inequality.
    /// </summary>
    public static bool operator !=(Bdd left, Bdd right)
    {
        return !left.Equals(right);
    }

    /// <summary>
    /// Returns the logical negation of a BDD value.
    /// </summary>
    public static Bdd operator !(Bdd value)
    {
        return GetManager(value).Not(value);
    }

    /// <summary>
    /// Returns the conjunction of two BDD values.
    /// </summary>
    public static Bdd operator &(Bdd left, Bdd right)
    {
        return GetManager(left).And(left, right);
    }

    /// <summary>
    /// Returns the disjunction of two BDD values.
    /// </summary>
    public static Bdd operator |(Bdd left, Bdd right)
    {
        return GetManager(left).Or(left, right);
    }

    /// <summary>
    /// Returns the exclusive-or of two BDD values.
    /// </summary>
    public static Bdd operator ^(Bdd left, Bdd right)
    {
        return GetManager(left).Xor(left, right);
    }

    private static BddManager GetManager(Bdd value)
    {
        if (value.Manager == null)
        {
            throw new DiagramManagerMismatchException(
                "The BDD value is not associated with a BddManager. Create BDD values through BddManager or DecisionDiagramManager.Bdd before using operators.");
        }

        return value.Manager;
    }
}
