using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Globalization;

namespace Machina.FFXIV.Headers.Opcodes
{
    public class OpcodeManager
    {
        public static OpcodeManager Instance { get; } = new OpcodeManager();

        private Dictionary<float, Dictionary<string, ushort>> _opcodes;

        public Dictionary<string, ushort> CurrentOpcodes { get; set; }

        public float Version { get; private set; }

        public OpcodeManager()
        {
            _opcodes = new Dictionary<float, Dictionary<string, ushort>>();
            LoadVersions();
        }

        private void LoadVersions()
        {
            var assembly = typeof(OpcodeManager).Assembly;
            foreach (var resource in assembly.GetManifestResourceNames())
            {
                if (!resource.Contains(".Opcodes."))
                    continue;

                string versionString = resource.Substring(resource.IndexOf(".Opcodes.") + 9, 4);
                if (!float.TryParse(versionString, NumberStyles.Float, CultureInfo.InvariantCulture, out float version))
                    continue;

                using (Stream stream = assembly.GetManifestResourceStream(resource))
                {
                    using (StreamReader sr = new StreamReader(stream))
                    {
                        string[][] data = sr.ReadToEnd()
                            .Split(new string[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(x => x.Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries)).ToArray();

                        var dict = data.ToDictionary(
                            x => x[0].Trim(),
                            x => Convert.ToUInt16(x[1].Trim(), 16));

                        _opcodes.Add(version, dict);
                    }
                }

            }
        }
        public void SetVersion(float version)
        {
            float v = _opcodes.Keys.OrderBy(x => x).Where(x => version <= x).FirstOrDefault();
            if (v == 0)
                v = _opcodes.Keys.Max();

            Version = v;

            CurrentOpcodes = _opcodes[v];

            System.Diagnostics.Trace.WriteLine($"Using FFXIV Opcodes for game version {v}", "Machina");
        }
    }
}
