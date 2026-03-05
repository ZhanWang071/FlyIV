namespace Genies.Utilities
{
    /// <summary>
    /// A marker interface for objects that can subscribe to process tracking events.
    /// Implement one or more of the derived interfaces (e.g., IProcessStartedSubscriber)
    /// to receive specific callbacks.
    ///
    /// There are two subscription modes:
    /// 1. Direct Subscription: The subscriber receives events only for the specific process it subscribed to.
    /// 2. Subscribe-to-All: When subscribing to a "trigger" process with this mode, the subscriber
    ///    will receive events for ALL processes (including children and unrelated processes) as long
    ///    as the trigger process is active.
    /// </summary>
    public interface IProcessSpanSubscriber
    {
    }

    /// <summary>
    /// Interface for subscribers that handle process start events.
    /// </summary>
    public interface IProcessStartedSubscriber : IProcessSpanSubscriber
    {
        /// <summary>
        /// Called when a subscribed process starts.
        /// This is invoked synchronously on the thread that created the ProcessSpan.
        /// </summary>
        /// <param name="processId">The process that started.</param>
        void ProcessStarted(in ProcessId processId);
    }

    /// <summary>
    /// Interface for subscribers that handle process end events.
    /// </summary>
    public interface IProcessEndedSubscriber : IProcessSpanSubscriber
    {
        /// <summary>
        /// Called when a subscribed process ends.
        /// This is invoked synchronously on the thread that disposed the ProcessSpan.
        /// </summary>
        /// <param name="processId">The process that ended.</param>
        /// <param name="elapsedMs">The elapsed time in milliseconds.</param>
        void ProcessEnded(in ProcessId processId, double elapsedMs);
    }
}
