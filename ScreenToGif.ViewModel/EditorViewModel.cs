using ScreenToGif.Domain.Models.Project.Recording;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ScreenToGif.Domain.ViewModels;
using ScreenToGif.Util.Project;
using System.Collections.ObjectModel;

namespace ScreenToGif.ViewModel;

public class EditorViewModel : BaseViewModel
{
    #region Variables

    private ProjectViewModel _project;
    private TimeSpan _currentTime = TimeSpan.Zero;
    private int _currentIndex = -1;
    private WriteableBitmap _renderedImage;
    private double _zoom = 1d;
    private bool _isLoading;

    //Erase it later.
    private ObservableCollection<FrameViewModel> _frames = new();

    #endregion

    #region Properties

    public CommandBindingCollection CommandBindings => new()
    {
        new CommandBinding(FindCommand("Command.NewRecording"), (sender, args) => { Console.WriteLine(""); }, (sender, args) => { args.CanExecute = true; }),
        new CommandBinding(FindCommand("Command.NewWebcamRecording"), (sender, args) => { Console.WriteLine(""); }, (sender, args) => { args.CanExecute = true; }),
    };

    public ProjectViewModel Project
    {
        get => _project;
        set => SetProperty(ref _project, value);
    }

    public TimeSpan CurrentTime
    {
        get => _currentTime;
        set
        {
            SetProperty(ref _currentTime, value);
            Seek();
        }
    }

    public int CurrentIndex
    {
        get => _currentIndex;
        set => SetProperty(ref _currentIndex, value);
    }

    internal WriteableBitmap RenderedImage
    {
        get => _renderedImage;
        set => SetProperty(ref _renderedImage, value);
    }

    public double Zoom
    {
        get => _zoom;
        set => SetProperty(ref _zoom, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    /// <summary>
    /// The list of frames. TODO: Erase it later.
    /// </summary>
    public ObservableCollection<FrameViewModel> Frames
    {
        get => _frames;
        set => SetProperty(ref _frames, value);
    }

    #endregion

    public EditorViewModel()
    {
        //?
    }

    #region Methods

    public async Task ImportFromRecording(RecordingProject project)
    {
        IsLoading = true;

        //TODO: Show progress.
        //Cancelable.

        //For progress:
        //Create list of progresses.
        //Pass the created progress reporter.
        
        var cached = await project.ConvertToCachedProject();
        Project = ProjectViewModel.FromModel(cached);

        InitializePreview();
        
        IsLoading = false;
    }

    internal void InitializePreview()
    {
        RenderedImage = new WriteableBitmap(Project.Width, Project.Height, Project.HorizontalDpi, Project.VerticalDpi, PixelFormats.Bgra32, null);

        Render();
    }

    internal void Render()
    {
        if (RenderedImage == null)
            return;

        //Get current timestamp/index and render the scene and apply to the RenderedImage property.

        //How are previews going to work?
        //  Text rendering
        //  Rendering that needs access to the all layers.
        //  Rendering that changes the size of the canvas.

        //Preview quality.
        //Render the list preview for the frames.
    }

    internal void Seek()
    {
        //Display mode: By timestamp or frame index.
        //Display properties in Statistic tab.

        Render();
    }

    internal void Play()
    {
        //?
    }

    //How are the frames/data going to be stored in the disk?
    //Project file for the user + opened project should have a cache
    //  Project file for user: I'll need to create a file spec.
    //  Cache folder for the app: 

    //As a single cache for each track? (storing as pixel array, to improve performance)
    //I'll need a companion json with positions and other details.
    //I also need to store in memory for faster usage.

    #endregion
}