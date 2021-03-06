﻿using Microsoft.AspNetCore.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Threading.Tasks;

namespace AlexaBotApp.Infrastructure
{
    public class ObjectLogger
    {
        public ObjectLogger(string environmentName, string logFolder)
        {
            Enabled = environmentName.Equals("development", StringComparison.OrdinalIgnoreCase);

            LogFolder = logFolder;
        }

        public bool Enabled { get; }

        public string LogFolder { get; }

        public string SessionFolderName { get; private set; }

        public string SessionFolderPath { get; private set; }

        public string SessionId { get; private set; }

        public async Task LogObjectAsync(object @object, string traceId)
        {
            if (@object is null) throw new ArgumentNullException(nameof(@object));
            if (string.IsNullOrWhiteSpace(traceId)) throw new ArgumentException("message", nameof(traceId));

            if (!Enabled) return;
            if (SessionId is null) return;

            var jObject = @object is string stringObject
                ? JObject.Parse(stringObject)
                : JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(@object));

            var objectFilename = $"{DateTime.Now:HH.mm.ss.fff}+{traceId.Replace(":", "-")}.json";

            await File.WriteAllTextAsync(Path.Combine(SessionFolderPath, objectFilename), JsonConvert.SerializeObject(jObject, Formatting.Indented));
        }

        public void SetSessionId(string sessionId)
        {
            if (!Enabled) return;
            if (sessionId == SessionId) return;

            SessionId = sessionId;
            SessionFolderName = $"{DateTime.Now:yyyy-MM-dd+HH.mm.ss}+{sessionId.Replace(":", "-")}";
            SessionFolderPath = Path.GetFullPath(Path.Combine(LogFolder, SessionFolderName));

            Directory.CreateDirectory(SessionFolderPath);
        }
    }
}