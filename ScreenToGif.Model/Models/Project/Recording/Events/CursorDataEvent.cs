using ScreenToGif.Domain.Enums;

namespace ScreenToGif.Domain.Models.Project.Recording.Events;

public class CursorDataEvent : RecordingEvent
{
    public CursorDataEvent()
    {
        EventType = RecordingEvents.CursorData;
    }

    /// <summary>
    /// 1 = Monochrome: The reference type is a monochrome mouse reference, which is a monochrome bitmap.
    ///     The bitmap's size is specified by width and height in a 1 bits per pixel (bpp) device independent bitmap (DIB) format
    ///     AND mask that is followed by another 1 bpp DIB format XOR mask of the same size.
    /// 2 = Color: The reference type is a color mouse reference, which is a color bitmap.
    ///     The bitmap's size is specified by width and height in a 32 bpp ARGB DIB format.
    /// 4 = Masked Color: The reference type is a masked color mouse reference.
    ///     A masked color mouse reference is a 32 bpp ARGB format bitmap with the mask value in the alpha bits.
    ///     The only allowed mask values are 0 and 0xFF. When the mask value is 0, the RGB value should replace the screen pixel.
    ///     When the mask value is 0xFF, an XOR operation is performed on the RGB value and the screen pixel; the result replaces the screen pixel.
    /// </summary>
    public int CursorType { get; set; }

    public byte[] Pixels { get; set; }

    public long PixelsLength { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }

    public int XPosition { get; set; }

    public int YPosition { get; set; }

    public int XHotspot { get; set; }

    public int YHotspot { get; set; }
}