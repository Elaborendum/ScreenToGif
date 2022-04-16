using ScreenToGif.Domain.Enums.Native;
using ScreenToGif.Domain.Models.Project.Recording;
using ScreenToGif.Native.External;
using ScreenToGif.Native.Structs;
using ScreenToGif.Util.Settings;
using System.Windows;

namespace ScreenToGif.Util.Capture;

public class GdiCapture : ScreenCapture
{
    #region Variables

    private readonly IntPtr _desktopWindow = IntPtr.Zero;
    private IntPtr _windowDeviceContext;
    private IntPtr _compatibleDeviceContext;
    private IntPtr _compatibleBitmap;
    private IntPtr _oldBitmap;

    private BitmapInfoHeader _bitmapHeader;
    private CopyPixelOperations _pixelOperations;
    private int _cursorStep;
    private ulong _byteLength;

    #endregion

    public override void Start(int delay, int left, int top, int width, int height, double scale, RecordingProject project)
    {
        base.Start(delay, left, top, width, height, scale, project);

        #region Pointers

        //http://winprog.org/tutorial/bitmaps.html
        _windowDeviceContext = User32.GetWindowDC(_desktopWindow);
        _compatibleDeviceContext = Gdi32.CreateCompatibleDC(_windowDeviceContext);
        _compatibleBitmap = Gdi32.CreateCompatibleBitmap(_windowDeviceContext, Width, Height);
        _oldBitmap = Gdi32.SelectObject(_compatibleDeviceContext, _compatibleBitmap);

        #endregion

        #region Pixel Operation

        _pixelOperations = CopyPixelOperations.SourceCopy;

        //If not in a remote desktop connection or if the improvement was disabled, capture layered windows too.
        if (!SystemParameters.IsRemoteSession || !UserSettings.All.RemoteImprovement)
            _pixelOperations |= CopyPixelOperations.CaptureBlt;

        #endregion

        //Bitmap details for each frame being captured.
        _bitmapHeader = new BitmapInfoHeader(false)
        {
            BitCount = 32, //Was 24
            ClrUsed = 0,
            ClrImportant = 0,
            Compression = 0,
            Height = -StartHeight, //Negative, so the Y-axis will be positioned correctly.
            Width = StartWidth,
            Planes = 1
        };

        //This was working with 32 bits: 3L * Width * Height;
        _byteLength = (ulong)((StartWidth * _bitmapHeader.BitCount + 31) / 32 * 4 * StartHeight);

        //Preemptively Capture the first cursor shape.
        CaptureCursor();
    }

    public override int Capture(RecordingFrame frame)
    {
        try
        {
            if (!Gdi32.StretchBlt(_compatibleDeviceContext, 0, 0, StartWidth, StartHeight, _windowDeviceContext, Left, Top, Width, Height, _pixelOperations))
                return FrameCount;

            //Set frame details.
            FrameCount++;

            frame.Ticks = Stopwatch.GetElapsedTicks();
            frame.Delay = Stopwatch.GetMillisecondsAsLong();
            frame.Pixels = new byte[_byteLength];

            if (Gdi32.GetDIBits(_windowDeviceContext, _compatibleBitmap, 0, (uint)StartHeight, frame.Pixels, ref _bitmapHeader, DibColorModes.RgbColors) == 0)
                frame.WasFrameSkipped = true;

            if (IsAcceptingFrames)
                FrameCollection.Add(frame);
        }
        catch (Exception)
        {
            //LogWriter.Log(ex, "Impossible to get screenshot of the screen");
        }

        return FrameCount;
    }

    public override int CaptureWithCursor(RecordingFrame frame)
    {
        try
        {
            if (!Gdi32.StretchBlt(_compatibleDeviceContext, 0, 0, StartWidth, StartHeight, _windowDeviceContext, Left, Top, Width, Height, _pixelOperations))
                return FrameCount;

            CaptureCursor();

            //Set frame details.
            FrameCount++;

            frame.Ticks = Stopwatch.GetElapsedTicks();
            frame.Delay = Stopwatch.GetMillisecondsAsLong();
            frame.Pixels = new byte[_byteLength];

            if (Gdi32.GetDIBits(_windowDeviceContext, _compatibleBitmap, 0, (uint)StartHeight, frame.Pixels, ref _bitmapHeader, DibColorModes.RgbColors) == 0)
                frame.WasFrameSkipped = true;

            if (IsAcceptingFrames)
                FrameCollection.Add(frame);
        }
        catch (Exception)
        {
            //LogWriter.Log(ex, "Impossible to get the screenshot of the screen");
        }

        return FrameCount;
    }
    
    public override void Save(RecordingFrame info)
    {
        if (UserSettings.All.PreventBlackFrames && info.Pixels != null && !info.WasFrameSkipped && info.Pixels[0] == 0)
        {
            if (!info.Pixels.Any(a => a > 0))
                info.WasFrameSkipped = true;
        }

        //If the frame skipped, just increase the delay to the previous frame.
        if (info.WasFrameSkipped || info.Pixels == null)
        {
            info.Pixels = null;

            //Pass the duration to the previous frame, if any.
            if (Project.Frames.Count > 0)
                Project.Frames[^1].Delay += info.Delay;

            return;
        }

        CompressStream.WriteByte(1); //1 byte, Frame event type.
        CompressStream.WriteInt64(info.Ticks); //8 bytes.
        CompressStream.WriteInt64(info.Delay); //8 bytes.
        CompressStream.WriteInt64(info.Pixels.LongLength); //8 bytes.
        CompressStream.WriteBytes(info.Pixels);

        info.PixelsLength = (ulong) info.Pixels.LongLength;
        info.Pixels = null;

        Project.Frames.Add(info);
    }

    public override async Task Stop()
    {
        if (!WasFrameCaptureStarted)
            return;

        //Stop the recording first.
        await base.Stop();

        //Release resources.
        try
        {
            Gdi32.SelectObject(_compatibleDeviceContext, _oldBitmap);
            Gdi32.DeleteObject(_compatibleBitmap);
            Gdi32.DeleteDC(_compatibleDeviceContext);
            User32.ReleaseDC(_desktopWindow, _windowDeviceContext);
        }
        catch (Exception e)
        {
            LogWriter.Log(e, "Impossible to stop and clean resources used by the recording.");
        }
    }

    private void CaptureCursor()
    {
        #region Get cursor details

        //ReSharper disable once RedundantAssignment, disable once InlineOutVariableDeclaration
        var cursorInfo = new CursorInfo(false);

        if (!User32.GetCursorInfo(out cursorInfo))
            return;

        if (cursorInfo.Flags != ScreenToGif.Native.Constants.CursorShowing)
        {
            Gdi32.DeleteObject(cursorInfo.CursorHandle);
            return;
        }

        var iconHandle = User32.CopyIcon(cursorInfo.CursorHandle);

        if (iconHandle == IntPtr.Zero)
        {
            Gdi32.DeleteObject(cursorInfo.CursorHandle);
            return;
        }

        if (!User32.GetIconInfo(iconHandle, out var iconInfo))
        {
            User32.DestroyIcon(iconHandle);
            Gdi32.DeleteObject(cursorInfo.CursorHandle);
            return;
        }

        //var iconInfoEx = new IconInfoEx();
        //iconInfoEx.cbSize = (uint)Marshal.SizeOf(iconInfoEx);

        //if (!User32.GetIconInfoEx(iconHandle, ref iconInfoEx))
        //{

        //}

        #endregion

        try
        {
            //Color.
            var colorHeader = new BitmapInfoHeader(false);

            Gdi32.GetDIBits(_windowDeviceContext, iconInfo.Color, 0, 0, null, ref colorHeader, DibColorModes.RgbColors);

            if (colorHeader.Height != 0)
            {
                //Create bitmap.
                var compatibleBitmap = Gdi32.CreateCompatibleBitmap(_windowDeviceContext, colorHeader.Width, colorHeader.Height);
                var oldBitmap = Gdi32.SelectObject(_compatibleDeviceContext, compatibleBitmap);

                //Draw image.
                var ok = User32.DrawIconEx(_compatibleDeviceContext, 0, 0, cursorInfo.CursorHandle, 0, 0, _cursorStep, IntPtr.Zero, DrawIconFlags.Image);

                if (!ok)
                {
                    _cursorStep = 0;
                    User32.DrawIconEx(_compatibleDeviceContext, 0, 0, cursorInfo.CursorHandle, 0, 0, _cursorStep, IntPtr.Zero, DrawIconFlags.Normal);
                }
                else
                    _cursorStep++;

                colorHeader.Height *= -1;
                var colorBuffer = new byte[colorHeader.SizeImage];

                Gdi32.GetDIBits(_windowDeviceContext, compatibleBitmap, 0, (uint)(colorHeader.Height * -1), colorBuffer, ref colorHeader, DibColorModes.RgbColors);

                RegisterCursorDataEvent(2, colorBuffer, colorHeader.Height, colorHeader.Width, cursorInfo.ScreenPosition.X - Left, cursorInfo.ScreenPosition.Y - Top, iconInfo.XHotspot, iconInfo.YHotspot);

                //Erase bitmaps.
                Gdi32.SelectObject(_compatibleDeviceContext, oldBitmap);
                Gdi32.DeleteObject(compatibleBitmap);
                return;
            }

            //Mask.
            var maskHeader = new BitmapInfoHeader(false);

            Gdi32.GetDIBits(_windowDeviceContext, iconInfo.Mask, 0, 0, null, ref maskHeader, DibColorModes.RgbColors);

            //Ignore masks with square size (such as 32x32), since they are useless. When that happens, use just the color bitmap.
            if (maskHeader.Height - maskHeader.Width <= 0)
                return;

            var maskBuffer = new byte[maskHeader.SizeImage];

            maskHeader.Height *= -1;
            Gdi32.GetDIBits(_windowDeviceContext, iconInfo.Mask, 0, (uint)maskHeader.Height, maskBuffer, ref maskHeader, DibColorModes.RgbColors);

            RegisterCursorDataEvent(1, maskBuffer, maskHeader.Height, maskHeader.Width, cursorInfo.ScreenPosition.X - Left, cursorInfo.ScreenPosition.Y - Top, iconInfo.XHotspot, iconInfo.YHotspot);
        }
        catch (Exception e)
        {
            LogWriter.Log(e, "Impossible to get the cursor");
        }
        finally
        {
            Gdi32.DeleteObject(iconInfo.Color);
            Gdi32.DeleteObject(iconInfo.Mask);
            User32.DestroyIcon(iconHandle);
            Gdi32.DeleteObject(cursorInfo.CursorHandle);
        }
    }
}