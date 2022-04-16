using ScreenToGif.Domain.Enums.Native;
using ScreenToGif.Native.External;
using ScreenToGif.Native.Helpers;
using System.Drawing;
using System.Windows;
using System.Windows.Media.Imaging;

namespace ScreenToGif.Util.Native
{
    public static class Capture
    {
        /// <summary>
        /// Captures the screen using the SourceCopy | CaptureBlt.
        /// </summary>
        /// <param name="width">The size of the final image.</param>
        /// <param name="height">The size of the final image.</param>
        /// <param name="positionX">Source capture Left position.</param>
        /// <param name="positionY">Source capture Top position.</param>
        /// <returns>A bitmap with the capture rectangle.</returns>
        public static BitmapSource CaptureScreenAsBitmapSource(int width, int height, int positionX, int positionY)
        {
            var hDesk = User32.GetDesktopWindow();
            var hSrce = User32.GetWindowDC(hDesk);
            var hDest = Gdi32.CreateCompatibleDC(hSrce);
            var hBmp = Gdi32.CreateCompatibleBitmap(hSrce, width, height);
            var hOldBmp = Gdi32.SelectObject(hDest, hBmp);

            try
            {
                var b = Gdi32.BitBlt(hDest, 0, 0, width, height, hSrce, positionX, positionY, CopyPixelOperations.SourceCopy | CopyPixelOperations.CaptureBlt);

                //return Image.FromHbitmap(hBmp);
                return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(hBmp, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Impossible to get screenshot of the screen");
            }
            finally
            {
                Gdi32.SelectObject(hDest, hOldBmp);
                Gdi32.DeleteObject(hBmp);
                Gdi32.DeleteDC(hDest);
                User32.ReleaseDC(hDesk, hSrce);
            }

            return null;
        }

        /// <summary>
        /// Captures the screen using the SourceCopy | CaptureBlt.
        /// </summary>
        /// <param name="height">Height of the capture region.</param>
        /// <param name="positionX">Source capture Left position.</param>
        /// <param name="positionY">Source capture Top position.</param>
        /// <param name="width">Width of the capture region.</param>
        /// <returns>A bitmap with the capture rectangle.</returns>
        public static Image CaptureScreenAsBitmap(int width, int height, int positionX, int positionY)
        {
            var hDesk = User32.GetDesktopWindow();
            var hSrce = User32.GetWindowDC(hDesk);
            var hDest = Gdi32.CreateCompatibleDC(hSrce);
            var hBmp = Gdi32.CreateCompatibleBitmap(hSrce, width, height);
            var hOldBmp = Gdi32.SelectObject(hDest, hBmp);

            try
            {
                var b = Gdi32.BitBlt(hDest, 0, 0, width, height, hSrce, positionX, positionY, CopyPixelOperations.SourceCopy | CopyPixelOperations.CaptureBlt);

                return b ? Image.FromHbitmap(hBmp) : null;
            }
            catch (Exception)
            {
                //LogWriter.Log(ex, "Impossible to get screenshot of the screen");
            }
            finally
            {
                Gdi32.SelectObject(hDest, hOldBmp);
                Gdi32.DeleteObject(hBmp);
                Gdi32.DeleteDC(hDest);
                User32.ReleaseDC(hDesk, hSrce);
            }

            return null;
        }

        public static Image CaptureWindow(IntPtr handle, double scale)
        {
            var rectangle = Windows.GetWindowRect(handle);
            var posX = (int)((rectangle.X + Util.Constants.LeftOffset) * scale);
            var posY = (int)((rectangle.Y + Util.Constants.TopOffset) * scale);
            var width = (int)((rectangle.Width - Util.Constants.HorizontalOffset) * scale);
            var height = (int)((rectangle.Height - Util.Constants.VerticalOffset) * scale);

            var hDesk = User32.GetDesktopWindow();
            var hSrce = User32.GetWindowDC(hDesk);
            var hDest = Gdi32.CreateCompatibleDC(hSrce);
            var hBmp = Gdi32.CreateCompatibleBitmap(hSrce, width, height);
            var hOldBmp = Gdi32.SelectObject(hDest, hBmp);

            Gdi32.BitBlt(hDest, 0, 0, width, height, hSrce, posX, posY, CopyPixelOperations.SourceCopy | CopyPixelOperations.CaptureBlt);

            try
            {
                return Image.FromHbitmap(hBmp);
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Impossible to get screenshot of the screen");
            }
            finally
            {
                Gdi32.SelectObject(hDest, hOldBmp);
                Gdi32.DeleteObject(hBmp);
                Gdi32.DeleteDC(hDest);
                User32.ReleaseDC(hDesk, hSrce);
            }

            return null;
        }
    }
}