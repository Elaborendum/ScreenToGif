namespace ScreenToGif.Domain.Models.Project.Cached;

public class Track
{
    public ushort Id { get; set; }

    public bool IsVisible { get; set; } = true;

    public bool IsLocked { get; set; }
        
    public string Name { get; set; }
        
    /// <summary>
    /// A track can have multiple sequences of the same type.
    /// </summary>
    public List<Sequence> Sequences { get; set; } = new();

    /// <summary>
    /// A binary cache containing a simple structure with the details of the track.
    /// </summary>
    public string CachePath { get; set; }
}