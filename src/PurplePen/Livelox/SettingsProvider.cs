using System;
using Newtonsoft.Json;

namespace PurplePen.Livelox
{
    class SettingsProvider
    {
        public LiveloxSettings LoadSettings()
        {
            try
            {
                var settings = JsonConvert.DeserializeObject<LiveloxSettings>(
                    System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(Settings.Default.LiveloxSettings))
                );
                return settings ?? new LiveloxSettings();
            }
            catch
            {
                return new LiveloxSettings();
            }
        }

        public void SaveSettings(LiveloxSettings liveloxSettings)
        {
            Settings.Default.LiveloxSettings = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(liveloxSettings)));
            Settings.Default.Save();
        }
    }
}