using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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

   
        public static void DeFunctionCode(CodeGenStyle Style,PscCls ParentCls,FunctionBlock Func, DecompileTracker TrackerRef, bool CanSkipPscDeCode)
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

                        //callmethod GetSize ::aaa_RDOPreventedActorsList_var ::temp269

                        //While iSelect<aaa_RDOPreventedActorsList.GetSize() ;

                        //callmethod SetMenuDialogStartIndex self::nonevar ::temp269
                        //Self.SetMenuDialogStartIndex(ActorsListIndex[iSelect]) ; 
                        //SetMenuDialogStartIndex(temp269);

                        string PscCode = "";
                        var Function = (AsmCall)Track;
                        if (Function.Links.HaveValue())
                        {
                            if (Function.OPCode.Equals("callmethod"))
                            {
                                //int Index = 0;
                                //bool Self = false;
                                //string Params = "";

                                //Function.Links.ForEachForward(new Action<AsmLink>((LinkItem) =>
                                //{
                                //    if (LinkItem.Value.StartsWith("::"))
                                //    {
                                //        if (!Self)
                                //        {
                                //            if (!LinkItem.IsNull())
                                //            {
                                //                if (!LinkItem.IsVar())
                                //                {
                                //                    PscCode = Function.Call + "." + LinkItem.GetValue() + "()";
                                //                }
                                //                else
                                //                {
                                //                    PscCode = LinkItem.GetValue() + "." + Function.Call + "()";
                                //                }

                                //                if ((LinkItem.Prev != null && !LinkItem.Prev.IsNull()) && PscCode.Length > 0)
                                //                {
                                //                    PscCode = LinkItem.Value + "&" + PscCode;
                                //                }

                                //                if (Style == CodeGenStyle.CSharp)
                                //                {
                                //                    PscCode += ";";
                                //                }
                                //            }
                                //        }
                                //        else
                                //        {
                                //            if (!LinkItem.IsNull())
                                //            {
                                //                Params += LinkItem.GetValue() + ",";
                                //            }
                                //        }
                                //    }

                                //    if (LinkItem.IsSelf())
                                //    {
                                //        Self = true;
                                //    }

                                //    Index++;
                                //}));

                                //if (Params.Length > 0)
                                //{
                                //    if (Params.EndsWith(","))
                                //    {
                                //        Params = Params.Substring(0, Params.Length - ",".Length);
                                //    }

                                //    PscCode = Function.Call + "(" + Params + ")";

                                //    if (Style == CodeGenStyle.CSharp)
                                //    {
                                //        PscCode += ";";
                                //    }
                                //}

                                //Function.PSCCode = PscCode;
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
