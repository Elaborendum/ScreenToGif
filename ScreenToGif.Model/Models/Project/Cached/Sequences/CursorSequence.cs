using ScreenToGif.Domain.Enums;
using ScreenToGif.Domain.Models.Project.Cached.Sequences.SubSequences;

namespace ScreenToGif.Domain.Models.Project.Cached.Sequences;

public class CursorSequence : SizeableSequence
{
    public List<CursorSubSequence> CursorEvents { get; set; } = new();


    public CursorSequence()
    {
        Type = SequenceTypes.Cursor;
    }
}