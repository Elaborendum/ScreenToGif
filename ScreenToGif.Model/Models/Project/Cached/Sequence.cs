using ScreenToGif.Domain.Enums;
using ScreenToGif.Domain.Models.Project.Cached.Sequences.Effects;
using System.Windows.Media;

namespace ScreenToGif.Domain.Models.Project.Cached;

public class Sequence
{
    public ushort Id { get; set; }

    public SequenceTypes Type { get; set; }

    public TimeSpan StartTime { get; set; }

    public TimeSpan EndTime { get; set; }

    public double Opacity { get; set; } = 1;

    public Brush Background { get; set; }

    public List<Shadow> Effects { get; set; } = new();

    public ulong StreamPosition { get; set; }
}