using ScreenToGif.Domain.Enums;

namespace ScreenToGif.Domain.Models.Project.Cached.Sequences.SubSequences;

public class SubSequence
{
    public SubSequenceTypes Type { get; set; }

    /// <summary>
    /// Ticks since the start of the sequence.
    /// </summary>
    public ulong TimeStampInTicks { get; set; }

    /// <summary>
    /// Position of this sub-sequence in the stream.
    /// </summary>
    public ulong StreamPosition { get; set; }
}