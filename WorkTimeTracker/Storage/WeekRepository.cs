using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using WorkTimeTracker.ViewModels;

namespace WorkTimeTracker.Storage
{
    public static class WeekRepository
    {
        private static readonly string DataFolder =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");

        static WeekRepository()
        {
            Directory.CreateDirectory(DataFolder);
        }

        private static string GetFilePath(DateTime weekStart)
        {
            int weekYear = ISOWeek.GetYear(weekStart);
            int weekNumber = ISOWeek.GetWeekOfYear(weekStart);

            return Path.Combine(DataFolder, $"{weekYear}-W{weekNumber:D2}.json");
        }

        public static WeekDto? Load(DateTime weekStart)
        {
            var path = GetFilePath(weekStart);
            if (!File.Exists(path))
                return null;

            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<WeekDto>(json);
        }

        public static void Save(DateTime weekStart, IEnumerable<DagUrenViewModel> dagen, bool reedsAfgedrukt)
        {
            var dto = new WeekDto
            {
                WeekStart = weekStart,
                ReedsAfgedrukt = reedsAfgedrukt,
                Dagen = dagen.Select(d => new DagUrenDto
                {
                    Datum = d.Datum,
                    StartTijd = d.StartTijd,
                    EindTijd = d.EindTijd,
                    Projectnaam = d.Projectnaam,
                    ExtraInformatie = d.ExtraInformatie,
                    Locatie = d.Locatie,
                    Status = d.Status
                }).ToList()
            };

            var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(GetFilePath(weekStart), json);
        }

        public static DateTime? GetLastPrintedWeek()
        {
            string folder = DataFolder;

            if (!Directory.Exists(folder))
                return null;

            DateTime? laatste = null;

            foreach (var file in Directory.GetFiles(folder, "*.json"))
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var dto = JsonSerializer.Deserialize<WeekDto>(json);

                    if (dto is { ReedsAfgedrukt: true })
                    {
                        if (laatste == null || dto.WeekStart > laatste.Value)
                            laatste = dto.WeekStart;
                    }
                }
                catch
                {
                    // corrupt bestand / ander formaat -> gewoon overslaan
                }
            }

            return laatste;
        }
    }
}
