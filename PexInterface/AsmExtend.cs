using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace PexInterface
{
    public class AsmExtend
    {
        public class TempVariable
        {
            public string Type = "";
            public string Variable = "";
            public int LinkLineIndex = 0;
            public TempVariable(string Variable,int LineIndex)
            {
                this.Variable = Variable;
                this.LinkLineIndex = LineIndex;
            }
        }

   
        public static void DeFunctionCode(PscCls ParentCls,FunctionBlock Func, DecompileTracker TrackerRef, bool CanSkipPscDeCode)
        {
            if (Func.FunctionName == "OnMenuOpenST")
            {
                if (CanSkipPscDeCode)
                {
                    return;
                }

                List<TempVariable> TempVariables = new List<TempVariable>();
                List<int> Keys = TrackerRef.Tracks.Keys.ToList();

                for (int i = 0; i < Keys.Count; i++)
                {
                    var Key = Keys[i];
                    var Track = TrackerRef.Tracks[Key].TrackRef;

                    string GetCodeLine = TrackerRef.Tracks[Keys[i]].Assembly;

                    if (Track is TVariable)
                    {
                        var Variable = (TVariable)Track;

                        var Get = TrackerRef.QueryMethodVariable(Variable.VariableName);
                        if (Get == "" && Variable.VariableName.Length > 0)
                        {
                            var GlobalVariable = ParentCls.QueryGlobalVariable(Variable.VariableName);
                            if (GlobalVariable == null)
                            {
                                var AutoVariable = ParentCls.QueryAutoGlobalVariable(Variable.VariableName);

                                if (AutoVariable == null)
                                {
                                    TempVariables.Add(new TempVariable(Variable.VariableName, i));
                                }
                                else
                                {
                                  
                                }
                            }
                            else
                            {
                               
                            }
                        }
                        else
                        { 
                        
                        }
                    }
                    else
                    if (Track is AsmCall)
                    {
                        var Function = (AsmCall)Track;
                        if (Function.Links.HaveValue())
                        {
                            if (Function.OPCode.Equals("callmethod"))
                            {
                                Function.Links.ForEachForward(new Action<AsmLink>((LinkItem) =>
                                {
                                    if (LinkItem.Value.StartsWith("::"))
                                    { 
                                        
                                    }
                                }));
                            }
                        }
                        else
                        { 
                        
                        }
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
