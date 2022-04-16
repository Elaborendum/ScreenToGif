using ScreenToGif.Domain.Enums;

namespace ScreenToGif.Domain.Models.Project.Cached.Sequences.SubSequences;

public class CursorSubSequence : SubSequence
{
    /// <summary>
    /// 0 = Unidentified: The cursor type needs to be identified by its pixel content.
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
    public byte CursorType { get; set; }

    public int Left { get; set; }

    public int Top { get; set; }

    public ushort Width { get; set; }

    public ushort Height { get; set; }

    public ushort XHotspot { get; set; }

    public ushort YHotspot { get; set; }

    public bool IsLeftButtonDown { get; set; }

    public bool IsRightButtonDown { get; set; }

    public bool IsMiddleButtonDown { get; set; }

    public bool IsFirstExtraButtonDown { get; set; }

    public bool IsSecondExtraButtonDown { get; set; }

    public short MouseWheelDelta { get; set; }

    public ulong PixelsLength { get; set; }

    public bool IsMiddleScrollUp => MouseWheelDelta > 0;

    public bool IsMiddleScrollDown => MouseWheelDelta < 0;

    public bool IsMiddleScroll => IsMiddleScrollUp || IsMiddleScrollDown;

    public CursorSubSequence()
    {
        Type = SubSequenceTypes.Cursor;
    }
}