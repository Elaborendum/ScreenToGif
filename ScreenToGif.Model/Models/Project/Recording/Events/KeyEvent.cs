using ScreenToGif.Domain.Enums;
using System.Windows.Input;

namespace ScreenToGif.Domain.Models.Project.Recording.Events;

public class KeyEvent : RecordingEvent
{
    public KeyEvent()
    {
        EventType = RecordingEvents.Key;
    }

    public Key Key { get; set; }

    public ModifierKeys Modifiers { get; set; }

    public bool IsUppercase { get; set; }

    public bool WasInjected { get; set; }
}