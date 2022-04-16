using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace ScreenToGif.Controls.Ex;

public class Previewer : Image 
{
    //Zoom?

    //Maybe use this, or just use the actual Source from the image itself.
    public static readonly DependencyProperty RenderedImageProperty = DependencyProperty.Register(nameof(RenderedImage), typeof(WriteableBitmap), typeof(Previewer), new PropertyMetadata(default(WriteableBitmap)));

    public WriteableBitmap RenderedImage
    {
        get => (WriteableBitmap)GetValue(RenderedImageProperty);
        set => SetValue(RenderedImageProperty, value);
    }
}