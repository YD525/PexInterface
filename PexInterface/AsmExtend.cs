using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PexInterface
{
    public class AsmExtend
    {
        public static void DeFunctionCode(FunctionBlock Func, DecompileTracker TrackerRef, bool CanSkipPscDeCode)
        {
            if (CanSkipPscDeCode)
            {
                return;
            }

            List<int> Keys = TrackerRef.Tracks.Keys.ToList();

            for (int i = 0; i < Keys.Count; i++)
            {
                var Track = TrackerRef.Tracks[Keys[i]];
                //.......
            }
        }
    }
}
