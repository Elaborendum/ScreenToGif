using ScreenToGif.Domain.Enums;
using ScreenToGif.Domain.Models.Project.Recording.Events;

namespace ScreenToGif.Domain.Models.Project.Recording;

public class RecordingProject
{
    #region Identity

    /// <summary>
    /// The date of reation of this project.
    /// </summary>
    public DateTime CreationDate { get; set; } = DateTime.Now;

    /// <summary>
    /// The source of this project.
    /// </summary>
    public ProjectSources CreatedBy { get; set; } = ProjectSources.Unknown;

    #endregion

    #region Visual

    /// <summary>
    /// The width of the canvas.
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// The height of the canvas.
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// The base dpi of the project.
    /// </summary>
    public double Dpi { get; set; } = 96;

    /// <summary>
    /// The number of channels in the captured frames.
    /// 4 is RGBA
    /// 3 is RGB
    /// </summary>
    public byte ChannelCount { get; set; } = 4;

    /// <summary>
    /// The bits per channel in the captured frames.
    /// </summary>
    public byte BitsPerChannel { get; set; } = 8;

    #endregion

    #region Path

    /// <summary>
    /// A binary cache containing a simple structure with the details of the project.
    /// </summary>
    public string PropertiesCachePath { get; set; }

    /// <summary>
    /// A binary cache containing a simple structure with all frames.
    /// </summary>
    public string FramesCachePath { get; set; }

    /// <summary>
    /// A binary cache containing a simple structure with all strokes. TODO: Make it work with Board capture.
    /// </summary>
    public string StrokesCachePath { get; set; }

    /// <summary>
    /// A binary cache containing a simple structure with all events (cursor, cursor data or key).
    /// </summary>
    public string EventsCachePath { get; set; }
    
    #endregion

    #region Events

    /// <summary>
    /// List of captured frames.
    /// </summary>
    public List<RecordingFrame> Frames { get; set; } = new();

    /// <summary>
    /// List of captured events (cursor, keys, etc).
    /// </summary>
    public List<RecordingEvent> Events { get; set; } = new();

    #endregion

    #region Status

    /// <summary>
    /// Check if there's any frame on this project.
    /// </summary>
    public bool Any => Frames != null && Frames.Any();

    #endregion
}