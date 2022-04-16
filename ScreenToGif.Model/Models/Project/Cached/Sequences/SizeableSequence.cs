namespace ScreenToGif.Domain.Models.Project.Cached.Sequences;

/// <summary>
/// Primitive sequence object which has a defined sizing information.
/// </summary>
public class SizeableSequence : Sequence
{
    public int Left { get; set; }

    public int Top { get; set; }
        
    public ushort Width { get; set; }

    public ushort Height { get; set; }

    public double Angle { get; set; }

    public double HorizontalDpi { get; set; }

    public double VerticalDpi { get; set; }
}