using ScreenToGif.Domain.Enums;
using System.Windows.Input;

namespace ScreenToGif.Domain.Models.Project.Cached.Sequences.SubSequences;

public class KeySubSequence : SubSequence
{
    public Key Key { get; set; }

    public ModifierKeys Modifiers { get; set; }

    public bool IsUppercase { get; set; }

    public bool WasInjected { get; set; }

    public KeySubSequence()
    {
        Type = SubSequenceTypes.Key;
    }
}