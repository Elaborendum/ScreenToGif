using ScreenToGif.Domain.Enums;

namespace ScreenToGif.Domain.Models.Project.Cached.Sequences.Effects;

public class Fade : Effect
{
    public Fade()
    {
        Type = EffectTypes.Fade;
    }

    public FadeDirections Direction { get; set; }

    public TimeSpan Duration { get; set; }

    //Ease type?
}