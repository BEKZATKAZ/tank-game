using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using Tanks.Core.Repositories;

namespace Tanks.Client.Input.Bindings
{
    public sealed class BindingStoreService
    {
        public BindingStoreService(IFileSystem files, string path)
        {
            _files = files;
            _path = path;
        }

        private readonly IFileSystem _files;
        private readonly string _path;

        public Dictionary<string, string> Load()
        {
            Dictionary<string, string> result = new();

            try
            {
                if (_files.Exists(_path) == false) return result;

                JObject document = JObject.Parse(_files.ReadAllText(_path));
                if (document["Version"]?.Value<int>() != 1) return result;
                if (document["Overrides"] is not JObject entries) return result;

                foreach (KeyValuePair<string, JToken> entry in entries)
                {
                    if (ControlBindingSlots.BindingIds
                        .ContainsKey(entry.Key) == false)
                        continue;
                    if (entry.Value.Type != JTokenType.String) continue;

                    string path = entry.Value.Value<string>();
                    if (BindingPathUtility.IsValid(entry.Key, path) == false)
                        continue;

                    result[entry.Key] = path;
                }
            }
            catch (Exception)
            {
                return new Dictionary<string, string>();
            }

            return result;
        }

        public bool Save(Dictionary<string, string> overrides, out string error)
        {
            try
            {
                BindingDocument document = new() { Overrides = overrides };
                string json = JsonConvert.SerializeObject(
                    document, Formatting.Indented);

                _files.WriteAllText(_path, json);
                error = null;
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }
    }
}
