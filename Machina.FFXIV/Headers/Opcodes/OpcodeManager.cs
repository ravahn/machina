// Machina.FFXIV ~ OpcodeManager.cs
// 
// Copyright © 2020 Ravahn - All Rights Reserved
// 
//This program is free software: you can redistribute it and/or modify
//it under the terms of the GNU General Public License as published by
//the Free Software Foundation, either version 3 of the License, or
//(at your option) any later version.

//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
//GNU General Public License for more details.

//You should have received a copy of the GNU General Public License
//along with this program.If not, see<http://www.gnu.org/licenses/>.
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

        private Dictionary<GameRegionEnum, Dictionary<string, ushort>> _opcodes;

        public Dictionary<string, ushort> CurrentOpcodes { get; set; }

        public GameRegionEnum GameRegion { get; private set; }

        public OpcodeManager()
        {
            _opcodes = new Dictionary<GameRegionEnum, Dictionary<string, ushort>>();
            LoadVersions();
        }

        private void LoadVersions()
        {
            var assembly = typeof(OpcodeManager).Assembly;
            foreach (var resource in assembly.GetManifestResourceNames())
            {
                if (!resource.Contains(".Opcodes."))
                    continue;

                string regionString = resource.Substring(resource.IndexOf(".Opcodes.") + 9, resource.LastIndexOf(".")-resource.IndexOf(".Opcodes.") - 9);
                if (!Enum.TryParse<GameRegionEnum>(regionString, out GameRegionEnum gameRegion))
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

                        _opcodes.Add(gameRegion, dict);
                    }
                }

            }
        }
        public void SetRegion(GameRegionEnum region)
        {
            if (!_opcodes.ContainsKey(region))
                region = GameRegionEnum.Global;

            GameRegion = region;
            CurrentOpcodes = _opcodes[GameRegion];

            System.Diagnostics.Trace.WriteLine($"Using FFXIV Opcodes for game region {region}", "Machina");
        }
    }
}
