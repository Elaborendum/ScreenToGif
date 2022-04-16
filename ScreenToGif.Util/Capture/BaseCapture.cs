using ScreenToGif.Domain.Interfaces;
using ScreenToGif.Domain.Models.Project.Recording;
using ScreenToGif.Util.Project;
using System.Collections.Concurrent;
using System.Windows;

namespace ScreenToGif.Util.Capture;

public abstract class BaseCapture : ICapture
{
    private Task _frameConsumerTask;
    protected readonly CaptureStopwatch Stopwatch = new();

    #region Properties

    /// <summary>
    /// Blocking collection that holds temporarily and consumes the frames that are captured.
    /// </summary>
    protected BlockingCollection<RecordingFrame> FrameCollection { get; private set; } = new();

    /// <summary>
    /// The project that holds details of the current recording.
    /// </summary>
    public RecordingProject Project { get; set; }

    /// <summary>
    /// True if the recording has started.
    /// </summary>
    public bool WasFrameCaptureStarted { get; set; }

    /// <summary>
    /// True if the frame consumer is still accepting data.
    /// No frame should accepted if the consumer is no longer working.
    /// </summary>
    public bool IsAcceptingFrames { get; set; }

    /// <summary>
    /// The total number of frames already captured.
    /// </summary>
    public int FrameCount { get; set; }

    /// <summary>
    /// The minimum capture delay chosen by the user.
    /// </summary>
    public int MinimumDelay { get; set; }

    /// <summary>
    /// The current width of the capture. It can fluctuate, based on the DPI of the current screen.
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// The current height of the capture. It can fluctuate, based on the DPI of the current screen.
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// The starting width of the capture. 
    /// </summary>
    public int StartWidth { get; set; }

    /// <summary>
    /// The starting height of the capture.
    /// </summary>
    public int StartHeight { get; set; }

    /// <summary>
    /// The starting scale of the recording.
    /// </summary>
    public double StartScale { get; set; }

    /// <summary>
    /// The current scale of the recording.
    /// </summary>
    public double Scale { get; set; }

    /// <summary>
    /// The difference in scale from the start frame to the current frame.
    /// </summary>
    public double ScaleDiff => StartScale / Scale;

    /// <summary>
    /// Action that is called when the capture fails.
    /// </summary>
    public Action<Exception> OnError { get; set; }

    #endregion

    ~BaseCapture()
    {
        Dispose();
    }

    public virtual void Start(int delay, int width, int height, double scale, RecordingProject project)
    {
        if (WasFrameCaptureStarted)
            throw new Exception("The capture was already started. Stop before trying again.");

        FrameCount = 0;
        MinimumDelay = delay;
        StartWidth = Width = width;
        StartHeight = Height = height;
        StartScale = scale;
        Scale = scale;

        Project = project;
        Project.Width = width;
        Project.Height = height;
        Project.Dpi = 96 * scale;
        Project.SaveProperties();

        ConfigureConsumer();

        WasFrameCaptureStarted = true;
        IsAcceptingFrames = true;
    }

    private void ConfigureConsumer()
    {
        FrameCollection ??= new BlockingCollection<RecordingFrame>();

        //Spin up a Task to consume the frames.
        _frameConsumerTask = Task.Factory.StartNew(() =>
        {
            try
            {
                while (true)
                    Save(FrameCollection.Take());
            }
            catch (InvalidOperationException)
            {
                //It means that Take() was called on a completed collection.
            }
            catch (Exception e)
            {
                //Uh-oh, fail hard when a frame fails to be saved.
                //This can occur for inumerous reasons, one being lack of disk space.
                Application.Current.Dispatcher.Invoke(() => OnError?.Invoke(e));
            }
        });
    }

    public void StartStopwatch(bool useFixed, int interval)
    {
        Stopwatch.Start(useFixed, interval);
    }

    public void StopStopwatch()
    {
        Stopwatch.Stop();
    }

    public virtual void ResetConfiguration()
    { }

    public abstract void Save(RecordingFrame frame);

    public abstract int Capture(RecordingFrame frame);

    public async Task<int> CaptureAsync(RecordingFrame frame)
    {
        return await Task.Factory.StartNew(() => Capture(frame));
    }

    public virtual int ManualCapture(RecordingFrame frame)
    {
        return Capture(frame);
    }

    public virtual Task<int> ManualCaptureAsync(RecordingFrame frame)
    {
        return CaptureAsync(frame);
    }

    public virtual async Task Stop()
    {
        if (!WasFrameCaptureStarted)
            return;

        IsAcceptingFrames = false;

        //Stop the consumer thread.
        FrameCollection.CompleteAdding();

        await _frameConsumerTask;

        WasFrameCaptureStarted = false;
    }

    internal virtual async Task DisposeInternal()
    {
        if (WasFrameCaptureStarted)
            await Stop();

        _frameConsumerTask?.Dispose();
        _frameConsumerTask = null;

        FrameCollection?.Dispose();
        FrameCollection = null;
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeInternal();
        GC.SuppressFinalize(this);
    }

    public void Dispose()
    {
        DisposeInternal().Wait();
        GC.SuppressFinalize(this);
    }
}