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
            if (Func.FunctionName == "OnMenuOpenST")
            {
                if (CanSkipPscDeCode)
                {
                    return;
                }

                List<int> CheckPoint = new List<int>();
                List<int> Keys = TrackerRef.Tracks.Keys.ToList();

                for (int i = 0; i < Keys.Count; i++)
                {
                    var Key = Keys[i];
                    var Track = TrackerRef.Tracks[Key].TrackRef;

                    string GetCodeLine = TrackerRef.Tracks[Keys[i]].Assembly;

                    if (Track is TVariable)
                    {
                        var Get = TrackerRef.QueryVariables(((TVariable)Track).VariableName);
                        if (Get == "")
                        {
                            CheckPoint.Add(Key);
                        }
                    }
                    else
                    if (Track is TFunction)
                    {

                    }
                    else
                    if (Track is TProp)
                    {

                    }
                    else
                    if (Track is TStrcat)
                    {

                    }
                    else
                    if (Track is TOperator)
                    {

                    }
                    else
                    if (Track is TJump)
                    {

                    }
                    else
                    if (Track is TValIncrease)
                    {

                    }
                    else
                    if (Track is TReturn)
                    {

                    }
                    else
                    if (Track is TArrayOP)
                    {

                    }
                    else
                    if (Track is TVariableSetter)
                    {

                    }
                }
            }
          
        }
    }
}
