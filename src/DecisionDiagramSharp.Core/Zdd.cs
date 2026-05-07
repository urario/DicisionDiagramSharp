using System;

namespace DecisionDiagramSharp;

/// <summary>
/// Typed handle to a ZDD value owned by a specific <see cref="ZddManager"/>.
/// </summary>
public readonly struct Zdd : IEquatable<Zdd>
{
    internal Zdd(ZddManager manager, int nodeId)
    {
        Manager = manager;
        NodeId = nodeId;
    }

    internal ZddManager Manager { get; }

    internal int NodeId { get; }

    /// <summary>
    /// Gets a value indicating whether this handle points to the Empty terminal.
    /// </summary>
    public bool IsEmpty => NodeId == ZddManager.EmptyNodeId;

    /// <summary>
    /// Gets a value indicating whether this handle points to the Base terminal.
    /// </summary>
    public bool IsBase => NodeId == ZddManager.BaseNodeId;

    /// <inheritdoc />
    public bool Equals(Zdd other)
    {
        return ReferenceEquals(Manager, other.Manager) && NodeId == other.NodeId;
    }

    /// <inheritdoc />
    public override bool Equals(object obj)
    {
        return obj is Zdd other && Equals(other);
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
        return "Zdd(" + NodeId + ")";
    }

    /// <summary>
    /// Compares two ZDD handles for equality.
    /// </summary>
    public static bool operator ==(Zdd left, Zdd right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Compares two ZDD handles for inequality.
    /// </summary>
    public static bool operator !=(Zdd left, Zdd right)
    {
        return !left.Equals(right);
    }
}
