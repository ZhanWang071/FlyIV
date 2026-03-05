using System;

namespace Genies.Utilities
{
    /// <summary>
    /// Lightweight identifier for a trackable process.
    /// Created via <see cref="ProcessSpan.CreateId"/> and stored as a static field.
    /// Zero allocation when used (just an int).
    /// </summary>
    public readonly struct ProcessId : IEquatable<ProcessId>
    {
        public readonly int Id;

        /// <summary>
        /// Returns true if this ProcessId has been properly initialized.
        /// </summary>
        public bool IsValid => Id > 0;

        internal ProcessId(int id)
        {
            Id = id;
        }

        /// <summary>
        /// Get the registered name for this process ID.
        /// </summary>
        public string GetName() => ProcessSpanStorage.GetName(Id);

        /// <summary>
        /// Get the registered name for this process ID.
        /// </summary>
        public override string ToString() => GetName();

        public bool Equals(ProcessId other) => Id == other.Id;

        public override bool Equals(object obj) => obj is ProcessId other && Equals(other);

        public override int GetHashCode() => Id;

        public static bool operator ==(ProcessId left, ProcessId right) => left.Equals(right);

        public static bool operator !=(ProcessId left, ProcessId right) => !left.Equals(right);
    }
}
