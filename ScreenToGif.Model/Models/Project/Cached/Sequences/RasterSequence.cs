using ScreenToGif.Domain.Enums;
using ScreenToGif.Domain.Models.Project.Cached.Sequences.SubSequences;

namespace ScreenToGif.Domain.Models.Project.Cached.Sequences;

public class RasterSequence : SizeableSequence
{
    /// <summary>
    /// Origin of the raster frames.
    /// It could be from capture (screen or webcam), media import (gif, apng, image or video) or rasterization of other sequences.
    /// </summary>
    public RasterSequenceSources Origin { get; set; }

    /// <summary>
    /// The number of channels of the images.
    /// 4 is RGBA
    /// 3 is RGB
    /// </summary>
    public byte ChannelCount { get; set; } = 4;

    /// <summary>
    /// The bits per channel in the images.
    /// </summary>
    public byte BitsPerChannel { get; set; } = 8;

    /// <summary>
    /// Each frame with its timings.
    /// </summary>
    public List<FrameSubSequence> Frames { get; set; }
    
    public RasterSequence()
    {
        Type = SequenceTypes.Raster;
    }
}