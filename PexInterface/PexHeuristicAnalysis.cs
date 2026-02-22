

using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace PexInterface
{
    // Copyright (c) 2026 YD525
    // Licensed under the LGPL3.0 License.
    // See LICENSE file in the project root for full license information.
    public class PexHeuristicAnalysis
    {
        public static string Version = "";

        public List<PexStringItem> Strings = new List<PexStringItem>();

        public void SaveAll(string Output)
        {
            int ModifyCount = 0;
            for (int i = 0; i < Strings.Count; i++)
            {
                if (Strings[i].Translated.Length > 0)
                {
                    PexInterop.ModifyStringTable((ushort)i, Strings[i].Translated);
                    ModifyCount++;
                }
            }
            if (ModifyCount > 0)
            {
                PexInterop.SavePex(Output);
            }
        }
    }


    public class PexStringItem
    {
        public int StringTableID = 0;
        public object Link = null;
        public int Score = 0;
        public string Original = "";
        public string Translated = "";
    }
}
