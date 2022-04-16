using ScreenToGif.Domain.Models.Project.Recording;
using ScreenToGif.Util.Settings;
using System.IO;
using System.IO.Compression;
using System.Windows.Ink;

namespace ScreenToGif.Util.Capture;

public sealed class SketchBoardCapture : BaseCapture
{
    #region Variables
    
    private FileStream _fileStream;
    private BufferedStream _bufferedStream;
    private DeflateStream _compressStream;

    #endregion

    public override void Start(int delay, int width, int height, double scale, RecordingProject project)
    {
        base.Start(delay, width, height, scale, project);
        
        //Frame cache on memory/disk.
        _fileStream = new FileStream(project.FramesCachePath, FileMode.Create, FileAccess.Write, FileShare.None);
        _bufferedStream = new BufferedStream(_fileStream, UserSettings.All.MemoryCacheSize * 1_048_576); //Each 1 MB has 1_048_576 bytes.
        _compressStream = new DeflateStream(_bufferedStream, UserSettings.All.CaptureCompression, true);
    }


    
    public override void Save(RecordingFrame frame) => throw new NotImplementedException();

    public override int Capture(RecordingFrame frame) => throw new NotImplementedException();


    public void RegisterStroke(StrokeCollection collection)
    {
        //Since the only data being captured are drawings, the BaseCapture may not be used.
        //

        var col = collection.Clone();
    }


    public override async Task Stop()
    {
        await base.Stop();
        
        //Finishing writing the events to the cache.
        await _compressStream.DisposeAsync();
        await _bufferedStream.DisposeAsync();
        await _fileStream.DisposeAsync();
    }
}