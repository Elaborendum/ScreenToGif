using ScreenToGif.Domain.ViewModels;

namespace ScreenToGif.ViewModel;

public class FrameViewModel : BaseViewModel
{
    private string _image;
    private long _imagePosition;
    private int _number;
    private int _delay;
    
    public string Image
    {
        get => _image;
        set => SetProperty(ref _image, value);
    }

    public long ImagePosition
    {
        get => _imagePosition;
        set => SetProperty(ref _imagePosition, value);
    }

    public int Number
    {
        get => _number;
        set => SetProperty(ref _number, value);
    }

    public int Delay
    {
        get => _delay;
        set => SetProperty(ref _delay, value);
    }
}