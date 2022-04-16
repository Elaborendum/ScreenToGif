using ScreenToGif.Domain.Exceptions;
using ScreenToGif.Domain.Models.Project.Recording;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using System.Runtime.InteropServices;
using System.Windows;
using Device = SharpDX.Direct3D11.Device;
using MapFlags = SharpDX.Direct3D11.MapFlags;

namespace ScreenToGif.Util.Capture;

/// <summary>
/// Frame capture using the DesktopDuplication API.
/// Adapted from:
/// https://github.com/ajorkowski/VirtualSpace
/// https://github.com/Microsoft/Windows-classic-samples/blob/master/Samples/DXGIDesktopDuplication
///
/// How to debug:
/// https://walbourn.github.io/dxgi-debug-device/
/// https://walbourn.github.io/direct3d-sdk-debug-layer-tricks/
/// https://devblogs.microsoft.com/cppblog/visual-studio-2015-and-graphics-tools-for-windows-10/
/// </summary>
public class DirectCapture : ScreenCapture
{
    #region Variables

    /// <summary>
    /// The current device being duplicated.
    /// </summary>
    protected internal Device Device;

    /// <summary>
    /// The desktop duplication interface.
    /// </summary>
    protected internal OutputDuplication DuplicatedOutput;

    /// <summary>
    /// The rotation of the screen.
    /// </summary>
    protected internal DisplayModeRotation DisplayRotation;

    /// <summary>
    /// The texture used to copy the pixel data from the desktop to the destination image. 
    /// </summary>
    protected internal Texture2D StagingTexture;

    /// <summary>
    /// The texture used exclusively to be a backing texture when capturing the cursor shape.
    /// This texture will always hold only the desktop texture, without the cursor.
    /// </summary>
    protected internal Texture2D BackingTexture;

    /// <summary>
    /// The texture used exclusively to be a backing texture when capturing screens which are rotated.
    /// </summary>
    protected internal Texture2D TransformTexture;

    /// <summary>
    /// Texture used to merge the cursor with the background image (desktop).
    /// </summary>
    protected internal Texture2D CursorStagingTexture;

    /// <summary>
    /// The buffer that holds all pixel data of the cursor.
    /// </summary>
    protected internal byte[] CursorShapeBuffer;

    /// <summary>
    /// The details of the cursor.
    /// </summary>
    protected internal OutputDuplicatePointerShapeInformation CursorShapeInfo;

    /// <summary>
    /// The previous position of the mouse cursor.
    /// </summary>
    protected internal OutputDuplicatePointerPosition PreviousPosition;

    /// <summary>
    /// The latest time in which a frame or metadata was captured.
    /// </summary>
    protected internal long LastProcessTime = 0;

    protected internal int OffsetLeft { get; set; }
    protected internal int OffsetTop { get; set; }
    protected internal int TrueLeft => Left + OffsetLeft;
    protected internal int TrueRight => Left + OffsetLeft + Width;
    protected internal int TrueTop => Top + OffsetTop;
    protected internal int TrueBottom => Top + OffsetTop + Height;

    /// <summary>
    /// Flag that holds the information wheter the previous capture had a major crash.
    /// </summary>
    protected internal bool MajorCrashHappened = false;

    #endregion

    public override void Start(int delay, int left, int top, int width, int height, double dpi, RecordingProject project)
    {
        base.Start(delay, left, top, width, height, dpi, project);

        //Only set as Started after actually finishing starting.
        WasFrameCaptureStarted = false;

        Initialize();

        WasFrameCaptureStarted = true;
    }

    public override void ResetConfiguration()
    {
        ExtraDisposeInternal();
        Initialize();
    }

    internal void Initialize()
    {
        MajorCrashHappened = false;

#if DEBUG
        Device = new Device(DriverType.Hardware, DeviceCreationFlags.Debug);

        var debug = SharpDX.DXGI.InfoQueue.TryCreate();
        debug?.SetBreakOnSeverity(DebugId.All, InformationQueueMessageSeverity.Corruption, true);
        debug?.SetBreakOnSeverity(DebugId.All, InformationQueueMessageSeverity.Error, true);
        debug?.SetBreakOnSeverity(DebugId.All, InformationQueueMessageSeverity.Warning, true);

        var debug2 = DXGIDebug.TryCreate();
        debug2?.ReportLiveObjects(DebugId.Dx, DebugRloFlags.Summary | DebugRloFlags.Detail);

#else
            Device = new Device(DriverType.Hardware, DeviceCreationFlags.VideoSupport);
#endif

        using (var multiThread = Device.QueryInterface<Multithread>())
            multiThread.SetMultithreadProtected(true);

        //Texture used to copy contents from the GPU to be accesible by the CPU.
        StagingTexture = new Texture2D(Device, new Texture2DDescription
        {
            ArraySize = 1,
            BindFlags = BindFlags.None,
            CpuAccessFlags = CpuAccessFlags.Read,
            Format = Format.B8G8R8A8_UNorm,
            Width = Width,
            Height = Height,
            OptionFlags = ResourceOptionFlags.None,
            MipLevels = 1,
            SampleDescription = new SampleDescription(1, 0),
            Usage = ResourceUsage.Staging
        });

        //Texture that is used to recieve the pixel data from the GPU.
        BackingTexture = new Texture2D(Device, new Texture2DDescription
        {
            ArraySize = 1,
            BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
            CpuAccessFlags = CpuAccessFlags.None,
            Format = Format.B8G8R8A8_UNorm,
            Width = Width,
            Height = Height,
            OptionFlags = ResourceOptionFlags.None,
            MipLevels = 1,
            SampleDescription = new SampleDescription(1, 0),
            Usage = ResourceUsage.Default
        });

        using (var factory = new Factory1())
        {
            //Get the Output1 based on the current capture region position.
            using (var output1 = GetOutput(factory))
            {
                try
                {
                    //Make sure to run with the integrated graphics adapter if using a Microsoft hybrid system. https://stackoverflow.com/a/54196789/1735672
                    DuplicatedOutput = output1.DuplicateOutput(Device);
                }
                catch (SharpDXException e) when (e.Descriptor == SharpDX.DXGI.ResultCode.NotCurrentlyAvailable)
                {
                    throw new Exception("Too many applications using the Desktop Duplication API. Please close one of the applications and try again.", e);
                }
                catch (SharpDXException e) when (e.Descriptor == SharpDX.DXGI.ResultCode.Unsupported)
                {
                    throw new GraphicsConfigurationException("The Desktop Duplication API is not supported on this computer.", e);
                }
                catch (SharpDXException e) when (e.Descriptor == SharpDX.DXGI.ResultCode.InvalidCall)
                {
                    throw new GraphicsConfigurationException("The Desktop Duplication API is not supported on this screen.", e);
                }
                catch (SharpDXException e) when (e.Descriptor.NativeApiCode == "E_INVALIDARG")
                {
                    throw new GraphicsConfigurationException("Looks like that the Desktop Duplication API is not supported on this screen.", e);
                }
            }
        }
    }

    /// <summary>
    /// Get the correct Output1 based on region to be captured.
    /// </summary>
    private Output1 GetOutput(Factory1 factory)
    {
        try
        {
            //Gets the output with the bigger area being intersected.
            var output = factory.Adapters1.SelectMany(s => s.Outputs).FirstOrDefault(f => f.Description.DeviceName == DeviceName) ??
                         factory.Adapters1.SelectMany(s => s.Outputs).OrderByDescending(f =>
                         {
                             var x = Math.Max(Left, f.Description.DesktopBounds.Left);
                             var num1 = Math.Min(Left + Width, f.Description.DesktopBounds.Right);
                             var y = Math.Max(Top, f.Description.DesktopBounds.Top);
                             var num2 = Math.Min(Top + Height, f.Description.DesktopBounds.Bottom);

                             if (num1 >= x && num2 >= y)
                                 return num1 - x + num2 - y;

                             return 0;
                         }).FirstOrDefault();

            if (output == null)
                throw new Exception($"Could not find a proper output device for the area of L: {Left}, T: {Top}, Width: {Width}, Height: {Height}.");

            //Position adjustments, so the correct region is captured.
            OffsetLeft = output.Description.DesktopBounds.Left;
            OffsetTop = output.Description.DesktopBounds.Top;
            DisplayRotation = output.Description.Rotation;

            if (DisplayRotation != DisplayModeRotation.Identity)
            {
                //Texture that is used to recieve the pixel data from the GPU.
                TransformTexture = new Texture2D(Device, new Texture2DDescription
                {
                    ArraySize = 1,
                    BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                    CpuAccessFlags = CpuAccessFlags.None,
                    Format = Format.B8G8R8A8_UNorm,
                    Width = Height,
                    Height = Width,
                    OptionFlags = ResourceOptionFlags.None,
                    MipLevels = 1,
                    SampleDescription = new SampleDescription(1, 0),
                    Usage = ResourceUsage.Default
                });
            }

            //Create textures in here, after detecting the orientation?

            return output.QueryInterface<Output1>();
        }
        catch (SharpDXException ex)
        {
            throw new Exception("Could not find the specified output device.", ex);
        }
    }

    public override int Capture(RecordingFrame frame)
    {
        var res = new Result(-1);

        try
        {
            //Try to get the duplicated output frame within given time.
            res = DuplicatedOutput.TryAcquireNextFrame(0, out var info, out var resource);

            if (FrameCount == 0 && (res.Failure || resource == null))
            {
                //Somehow, it was not possible to retrieve the resource, frame or metadata.
                resource?.Dispose();
                return FrameCount;
            }

            #region Process changes

            //Something on screen was moved or changed.
            if (info.TotalMetadataBufferSize > 0)
            {
                //Copy resource into memory that can be accessed by the CPU.
                using (var screenTexture = resource.QueryInterface<Texture2D>())
                {
                    #region Moved rectangles

                    var movedRectangles = new OutputDuplicateMoveRectangle[info.TotalMetadataBufferSize];
                    DuplicatedOutput.GetFrameMoveRects(movedRectangles.Length, movedRectangles, out var movedRegionsLength);

                    for (var movedIndex = 0; movedIndex < movedRegionsLength / Marshal.SizeOf(typeof(OutputDuplicateMoveRectangle)); movedIndex++)
                    {
                        //Crop the destination rectangle to the screen area rectangle.
                        var left = Math.Max(movedRectangles[movedIndex].DestinationRect.Left, Left - OffsetLeft);
                        var right = Math.Min(movedRectangles[movedIndex].DestinationRect.Right, Left + Width - OffsetLeft);
                        var top = Math.Max(movedRectangles[movedIndex].DestinationRect.Top, Top - OffsetTop);
                        var bottom = Math.Min(movedRectangles[movedIndex].DestinationRect.Bottom, Top + Height - OffsetTop);

                        //Copies from the screen texture only the area which the user wants to capture.
                        if (right > left && bottom > top)
                        {
                            //Limit the source rectangle to the available size within the destination rectangle.
                            var sourceWidth = movedRectangles[movedIndex].SourcePoint.X + (right - left);
                            var sourceHeight = movedRectangles[movedIndex].SourcePoint.Y + (bottom - top);

                            Device.ImmediateContext.CopySubresourceRegion(screenTexture, 0,
                                new ResourceRegion(movedRectangles[movedIndex].SourcePoint.X, movedRectangles[movedIndex].SourcePoint.Y, 0, sourceWidth, sourceHeight, 1),
                                StagingTexture, 0, left - (Left - OffsetLeft), top - (Top - OffsetTop));
                        }
                    }

                    #endregion

                    #region Dirty rectangles

                    var dirtyRectangles = new RawRectangle[info.TotalMetadataBufferSize];
                    DuplicatedOutput.GetFrameDirtyRects(dirtyRectangles.Length, dirtyRectangles, out var dirtyRegionsLength);

                    for (var dirtyIndex = 0; dirtyIndex < dirtyRegionsLength / Marshal.SizeOf(typeof(RawRectangle)); dirtyIndex++)
                    {
                        //Crop screen positions and size to frame sizes.
                        var left = Math.Max(dirtyRectangles[dirtyIndex].Left, Left - OffsetLeft);
                        var right = Math.Min(dirtyRectangles[dirtyIndex].Right, Left + Width - OffsetLeft);
                        var top = Math.Max(dirtyRectangles[dirtyIndex].Top, Top - OffsetTop);
                        var bottom = Math.Min(dirtyRectangles[dirtyIndex].Bottom, Top + Height - OffsetTop);

                        //Copies from the screen texture only the area which the user wants to capture.
                        if (right > left && bottom > top)
                            Device.ImmediateContext.CopySubresourceRegion(screenTexture, 0, new ResourceRegion(left, top, 0, right, bottom, 1), StagingTexture, 0, left - (Left - OffsetLeft), top - (Top - OffsetTop));
                    }

                    #endregion
                }
            }

            #endregion

            #region Gets the image data

            //Gets the staging texture as a stream.
            var data = Device.ImmediateContext.MapSubresource(StagingTexture, 0, MapMode.Read, MapFlags.None, out var stream);

            if (data.IsEmpty)
            {
                Device.ImmediateContext.UnmapSubresource(StagingTexture, 0);
                stream?.Dispose();
                resource?.Dispose();
                return FrameCount;
            }

            //Set frame details.
            FrameCount++;

            frame.Ticks = Stopwatch.GetElapsedTicks();
            frame.Delay = Stopwatch.GetMilliseconds();
            frame.Pixels = new byte[stream.Length];

            //BGRA32 is 4 bytes.
            for (var height = 0; height < Height; height++)
            {
                stream.Position = height * data.RowPitch;
                Marshal.Copy(new IntPtr(stream.DataPointer.ToInt64() + height * data.RowPitch), frame.Pixels, height * Width * 4, Width * 4);
            }

            if (IsAcceptingFrames)
                FrameCollection.Add(frame);

            #endregion

            Device.ImmediateContext?.UnmapSubresource(StagingTexture, 0);

            resource?.Dispose();
            return FrameCount;
        }
        catch (SharpDXException se) when (se.ResultCode.Code == SharpDX.DXGI.ResultCode.WaitTimeout.Result.Code)
        {
            return FrameCount;
        }
        catch (SharpDXException se) when (se.ResultCode.Code == SharpDX.DXGI.ResultCode.DeviceRemoved.Result.Code || se.ResultCode.Code == SharpDX.DXGI.ResultCode.DeviceReset.Result.Code)
        {
            //When the device gets lost or reset, the resources should be instantiated again.
            ExtraDisposeInternal();
            Initialize();

            return FrameCount;
        }
        catch (Exception ex)
        {
            LogWriter.Log(ex, "It was not possible to finish capturing the frame with DirectX.");

            MajorCrashHappened = true;

            if (IsAcceptingFrames)
                Application.Current.Dispatcher.Invoke(() => OnError.Invoke(ex));

            return FrameCount;
        }
        finally
        {
            try
            {
                //Only release the frame if there was a sucess in capturing it.
                if (res.Success)
                    DuplicatedOutput.ReleaseFrame();
            }
            catch (Exception e)
            {
                LogWriter.Log(e, "It was not possible to release the frame.");
            }
        }
    }

    public override int CaptureWithCursor(RecordingFrame frame)
    {
        var res = new Result(-1);

        try
        {
            //Try to get the duplicated output frame within given time.
            res = DuplicatedOutput.TryAcquireNextFrame(0, out var info, out var resource);

            //Checks how to proceed with the capture. It could have failed, or the screen, cursor or both could have been captured.
            if (FrameCount == 0 && info.LastMouseUpdateTime == 0 && (res.Failure || resource == null))
            {
                //Somehow, it was not possible to retrieve the resource, frame or metadata.
                resource?.Dispose();
                return FrameCount;
            }
            else if (FrameCount == 0 && info.TotalMetadataBufferSize == 0 && info.LastMouseUpdateTime > 0)
            {
                //Sometimes, the first frame has cursor info, but no screen changes.
                GetCursor(null, info, frame);

                resource?.Dispose();
                return FrameCount;
            }

            #region Process changes

            //Something on screen was moved or changed.
            if (info.TotalMetadataBufferSize > 0)
            {
                //Copies the screen data into memory that can be accessed by the CPU.
                using (var screenTexture = resource.QueryInterface<Texture2D>())
                {
                    #region Moved rectangles

                    var movedRectangles = new OutputDuplicateMoveRectangle[info.TotalMetadataBufferSize];
                    DuplicatedOutput.GetFrameMoveRects(movedRectangles.Length, movedRectangles, out var movedRegionsLength);

                    for (var movedIndex = 0; movedIndex < movedRegionsLength / Marshal.SizeOf(typeof(OutputDuplicateMoveRectangle)); movedIndex++)
                    {
                        //Crop the destination rectangle to the screen area rectangle.
                        var left = Math.Max(movedRectangles[movedIndex].DestinationRect.Left, Left - OffsetLeft);
                        var right = Math.Min(movedRectangles[movedIndex].DestinationRect.Right, Left + Width - OffsetLeft);
                        var top = Math.Max(movedRectangles[movedIndex].DestinationRect.Top, Top - OffsetTop);
                        var bottom = Math.Min(movedRectangles[movedIndex].DestinationRect.Bottom, Top + Height - OffsetTop);

                        //Copies from the screen texture only the area which the user wants to capture.
                        if (right > left && bottom > top)
                        {
                            //Limit the source rectangle to the available size within the destination rectangle.
                            var sourceWidth = movedRectangles[movedIndex].SourcePoint.X + (right - left);
                            var sourceHeight = movedRectangles[movedIndex].SourcePoint.Y + (bottom - top);

                            Device.ImmediateContext.CopySubresourceRegion(screenTexture, 0,
                                new ResourceRegion(movedRectangles[movedIndex].SourcePoint.X, movedRectangles[movedIndex].SourcePoint.Y, 0, sourceWidth, sourceHeight, 1),
                                BackingTexture, 0, left - (Left - OffsetLeft), top - (Top - OffsetTop));
                        }
                    }

                    #endregion

                    #region Dirty rectangles

                    var dirtyRectangles = new RawRectangle[info.TotalMetadataBufferSize];
                    DuplicatedOutput.GetFrameDirtyRects(dirtyRectangles.Length, dirtyRectangles, out var dirtyRegionsLength);

                    for (var dirtyIndex = 0; dirtyIndex < dirtyRegionsLength / Marshal.SizeOf(typeof(RawRectangle)); dirtyIndex++)
                    {
                        //Crop screen positions and size to frame sizes.
                        var left = Math.Max(dirtyRectangles[dirtyIndex].Left, Left - OffsetLeft);
                        var right = Math.Min(dirtyRectangles[dirtyIndex].Right, Left + Width - OffsetLeft);
                        var top = Math.Max(dirtyRectangles[dirtyIndex].Top, Top - OffsetTop);
                        var bottom = Math.Min(dirtyRectangles[dirtyIndex].Bottom, Top + Height - OffsetTop);

                        //TODO: Add support for rotate screens.
                        //int left, right, top, bottom;
                        //switch (DisplayRotation)
                        //{
                        //    case DisplayModeRotation.Rotate90:
                        //    {
                        //        left = Math.Max(dirtyRectangles[dirtyIndex].Left, Left - OffsetLeft);
                        //        right = Math.Min(dirtyRectangles[dirtyIndex].Right, Left + Width - OffsetLeft);
                        //        top = Math.Max(dirtyRectangles[dirtyIndex].Top, Top - OffsetTop);
                        //        bottom = Math.Min(dirtyRectangles[dirtyIndex].Bottom, Top + Height - OffsetTop);

                        //        //left = Math.Min(dirtyRectangles[dirtyIndex].Bottom, Top + Height - OffsetTop); 
                        //        //right = Math.Max(dirtyRectangles[dirtyIndex].Top, Top - OffsetTop);
                        //        //top = Math.Max(dirtyRectangles[dirtyIndex].Left, Left - OffsetLeft);
                        //        //bottom = Math.Min(dirtyRectangles[dirtyIndex].Right, Left + Width - OffsetLeft);

                        //        break;
                        //    }

                        //    case DisplayModeRotation.Rotate180:
                        //    {
                        //        left = Math.Max(dirtyRectangles[dirtyIndex].Top + OffsetTop, Top); 
                        //        right = Math.Min(dirtyRectangles[dirtyIndex].Bottom + OffsetTop, Top + Height); 
                        //        top = Math.Min(dirtyRectangles[dirtyIndex].Right + OffsetLeft, Left + Width);
                        //        bottom = Math.Max(dirtyRectangles[dirtyIndex].Left + OffsetLeft, Left); 
                        //        break;
                        //    }

                        //    default:
                        //    {
                        //        //In this context, the screen positions are relative to the current screen, not to the whole set of screens (virtual space).
                        //        left = Math.Max(dirtyRectangles[dirtyIndex].Left, Left - OffsetLeft);
                        //        right = Math.Min(dirtyRectangles[dirtyIndex].Right, Left + Width - OffsetLeft);
                        //        top = Math.Max(dirtyRectangles[dirtyIndex].Top, Top - OffsetTop);
                        //        bottom = Math.Min(dirtyRectangles[dirtyIndex].Bottom, Top + Height - OffsetTop);
                        //        break;
                        //    }
                        //}

                        //Copies from the screen texture only the area which the user wants to capture.
                        if (right > left && bottom > top)
                            Device.ImmediateContext.CopySubresourceRegion(screenTexture, 0, new ResourceRegion(left, top, 0, right, bottom, 1), BackingTexture, 0, left - (Left - OffsetLeft), top - (Top - OffsetTop));
                    }

                    #endregion
                }
            }

            if (info.TotalMetadataBufferSize > 0 || info.LastMouseUpdateTime > 0)
            {
                //Copy the captured desktop texture into a staging texture, in order to show the mouse cursor and not make the captured texture dirty with it.
                //Device.ImmediateContext.CopyResource(BackingTexture, StagingTexture);

                //Gets the cursor image and merges with the staging texture.
                GetCursor(StagingTexture, info, frame);
            }

            //Saves the most recent capture time.
            LastProcessTime = Math.Max(info.LastPresentTime, info.LastMouseUpdateTime);

            #endregion

            #region Gets the image data

            //Gets the staging texture as a stream.
            var data = Device.ImmediateContext.MapSubresource(BackingTexture, 0, MapMode.Read, MapFlags.None, out var stream);

            if (data.IsEmpty)
            {
                Device.ImmediateContext.UnmapSubresource(StagingTexture, 0);
                stream?.Dispose();
                resource?.Dispose();
                return FrameCount;
            }

            //Sets the frame details.
            FrameCount++;

            frame.Ticks = Stopwatch.GetElapsedTicks();
            frame.Delay = Stopwatch.GetMilliseconds();
            frame.Pixels = new byte[stream.Length];

            //BGRA32 is 4 bytes.
            for (var height = 0; height < Height; height++)
            {
                stream.Position = height * data.RowPitch;
                Marshal.Copy(new IntPtr(stream.DataPointer.ToInt64() + height * data.RowPitch), frame.Pixels, height * Width * 4, Width * 4);
            }

            if (IsAcceptingFrames)
                FrameCollection.Add(frame);

            #endregion

            Device.ImmediateContext?.UnmapSubresource(StagingTexture, 0);
            stream.Dispose();
            resource?.Dispose();

            return FrameCount;
        }
        catch (SharpDXException se) when (se.ResultCode.Code == SharpDX.DXGI.ResultCode.WaitTimeout.Result.Code)
        {
            return FrameCount;
        }
        catch (SharpDXException se) when (se.ResultCode.Code == SharpDX.DXGI.ResultCode.DeviceRemoved.Result.Code || se.ResultCode.Code == SharpDX.DXGI.ResultCode.DeviceReset.Result.Code)
        {
            //When the device gets lost or reset, the resources should be instantiated again.
            ExtraDisposeInternal();
            Initialize();

            return FrameCount;
        }
        catch (Exception ex)
        {
            LogWriter.Log(ex, "It was not possible to finish capturing the frame with DirectX.");

            MajorCrashHappened = true;

            if (IsAcceptingFrames)
                Application.Current.Dispatcher.Invoke(() => OnError.Invoke(ex));

            return FrameCount;
        }
        finally
        {
            try
            {
                //Only release the frame if there was a sucess in capturing it.
                if (res.Success)
                    DuplicatedOutput.ReleaseFrame();
            }
            catch (Exception e)
            {
                LogWriter.Log(e, "It was not possible to release the frame.");
            }
        }
    }

    public override int ManualCapture(RecordingFrame frame, bool showCursor = false)
    {
        var res = new Result(-1);

        try
        {
            //Try to get the duplicated output frame within given time.
            res = DuplicatedOutput.TryAcquireNextFrame(1000, out var info, out var resource);

            //Checks how to proceed with the capture. It could have failed, or the screen, cursor or both could have been captured.
            if (res.Failure || resource == null || (!showCursor && info.AccumulatedFrames == 0) || (showCursor && info.AccumulatedFrames == 0 && info.LastMouseUpdateTime <= LastProcessTime))
            {
                //Somehow, it was not possible to retrieve the resource, frame or metadata.
                resource?.Dispose();
                return FrameCount;
            }
            else if (showCursor && info.AccumulatedFrames == 0 && info.LastMouseUpdateTime > LastProcessTime)
            {
                //Gets the cursor shape if the screen hasn't changed in between, so the cursor will be available for the next frame.
                GetCursor(null, info, frame);

                resource.Dispose();
                return FrameCount;

                //TODO: if only the mouse changed, but there's no frame accumulated, but there's data in the texture from the previous frame, I need to merge with the cursor and add to the list. 
            }

            //Saves the most recent capture time.
            LastProcessTime = Math.Max(info.LastPresentTime, info.LastMouseUpdateTime);

            //Copy resource into memory that can be accessed by the CPU.
            using (var screenTexture = resource.QueryInterface<Texture2D>())
            {
                if (showCursor)
                {
                    //Copies from the screen texture only the area which the user wants to capture.
                    Device.ImmediateContext.CopySubresourceRegion(screenTexture, 0, new ResourceRegion(TrueLeft, TrueTop, 0, TrueRight, TrueBottom, 1), BackingTexture, 0);

                    //Copy the captured desktop texture into a staging texture, in order to show the mouse cursor and not make the captured texture dirty with it.
                    Device.ImmediateContext.CopyResource(BackingTexture, StagingTexture);

                    //Gets the cursor image and merges with the staging texture.
                    GetCursor(StagingTexture, info, frame);
                }
                else
                {
                    //Copies from the screen texture only the area which the user wants to capture.
                    Device.ImmediateContext.CopySubresourceRegion(screenTexture, 0, new ResourceRegion(TrueLeft, TrueTop, 0, TrueRight, TrueBottom, 1), StagingTexture, 0);
                }
            }

            //Get the desktop capture texture.
            var data = Device.ImmediateContext.MapSubresource(StagingTexture, 0, MapMode.Read, MapFlags.None, out var stream);

            if (data.IsEmpty)
            {
                Device.ImmediateContext.UnmapSubresource(StagingTexture, 0);
                stream?.Dispose();
                resource.Dispose();
                return FrameCount;
            }

            #region Get image data

            //Set frame details.
            FrameCount++;

            frame.Ticks = Stopwatch.GetElapsedTicks();
            frame.Delay = Stopwatch.GetMilliseconds();
            frame.Pixels = new byte[stream.Length];

            //BGRA32 is 4 bytes.
            for (var height = 0; height < Height; height++)
            {
                stream.Position = height * data.RowPitch;
                Marshal.Copy(new IntPtr(stream.DataPointer.ToInt64() + height * data.RowPitch), frame.Pixels, height * Width * 4, Width * 4);
            }

            FrameCollection.Add(frame);

            #endregion

            Device.ImmediateContext.UnmapSubresource(StagingTexture, 0);
            stream.Dispose();
            resource.Dispose();

            return FrameCount;
        }
        catch (SharpDXException se) when (se.ResultCode.Code == SharpDX.DXGI.ResultCode.WaitTimeout.Result.Code)
        {
            return FrameCount;
        }
        catch (SharpDXException se) when (se.ResultCode.Code == SharpDX.DXGI.ResultCode.DeviceRemoved.Result.Code || se.ResultCode.Code == SharpDX.DXGI.ResultCode.DeviceReset.Result.Code)
        {
            //When the device gets lost or reset, the resources should be instantiated again.
            ExtraDisposeInternal();
            Initialize();

            return FrameCount;
        }
        catch (Exception ex)
        {
            LogWriter.Log(ex, "It was not possible to finish capturing the frame with DirectX.");

            MajorCrashHappened = true;

            if (IsAcceptingFrames)
                Application.Current.Dispatcher.Invoke(() => OnError.Invoke(ex));

            return FrameCount;
        }
        finally
        {
            try
            {
                //Only release the frame if there was a sucess in capturing it.
                if (res.Success)
                    DuplicatedOutput.ReleaseFrame();
            }
            catch (Exception e)
            {
                LogWriter.Log(e, "It was not possible to release the frame.");
            }
        }
    }

    protected internal bool GetCursor(Texture2D screenTexture, OutputDuplicateFrameInformation info, RecordingFrame frame)
    {
        //Prepare buffer array to hold the cursor shape.
        if (CursorShapeBuffer == null || info.PointerShapeBufferSize > CursorShapeBuffer.Length)
            CursorShapeBuffer = new byte[info.PointerShapeBufferSize];

        //If there's a cursor shape available to be captured.
        if (info.PointerShapeBufferSize > 0)
        {
            //Pin the buffer in order to pass the address as parameter later.
            var pinnedBuffer = GCHandle.Alloc(CursorShapeBuffer, GCHandleType.Pinned);
            var cursorBufferAddress = pinnedBuffer.AddrOfPinnedObject();

            //Load the cursor shape into the buffer.
            DuplicatedOutput.GetFramePointerShape(info.PointerShapeBufferSize, cursorBufferAddress, out _, out CursorShapeInfo);

            //If the cursor is monochrome, it will return the cursor shape twice, one is the mask.
            if (CursorShapeInfo.Type == 1)
                CursorShapeInfo.Height /= 2;

            //The buffer must be unpinned, to free resources.
            pinnedBuffer.Free();
        }

        //Store the current cursor position, if it was moved.
        if (info.LastMouseUpdateTime != 0)
            PreviousPosition = info.PointerPosition;
        
        //Saves the position of the cursor, so the editor can add the mouse clicks overlay later.
        var cursorX = PreviousPosition.Position.X - (Left - OffsetLeft);
        var cursorY = PreviousPosition.Position.Y - (Top - OffsetTop);

        //If the method is supposed to simply the get the cursor shape no shape was loaded before, there's nothing else to do.
        //if (CursorShapeBuffer?.Length == 0 || (info.LastPresentTime == 0 && info.LastMouseUpdateTime == 0) || !info.PointerPosition.Visible)
        if (screenTexture == null || CursorShapeBuffer?.Length == 0)// || !info.PointerPosition.Visible)
            return false;

        RegisterCursorDataEvent(CursorShapeInfo.Type, CursorShapeBuffer, CursorShapeInfo.Width, CursorShapeInfo.Height, cursorX, cursorY, CursorShapeInfo.HotSpot.X, CursorShapeInfo.HotSpot.Y);
        return true;

        //Don't let it bleed beyond the top-left corner, calculate the dimensions of the portion of the cursor that will appear.
        var leftCut = cursorX;
        var topCut = cursorY;
        var rightCut = screenTexture.Description.Width - (cursorX + CursorShapeInfo.Width);
        var bottomCut = screenTexture.Description.Height - (cursorY + CursorShapeInfo.Height);

        //Adjust to the hotspot offset, so it's possible to add the highlight correctly later.
        cursorX += CursorShapeInfo.HotSpot.X;
        cursorY += CursorShapeInfo.HotSpot.Y;

        //Don't try merging the textures if the cursor is out of bounds.
        if (leftCut + CursorShapeInfo.Width < 1 || topCut + CursorShapeInfo.Height < 1 || rightCut + CursorShapeInfo.Width < 1 || bottomCut + CursorShapeInfo.Height < 1)
            return false;

        var cursorLeft = Math.Max(leftCut, 0);
        var cursorTop = Math.Max(topCut, 0);
        var cursorWidth = leftCut < 0 ? CursorShapeInfo.Width + leftCut : rightCut < 0 ? CursorShapeInfo.Width + rightCut : CursorShapeInfo.Width;
        var cursorHeight = topCut < 0 ? CursorShapeInfo.Height + topCut : bottomCut < 0 ? CursorShapeInfo.Height + bottomCut : CursorShapeInfo.Height;

        //The staging texture must be able to hold all pixels.
        if (CursorStagingTexture == null || CursorStagingTexture.Description.Width != cursorWidth || CursorStagingTexture.Description.Height != cursorHeight)
        {
            //In order to change the size of the texture, I need to instantiate it again with the new size.
            CursorStagingTexture?.Dispose();
            CursorStagingTexture = new Texture2D(Device, new Texture2DDescription
            {
                ArraySize = 1,
                BindFlags = BindFlags.None,
                CpuAccessFlags = CpuAccessFlags.Write,
                Height = cursorHeight,
                Format = Format.B8G8R8A8_UNorm,
                Width = cursorWidth,
                MipLevels = 1,
                OptionFlags = ResourceOptionFlags.None,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Staging
            });
        }

        //The region where the cursor is located is copied to the staging texture to act as the background when dealing with masks and transparency.
        //The cutout must be the exact region needed and it can't overflow. It's not allowed to try to cut outside of the screenTexture region.
        var region = new ResourceRegion
        {
            Left = cursorLeft,
            Top = cursorTop,
            Front = 0,
            Right = cursorLeft + cursorWidth,
            Bottom = cursorTop + cursorHeight,
            Back = 1
        };

        //Copy from the screen the region in which the cursor is located.
        Device.ImmediateContext.CopySubresourceRegion(screenTexture, 0, region, CursorStagingTexture, 0);

        //Get cursor details and draw it to the staging texture.
        DrawCursorShape(CursorStagingTexture, CursorShapeInfo, CursorShapeBuffer, leftCut < 0 ? leftCut * -1 : 0, topCut < 0 ? topCut * -1 : 0, cursorWidth, cursorHeight);

        //Copy back the cursor texture to the screen texture.
        Device.ImmediateContext.CopySubresourceRegion(CursorStagingTexture, 0, null, screenTexture, 0, cursorLeft, cursorTop);

        return true;
    }

    private void DrawCursorShape(Texture2D texture, OutputDuplicatePointerShapeInformation info, byte[] buffer, int leftCut, int topCut, int cursorWidth, int cursorHeight)
    {
        using (var surface = texture.QueryInterface<Surface>())
        {
            //Maps the surface, indicating that the CPU needs access to the data.
            var rect = surface.Map(SharpDX.DXGI.MapFlags.Write);

            //Cursors can be divided into 3 types:
            switch (info.Type)
            {
                //Masked monochrome, a cursor which reacts with the background.
                case (int)OutputDuplicatePointerShapeType.Monochrome:
                    DrawMonochromeCursor(leftCut, topCut, cursorWidth, cursorHeight, rect, info.Pitch, buffer, info.Height);
                    break;

                //Color, a colored cursor which supports transparency.
                case (int)OutputDuplicatePointerShapeType.Color:
                    DrawColorCursor(leftCut, topCut, cursorWidth, cursorHeight, rect, info.Pitch, buffer);
                    break;

                //Masked color, a mix of both previous types.
                case (int)OutputDuplicatePointerShapeType.MaskedColor:
                    DrawMaskedColorCursor(leftCut, topCut, cursorWidth, cursorHeight, rect, info.Pitch, buffer);
                    break;
            }

            surface.Unmap();
        }
    }

    private void DrawMonochromeCursor(int offsetX, int offsetY, int width, int height, DataRectangle rect, int pitch, byte[] buffer, int actualHeight)
    {
        for (var row = offsetY; row < height; row++)
        {
            //128 in binary.
            byte mask = 0x80;

            //Simulate the offset, adjusting the mask.
            for (var off = 0; off < offsetX; off++)
            {
                if (mask == 0x01)
                    mask = 0x80;
                else
                    mask = (byte)(mask >> 1);
            }

            for (var col = offsetX; col < width; col++)
            {
                var pos = (row - offsetY) * rect.Pitch + (col - offsetX) * 4;
                var and = (buffer[row * pitch + col / 8] & mask) == mask; //Mask is take from the first half of the cursor image.
                var xor = (buffer[row * pitch + col / 8 + actualHeight * pitch] & mask) == mask; //Mask is taken from the second half of the cursor image, hence the "+ height * pitch". 

                //Reads current pixel and applies AND and XOR. (AND/XOR ? White : Black)
                Marshal.WriteByte(rect.DataPointer, pos, (byte)((Marshal.ReadByte(rect.DataPointer, pos) & (and ? 255 : 0)) ^ (xor ? 255 : 0)));
                Marshal.WriteByte(rect.DataPointer, pos + 1, (byte)((Marshal.ReadByte(rect.DataPointer, pos + 1) & (and ? 255 : 0)) ^ (xor ? 255 : 0)));
                Marshal.WriteByte(rect.DataPointer, pos + 2, (byte)((Marshal.ReadByte(rect.DataPointer, pos + 2) & (and ? 255 : 0)) ^ (xor ? 255 : 0)));
                Marshal.WriteByte(rect.DataPointer, pos + 3, (byte)((Marshal.ReadByte(rect.DataPointer, pos + 3) & 255) ^ 0));

                //Shifts the mask around until it reaches 1, then resets it back to 128.
                if (mask == 0x01)
                    mask = 0x80;
                else
                    mask = (byte)(mask >> 1);
            }
        }
    }

    private void DrawColorCursor(int offsetX, int offsetY, int width, int height, DataRectangle rect, int pitch, byte[] buffer)
    {
        for (var row = offsetY; row < height; row++)
        {
            for (var col = offsetX; col < width; col++)
            {
                var surfaceIndex = (row - offsetY) * rect.Pitch + (col - offsetX) * 4;
                var bufferIndex = row * pitch + col * 4;
                var alpha = buffer[bufferIndex + 3] + 1;

                if (alpha == 1)
                    continue;

                //Premultiplied alpha values.
                var invAlpha = 256 - alpha;
                alpha += 1;

                Marshal.WriteByte(rect.DataPointer, surfaceIndex, (byte)((alpha * buffer[bufferIndex] + invAlpha * Marshal.ReadByte(rect.DataPointer, surfaceIndex)) >> 8));
                Marshal.WriteByte(rect.DataPointer, surfaceIndex + 1, (byte)((alpha * buffer[bufferIndex + 1] + invAlpha * Marshal.ReadByte(rect.DataPointer, surfaceIndex + 1)) >> 8));
                Marshal.WriteByte(rect.DataPointer, surfaceIndex + 2, (byte)((alpha * buffer[bufferIndex + 2] + invAlpha * Marshal.ReadByte(rect.DataPointer, surfaceIndex + 2)) >> 8));
            }
        }
    }

    private void DrawMaskedColorCursor(int offsetX, int offsetY, int width, int height, DataRectangle rect, int pitch, byte[] buffer)
    {
        //ImageUtil.ImageMethods.SavePixelArrayToFile(buffer, width, height, 4, System.IO.Path.GetFullPath(".\\MaskedColor.png"));

        for (var row = offsetY; row < height; row++)
        {
            for (var col = offsetX; col < width; col++)
            {
                var surfaceIndex = (row - offsetY) * rect.Pitch + (col - offsetX) * 4;
                var bufferIndex = row * pitch + col * 4;
                var maskFlag = buffer[bufferIndex + 3];

                //Just copies the pixel color.
                if (maskFlag == 0)
                {
                    Marshal.WriteByte(rect.DataPointer, surfaceIndex, buffer[bufferIndex]);
                    Marshal.WriteByte(rect.DataPointer, surfaceIndex + 1, buffer[bufferIndex + 1]);
                    Marshal.WriteByte(rect.DataPointer, surfaceIndex + 2, buffer[bufferIndex + 2]);
                    return;
                }

                //Applies the XOR opperation with the current color.
                Marshal.WriteByte(rect.DataPointer, surfaceIndex, (byte)(buffer[bufferIndex] ^ Marshal.ReadByte(rect.DataPointer, surfaceIndex)));
                Marshal.WriteByte(rect.DataPointer, surfaceIndex + 1, (byte)(buffer[bufferIndex + 1] ^ Marshal.ReadByte(rect.DataPointer, surfaceIndex + 1)));
                Marshal.WriteByte(rect.DataPointer, surfaceIndex + 2, (byte)(buffer[bufferIndex + 2] ^ Marshal.ReadByte(rect.DataPointer, surfaceIndex + 2)));
            }
        }
    }

    public override void Save(RecordingFrame info)
    {
        CompressStream.WriteInt64(1); //1 byte, Frame event type.
        CompressStream.WriteInt64(info.Ticks); //8 bytes.
        CompressStream.WriteInt64(info.Delay); //8 bytes.
        CompressStream.WriteInt64(info.Pixels.LongLength); //8 bytes.
        CompressStream.WriteBytes(info.Pixels);

        info.PixelsLength = (ulong)info.Pixels.LongLength;
        info.Pixels = null;

        Project.Frames.Add(info);
    }

    public override async Task Stop()
    {
        if (!WasFrameCaptureStarted)
            return;

        ExtraDisposeInternal();

        await base.Stop();
    }

    internal void ExtraDisposeInternal()
    {
        Device.Dispose();

        if (MajorCrashHappened)
            return;

        BackingTexture.Dispose();
        StagingTexture.Dispose();
        DuplicatedOutput.Dispose();

        CursorStagingTexture?.Dispose();
    }
}