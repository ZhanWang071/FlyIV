using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Genies.Naf
{
    /// <summary>
    /// Provides a serialized execution policy for asynchronous operations, ensuring that only one operation runs at a
    /// time. When a new operation is requested, any currently running operation is completed and awaited before the new
    /// one begins. This guarantees serialized execution and prevents overlapping work, while still allowing external
    /// cancellation through a provided CancellationToken.
    /// <br/><br/>
    /// When the LatestWins property is enabled, if a new operation is requested while another one is still running,
    /// the currently running operation will be cancelled and awaited for completion before starting the new one.
    /// <br/><br/>
    /// IMPORTANT: this class uses UniTask and it is designed to be used within the Unity main thread context, where
    /// awaiters will execute in FIFO order. DO NOT use this class in a multi-threaded context, as it is not thread-safe.
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal sealed class SerializedAsyncExecutor
#else
    public sealed class SerializedAsyncExecutor
#endif
    {
        public readonly bool LatestWins;

        private UniTaskCompletionSource _operation;
        private CancellationTokenSource _cancellation;

        public SerializedAsyncExecutor(bool latestWins = false)
        {
            LatestWins = latestWins;
        }

        /// <summary>
        /// Executes the provided asynchronous operation, ensuring a serialized FIFO execution if multiple operations
        /// are triggered in quick succession. When LatestWins is enabled, only the latest one will be executed to
        /// completion, while any previous ones will be cancelled and awaited for completion before starting the new one.
        /// </summary>
        public UniTask ExecuteAsync(Func<UniTask> asyncOperation, CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(_ => asyncOperation(), cancellationToken);
        }

        /// <summary>
        /// Executes the provided asynchronous operation, ensuring a serialized FIFO execution if multiple operations
        /// are triggered in quick succession. When LatestWins is enabled, only the latest one will be executed to
        /// completion, while any previous ones will be cancelled and awaited for completion before starting the new one.
        /// </summary>
        public async UniTask ExecuteAsync(Func<CancellationToken, UniTask> asyncOperation, CancellationToken cancellationToken = default)
        {
            // if for some reason the external cancellation token is already cancelled, we can skip the whole process
            cancellationToken.ThrowIfCancellationRequested();

            // generate a unique cancellation token for this operation, linked with the external one
            cancellationToken = GenerateCancellationToken(
                cancellationToken,
                out CancellationTokenSource cancellation,
                out CancellationTokenSource linkedCancellation
            );

            // intentionally propagate exceptions to the caller, and ensure proper cleanup of the operation/cancellation sources
            try
            {
                // wait until the previous operations are completed, if any
                while (_operation is not null)
                {
                    /**
                     * By design, we are not propagating any exceptions through the _operation task. Any exceptions will
                     * be propagated only to the original caller. Also keep in mind that within the Unity's main thread,
                     * awaiters here will execute in FIFO order, which is what we need. This class is not designed to
                     * work in a multi-threaded context.
                     */

                    // if the operation is cancelled while waiting, we will exit immediately and propagate the cancellation to the caller
                    await _operation.Task.AttachExternalCancellation(cancellationToken);
                }

                // execute the operation
                try
                {
                    _operation = new UniTaskCompletionSource();
                    await asyncOperation(cancellationToken);
                }
                finally
                {
                    UniTaskCompletionSource operation = _operation;
                    _operation = null;
                    operation?.TrySetResult();
                }
            }
            finally
            {
                if (_cancellation == cancellation)
                {
                    _cancellation = null;
                }

                // these will be null if LatestWins is disabled
                linkedCancellation?.Dispose();
                cancellation?.Dispose();
            }
        }

        // generates the cancellation token for a new operation
        private CancellationToken GenerateCancellationToken(
            CancellationToken           externalToken,
            out CancellationTokenSource cancellation,
            out CancellationTokenSource linkedCancellation
        ) {
            // if latest-wins policy is disabled, we don't need a new generation token, just use the external one
            if (!LatestWins)
            {
                cancellation = null;
                linkedCancellation = null;
                return externalToken;
            }

            // cancel the previous operation, if any
            _cancellation?.Cancel();

            // obtain a new cancellation token, unique to this operation, linked with the external one
            _cancellation = cancellation = new CancellationTokenSource();
            linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellation.Token, externalToken);
            return linkedCancellation.Token;
        }
    }
}
