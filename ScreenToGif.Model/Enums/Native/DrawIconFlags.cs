namespace ScreenToGif.Domain.Enums.Native;

/// <summary>
/// The drawing flags for the DrawIconEx method.
/// </summary>
public enum DrawIconFlags
{
    /// <summary>
    /// Draws the icon or cursor using the mask.
    /// </summary>
    Mask = 1,

    /// <summary>
    /// Draws the icon or cursor using the image.
    /// </summary>
    Image = 2,

    /// <summary>
    /// Combination of Image and Mask.
    /// </summary>
    Normal = Mask | Image,

    /// <summary>
    /// This flag is ignored.
    /// </summary>
    Compat = 4,

    /// <summary>
    /// Draws the icon or cursor using the width and height specified by the system metric values for icons,
    /// if the cxWidth and cyWidth parameters are set to zero.
    /// If this flag is not specified and cxWidth and cyWidth are set to zero, the function uses the actual resource size.
    /// </summary>
    DefaultSize = 8,

    /// <summary>
    /// Draws the icon as an unmirrored icon. By default, the icon is drawn as a mirrored icon if hdc is mirrored.
    /// </summary>
    NoMirror = 10,
}