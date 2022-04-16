using ScreenToGif.Domain.Models.Project.Recording;
using System;
using System.Windows;
using ScreenToGif.Util;
using ScreenToGif.ViewModel;
using ScreenToGif.Windows.Other;
using System.Threading.Tasks;

namespace ScreenToGif.Windows;

public partial class EditorEx : Window
{
    #region Variables

    /// <summary>
    /// Lock used to prevent firing multiple times (at the same time) both the Activated/Deactivated events.
    /// </summary>
    public static readonly object ActivateLock = new();

    private readonly EditorViewModel _editorViewModel;

    #endregion

    #region Properties

    public bool HasProjectLoaded => _editorViewModel?.Project != null;

    #endregion

    public EditorEx()
    {
        InitializeComponent();

        DataContext = _editorViewModel = new EditorViewModel();
        CommandBindings.Clear();
        CommandBindings.AddRange(_editorViewModel.CommandBindings);
    }

    //Panels:
    //Not stored directly in this window, but in user controls.
    //When a property is changed, the panel will report the event, which will trigger the rendering.

    #region Main events

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        
    }

    private void Window_Activated(object sender, EventArgs e)
    {
        lock (ActivateLock)
        {
            //Returns the preview if was playing before the deactivation of the window.
            //if (WasPreviewing)
            //{
            //    WasPreviewing = false;
            //    PlayPause();
            //}
        }
    }

    private void Window_Deactivated(object sender, EventArgs e)
    {
        if (!IsLoaded)
            return;

        lock (ActivateLock)
        {
            try
            {
                //Pauses the recording preview.
                //if (_timerPreview.Enabled)
                //{
                //    WasPreviewing = true;
                //    Pause();
                //}
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Exception when losing focus on window.");
            }
        }
    }

    private void ShowClipboardButton_Click(object sender, RoutedEventArgs e)
    {

    }

    #endregion

    public async Task LoadProject(RecordingProject project)
    {
        Activate();

        if (project?.Any == true)
            await _editorViewModel.ImportFromRecording(project);
        
        Encoder.Restore();
        ShowInTaskbar = true;
        WindowState = WindowState == WindowState.Minimized ? WindowState.Normal : WindowState;
    }

    public void LoadFromArguments()
    {
        //Identify arguments
        //Validate what's comming.
        //Only load groups of the same type (media, project).
        //Load files
        //Parse data.
        //Create tracks/sequences for each file.
    }
}