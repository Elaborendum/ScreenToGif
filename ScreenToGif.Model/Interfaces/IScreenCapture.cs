using ScreenToGif.Domain.Models.Project.Recording;
using System.Windows.Input;

namespace ScreenToGif.Domain.Interfaces;

public interface IScreenCapture : ICapture
{
    int Left { get; set; }
    int Top { get; set; }
    string DeviceName { get; set; }

    void Start(int delay, int left, int top, int width, int height, double dpi, RecordingProject project);

    int CaptureWithCursor(RecordingFrame frame);
    int ManualCapture(RecordingFrame frame, bool showCursor = false);
    Task<int> CaptureWithCursorAsync(RecordingFrame frame);
    Task<int> ManualCaptureAsync(RecordingFrame frame, bool showCursor = false);

    void RegisterCursorEvent(int x, int y, MouseButtonState left, MouseButtonState right, MouseButtonState middle, MouseButtonState firstExtra, MouseButtonState secondExtra, short mouseDelta = 0);
    void RegisterCursorDataEvent(int type, byte[] pixels, int width, int height, int xPosition, int yPosition, int xHotspot, int yHotspot);
    void RegisterKeyEvent(Key key, ModifierKeys modifiers, bool isUppercase, bool wasInjected);
}