using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using WonderLab.Classes.Interfaces;

namespace WonderLab.Services;

/// <summary>
/// 调度任务管理类
/// </summary>
public partial class TaskService(IBackgroundTaskQueue queue) : ObservableObject
{
    private int _currentRunningJobs;

    private readonly IBackgroundTaskQueue _taskQueue = queue;

    [ObservableProperty]
    public ObservableCollection<ITaskJob> taskJobs = [];

    public void QueueJob(ITaskJob job)
    {
        if (job is null)
        {
            return;
        }

        Task.Run(async () =>
        {
            await _taskQueue.QueueBackgroundWorkItemAsync(job);
            job.TaskFinished += (_, args) =>
            {
                using (job)
                {
                    Interlocked.Decrement(ref _currentRunningJobs);
                    TaskJobs.Remove(job);
                }
            };

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                TaskJobs.Add(job);
            });

            Interlocked.Increment(ref _currentRunningJobs);
        });
    }

}

public class BackgroundTaskQueue : IBackgroundTaskQueue
{
    private readonly Channel<ITaskJob> _queue;

    public BackgroundTaskQueue(int queueLength)
    {
        BoundedChannelOptions boundedChannelOptions = new(queueLength)
        {
            FullMode = BoundedChannelFullMode.Wait
        };

        _queue = Channel.CreateBounded<ITaskJob>(boundedChannelOptions);
    }

    public async ValueTask QueueBackgroundWorkItemAsync(ITaskJob? job)
    {
        if (job == null)
        {
            throw new ArgumentNullException(nameof(job));
        }

        await _queue.Writer.WriteAsync(job);
    }

    public async ValueTask<ITaskJob> DequeueAsync(CancellationToken cancellationToken)
    {
        return await _queue.Reader.ReadAsync(cancellationToken);
    }
}