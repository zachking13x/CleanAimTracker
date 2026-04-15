using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using CleanAimTracker.Models;

namespace CleanAimTracker.Helpers
{
    public static class SessionStorage
    {
        private static readonly string SessionsFolder =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                         "CleanAimTracker", "Sessions");

        static SessionStorage()
        {
            if (!Directory.Exists(SessionsFolder))
            {
                Directory.CreateDirectory(SessionsFolder);
            }
        }

        public static void SaveSession(SessionSummary summary)
        {
            var fileName = $"{summary.SessionEnd:yyyyMMdd_HHmmss}_{summary.Id}.json";
            var path = Path.Combine(SessionsFolder, fileName);

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            var json = JsonSerializer.Serialize(summary, options);
            File.WriteAllText(path, json);
        }

        public static List<SessionSummary> LoadAllSessions()
        {
            var list = new List<SessionSummary>();

            if (!Directory.Exists(SessionsFolder))
                return list;

            var files = Directory.GetFiles(SessionsFolder, "*.json");
            foreach (var file in files)
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var summary = JsonSerializer.Deserialize<SessionSummary>(json);
                    if (summary != null)
                        list.Add(summary);
                }
                catch
                {
                    // Ignore bad files for now
                }
            }

            return list;
        }
    }
}

