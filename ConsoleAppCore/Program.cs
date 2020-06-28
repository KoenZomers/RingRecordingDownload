using System;
using KoenZomers.Ring.Api;
using System.Configuration;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.IO;
using System.Threading.Tasks;

namespace KoenZomers.Ring.RecordingDownload
{
    class Program
    {
        /// <summary>
        /// Refresh token to use to authenticate to the Ring API
        /// </summary>
        public static string RefreshToken
        {
            get { return ConfigurationManager.AppSettings["RefreshToken"]; }
            set
            {
                var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                if (configFile.AppSettings.Settings["RefreshToken"] == null)
                {
                    configFile.AppSettings.Settings.Add("RefreshToken", value);
                }
                else
                {
                    configFile.AppSettings.Settings["RefreshToken"].Value = value;
                }
                configFile.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
            }
        }

        /// <summary>
        /// The Id of the last downloaded historical event
        /// </summary>
        public static string LastdownloadedRecordingId
        {
            get { return ConfigurationManager.AppSettings["LastdownloadedRecordingId"]; }
            set
            {
                var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                if (configFile.AppSettings.Settings["LastdownloadedRecordingId"] == null)
                {
                    configFile.AppSettings.Settings.Add("LastdownloadedRecordingId", value);
                }
                else
                {
                    configFile.AppSettings.Settings["LastdownloadedRecordingId"].Value = value;
                }
                configFile.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
            }
        }

        static async Task Main(string[] args)
        {
            Console.WriteLine();

            var appVersion = Assembly.GetExecutingAssembly().GetName().Version;

            Console.WriteLine($"Ring Recordings Download Tool v{appVersion.Major}.{appVersion.Minor}.{appVersion.Build}.{appVersion.Revision} by Koen Zomers");
            Console.WriteLine();

            // Ensure arguments have been provided
            if (args.Length == 0)
            {
                DisplayHelp();
                Environment.Exit(1);
            }

            // Parse the provided arguments
            var configuration = ParseArguments(args);

            // Ensure we have the required configuration
            if (string.IsNullOrWhiteSpace(configuration.Username) && string.IsNullOrWhiteSpace(RefreshToken))
            {
                Console.WriteLine("-username is required");
                Environment.Exit(1);
            }

            if (string.IsNullOrWhiteSpace(configuration.Password) && string.IsNullOrWhiteSpace(RefreshToken))
            {
                Console.WriteLine("-password is required");
                Environment.Exit(1);
            }

            if (!configuration.StartDate.HasValue)
            {
                Console.WriteLine("-startdate or -lastdays is required");
                Environment.Exit(1);
            }

            // Connect to Ring
            Console.WriteLine("Connecting to Ring services");
            Session session;
            if (!string.IsNullOrWhiteSpace(RefreshToken))
            {
                // Use refresh token from previous session
                Console.WriteLine("Authenticating using refresh token from previous session");

                session = Session.GetSessionByRefreshToken(RefreshToken).Result;
            }
            else
            {
                // Use the username and password provided
                Console.WriteLine("Authenticating using provided username and password");

                session = new Session(configuration.Username, configuration.Password);

                try
                {
                    await session.Authenticate();
                }
                catch (Api.Exceptions.TwoFactorAuthenticationRequiredException)
                {
                    // Two factor authentication is enabled on the account. The above Authenticate() will trigger a text or e-mail message to be sent. Ask for the token sent in that message here.
                    Console.WriteLine($"Two factor authentication enabled on this account, please enter the token received in the text message on your phone or the e-mail from Ring:");
                    var token = Console.ReadLine();

                    // Authenticate again using the two factor token
                    await session.Authenticate(twoFactorAuthCode: token);
                }
                catch(Api.Exceptions.ThrottledException e)
                {
                    Console.WriteLine(e.Message);
                    Environment.Exit(1);
                }
                catch (System.Net.WebException)
                {
                    Console.WriteLine("Connection failed. Validate your credentials.");
                    Environment.Exit(1);
                }
            }

            // If we have a refresh token, update the config file with it so we don't need to authenticate again next time
            if (session.OAuthToken != null)
            {
                RefreshToken = session.OAuthToken.RefreshToken;
            }

            // Retrieve all sessions
            Console.WriteLine($"Downloading {(string.IsNullOrWhiteSpace(configuration.Type) ? "all" : configuration.Type)} historical events between {configuration.StartDate.Value:dddd d MMMM yyyy HH:mm:ss} and {(configuration.EndDate.HasValue ? configuration.EndDate.Value.ToString("dddd d MMMM yyyy HH:mm:ss") : "now")}{(configuration.RingDeviceId.HasValue ? $" for Ring device {configuration.RingDeviceId.Value}" : "")}");


            List <Api.Entities.DoorbotHistoryEvent> doorbotHistory = null;
            try
            {
                doorbotHistory = await session.GetDoorbotsHistory(configuration.StartDate.Value, configuration.EndDate, configuration.RingDeviceId);
            }
            catch (Exception e) when ((e is AggregateException && e.InnerException != null && e.InnerException.Message.Contains("404")) || e is Api.Exceptions.DeviceUnknownException)
            {
                Console.WriteLine($"No Ring device with Id {configuration.RingDeviceId} found under this account");
                Environment.Exit(1);
            }

            // If a specific Type has been provided, filter out all the ones that don't match this type
            if (!string.IsNullOrWhiteSpace(configuration.Type))
            {
                doorbotHistory = doorbotHistory.Where(h => h.Kind.Equals(configuration.Type, StringComparison.CurrentCultureIgnoreCase)).ToList();
            }

            // If we should only continue downloading from the most recently successfully downloaded recording, remove all entries that are older than this one
            if (configuration.ResumeFromLastDownload && !string.IsNullOrWhiteSpace(LastdownloadedRecordingId))
            {
                // Try to find the last successfully downloaded item
                var lastDownloadedItem = doorbotHistory.FirstOrDefault(h => h.Id == LastdownloadedRecordingId);

                if (lastDownloadedItem != null && lastDownloadedItem.CreatedAtDateTime.HasValue)
                {
                    // Only take all historical recordings which are newer than the last successfully downloaded historical item
                    Console.WriteLine($"Filtering for recordings newer than {lastDownloadedItem.CreatedAtDateTime.Value:dddd dd MMMM yyyy} at {lastDownloadedItem.CreatedAtDateTime.Value:HH:mm:ss}");
                    doorbotHistory = doorbotHistory.Where(h => h.CreatedAtDateTime.HasValue && h.CreatedAtDateTime.Value > lastDownloadedItem.CreatedAtDateTime.Value).ToList();
                }
            }

            // Ensure we have items to download
            if(doorbotHistory.Count == 0)
            {
                Console.WriteLine("No items found. Done.");
                Environment.Exit(0);
            }

            Console.WriteLine($"{doorbotHistory.Count} item{(doorbotHistory.Count == 1 ? "" : "s")} found, downloading to {configuration.OutputPath}");

            for (var itemCount = 0; itemCount < doorbotHistory.Count; itemCount++)
            {
                var doorbotHistoryItem = doorbotHistory[itemCount];

                if(configuration.ResumeFromLastDownload && !string.IsNullOrWhiteSpace(LastdownloadedRecordingId) && doorbotHistoryItem.Id == LastdownloadedRecordingId)
                {
                    Console.WriteLine($"Reached previously downloaded recording with Id {LastdownloadedRecordingId}. Done.");
                    Environment.Exit(0);
                }

                // If no valid date on the item, skip it and continue with the next
                if (!doorbotHistoryItem.CreatedAtDateTime.HasValue) continue;

                // Construct the filename and path where to save the file
                var downloadFileName = $"{doorbotHistoryItem.CreatedAtDateTime.Value:yyyy-MM-dd HH-mm-ss} ({doorbotHistoryItem.Id}).mp4";
                var downloadFullPath = Path.Combine(configuration.OutputPath, downloadFileName);

                short attempt = 0;
                do
                {
                    attempt++;

                    Console.Write($"{itemCount + 1} - {downloadFileName}... ");

                    try
                    {
                        // Download the recording
                        await session.GetDoorbotHistoryRecording(doorbotHistoryItem, downloadFullPath);

                        Console.WriteLine($"done ({new FileInfo(downloadFullPath).Length / 1048576} MB)");

                        if (itemCount == 0)
                        {
                            // Store the Id of this historical item as the most recent downloaded item so it can download up to this one on a next attempt
                            LastdownloadedRecordingId = doorbotHistoryItem.Id;
                        }
                        break;
                    }
                    catch (System.Net.WebException e)
                    {
                        if (e.Response != null)
                        {
                            var response = new StreamReader(e.Response.GetResponseStream()).ReadToEnd();

                            Console.Write($"failed ({e.Message} - {response})");
                        }
                        else
                        {
                            Console.Write($"failed ({e.Message})");
                        }
                    }

                    if (attempt >= configuration.MaxRetries)
                    {
                        Console.WriteLine(". Giving up.");
                    }
                    else
                    {
                        Console.WriteLine($". Retrying {attempt + 1}/{configuration.MaxRetries}.");
                    }
                } while (attempt < configuration.MaxRetries);
            }

            Console.WriteLine("Done");
            Environment.Exit(0);
        }

        /// <summary>
        /// Parses all provided arguments
        /// </summary>
        /// <param name="args">String array with arguments passed to this console application</param>
        private static Configuration ParseArguments(IList<string> args)
        {
            var configuration = new Configuration
            {
                Username = ConfigurationManager.AppSettings["RingUsername"],
                Password = ConfigurationManager.AppSettings["RingPassword"],
                OutputPath = Environment.CurrentDirectory
            };

            if (args.Contains("-out"))
            {
                configuration.OutputPath = args[args.IndexOf("-out") + 1];
            }

            if (args.Contains("-username"))
            {
                configuration.Username = args[args.IndexOf("-username") + 1];
            }

            if (args.Contains("-password"))
            {
                configuration.Password = args[args.IndexOf("-password") + 1];
            }

            if (args.Contains("-type"))
            {
                configuration.Type = args[args.IndexOf("-type") + 1];
            }

            if (args.Contains("-lastdays"))
            {
                if (double.TryParse(args[args.IndexOf("-lastdays") + 1], out double lastDays))
                {
                    configuration.StartDate = DateTime.Now.AddDays(lastDays * -1);
                    configuration.EndDate = DateTime.Now;
                }
            }

            if (args.Contains("-startdate"))
            {
                if (DateTime.TryParse(args[args.IndexOf("-startdate") + 1], out DateTime startDate))
                {
                    configuration.StartDate = startDate;
                }
            }

            if (args.Contains("-enddate"))
            {
                if (DateTime.TryParse(args[args.IndexOf("-enddate") + 1], out DateTime endDate))
                {
                    configuration.EndDate = endDate;
                }
            }

            if (args.Contains("-retries"))
            {
                if (short.TryParse(args[args.IndexOf("-retries") + 1], out short maxRetries))
                {
                    configuration.MaxRetries = maxRetries;
                }
            }

            if (args.Contains("-deviceid"))
            {
                if (int.TryParse(args[args.IndexOf("-deviceid") + 1], out int deviceId))
                {
                    configuration.RingDeviceId = deviceId;
                }
            }

            if (args.Contains("-resumefromlastdownload"))
            {
                configuration.ResumeFromLastDownload = true;
            }

            return configuration;
        }

        /// <summary>
        /// Shows the syntax
        /// </summary>
        private static void DisplayHelp()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("   RingRecordingDownload -username <username> -password <password> [-out <folder location> -type <motion/ring/...> -lastdays X -startdate <date> -enddate <date>]");
            Console.WriteLine();
            Console.WriteLine("username: Username of the account to use to log on to Ring");
            Console.WriteLine("password: Password of the account to use to log on to Ring");
            Console.WriteLine("out: The folder where to store the recordings (optional, will use current directory if not specified)");
            Console.WriteLine("type: The type of events to store the recordings of, i.e. motion or ring (optional, will download them all if not specified)");
            Console.WriteLine("lastdays: The amount of days in the past to download recordings of (optional)");
            Console.WriteLine("startdate: Date and time from which to start downloading events (optional)");
            Console.WriteLine("enddate: Date and time until which to download events (optional, will use now if not specified)");
            Console.WriteLine("retries: Amount of retries on download failures (optional, will use 3 retries by default)");
            Console.WriteLine("deviceid: Id of the Ring device to download the recordings for (optional, will download for all registered Ring devices by default)");
            Console.WriteLine("resumefromlastdownload: If provided, it will try to start downloading recordings since the last successful download");
            Console.WriteLine();
            Console.WriteLine("Example:");
            Console.WriteLine("   RingRecordingDownload -username my@email.com -password mypassword -lastdays 7");
            Console.WriteLine("   RingRecordingDownload -username my@email.com -password mypassword -lastdays 1 -resumefromlastdownload");
            Console.WriteLine("   RingRecordingDownload -username my@email.com -password mypassword -lastdays 7 -retries 5");
            Console.WriteLine("   RingRecordingDownload -username my@email.com -password mypassword -lastdays 7 -type ring");
            Console.WriteLine("   RingRecordingDownload -username my@email.com -password mypassword -lastdays 7 -type ring -out \"c:\\recordings path\"");
            Console.WriteLine("   RingRecordingDownload -username my@email.com -password mypassword -startdate \"12-02-2019 08:12:45\"");
            Console.WriteLine("   RingRecordingDownload -username my@email.com -password mypassword -startdate \"12-02-2019 08:12:45\" -enddate \"12-03-2019 10:53:12\"");
            Console.WriteLine("   RingRecordingDownload -username my@email.com -password mypassword -startdate \"12-02-2019 08:12:45\" -enddate \"12-03-2019 10:53:12\" -deviceId 1234567");
            Console.WriteLine();
        }
    }
}
