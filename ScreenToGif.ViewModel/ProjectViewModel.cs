using ScreenToGif.Domain.Models.Project.Cached;
using ScreenToGif.Domain.ViewModels;
using System.Collections.ObjectModel;
using System.Windows.Media;

namespace ScreenToGif.ViewModel;

public class ProjectViewModel : BaseViewModel
{
    private string _name = "";
    private int _width = 0;
    private int _height = 0;
    private double _horizontalDpi = 96d;
    private double _verticalDpi = 96d;
    private Brush _background = Brushes.White;
    private ObservableCollection<TrackViewModel> _tracks = new();

    public CachedProject Project { get; set; }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public int Width
    {
        get => _width;
        set => SetProperty(ref _width, value);
    }

    public int Height
    {
        get => _height;
        set => SetProperty(ref _height, value);
    }

    public double HorizontalDpi
    {
        get => _horizontalDpi;
        set => SetProperty(ref _horizontalDpi, value);
    }

    public double VerticalDpi
    {
        get => _verticalDpi;
        set => SetProperty(ref _verticalDpi, value);
    }

    public Brush Background
    {
        get => _background;
        set => SetProperty(ref _background, value);
    }

    public ObservableCollection<TrackViewModel> Tracks
    {
        get => _tracks;
        set => SetProperty(ref _tracks, value);
    }
    
    public static ProjectViewModel FromModel(CachedProject project)
    {
        return new ProjectViewModel
        {
            Project = project,
            Name = project.Name,
            Width = project.Width,
            Height = project.Height,
            HorizontalDpi = project.HorizontalDpi,
            VerticalDpi = project.VerticalDpi,
            Background = project.Background,
            Tracks = new ObservableCollection<TrackViewModel>(project.Tracks.Select(TrackViewModel.FromModel).ToList())
        };
    }

    public CachedProject ToModel()
    {
        return new CachedProject
        {
            Name  = Name,
        };
    }
}