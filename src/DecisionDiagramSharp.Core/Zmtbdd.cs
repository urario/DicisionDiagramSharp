using System;

namespace DecisionDiagramSharp;

/// <summary>
/// Typed handle to a ZMTBDD value owned by a specific <see cref="ZmtbddManager"/>.
/// </summary>
public readonly struct Zmtbdd : IEquatable<Zmtbdd>
{
    internal Zmtbdd(ZmtbddManager manager, int nodeId)
    {
        Manager = manager;
        NodeId = nodeId;
    }

    /// <summary>
    /// Gets the manager that owns this ZMTBDD value.
    /// </summary>
    public ZmtbddManager Manager { get; }

    internal int NodeId { get; }

    /// <summary>
    /// Gets a value indicating whether this handle points to the zero terminal.
    /// </summary>
    public bool IsZero => NodeId == ZmtbddManager.ZeroNodeId;

    /// <summary>
    /// Gets a value indicating whether this handle points to any numeric terminal.
    /// </summary>
    public bool IsTerminal => ZmtbddManager.IsTerminalId(NodeId);

    /// <inheritdoc />
    public bool Equals(Zmtbdd other)
    {
        return ReferenceEquals(Manager, other.Manager) && NodeId == other.NodeId;
    }

    /// <inheritdoc />
    public override bool Equals(object obj)
    {
        return obj is Zmtbdd other && Equals(other);
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
        return "Zmtbdd(" + NodeId + ")";
    }

    /// <summary>
    /// Compares two ZMTBDD handles for equality.
    /// </summary>
    public static bool operator ==(Zmtbdd left, Zmtbdd right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Compares two ZMTBDD handles for inequality.
    /// </summary>
    public static bool operator !=(Zmtbdd left, Zmtbdd right)
    {
        return !left.Equals(right);
    }
}
