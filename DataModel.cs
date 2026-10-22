using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace SlideApp {
    [DataContract]
    public class Project {
        [DataMember] public string Name = "";
        [DataMember] public List<string> Slides = new List<string>();
    }

    public static class ProjectStore {
        public static string BaseDir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SlideApp", "Projects");
        public static string GetProjectDir(string name) => Path.Combine(BaseDir, Sanitize(name));
        public static string GetSlidesPath(string name) => Path.Combine(GetProjectDir(name), "slides.json");

        public static string Sanitize(string s) {
            foreach (var c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
            return string.IsNullOrWhiteSpace(s) ? "Project" : s;
        }

        public static List<string> ListProjects() {
            var list = new List<string>();
            if (!Directory.Exists(BaseDir)) return list;
            foreach (var d in Directory.GetDirectories(BaseDir)) list.Add(Path.GetFileName(d));
            list.Sort(StringComparer.OrdinalIgnoreCase);
            return list;
        }

        public static Project Load(string name) {
            var path = GetSlidesPath(name);
            if (!File.Exists(path)) return new Project { Name = name };
            try {
                using (var fs = File.OpenRead(path)) {
                    var ser = new DataContractJsonSerializer(typeof(Project));
                    var p = (Project)ser.ReadObject(fs);
                    if (p != null) { p.Name = name; return p; }
                }
            } catch { }
            return new Project { Name = name };
        }

        public static void Save(Project p) {
            var dir = GetProjectDir(p.Name);
            Directory.CreateDirectory(dir);
            using (var fs = File.Create(GetSlidesPath(p.Name))) {
                new DataContractJsonSerializer(typeof(Project)).WriteObject(fs, p);
            }
        }

        public static void Delete(string name) {
            var dir = GetProjectDir(name);
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }
    }
}