using ScreenToGif.Domain.Enums;
using ScreenToGif.Domain.Models.Project.Cached.Sequences.SubSequences;

namespace ScreenToGif.Domain.Models.Project.Cached.Sequences;

/// <summary>
/// KeyEvents can happen out of sync with the recording. 
/// </summary>
public class KeySequence : SizeableSequence
{
    public List<KeySubSequence> KeyEvents { get; set; } = new();


    public KeySequence()
    {
        Type = SequenceTypes.Key;
    }
}