using System;
using System.Threading;

namespace LibXGD
{
    public static class ProgressReporter
    {
        public static Action<long, long, string>? OnProgress;
        public static CancellationToken CancellationToken = CancellationToken.None;

        public static void Report(long current, long total, string status = "")
        {
            OnProgress?.Invoke(current, total, status);
        }

        public static void CheckCancelled()
        {
            if (CancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException("작업이 사용자에 의해 취소되었습니다.");
            }
        }

        public static void Reset()
        {
            OnProgress = null;
            CancellationToken = CancellationToken.None;
        }
    }
}
