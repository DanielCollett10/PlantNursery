using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PlantNursery.Sim.Content
{
    /// <summary>
    /// A JSON object being validated. Every accessor throws <see cref="ContentException"/>
    /// naming the file and field path on a missing or invalid value.
    /// </summary>
    internal sealed class ContentNode
    {
        private readonly JObject _obj;

        public string File { get; }
        public string Path { get; }

        private ContentNode(string file, string path, JObject obj)
        {
            File = file;
            Path = path;
            _obj = obj;
        }

        public JObject Raw => _obj;

        /// <summary>Parses a file's text; the root must be an object.</summary>
        public static ContentNode ParseRoot(string file, string json)
        {
            if (json == null) throw new ContentException(file, "", "content is null");
            try
            {
                var token = JToken.Parse(json);
                if (!(token is JObject o)) throw new ContentException(file, "", "root must be a JSON object");
                return new ContentNode(file, "", o);
            }
            catch (JsonException e)
            {
                throw new ContentException(file, "", "invalid JSON: " + e.Message);
            }
        }

        public static ContentNode FromObject(string file, JObject o) => new ContentNode(file, "", o);

        private string P(string name) => Path.Length == 0 ? name : Path + "." + name;

        public ContentException Error(string name, string message) => new ContentException(File, P(name), message);

        public bool Has(string name) => _obj.TryGetValue(name, out var t) && t.Type != JTokenType.Null;

        private JToken Get(string name)
        {
            if (!_obj.TryGetValue(name, out var t) || t.Type == JTokenType.Null)
                throw Error(name, "missing required field");
            return t;
        }

        public IEnumerable<string> Keys()
        {
            foreach (var p in _obj.Properties()) yield return p.Name;
        }

        public ContentNode Obj(string name)
        {
            if (!(Get(name) is JObject o)) throw Error(name, "must be an object");
            return new ContentNode(File, P(name), o);
        }

        public List<ContentNode> ObjArray(string name)
        {
            if (!(Get(name) is JArray a)) throw Error(name, "must be an array");
            var list = new List<ContentNode>();
            for (int i = 0; i < a.Count; i++)
            {
                if (!(a[i] is JObject o)) throw Error(name + "[" + i + "]", "must be an object");
                list.Add(new ContentNode(File, P(name) + "[" + i + "]", o));
            }
            return list;
        }

        public JArray Array(string name)
        {
            if (!(Get(name) is JArray a)) throw Error(name, "must be an array");
            return a;
        }

        public string Str(string name)
        {
            var t = Get(name);
            if (t.Type != JTokenType.String) throw Error(name, "must be a string");
            var s = (string)t;
            if (s.Length == 0) throw Error(name, "must not be empty");
            return s;
        }

        public string OptStr(string name) => Has(name) ? Str(name) : null;

        public bool Bool(string name)
        {
            var t = Get(name);
            if (t.Type != JTokenType.Boolean) throw Error(name, "must be true or false");
            return (bool)t;
        }

        public int Int(string name, int min = 0)
        {
            var t = Get(name);
            if (t.Type != JTokenType.Integer) throw Error(name, "must be an integer");
            long v = (long)t;
            if (v < min) throw Error(name, min == 0 ? "must not be negative (got " + v + ")" : "must be at least " + min + " (got " + v + ")");
            if (v > int.MaxValue) throw Error(name, "is too large");
            return (int)v;
        }

        public float Float(string name, float min = 0f)
        {
            var t = Get(name);
            if (t.Type != JTokenType.Integer && t.Type != JTokenType.Float) throw Error(name, "must be a number");
            return CheckFloat(name, (float)t, min);
        }

        public float FloatAt(JToken t, string name, float min)
        {
            if (t.Type != JTokenType.Integer && t.Type != JTokenType.Float) throw Error(name, "must be a number");
            return CheckFloat(name, (float)t, min);
        }

        private float CheckFloat(string name, float v, float min)
        {
            if (float.IsNaN(v) || float.IsInfinity(v)) throw Error(name, "must be a finite number");
            if (v < min) throw Error(name, min == 0f ? "must not be negative (got " + v + ")" : "must be at least " + min + " (got " + v + ")");
            return v;
        }

        public T Enum<T>(string name) where T : struct
        {
            var s = Str(name);
            if (!System.Enum.TryParse<T>(s, false, out var v) || !System.Enum.IsDefined(typeof(T), v))
                throw Error(name, "unknown value '" + s + "' (expected one of " + string.Join(", ", System.Enum.GetNames(typeof(T))) + ")");
            return v;
        }

        public Size2 Size(string name)
        {
            var n = Obj(name);
            return new Size2(n.Int("w", 1), n.Int("d", 1));
        }
    }
}
