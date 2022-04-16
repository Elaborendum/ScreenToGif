using System.Diagnostics;
using ScreenToGif.Util.Settings;

namespace ScreenToGif.Util;

/// <summary>
/// Frame rate monitor. 
/// </summary>
public class CaptureStopwatch
{
    #region Private Variables

    private Stopwatch _stopwatch = new();
    private int _interval = 15;
    private bool _started = true;
    private bool _fixedRate;

    #endregion

    /// <summary>
    /// Prepares the FrameRate monitor.
    /// </summary>
    /// <param name="interval">The selected interval of each snapshot.</param>
    public void Start(int interval)
    {
        _stopwatch = new Stopwatch();

        _interval = interval;
        _fixedRate = UserSettings.All.FixedFrameRate;
    }

    /// <summary>
    /// Prapares the framerate monitor
    /// </summary>
    /// <param name="useFixed">If true, uses the fixed internal provided.</param>
    /// <param name="interval">The fixed interval to be used.</param>
    public void Start(bool useFixed, int interval)
    {
        _stopwatch = new Stopwatch();

        _interval = interval;
        _fixedRate = useFixed;
    }

    /// <summary>
    /// Gets the diff between the last call.
    /// </summary>
    /// <returns>The amount of seconds.</returns>
    public int GetMilliseconds()
    {
        if (_fixedRate)
            return _interval;

        if (_started)
        {
            _started = false;
            _stopwatch.Start();
            return _interval;
        }

        var mili = (int)_stopwatch.ElapsedMilliseconds;
        _stopwatch.Restart();

        return mili;
    }

    /// <summary>
    /// Gets the diff between the last call.
    /// </summary>
    /// <returns>The amount of seconds.</returns>
    public long GetMillisecondsAsLong()
    {
        if (_fixedRate)
            return _interval;

        if (_started)
        {
            _started = false;
            _stopwatch.Start();
            return _interval;
        }

        var mili = _stopwatch.ElapsedMilliseconds;
        _stopwatch.Restart();

        return mili;
    }

    public long GetElapsedTicks() => _stopwatch?.ElapsedTicks ?? -1L;

    /// <summary>
    /// Determine that a stop/pause of the recording.
    /// </summary>
    public void Stop()
    {
        _stopwatch.Stop();
        _started = true;
    }
}