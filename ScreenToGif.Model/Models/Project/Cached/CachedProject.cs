using ScreenToGif.Domain.Enums;
using System.Windows.Media;

namespace ScreenToGif.Domain.Models.Project.Cached;

public class CachedProject
{
    #region Identity

    /// <summary>
    /// Just the name to the project file.
    /// </summary>
    public string Name { get; set; }
        
    /// <summary>
    /// The full path of the project (saved by the user).
    /// It's the path + filename + extension.
    /// </summary>
    public string Path { get; set; }

    /// <summary>
    /// The version of ScreenToGif used to create this project.
    /// </summary>
    public Version Version { get; set; }

    public DateTime CreationDate { get; set; }

    public DateTime? LastModificationDate { get; set; }

    /// <summary>
    /// The source of the project.
    /// </summary>
    public ProjectSources CreatedBy { get; set; } = ProjectSources.Unknown;

    #endregion

    #region Visual

    /// <summary>
    /// The canvas width of the project.
    /// </summary>
    public ushort Width { get; set; }

    /// <summary>
    /// The canvas height of the project.
    /// </summary>
    public ushort Height { get; set; }

    /// <summary>
    /// The DPI of the X axis of the project.
    /// </summary>
    public double HorizontalDpi { get; set; }

    /// <summary>
    /// The DPI of the Y axis of the project.
    /// </summary>
    public double VerticalDpi { get; set; }

    /// <summary>
    /// The background of the whole project.
    /// </summary>
    public Brush Background { get; set; }

    /// <summary>
    /// The number of channels of the project.
    /// 4 is RGBA
    /// 3 is RGB
    /// </summary>
    public byte ChannelCount { get; set; } = 4;

    /// <summary>
    /// The bits per channel in the captured frames.
    /// </summary>
    public byte BitsPerChannel { get; set; } = 8;

    #endregion

    /// <summary>
    /// Tracks can hold multiple sequences of the same type, but not overlapping.
    /// </summary>
    public List<Track> Tracks { get; set; } = new();

    /// <summary>
    /// Action undo stack.
    /// TODO: Create actions and save as reverse actions.
    /// How to save each registered action in cache?
    /// Maybe the view model handles that and saves periodically to the disk.
    /// </summary>
    public List<object> UndoStack { get; set; }

    /// <summary>
    /// Action redo stack.
    /// </summary>
    public List<object> RedoStack { get; set; }

    #region Cache Path

    /// <summary>
    /// The path for the folder that contains the cache files.
    /// </summary>
    public string CacheRootPath { get; set; }

    /// <summary>
    /// A binary cache containing a simple structure with the details of the project.
    /// </summary>
    public string PropertiesCachePath { get; set; }

    /// <summary>
    /// A binary cache containing a list of undo actions for the project.
    /// </summary>
    public string UndoCachePath { get; set; }

    /// <summary>
    /// A binary cache containing a list of redo actions for the project.
    /// </summary>
    public string RedoCachePath { get; set; }

    #endregion

    #region Temporal

    /// <summary>
    /// True if changes were detected and need to be saved.
    /// Transient, not persisted to output file.
    /// </summary>
    public bool HasChanges { get; set; }
    
    #endregion
}