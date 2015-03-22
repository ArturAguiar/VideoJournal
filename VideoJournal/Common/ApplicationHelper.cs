using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Capture;
using Windows.Storage;
using Windows.Storage.FileProperties;
using System.Runtime.InteropServices.WindowsRuntime;

namespace VideoJournal.Common
{
    class ApplicationHelper
    {
        private const string FOLDER_NAME = "VideoJournal";

        public const string DEFAULT_VLOG_IMAGE_PATH = "Assets/LightGray.png";
        public const string DEFAULT_GROUP_IMAGE_PATH = "Assets/DarkGray.png";

        private static CameraCaptureUIVideoFormat videoFormat = CameraCaptureUIVideoFormat.Wmv;

        // PRIVATE METHODS //
        private static string GetExtensionForFormat()
        {
            if (videoFormat == CameraCaptureUIVideoFormat.Wmv)
                return ".wmv";
            else //if (videoFormat == CameraCaptureUIVideoFormat.Mp4)
                return ".mp4";
        }

        // PUBLIC METHODS //
        public static async Task<StorageFolder> GetDestinationFolder()
        {
            return await Windows.Storage.KnownFolders.VideosLibrary.CreateFolderAsync(FOLDER_NAME, CreationCollisionOption.OpenIfExists);
        }

        public static CameraCaptureUIVideoFormat GetVideoFormat()
        {
            return videoFormat;
        }

        public static string GenerateFilename(string suffix = "")
        {
            return GetCurrentDayID() + suffix + GetExtensionForFormat();
        }

        public static string GetCurrentDayID()
        {
            DateTime date = DateTime.Now;
            string dateFormat = "yyyy-MM-dd";
            return date.ToString(dateFormat);
        }

        public static string GetCurrentMonthID()
        {
            DateTime date = DateTime.Now;
            string dateFormat = "yyyy-MM";
            return date.ToString(dateFormat);
        }

        public static string GetCurrentMonthTitle()
        {
            DateTime date = DateTime.Now;
            string dateFormat = "y";
            return date.ToString(dateFormat);
        }

        public static string GetCurrentDayTitle()
        {
            DateTime date = DateTime.Now;
            string dateFormat = "D";
            return date.ToString(dateFormat);
        }

        public static async Task<StorageFile> GetVlogFile(string filename)
        {
            //await Windows.Storage.KnownFolders.VideosLibrary
            StorageFolder folder = await GetDestinationFolder();

            return await folder.GetFileAsync(filename);
        }

        public static async Task<string> GetThumbnailPath(string filename)
        {
            string thumbFilePath = filename + "-thumb.png"; // Extension?

            // Check if it is in the temp folder.
            StorageFolder tempFolder = Windows.Storage.ApplicationData.Current.TemporaryFolder;

            StorageFile thumbFile;

            try
            {
                thumbFile = await tempFolder.GetFileAsync(thumbFilePath);
                return "ms-appdata:///temp/" + thumbFilePath;
            }
            catch (System.IO.FileNotFoundException)
            {
                // Thumbnail image file not found, create it.
            }

            StorageFile vlogFile = await GetVlogFile(filename);

            StorageItemThumbnail thumbnail = await vlogFile.GetThumbnailAsync(Windows.Storage.FileProperties.ThumbnailMode.VideosView, 190U);

            thumbFile = await Windows.Storage.ApplicationData.Current.TemporaryFolder.CreateFileAsync(thumbFilePath, CreationCollisionOption.ReplaceExisting);

            Windows.Storage.Streams.IBuffer buffer = new Windows.Storage.Streams.Buffer((uint)thumbnail.Size);
            await thumbnail.ReadAsync(buffer, (uint)thumbnail.Size, Windows.Storage.Streams.InputStreamOptions.None);

            await Windows.Storage.FileIO.WriteBytesAsync(thumbFile, buffer.ToArray(0, (int)thumbnail.Size));

            return "ms-appdata:///temp/" + thumbFilePath;
        }
    }
}
