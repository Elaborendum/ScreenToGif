using ScreenToGif.Domain.Enums;

namespace ScreenToGif.Domain.Models.Project.Cached.Sequences.SubSequences;

public class FrameSubSequence : SubSequence
{
    /// <summary>
    /// Frame delay in milliseconds.
    /// </summary>
    public long Delay { get; set; }

    /// <summary>
    /// The number of bytes of the capture content.
    /// </summary>
    public ulong PixelsLength { get; set; }

    public FrameSubSequence()
    {
        Type = SubSequenceTypes.Frame;
    }

    public FrameSubSequence(ulong ticks, long delay, ulong pixelsLength) : this()
    {
        TimeStampInTicks = ticks;
        Delay = delay;
        PixelsLength = pixelsLength;
    }
}