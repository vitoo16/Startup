#nullable enable
using System;

namespace StartupLife.Core
{
    /// <summary>
    /// Signals that structurally valid persisted state references content that the active catalog cannot resolve.
    /// This must not be treated as save corruption or trigger backup rollback.
    /// </summary>
    public sealed class ContentCompatibilityException : ArgumentException
    {
        public string ReasonKey { get; }
        public ContentCompatibilityException(string reasonKey, string message) : base(message) { ReasonKey = reasonKey; }
    }
}
