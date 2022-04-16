using System;
using System.Threading.Tasks;
using ScreenToGif.Model;

namespace ScreenToGif.Capture;

public interface ICaptureOld : IAsyncDisposable, IDisposable
{
    bool WasFrameCaptureStarted { get; set; }
    int FrameCount { get; set; }
    int MinimumDelay { get; set; }
    int Width { get; set; }
    int Height { get; set; }

    ProjectInfo Project { get; set; } //TODO: Change later.

    Action<Exception> OnError {get;set;}

    void Start(int delay, int width, int height, double dpi, ProjectInfo project);
    void ResetConfiguration();
    void StartStopwatch(bool useFixed, int interval);
    void StopStopwatch();

    int Capture(FrameInfo frame);
    Task<int> CaptureAsync(FrameInfo frame);
    int ManualCapture(FrameInfo frame);
    Task<int> ManualCaptureAsync(FrameInfo frame);
    void Save(FrameInfo info);
    Task Stop();
}