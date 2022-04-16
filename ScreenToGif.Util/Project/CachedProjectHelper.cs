using ScreenToGif.Domain.Enums;
using ScreenToGif.Domain.Models.Project.Cached;
using ScreenToGif.Domain.Models.Project.Cached.Sequences;
using ScreenToGif.Domain.Models.Project.Cached.Sequences.SubSequences;
using ScreenToGif.Domain.Models.Project.Recording;
using ScreenToGif.Domain.Models.Project.Recording.Events;
using ScreenToGif.Util.Settings;
using System.IO;
using System.Windows.Input;

namespace ScreenToGif.Util.Project;

public static class CachedProjectHelper
{
    public static CachedProject Create(DateTime? creationDate = null)
    {
        var date = creationDate ?? DateTime.Now;
        var path = Path.Combine(UserSettings.All.TemporaryFolderResolved, "ScreenToGif", "Projects", date.ToString("yyyy-MM-dd HH-mm-ss"));

        //What else create paths for?

        var project = new CachedProject
        {
            CacheRootPath = path,
            PropertiesCachePath = Path.Combine(path, "Properties.cache"),
            UndoCachePath = Path.Combine(path, "Undo.cache"),
            RedoCachePath = Path.Combine(path, "Redo.cache"),

            CreationDate = date,
            LastModificationDate = date
        };

        Directory.CreateDirectory(path);

        return project;
    }

    public static async Task CreateFrameTrack(RecordingProject recording, CachedProject project)
    {
        var track = new Track
        {
            Id = (ushort)(project.Tracks.Count + 1),
            Name = "Frames"
        };

        track.CachePath = Path.Combine(project.CacheRootPath, $"Track-{track.Id}.cache");

        var lastFrame = recording.Frames.Last();

        var sequence = new RasterSequence
        {
            Id = 1,
            Width = project.Width,
            Height = project.Height,
            HorizontalDpi = project.HorizontalDpi,
            VerticalDpi = project.VerticalDpi,
            Origin = project.CreatedBy == ProjectSources.ScreenRecorder ? RasterSequenceSources.Screen : RasterSequenceSources.Webcam,
            ChannelCount = project.ChannelCount,
            BitsPerChannel = project.BitsPerChannel,
            StartTime = TimeSpan.Zero,
            EndTime = TimeSpan.FromTicks(lastFrame.Ticks) + TimeSpan.FromMilliseconds(lastFrame.Delay),
            Frames = recording.Frames.Select(s => new FrameSubSequence((ulong) s.Ticks, s.Delay, s.PixelsLength)).ToList()
        };

        await using var readStream = new FileStream(recording.FramesCachePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        await using var writeStream = new FileStream(track.CachePath, FileMode.Create, FileAccess.Write, FileShare.None);

        //Track details.
        writeStream.WriteUInt16(track.Id); //2 bytes.
        writeStream.WritePascalString(track.Name); //1 byte + (0 -> 255) bytes.
        writeStream.WriteByte(track.IsVisible ? (byte)1 : (byte)0); //1 byte.
        writeStream.WriteByte(track.IsLocked ? (byte)1 : (byte)0); //1 byte.
        writeStream.WriteUInt16(1); //Sequence count, 2 bytes.

        //Sequence details.
        writeStream.WriteUInt16(sequence.Id); //2 bytes.
        writeStream.WriteByte((byte)sequence.Type); //1 bytes.
        writeStream.WriteUInt64((ulong)sequence.StartTime.Ticks); //8 bytes.
        writeStream.WriteUInt64((ulong)sequence.EndTime.Ticks); //8 bytes.
        writeStream.WriteBytes(BitConverter.GetBytes(Convert.ToSingle(sequence.Opacity))); //4 bytes.
        writeStream.WritePascalStringUInt32(await sequence.Background.ToXamlStringAsync()); //4 bytes + (0 - 2^32)

        //Sequence effects.
        writeStream.WriteByte(0); //Effect count, 1 bytes.

        //Sizeable sequence.
        writeStream.WriteInt32(sequence.Left); //4 bytes.
        writeStream.WriteInt32(sequence.Top); //4 bytes.
        writeStream.WriteUInt16(sequence.Width); //2 bytes.
        writeStream.WriteUInt16(sequence.Height); //2 bytes.
        writeStream.WriteBytes(BitConverter.GetBytes(Convert.ToSingle(sequence.Angle))); //4 bytes.
        writeStream.WriteBytes(BitConverter.GetBytes(Convert.ToSingle(sequence.HorizontalDpi))); //4 bytes.
        writeStream.WriteBytes(BitConverter.GetBytes(Convert.ToSingle(sequence.VerticalDpi))); //4 bytes.

        //Raster sequence.
        writeStream.WriteByte((byte)sequence.Origin); //1 byte.
        writeStream.WriteByte(sequence.ChannelCount); //1 byte.
        writeStream.WriteByte(sequence.BitsPerChannel); //1 byte.
        writeStream.WriteUInt32((uint)sequence.Frames.Count); //1 byte.

        //Raster frames.
        await readStream.CopyToAsync(writeStream);
        //writeStream.WriteInt64(info.Ticks);
        //writeStream.WriteInt64(info.Delay);
        //writeStream.WriteInt64(info.Pixels.LongLength);
        //writeStream.WriteBytes(info.Pixels);

        track.Sequences.Add(sequence);
    }

    public static async Task CreateCursorTrack(RecordingProject recording, CachedProject project)
    {
        var lastEvent = recording.Events.LastOrDefault(l => l.EventType is RecordingEvents.Cursor or RecordingEvents.CursorData);

        if (lastEvent == null)
            return;

        var track = new Track
        {
            Id = (ushort)(project.Tracks.Count + 1),
            Name = "Cursor Events"
        };

        track.CachePath = Path.Combine(project.CacheRootPath, $"Track-{track.Id}.cache");

        var sequence = new CursorSequence
        {
            Id = 1,
            Width = project.Width,
            Height = project.Height,
            HorizontalDpi = project.HorizontalDpi,
            VerticalDpi = project.VerticalDpi,
            StartTime = TimeSpan.Zero,
            EndTime = TimeSpan.FromTicks(lastEvent.TimeStampInTicks) + TimeSpan.FromMilliseconds(recording.Frames[0].Delay),
            StreamPosition = 0
        };

        await using var writeStream = new FileStream(track.CachePath, FileMode.Create, FileAccess.Write, FileShare.None);

        //Track details.
        writeStream.WriteUInt16(track.Id); //2 bytes.
        writeStream.WritePascalString(track.Name); //1 byte + (0 -> 255) bytes.
        writeStream.WriteByte(track.IsVisible ? (byte)1 : (byte)0); //1 byte.
        writeStream.WriteByte(track.IsLocked ? (byte)1 : (byte)0); //1 byte.
        writeStream.WriteUInt16(1); //Sequence count, 2 bytes.

        //Sequence details.
        writeStream.WriteUInt16(sequence.Id); //2 bytes.
        writeStream.WriteByte((byte)sequence.Type); //1 bytes.
        writeStream.WriteUInt64((ulong)sequence.StartTime.Ticks); //8 bytes.
        writeStream.WriteUInt64((ulong)sequence.EndTime.Ticks); //8 bytes.
        writeStream.WriteBytes(BitConverter.GetBytes(Convert.ToSingle(sequence.Opacity))); //4 bytes.
        writeStream.WritePascalStringUInt32(await sequence.Background.ToXamlStringAsync()); //4 bytes + (0 - 2^32)

        //Sequence effects.
        writeStream.WriteByte(0); //Effect count, 1 bytes.

        //Sizeable sequence.
        writeStream.WriteInt32(sequence.Left); //4 bytes.
        writeStream.WriteInt32(sequence.Top); //4 bytes.
        writeStream.WriteUInt16(sequence.Width); //2 bytes.
        writeStream.WriteUInt16(sequence.Height); //2 bytes.
        writeStream.WriteBytes(BitConverter.GetBytes(Convert.ToSingle(sequence.Angle))); //4 bytes.
        writeStream.WriteBytes(BitConverter.GetBytes(Convert.ToSingle(sequence.HorizontalDpi))); //4 bytes.
        writeStream.WriteBytes(BitConverter.GetBytes(Convert.ToSingle(sequence.VerticalDpi))); //4 bytes.

        //Cursor sub-sequence.
        var cursorEvents = recording.Events.Where(w => w.EventType is RecordingEvents.Cursor or RecordingEvents.CursorData).ToList();
        var cursorData = cursorEvents.OfType<CursorDataEvent>().FirstOrDefault();
        var cursorState = cursorEvents.OfType<CursorEvent>().FirstOrDefault();

        writeStream.WriteUInt32((uint) cursorEvents.Count); //4 bytes.

        await using var readStream = new FileStream(recording.FramesCachePath, FileMode.Open, FileAccess.Read, FileShare.Read);

        foreach (var entry in cursorEvents)
        {
            if (entry is CursorEvent state)
                cursorState = state;
            else if (entry is CursorDataEvent data)
                cursorData = data;

            var sub = new CursorSubSequence
            {
                TimeStampInTicks = (ulong)(cursorState?.TimeStampInTicks ?? 0),
                CursorType = (byte)(cursorData?.CursorType ?? 0),
                Left = cursorState?.PosX ?? 0,
                Top = cursorState?.PosY ?? 0,
                Width = (ushort)(cursorData?.Width ?? 32),
                Height = (ushort)(cursorData?.Height ?? 32),
                XHotspot = (ushort)(cursorData?.XHotspot ?? 0),
                YHotspot = (ushort)(cursorData?.YHotspot ?? 0),
                IsLeftButtonDown = cursorState?.LeftButton == MouseButtonState.Pressed,
                IsRightButtonDown = cursorState?.RightButton == MouseButtonState.Pressed,
                IsMiddleButtonDown = cursorState?.MiddleButton == MouseButtonState.Pressed,
                IsFirstExtraButtonDown = cursorState?.FirstExtraButton == MouseButtonState.Pressed,
                IsSecondExtraButtonDown = cursorState?.SecondExtraButton == MouseButtonState.Pressed,
                MouseWheelDelta = cursorState?.MouseDelta ?? 0,
                PixelsLength = (ushort)(cursorData?.PixelsLength ?? 0),
                StreamPosition = (ulong)writeStream.Position
            };

            writeStream.WriteByte((byte)sub.Type); //1 byte.
            writeStream.WriteUInt64(sub.TimeStampInTicks); //8 bytes.
            writeStream.WriteByte(sub.CursorType); //1 byte.
            writeStream.WriteInt32(sub.Left); //4 bytes.
            writeStream.WriteInt32(sub.Top); //4 bytes.
            writeStream.WriteUInt16(sub.Width); //2 bytes.
            writeStream.WriteUInt16(sub.Height); //2 bytes.
            writeStream.WriteUInt16(sub.XHotspot); //2 bytes.
            writeStream.WriteUInt16(sub.YHotspot); //2 bytes.
            writeStream.WriteBoolean(sub.IsLeftButtonDown); //1 byte.
            writeStream.WriteBoolean(sub.IsRightButtonDown); //1 byte.
            writeStream.WriteBoolean(sub.IsMiddleButtonDown); //1 byte.
            writeStream.WriteBoolean(sub.IsFirstExtraButtonDown); //1 byte.
            writeStream.WriteBoolean(sub.IsSecondExtraButtonDown); //1 byte.
            writeStream.WriteInt16(sub.MouseWheelDelta); //2 bytes.
            writeStream.WriteUInt64(sub.PixelsLength); //8 bytes.

            //The pixel location is 42 bytes after the start of the event stream position.
            await using (var part = new SubStream(readStream, (cursorData?.StreamPosition ?? 0) + 42, (long)sub.PixelsLength))
                await part.CopyToAsync(writeStream);

            sequence.CursorEvents.Add(sub);
        }

        track.Sequences.Add(sequence);
    }

    public static async Task CreateKeyTrack(RecordingProject recording, CachedProject project)
    {
        var lastEvent = recording.Events.LastOrDefault(l => l.EventType == RecordingEvents.Key);

        if (lastEvent == null)
            return;

        var track = new Track
        {
            Id = (ushort)(project.Tracks.Count + 1),
            Name = "Key Events"
        };

        track.CachePath = Path.Combine(project.CacheRootPath, $"Track-{track.Id}.cache");

        var sequence = new KeySequence
        {
            Id = 1,
            Width = project.Width,
            Height = project.Height,
            HorizontalDpi = project.HorizontalDpi,
            VerticalDpi = project.VerticalDpi,
            StartTime = TimeSpan.Zero,
            EndTime = TimeSpan.FromTicks(lastEvent.TimeStampInTicks) + TimeSpan.FromMilliseconds(recording.Frames[0].Delay)
        };

        await using var writeStream = new FileStream(track.CachePath, FileMode.Create, FileAccess.Write, FileShare.None);

        //Track details.
        writeStream.WriteUInt16(track.Id); //2 bytes.
        writeStream.WritePascalString(track.Name); //1 byte + (0 -> 255) bytes.
        writeStream.WriteByte(track.IsVisible ? (byte)1 : (byte)0); //1 byte.
        writeStream.WriteByte(track.IsLocked ? (byte)1 : (byte)0); //1 byte.
        writeStream.WriteUInt16(1); //Sequence count, 2 bytes.

        //Sequence details.
        writeStream.WriteUInt16(sequence.Id); //2 bytes.
        writeStream.WriteByte((byte)sequence.Type); //1 bytes.
        writeStream.WriteUInt64((ulong)sequence.StartTime.Ticks); //8 bytes.
        writeStream.WriteUInt64((ulong)sequence.EndTime.Ticks); //8 bytes.
        writeStream.WriteBytes(BitConverter.GetBytes(Convert.ToSingle(sequence.Opacity))); //4 bytes.
        writeStream.WritePascalStringUInt32(await sequence.Background.ToXamlStringAsync()); //4 bytes + (0 - 2^32)

        //Sequence effects.
        writeStream.WriteByte(0); //Effect count, 1 bytes.

        //Sizeable sequence.
        writeStream.WriteInt32(sequence.Left); //4 bytes.
        writeStream.WriteInt32(sequence.Top); //4 bytes.
        writeStream.WriteUInt16(sequence.Width); //2 bytes.
        writeStream.WriteUInt16(sequence.Height); //2 bytes.
        writeStream.WriteBytes(BitConverter.GetBytes(Convert.ToSingle(sequence.Angle))); //4 bytes.
        writeStream.WriteBytes(BitConverter.GetBytes(Convert.ToSingle(sequence.HorizontalDpi))); //4 bytes.
        writeStream.WriteBytes(BitConverter.GetBytes(Convert.ToSingle(sequence.VerticalDpi))); //4 bytes.

        //Key sub-sequence.
        var keyEvents = recording.Events.OfType<KeyEvent>().ToList();

        writeStream.WriteUInt32((uint)keyEvents.Count); //4 bytes.

        foreach (var keyEvent in keyEvents)
        {
            var sub = new KeySubSequence
            {
                TimeStampInTicks = (ulong)keyEvent.TimeStampInTicks,
                Key = keyEvent.Key,
                Modifiers = keyEvent.Modifiers,
                IsUppercase = keyEvent.IsUppercase,
                WasInjected = keyEvent.WasInjected,
                StreamPosition = (ulong)writeStream.Position
            };

            writeStream.WriteByte((byte)sub.Type); //1 byte.
            writeStream.WriteUInt64(sub.TimeStampInTicks); //8 bytes.
            writeStream.WriteByte((byte)sub.Key); //1 byte.
            writeStream.WriteByte((byte)sub.Modifiers); //1 byte.
            writeStream.WriteBoolean(sub.IsUppercase); //1 byte.
            writeStream.WriteBoolean(sub.WasInjected); //1 byte.
            
            sequence.KeyEvents.Add(sub);
        }

        track.Sequences.Add(sequence);
    }

    //Read from disk, to load recent projects.

    //Discard?

    //Save to StorageProject.
}