using ScreenToGif.Domain.Enums;
using System.Windows.Media;

namespace ScreenToGif.Domain.Models.Project.Cached.Sequences;

public class BrushSequence : SizeableSequence
{
    public Brush Brush { get; set; }


    public BrushSequence()
    {
        Type = SequenceTypes.Brush;
    }
}