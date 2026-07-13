using ProductManagerApp.Infrastructure.Commands;

namespace ProductManagerApp.Tests.Infrastructure.Commands;

/// <summary>
/// 验证异步命令执行期间的状态通知和防重复执行行为。
/// </summary>
public class AsyncRelayCommandTests
{
    [Fact]
    public async Task Execute_WhileRunning_NotifiesStateAndPreventsDuplicateExecution()
    {
        var started = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var finished = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var executionCount = 0;

        var command = new AsyncRelayCommand(async _ =>
        {
            Interlocked.Increment(ref executionCount);
            started.TrySetResult(true);
            await release.Task;
        });

        var observedStates = new List<bool>();
        command.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != nameof(AsyncRelayCommand.IsExecuting))
            {
                return;
            }

            observedStates.Add(command.IsExecuting);
            if (!command.IsExecuting)
            {
                finished.TrySetResult(true);
            }
        };

        command.Execute(null);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(1));

        Assert.True(command.IsExecuting);
        Assert.False(command.CanExecute(null));

        command.Execute(null);
        Assert.Equal(1, Volatile.Read(ref executionCount));

        release.TrySetResult(true);
        await finished.Task.WaitAsync(TimeSpan.FromSeconds(1));

        Assert.False(command.IsExecuting);
        Assert.True(command.CanExecute(null));
        Assert.Equal(new[] { true, false }, observedStates);
    }
}
