using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace WorkTimeTracker.Storage
{
    public class AppSettings
    {
        public string GebruikersNaam { get; set; } = string.Empty;
    }

    public static class AppSettingsRepository
    {
        private static readonly string DataFolder =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");

        private static readonly string SettingsFilePath =
            Path.Combine(DataFolder, "settings.json");

        static AppSettingsRepository()
        {
            Directory.CreateDirectory(DataFolder);
        }

        public static AppSettings Load()
        {
            if (!File.Exists(SettingsFilePath))
                return new AppSettings();

            try
            {
                var json = File.ReadAllText(SettingsFilePath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
            catch
            {
                // Bij corrupte file gewoon met lege settings starten
                return new AppSettings();
            }
        }

        public static void Save(AppSettings settings)
        {
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(SettingsFilePath, json);
        }
    }
}
