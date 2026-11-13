using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

namespace TuanTool
{
    /// <summary>
    /// Tiện ích cho UniTask.
    /// </summary>
    public static class UnitaskExtension
    {
        /// <summary>
        /// Chạy nhiều task Defer để tất cả kết thúc cùng lúc.
        /// Mỗi task bắt đầu sau (thời gian lớn nhất - thời gian của nó), nên task ngắn nhất sẽ chờ lâu nhất.
        /// Tasks need Defered.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <param name="timeTaksDefered"></param>
        /// Duration in seconds and Defered Unitask (use  UniTask.Defer())
        /// <returns></returns>
        public static async UniTask TimingTasks(CancellationToken cancellationToken = default, params (float, UniTask)[] timeTaksDefered)
        {
            var maxTime = timeTaksDefered.Max(_ => _.Item1);
            List<UniTask> uniTasks = new List<UniTask>();
            foreach (var item in timeTaksDefered)
            {
                var a = UniTask.Create(async (cancellationToken) =>
                    {
                        await UniTask.WaitForSeconds(maxTime - item.Item1);
                        await item.Item2;
                    }, cancellationToken);
                uniTasks.Add(a);
            }
            await UniTask.WhenAll(uniTasks);
        }
    }
}
