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
        private Dictionary<float, Dictionary<string, ushort>> _opcodesCn;
        private Dictionary<float, Dictionary<string, ushort>> _opcodesKr;

        public Dictionary<string, ushort> CurrentOpcodes { get; set; }

        public float Version { get; private set; }

        public int Language { get; private set; }

        public OpcodeManager()
        {
            _opcodes = new Dictionary<float, Dictionary<string, ushort>>();
            _opcodesCn = new Dictionary<float, Dictionary<string, ushort>>();
            _opcodesKr = new Dictionary<float, Dictionary<string, ushort>>();
            LoadVersions();
        }

        private void LoadVersions()
        {
            var assembly = typeof(OpcodeManager).Assembly;
            foreach (var resource in assembly.GetManifestResourceNames())
            {
                if (!resource.Contains(".Opcodes."))
                    continue;

                string vendorString = resource.Substring(resource.IndexOf(".Opcodes.") + 9, 2).ToLower();
                if (vendorString != "cn" && vendorString != "kr")
                {
                    vendorString = "intl";
                }

                int versionStringOffset = vendorString == "intl" ? 9 : 12;

                string versionString = resource.Substring(resource.IndexOf(".Opcodes.") + versionStringOffset, 4);
                if (!float.TryParse(versionString, NumberStyles.Float, CultureInfo.InvariantCulture, out float version))
                    continue;

                using (Stream stream = assembly.GetManifestResourceStream(resource))
                {
                    using (StreamReader sr = new StreamReader(stream))
                    {
                        string[][] data = sr.ReadToEnd()
                            .Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(x => x.Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries)).ToArray();

                        var dict = data.ToDictionary(
                            x => x[0].Trim(),
                            x => Convert.ToUInt16(x[1].Trim(), 16));

                        if (vendorString == "cn")
                        {
                            _opcodesCn.Add(version, dict);
                        }
                        else if (vendorString == "kr")
                        {
                            _opcodesKr.Add(version, dict);
                        }
                        else
                        {
                            _opcodes.Add(version, dict);
                        }
                    }
                }

            }
        }
        public void SetVersion(float version, int language)
        {
            var opcodes = _opcodes;
            if (language == 5)
            {
                opcodes = _opcodesCn;
            }
            else if (language == 6)
            {
                opcodes = _opcodesKr;
            }

            float v = opcodes.Keys.OrderBy(x => x).Where(x => version <= x).FirstOrDefault();
            if (v == 0)
                v = opcodes.Keys.Max();

            Version = v;
            Language = language;

            CurrentOpcodes = opcodes[v];

            System.Diagnostics.Trace.WriteLine($"Using FFXIV Opcodes for game version {v}", "Machina");
        }

        public void SetVersion(float version)
        {
            SetVersion(version, 1);
        }
    }
}
