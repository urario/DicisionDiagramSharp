using System;

namespace DecisionDiagramSharp;

/// <summary>
/// Typed handle to an MTBDD value owned by a specific <see cref="MtbddManager"/>.
/// </summary>
public readonly struct Mtbdd : IEquatable<Mtbdd>
{
    internal Mtbdd(MtbddManager manager, int nodeId)
    {
        Manager = manager;
        NodeId = nodeId;
    }

    /// <summary>
    /// Gets the manager that owns this MTBDD value.
    /// </summary>
    public MtbddManager Manager { get; }

    internal int NodeId { get; }

    /// <summary>
    /// Gets a value indicating whether this handle points to an integer terminal.
    /// </summary>
    public bool IsTerminal => MtbddManager.IsTerminalId(NodeId);

    /// <inheritdoc />
    public bool Equals(Mtbdd other)
    {
        return ReferenceEquals(Manager, other.Manager) && NodeId == other.NodeId;
    }

    /// <inheritdoc />
    public override bool Equals(object obj)
    {
        return obj is Mtbdd other && Equals(other);
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
        return "Mtbdd(" + NodeId + ")";
    }

    /// <summary>
    /// Compares two MTBDD handles for equality.
    /// </summary>
    public static bool operator ==(Mtbdd left, Mtbdd right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Compares two MTBDD handles for inequality.
    /// </summary>
    public static bool operator !=(Mtbdd left, Mtbdd right)
    {
        return !left.Equals(right);
    }
}
