using ScreenToGif.Domain.Enums;
using System.Windows.Input;

namespace ScreenToGif.Domain.Models.Project.Recording.Events;

public class CursorEvent : RecordingEvent
{
    public CursorEvent()
    {
        EventType = RecordingEvents.Cursor;
    }

    public CursorEvent(int x, int y, MouseButtonState left, MouseButtonState right, MouseButtonState middle, MouseButtonState firstExtra, MouseButtonState secondExtra, short mouseDelta = 0)
    {
        PosX = x;
        PosY = y;

        LeftButton = left;
        RightButton = right;
        MiddleButton = middle;
        FirstExtraButton = firstExtra;
        SecondExtraButton = secondExtra;
        MouseDelta = mouseDelta;
    }

    /// <summary>
    /// Horizontal axis position.
    /// </summary>
    public int PosX { get; set; }

    /// <summary>
    /// Vertical axis position.
    /// </summary>
    public int PosY { get; set; }

    /// <summary>
    /// State of the left mouse button.
    /// </summary>
    public MouseButtonState LeftButton { get; set; }

    /// <summary>
    /// State of the right mouse button.
    /// </summary>
    public MouseButtonState RightButton { get; set; }

    /// <summary>
    /// State of the middle mouse button.
    /// </summary>
    public MouseButtonState MiddleButton { get; set; }

    /// <summary>
    /// State of the first extra mouse buttons.
    /// </summary>
    public MouseButtonState FirstExtraButton { get; set; }

    /// <summary>
    /// State of the second extra mouse buttons.
    /// </summary>
    public MouseButtonState SecondExtraButton { get; set; }

    /// <summary>
    /// The state of the scroll wheel. Up or down scroll flow.
    /// </summary>
    public short MouseDelta { get; set; }
}