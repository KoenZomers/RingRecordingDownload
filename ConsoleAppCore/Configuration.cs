using System;

namespace KoenZomers.Ring.RecordingDownload
{
    /// <summary>
    /// Configuration to use for downloading the Ring recordings
    /// </summary>
    public class Configuration
    {
        /// <summary>
        /// Start date of the period for which to download the recordings
        /// </summary>
        public DateTime? StartDate;

        /// <summary>
        /// End date/time of the period for which to download the recordings
        /// </summary>
        public DateTime? EndDate;

        /// <summary>
        /// Path where to download the recordings to
        /// </summary>
        public string OutputPath;

        /// <summary>
        /// Username to use to connect to Ring
        /// </summary>
        public string Username;

        /// <summary>
        /// Password to use to connect to Ring
        /// </summary>
        public string Password;

        /// <summary>
        /// Type of historical event to download
        /// </summary>
        public string Type;

        /// <summary>
        /// Amount of times to retry downloading a recording if it fails
        /// </summary>
        public short MaxRetries = 3;

        /// <summary>
        /// The Id of a specific Ring device to download the recordings for
        /// </summary>
        public int? RingDeviceId;

        /// <summary>
        /// Indicates if the downloads should be resumed from the last successfully downloaded recording
        /// </summary>
        public bool ResumeFromLastDownload;
    }
}
