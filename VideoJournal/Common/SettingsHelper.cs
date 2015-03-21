using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Capture;
using Windows.Storage;

namespace VideoJournal.Common
{
    class SettingsHelper
    {
        private const string FOLDER_NAME = "VideoJournal";

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
    }
}
