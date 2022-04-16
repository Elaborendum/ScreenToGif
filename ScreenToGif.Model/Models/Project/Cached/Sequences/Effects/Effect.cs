using ScreenToGif.Domain.Enums;

namespace ScreenToGif.Domain.Models.Project.Cached.Sequences.Effects;

public class Effect
{
    public int Id { get; set; }

    public EffectTypes Type { get; set; }

    //Effects
    //  Invert
    //  Color masks
    //  Rotation?
}