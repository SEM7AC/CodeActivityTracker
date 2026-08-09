using System.Windows.Threading;
using System.Runtime.InteropServices;

namespace CodeActivityTracker.Services;

public class TimerService
    {
    private readonly DispatcherTimer _timer;

    public event Action? Tick;

    public TimerService(int intervalMs = 1000)
        {
        _timer = new DispatcherTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(intervalMs);
        _timer.Tick += Timer_Tick;
        }

    public void Start()
        {
        _timer.Start();
        }
    public void Stop()
        {
        _timer.Stop();
        }

    private void Timer_Tick(object? sender, EventArgs e)
        {
        Tick?.Invoke();
        }


    }

