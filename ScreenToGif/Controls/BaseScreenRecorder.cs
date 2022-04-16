using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ScreenToGif.Domain.Enums;
using ScreenToGif.Domain.Interfaces;
using ScreenToGif.Domain.Models.Project.Recording;
using ScreenToGif.Native.Helpers;
using ScreenToGif.Util;
using ScreenToGif.Util.Capture;
using ScreenToGif.Util.Settings;

namespace ScreenToGif.Controls;

public class BaseScreenRecorder : BaseRecorder, IDisposable
{
    #region Variables

    /// <summary>
    /// The token in use to control the execution of the capture.
    /// </summary>
    private CancellationTokenSource _captureToken;

    /// <summary>
    /// Deals with all screen capture methods.
    /// </summary>
    internal IScreenCapture Capture;

    /// <summary>
    /// Timer responsible for the forced clean up of the objects in memory.
    /// </summary>
    internal readonly System.Timers.Timer GarbageTimer = new();

    #endregion
    
    public BaseScreenRecorder()
    {
        GarbageTimer.Interval = 3000;
        GarbageTimer.Elapsed += GarbageTimer_Tick;
    }

    private void GarbageTimer_Tick(object sender, EventArgs e)
    {
        GC.Collect(2);
    }
    
    internal bool HasFixedDelay()
    {
        return UserSettings.All.CaptureFrequency != CaptureFrequencies.PerSecond || UserSettings.All.FixedFrameRate;
    }

    internal int GetFixedDelay()
    {
        return UserSettings.All.CaptureFrequency switch
        {
            CaptureFrequencies.Manual => UserSettings.All.PlaybackDelayManual,
            CaptureFrequencies.Interaction => UserSettings.All.PlaybackDelayInteraction,
            CaptureFrequencies.PerMinute => UserSettings.All.PlaybackDelayMinute,
            CaptureFrequencies.PerHour => UserSettings.All.PlaybackDelayHour,

            //When the capture is 'PerSecond', the fixed delay is set to use the current framerate.
            _ => 1000 / UserSettings.All.LatestFps 
        };
    }

    internal int GetCaptureInterval()
    {
        return UserSettings.All.CaptureFrequency switch
        {
            //15 frames per hour = 240,000 ms (240 sec, 4 min).
            CaptureFrequencies.PerHour => (1000 * 60 * 60) / UserSettings.All.LatestFps,

            //15 frames per minute = 4,000 ms (4 sec).
            CaptureFrequencies.PerMinute => (1000 * 60) / UserSettings.All.LatestFps,

            //15 frames per second = 66.66 ms
            _ => 1000 / UserSettings.All.LatestFps
        };
    }

    internal IScreenCapture GetDirectCapture()
    {
        return UserSettings.All.OnlyCaptureChanges ? new DirectChangedCapture() : new DirectCapture();
    }
    
    internal virtual void StartCapture()
    {
        Capture.StartStopwatch(HasFixedDelay(), GetFixedDelay());
        HasImpreciseCapture = false;

        if (UserSettings.All.ForceGarbageCollection)
            GarbageTimer.Start();

        lock (UserSettings.Lock)
        {
            //Starts the capture.
            _captureToken = new CancellationTokenSource();

            Task.Run(() => PrepareCaptureLoop(GetCaptureInterval()), _captureToken.Token);
        }
    }

    internal virtual void PauseCapture()
    {
        Capture.StopStopwatch();

        StopInternalCapture();
    }

    internal virtual async Task StopCapture()
    {
        Capture.StopStopwatch();

        StopInternalCapture();

        if (Capture != null)
            await Capture.Stop();

        GarbageTimer.Stop();
    }

    private void StopInternalCapture()
    {
        if (_captureToken == null)
            return;

        _captureToken.Cancel();
        _captureToken.Dispose();
        _captureToken = null;
    }

    private void PrepareCaptureLoop(int interval)
    {
        using (var resolution = new TimerResolution(1))
        {
            if (!resolution.SuccessfullySetTargetResolution)
            {
                LogWriter.Log($"Imprecise timer resolution... Target: {resolution.TargetResolution}, Current: {resolution.CurrentResolution}");
                Dispatcher.Invoke(() => HasImpreciseCapture = true);
            }

            if (UserSettings.All.ShowCursor)
                CaptureWithCursor(interval);
            else
                CaptureWithoutCursor(interval);

            Dispatcher.Invoke(() => HasImpreciseCapture = false);
        }
    }

    private void CaptureWithCursor(int interval)
    {
        var sw = new Stopwatch();

        while (_captureToken != null && !_captureToken.IsCancellationRequested)
        {
            sw.Restart();

            //Capture frame.
            var frame = new RecordingFrame();

            var frameCount = Capture.CaptureWithCursor(frame);
            Dispatcher.Invoke(() => FrameCount = frameCount);

            //If behind wait time, wait before capturing new frame.
            if (sw.ElapsedMilliseconds >= interval)
                continue;

            while (sw.Elapsed.TotalMilliseconds < interval)
                Thread.Sleep(1);

            //SpinWait.SpinUntil(() => sw.ElapsedMilliseconds >= interval);
        }

        sw.Stop();
    }

    private void CaptureWithoutCursor(int interval)
    {
        var sw = new Stopwatch();

        while (_captureToken != null && !_captureToken.IsCancellationRequested)
        {
            sw.Restart();

            //Capture frame.
            var frame = new RecordingFrame();
            
            var frameCount = Capture.Capture(frame);
            Dispatcher.Invoke(() => FrameCount = frameCount);

            //If behind wait time, wait before capturing new frame.
            if (sw.ElapsedMilliseconds >= interval)
                continue;

            while (sw.Elapsed.TotalMilliseconds < interval)
                Thread.Sleep(1);

            //SpinWait.SpinUntil(() => sw.ElapsedMilliseconds >= interval);
        }

        sw.Stop();
    }

    public void Dispose()
    {
        StopInternalCapture();

        GarbageTimer?.Dispose();
    }
}