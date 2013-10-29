using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Common
{
    public class Settings
    {
        private static object _instanceLocker = new object();
        private string _fileName;
        private string _separator;
        private static Settings _instance;


        public string TryGetValue(string key)
        {
            if (!_settings.ContainsKey(key)) return null;
            List<string> values = _settings[key];
            return values[values.Count - 1];
        }

        private Settings(string fileName, string separator, Encoding encoding)
        {
            _fileName = fileName;
            _separator = separator;
            if (!File.Exists(fileName))
                return;
            using (FileStream fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                Init(fileStream, encoding);
        }

        public static Settings GetInstance()
        {
            lock (_instanceLocker)
            {
                if (_instance == null)
                {
                    if (File.Exists("settings\\settings"))
                        _instance = new Settings("settings\\settings", ":=", Encoding.Default);
                    else if (File.Exists("..\\settings\\settings"))
                        _instance = new Settings("..\\settings\\settings", ":=", Encoding.Default);
                    else
                        throw new Exception("Settings file does not exists");
                }
            }
            return _instance;
        }




        private Settings(Stream stream, Encoding encoding, string separator)
        {
            this._separator = separator;
            Init(stream, encoding);
        }

        private void Init(Stream stream, Encoding encoding)
        {
            using (StreamReader reader = new StreamReader(stream, encoding))
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    if (IsEmpty(line)) continue;
                    KeyValuePair<string, string> keyValuePair = GetKeyAndValue(line);
                    if (!_settings.ContainsKey(keyValuePair.Key))
                        _settings.Add(keyValuePair.Key, new List<string>());
                    _settings[keyValuePair.Key].Add(keyValuePair.Value);
                }
            }
        }

        private KeyValuePair<string, string> GetKeyAndValue(string line)
        {
            CheckLineFormat(line);
            int i = line.IndexOf(_separator);
            string key = line.Substring(0, i).Trim();
            string value = line.Substring(i + _separator.Length, line.Length - i - _separator.Length).Trim();
            return new KeyValuePair<string, string>(key, value);
        }

        private void CheckLineFormat(string line)
        {
            if (line.IndexOf(_separator) == -1)
                throw new Exception(string.Format("Can't find separator '{0}' in config line '{1}'.", _separator, line));
        }


        private static bool IsEmpty(string line)
        {
            string t = line.TrimStart(' ', '\t');
            return (string.IsNullOrEmpty(t)) || t.StartsWith("#");
        }
        private readonly Dictionary<string, List<string>> _settings = new Dictionary<string, List<string>>(new IgnoreCaseComparer());
    }

    internal class IgnoreCaseComparer : IEqualityComparer<string>
    {
        public bool Equals(string x, string y)
        {
            return x.Equals(y, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode(string obj)
        {
            return obj.ToLower().GetHashCode();
        }
    }
}