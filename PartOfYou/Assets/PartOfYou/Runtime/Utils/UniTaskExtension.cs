using System.Threading;
using Cysharp.Threading.Tasks;

namespace PartOfYou.Runtime.Utils
{
    public static class UniTaskExtension
    {
        public static void DisposeUniTask(this UniTask uniTask)
        {
            var cancellationTokenSource = new CancellationTokenSource();
            uniTask.AttachExternalCancellation(cancellationTokenSource.Token);
            cancellationTokenSource.Cancel();
        }
        
        public static void DisposeUniTask<T>(this UniTask<T> uniTask)
        {
            var cancellationTokenSource = new CancellationTokenSource();
            uniTask.AttachExternalCancellation(cancellationTokenSource.Token);
            cancellationTokenSource.Cancel();
        }
    }
}