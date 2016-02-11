﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ScreenToGif.Properties;
using ScreenToGif.Windows.Other;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace ScreenToGif.Util
{
    /// <summary>
    /// Other helper methods.
    /// </summary>
    public static class Other
    {
        static Point TransformToScreen(Point point, Visual relativeTo)
        {
            HwndSource hwndSource = PresentationSource.FromVisual(relativeTo) as HwndSource;
            Visual root = hwndSource.RootVisual;

            // Translate the point from the visual to the root.
            GeneralTransform transformToRoot = relativeTo.TransformToAncestor(root);

            Point pointRoot = transformToRoot.Transform(point);

            // Transform the point from the root to client coordinates.
            Matrix m = Matrix.Identity;

            Transform transform = VisualTreeHelper.GetTransform(root);

            if (transform != null)
            {
                m = Matrix.Multiply(m, transform.Value);
            }

            Vector offset = VisualTreeHelper.GetOffset(root);
            m.Translate(offset.X, offset.Y);

            Point pointClient = m.Transform(pointRoot);

            // Convert from “device-independent pixels” into pixels.
            pointClient = hwndSource.CompositionTarget.TransformToDevice.Transform(pointClient);

            Native.POINT pointClientPixels = new Native.POINT();
            pointClientPixels.x = (0 < pointClient.X) ? (int)(pointClient.X + 0.5) : (int)(pointClient.X - 0.5);
            pointClientPixels.y = (0 < pointClient.Y) ? (int)(pointClient.Y + 0.5) : (int)(pointClient.Y - 0.5);

            // Transform the point into screen coordinates.
            Native.POINT pointScreenPixels = pointClientPixels;
            Native.ClientToScreen(hwndSource.Handle, pointScreenPixels);

            return new Point(pointScreenPixels.x, pointScreenPixels.y);
        }

        /// <summary>
        /// The Greater Common Divisor.
        /// </summary>
        /// <param name="a">Size a</param>
        /// <param name="b">Size b</param>
        /// <returns>The GCD number.</returns>
        public static double Gcd(double a, double b)
        {
            return b == 0 ? a : Gcd(b, a % b);
        }

        /// <summary>
        /// Gets the DPI of the current window.
        /// </summary>
        /// <param name="window">The Window.</param>
        /// <returns>The DPI of the given Window.</returns>
        public static double Dpi(this Window window)
        {
            var source = PresentationSource.FromVisual(window);

            if (source != null)
                if (source.CompositionTarget != null)
                    return 96d * source.CompositionTarget.TransformToDevice.M11;

            return 96d;
        }

        /// <summary>
        /// Gets the scale of the current window.
        /// </summary>
        /// <param name="window">The Window.</param>
        /// <returns>The scale of the given Window.</returns>
        public static double Scale(this Window window)
        {
            var source = PresentationSource.FromVisual(window);

            if (source != null)
                if (source.CompositionTarget != null)
                    return source.CompositionTarget.TransformToDevice.M11;

            return 1d;
        }

        /// <summary>
        /// Generates a file name.
        /// </summary>
        /// <param name="fileType">The desired output file type.</param>
        /// <param name="frameCount">The number of frames of the recording.</param>
        /// <returns>A valid file name.</returns>
        public static string FileName(string fileType, int frameCount = 0)
        {
            if (!Settings.Default.UseDefaultOutput || String.IsNullOrEmpty(Settings.Default.DefaultOutput) || !Directory.Exists(Settings.Default.DefaultOutput))
            {
                #region Invalid Directory

                if (!Directory.Exists(Settings.Default.DefaultOutput))
                {
                    Dialog.Ok("Invalid Directory", "The selected default directory is invalid.", //TODO: Localize.
                        "The default directory: \"" + Settings.Default.DefaultOutput + "\" does not exist or it cannot be accessed.", Dialog.Icons.Warning);
                }

                #endregion

                #region Ask where to save.

                var ofd = new SaveFileDialog();
                ofd.AddExtension = true;

                switch (fileType)
                {
                    case "gif":
                        ofd.Filter = "Gif Animation (*.gif)|*.gif";
                        ofd.Title = "Save Animation As Gif";
                        ofd.FileName = "Animation"; //TODO: Localize
                        break;
                    case "avi":
                        ofd.Filter = "Avi Video (*.avi)|*.avi";
                        ofd.Title = "Save Animation As AVI"; 
                        ofd.FileName = "Video"; //TODO: Localize
                        break;
                    case "stg":
                    case "zip":
                        ofd.Filter = "*.stg|(ScreenToGif Project)|*.zip|(Zip Archive)";
                        ofd.Title = "Select the File Location"; //TODO: Localize
                        ofd.FileName = String.Format(frameCount > 1 ? "Project - {0} Frames [{1: hh-mm-ss}]" : "Project - {0} Frame [{1: hh-mm-ss}]", frameCount, DateTime.Now);
                        break;
                }
                
                ofd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

                var result = ofd.ShowDialog();

                if (!result.HasValue || !result.Value)
                    return null;

                return ofd.FileName;

                #endregion
            }
            else
            {
                //Save to default folder.
                return IncrementalFileName(Settings.Default.DefaultOutput, fileType);
            }
        }

        /// <summary>
        /// Searchs for a valid file name.
        /// </summary>
        /// <param name="directory">The output directory.</param>
        /// <param name="fileType">The type of the file (gif, video, project).</param>
        /// <returns>A valid file name.</returns>
        private static string IncrementalFileName(string directory, string fileType)
        {
            for (int number = 1; number < 9999; number++)
            {
                if (!File.Exists(Path.Combine(directory, "Animation " + number + "." + fileType)))
                {
                    return Path.Combine(directory, "Animation " + number + "." + fileType);
                }
            }

            return Path.Combine(directory, "No filename for you." + fileType);
        }

        #region List

        public static List<FrameInfo> CopyList(this List<FrameInfo> target)
        {
            return target.Select(item => new FrameInfo(item.ImageLocation, item.Delay, item.CursorInfo)).ToList();
        }

        /// <summary>
        /// Copies the List and saves the images in another folder.
        /// </summary>
        /// <param name="target">The List to copy</param>
        /// <returns>The copied list.</returns>
        public static List<FrameInfo> CopyToEncode(this List<FrameInfo> target)
        {
            #region Folder

            string fileNameAux = Path.GetFileName(target[0].ImageLocation);

            if (fileNameAux == null)
                throw new ArgumentException("Impossible to get filename.");

            var encodeFolder = Path.Combine(target[0].ImageLocation.Replace(fileNameAux, ""), "Encode " + DateTime.Now.ToString("yyyy-MM-dd hh-mm-ss"));

            if (!Directory.Exists(encodeFolder))
                Directory.CreateDirectory(encodeFolder);

            #endregion

            var newList = new List<FrameInfo>();

            foreach (FrameInfo frameInfo in target)
            {
                //Changes the path of the image.
                var filename = Path.Combine(encodeFolder, Path.GetFileName(frameInfo.ImageLocation));

                //Copy the image to the folder.
                File.Copy(frameInfo.ImageLocation, filename);

                //Create the new object and add to the list.
                newList.Add(new FrameInfo(filename, frameInfo.Delay, frameInfo.CursorInfo));
            }

            return newList;
        }

        public static List<FrameInfo> CopyToClipboard(this List<FrameInfo> target, bool move = false)
        {
            #region Folder

            string fileNameAux = Path.GetFileName(target[0].ImageLocation);

            if (fileNameAux == null)
                throw new ArgumentException("Impossible to get filename.");

            var clipFolder = Path.Combine(target[0].ImageLocation.Replace(fileNameAux, ""), "Clipboard");

            if (!Directory.Exists(clipFolder))
                Directory.CreateDirectory(clipFolder);

            #endregion

            var newList = new List<FrameInfo>();

            foreach (FrameInfo frameInfo in target)
            {
                //Changes the path of the image.
                var filename = Path.Combine(clipFolder, Path.GetFileName(frameInfo.ImageLocation));

                //Copy the image to the folder.
                File.Copy(frameInfo.ImageLocation, filename, true);

                if (move)
                    File.Delete(frameInfo.ImageLocation);

                //Create the new object and add to the list.
                newList.Add(new FrameInfo(filename, frameInfo.Delay, frameInfo.CursorInfo));
            }

            return newList;
        }

        public static List<FrameInfo> CopyBackFromClipboard(this List<FrameInfo> target, int pasteIndex)
        {
            #region Folder

            var recordingFolder = Path.GetDirectoryName(Path.GetDirectoryName(target[0].ImageLocation));

            if (String.IsNullOrEmpty(recordingFolder))
                throw new ArgumentException("Impossible to get the folder name.");

            #endregion

            var newList = new List<FrameInfo>();

            foreach (FrameInfo frameInfo in target)
            {
                //Changes the path of the image.
                var filename = Path.Combine(recordingFolder,
                    String.Format("{0} - {1} {2}", pasteIndex, Path.GetFileNameWithoutExtension(frameInfo.ImageLocation), DateTime.Now.ToString("hh-mm-ss-fff")));

                //Copy the image to the folder.
                File.Copy(frameInfo.ImageLocation, filename, true);

                //Create the new object and add to the list.
                newList.Add(new FrameInfo(filename, frameInfo.Delay, frameInfo.CursorInfo));
            }

            return newList;
        }

        /// <summary>
        /// Makes a Yo-yo efect with the given List (List + Reverted List)
        /// </summary>
        /// <param name="list">The list to apply the efect</param>
        /// <returns>A List with the Yo-yo efect</returns>
        public static List<FrameInfo> Yoyo(List<FrameInfo> list)
        {
            var listReverted = new List<FrameInfo>(list);
            listReverted.Reverse();

            var currentFolder = Path.GetDirectoryName(list[0].ImageLocation);

            foreach (FrameInfo frame in listReverted)
            {
                var newPath = Path.Combine(currentFolder, list.Count + ".bmp");

                File.Copy(frame.ImageLocation, newPath);

                var newFrame = new FrameInfo(newPath, frame.Delay, frame.CursorInfo);

                list.Add(newFrame);
            }

            return list;
        }

        #endregion
    }
}