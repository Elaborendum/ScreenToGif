using ScreenToGif.Domain.Models.Project.Recording;

namespace ScreenToGif.Domain.Interfaces;

public interface ICapture : IAsyncDisposable, IDisposable
{
    RecordingProject Project { get; set; }
    bool WasFrameCaptureStarted { get; set; }
    int FrameCount { get; set; }
    int MinimumDelay { get; set; }
    int Width { get; set; }
    int Height { get; set; }
    
    Action<Exception> OnError {get;set;}

    void Start(int delay, int width, int height, double dpi, RecordingProject project);
    void ResetConfiguration();
    void StartStopwatch(bool useFixed, int interval);
    void StopStopwatch();

    int Capture(RecordingFrame frame);
    int ManualCapture(RecordingFrame frame);
    Task<int> CaptureAsync(RecordingFrame frame);
    Task<int> ManualCaptureAsync(RecordingFrame frame);
    void Save(RecordingFrame info);
    Task Stop();
}