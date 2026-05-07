using System;

namespace DecisionDiagramSharp;

/// <summary>
/// Base exception type for decision diagram failures.
/// </summary>
public class DiagramException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DiagramException"/> class.
    /// </summary>
    public DiagramException(string message)
        : base(message)
    {
    }
}

/// <summary>
/// Thrown when diagram handles from different manager instances are combined.
/// </summary>
public sealed class DiagramManagerMismatchException : DiagramException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DiagramManagerMismatchException"/> class.
    /// </summary>
    public DiagramManagerMismatchException(string message)
        : base(message)
    {
    }
}

/// <summary>
/// Thrown when a configured diagram size limit is exceeded.
/// </summary>
public sealed class DiagramSizeLimitExceededException : DiagramException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DiagramSizeLimitExceededException"/> class.
    /// </summary>
    public DiagramSizeLimitExceededException(string message)
        : base(message)
    {
    }
}

/// <summary>
/// Thrown when enumeration limits are exceeded.
/// </summary>
public sealed class DiagramEnumerationLimitExceededException : DiagramException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DiagramEnumerationLimitExceededException"/> class.
    /// </summary>
    public DiagramEnumerationLimitExceededException(string message)
        : base(message)
    {
    }
}

/// <summary>
/// Thrown when variable ordering invariants are violated.
/// </summary>
public sealed class InvalidVariableOrderingException : DiagramException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidVariableOrderingException"/> class.
    /// </summary>
    public InvalidVariableOrderingException(string message)
        : base(message)
    {
    }
}
