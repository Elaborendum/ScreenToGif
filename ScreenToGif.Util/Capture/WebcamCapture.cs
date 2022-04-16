using ScreenToGif.Domain.Models.Project.Recording;

namespace ScreenToGif.Util.Capture;

public class WebcamCapture : BaseCapture
{
    //WebcamCapture needs and overhaul (possibily using WinRT methods, limiting to newer versions).

    public override void Save(RecordingFrame frame) => throw new NotImplementedException();

    public override int Capture(RecordingFrame frame) => throw new NotImplementedException();
}