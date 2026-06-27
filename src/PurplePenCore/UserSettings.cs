using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace PurplePen
{
    // This class holds user settings. To add new settings, just add in the list of public
    // properties, and be sure to initialize to the correct default value.
    // Settings are saved in JSON format.
    public class UserSettings
    {
        public string UILanguage;
        public string LastLoadedFile;
        public float MapIntensity = 0.7F;
        public bool MapHighQuality = true;
        public bool ShowPopupInfo = true;
        public Guid ClientId = Guid.NewGuid();
        public bool ViewAllControls = false;
        public bool ShowPrintArea = true;
        public string DefaultDescriptionLanguage;
        public string NewEventMapStandard = "2017";
        public string NewEventDescriptionStandard = "2018";
        public string LiveloxSettings;

        public static UserSettings Current;

        public static string SettingsPath { get; private set; }

        private static JsonSerializerOptions jsonOptions = new JsonSerializerOptions {
            IncludeFields = true,
            WriteIndented = true
        };

        // Save the settings to the path used in Initialize.
        public void Save()
        {
            Debug.Assert(SettingsPath != null, "Initialize hasn't been called yet.");

            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath));
            string json = JsonSerializer.Serialize(this, jsonOptions);
            
            // Call File.WriteAllText up to 10 times with variable retries. Otherwise
            // this tends to file while running tests.
            Random random = new Random();
            int retryCount = 0;

            while (true) {
                try {
                    File.WriteAllText(SettingsPath, json);
                    return;
                }
                catch (IOException) {
                    if (retryCount == 10)
                        throw;

                    ++retryCount;
                    Thread.Sleep(random.Next(30, 300));
                }
            }
        }

        // Initialize the user settings, setting them into "UserSettings.Current". If the
        // file given doesn't exist, then default settings are used. If the file does exist, but
        // can't be loaded, it is deleted and default settings are used.
        public static void Initialize(string pathName)
        {
            Debug.Assert(Current == null, "Should only call Initialize once.");

            SettingsPath = pathName;
            try {
                if (File.Exists(SettingsPath)) {
                    var json = File.ReadAllText(SettingsPath);
                    Current = JsonSerializer.Deserialize<UserSettings>(json, jsonOptions) ?? new UserSettings();
                }
                else {
                    Current = new UserSettings();
                }
            } catch {
                // use default.
                Current = new UserSettings();
            }
        }

       
    }
}
