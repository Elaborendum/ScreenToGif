using ScreenToGif.Model;
using System.Threading.Tasks;

namespace ScreenToGif.Capture;

public interface IScreenCaptureOld : ICaptureOld
{
    int Left { get; set; }
    int Top { get; set; }
    string DeviceName { get; set; }

    void Start(int delay, int left, int top, int width, int height, double dpi, ProjectInfo project);
    int CaptureWithCursor(FrameInfo frame);
    Task<int> CaptureWithCursorAsync(FrameInfo frame);
    int ManualCapture(FrameInfo frame, bool showCursor = false);
    Task<int> ManualCaptureAsync(FrameInfo frame, bool showCursor = false);
}