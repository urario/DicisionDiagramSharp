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

    internal BddManager Manager { get; }

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
}
