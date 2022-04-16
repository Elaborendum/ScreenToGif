using ScreenToGif.Domain.Enums;

namespace ScreenToGif.Domain.Models.Project.Recording.Events;

public class RecordingEvent
{
    public RecordingEvents EventType { get; set; }

    public long TimeStampInTicks { get; set; }

    public long StreamPosition { get; set; }
}