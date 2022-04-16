using ScreenToGif.Domain.Enums;
using ScreenToGif.Domain.Models.Project.Cached;
using ScreenToGif.Domain.Models.Project.Recording;
using ScreenToGif.Util.Settings;
using System.IO;
using System.Text;
using System.Windows.Media;

namespace ScreenToGif.Util.Project;

public static class RecordingProjectHelper
{
    public static RecordingProject Create(ProjectSources source)
    {
        var date = DateTime.Now;
        var path = Path.Combine(UserSettings.All.TemporaryFolderResolved, "ScreenToGif", "Recordings", date.ToString("yyyy-MM-dd HH-mm-ss"));

        var project = new RecordingProject
        {
            PropertiesCachePath = Path.Combine(path, "Properties.cache"),
            FramesCachePath = Path.Combine(path, "Frames.cache"),
            EventsCachePath = Path.Combine(path, "Events.cache"),
            CreatedBy = source,
            CreationDate = date
        };

        Directory.CreateDirectory(path);
        
        return project;
    }

    public static void SaveProperties(this RecordingProject project)
    {
        using var fileStream = new FileStream(project.PropertiesCachePath, FileMode.Create, FileAccess.Write, FileShare.None);

        fileStream.WriteBytes(Encoding.ASCII.GetBytes("stgR")); //Signature, 4 bytes.
        fileStream.WriteUInt16(1); //File version, 2 bytes.
        fileStream.WriteUInt16((ushort) project.Width); //Width, 2 bytes.
        fileStream.WriteUInt16((ushort) project.Height); //Height, 2 bytes.
        fileStream.WriteBytes(BitConverter.GetBytes(Convert.ToSingle(project.Dpi))); //DPI, 4 bytes.
        fileStream.WriteByte(project.ChannelCount); //Number of channels, 1 byte.
        fileStream.WriteByte(project.BitsPerChannel); //Bits per channels, 1 byte.
        fileStream.WritePascalString("ScreenToGif"); //App name, 1 byte + X bytes (255 max).
        fileStream.WritePascalString(UserSettings.All.VersionText); //App version, 1 byte + X bytes (255 max).
        fileStream.WriteByte((byte) project.CreatedBy); //Recording source, 1 byte.
        fileStream.WriteUInt64((ulong) project.CreationDate.Ticks); //Creation date, 8 bytes.
    }

    //Read and Parse RecordingProject from cache, when for example the project failed to be converted to CachedProject.

    public static async Task<CachedProject> ConvertToCachedProject(this RecordingProject recording)
    {
        var project = CachedProjectHelper.Create(recording.CreationDate);
        project.Width = (ushort) recording.Width;
        project.Height = (ushort) recording.Height;
        project.VerticalDpi = recording.Dpi;
        project.HorizontalDpi = recording.Dpi;
        project.ChannelCount = recording.ChannelCount;
        project.BitsPerChannel = recording.BitsPerChannel;
        project.Version = UserSettings.All.Version;
        project.CreatedBy = recording.CreatedBy;

        //Properties.
        await using var writeStream = new FileStream(project.PropertiesCachePath, FileMode.Create, FileAccess.Write, FileShare.None);
        writeStream.WriteBytes(Encoding.ASCII.GetBytes("stgC")); //Signature, 4 bytes.
        writeStream.WriteUInt16(1); //File version, 2 bytes.
        writeStream.WriteUInt16(project.Width); //Width, 2 bytes.
        writeStream.WriteUInt16(project.Height); //Height, 2 bytes.
        writeStream.WriteBytes(BitConverter.GetBytes(Convert.ToSingle(project.HorizontalDpi))); //DPI, 4 bytes.
        writeStream.WriteBytes(BitConverter.GetBytes(Convert.ToSingle(project.VerticalDpi))); //DPI, 4 bytes.
        writeStream.WritePascalStringUInt32(await new SolidColorBrush(Colors.White).ToXamlStringAsync());
        writeStream.WriteByte(project.ChannelCount); //Number of channels, 1 byte.
        writeStream.WriteByte(project.BitsPerChannel); //Bits per channels, 1 byte.
        writeStream.WritePascalString("ScreenToGif"); //App name, 1 byte + X bytes (255 max).
        writeStream.WritePascalString(UserSettings.All.VersionText); //App version, 1 byte + X bytes (255 max).
        writeStream.WriteByte((byte)project.CreatedBy); //Recording source, 1 byte.
        writeStream.WriteUInt64((ulong)project.CreationDate.Ticks); //Creation date, 8 bytes.
        writeStream.WritePascalString(project.Name); //Project's name, 1 byte + X bytes (255 max).
        writeStream.WritePascalString(project.Path); //Project's last used path, 1 byte + X bytes (255 max).

        //Tracks (Frames and Cursor/Key events).
        if (project.CreatedBy != ProjectSources.BoardRecorder)
        {
            await CachedProjectHelper.CreateFrameTrack(recording, project);
            await CachedProjectHelper.CreateCursorTrack(recording, project);
            await CachedProjectHelper.CreateKeyTrack(recording, project);
        }
        else
        {
            //await CachedProjectHelper.CreateStrokeTrack(recording.FramesCachePath, project);
        }
        
        //Create ActionStack cache?

        await Task.Run(() => Discard(recording));

        return project;
    }

    public static bool Discard(this RecordingProject project)
    {
        try
        {
            File.Delete(project.PropertiesCachePath);
            File.Delete(project.FramesCachePath);
            File.Delete(project.EventsCachePath);

            return true;
        }
        catch (Exception e)
        {
            LogWriter.Log(e, "Not possible to discard the recording");
            return true;
        }
    }
}