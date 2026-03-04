using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static PexInterface.PexReader;

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

   
        public static void DeFunctionCode(CodeGenStyle Style,List<PexString> TempStrings,PscCls ParentCls,FunctionBlock Func, DecompileTracker TrackerRef, bool CanSkipPscDeCode)
        {
//assign iSelect
//callmethod GetSize ::aaa_RDOPreventedActorsList_var ::temp269
//cmp_lt ::temp270 iSelect ::temp269
//jmpf ::temp270
//callmethod SetMenuDialogOptions self ::nonevar  ActorsPrevented
//array_getelement ::temp269 ActorsListIndex iSelect
//callmethod SetMenuDialogStartIndex self ::nonevar  ::temp269
//callmethod SetMenuDialogDefaultIndex self ::nonevar
//iadd ::temp269 iSelect
//assign iSelect ::temp269
//jmp 


//Int iSelect = 0 ; 
//While iSelect < aaa_RDOPreventedActorsList.GetSize() ; 
//Self.SetMenuDialogOptions(ActorsPrevented) ;
//Self.SetMenuDialogStartIndex(ActorsListIndex[iSelect]) ; 
//Self.SetMenuDialogDefaultIndex(0) ; 
//iSelect += 1 ; 
//EndWhile
            if (Func.FunctionName == "OnMenuOpenST")
            {
                if (CanSkipPscDeCode)
                {
                    return;
                }

                for (int i = 0; i < TrackerRef.Lines.Count; i++)
                {
                    var AsmLine = TrackerRef.Lines[i];

                    if (AsmLine != null)
                    {
                        if (AsmLine.Links != null)
                        {
                            var Head = AsmLine.Links.GetHead();
                            var Tail = AsmLine.Links.GetTail();

                            switch (AsmLine.OPCode)
                            {
                                case "assign":
                                    {
                                        string GetVariableName = Head.GetValue();
                                        TrackerRef.Variables.Add(new AsmOffset(i, 0), GetVariableName, 0, null, VariableAccess.None);
                                        if (Head.InFo != null)
                                        { 
                                        }
                                    }
                                    break;
                                case "callmethod":
                                    {
                                        int State = 0;
                                        AsmLine.Links.ForEachForward(new Action<AsmLink>((Link) => {
                                            if (!Link.IsNull() && !Link.IsSelf())
                                            {
                                                State++;
                                            }
                                        }));
                                        var GetFunctionName = AsmLine.Links.GetHead().GetValue();
                                        TrackerRef.Variables.Add(new AsmOffset(i, 0), GetFunctionName, State, null, VariableAccess.None);

                                        if (State == 3)
                                        {
                                            string GetVariableName = Tail.GetValue();

                                            //Test
                                            AsmLine.PSCCode = GetVariableName + " = " + Tail.Prev.GetValue() + "." + GetFunctionName + "();";

                                            TrackerRef.Variables.Add(new AsmOffset(i, 2), GetVariableName, State, new List<AsmLink>() {
                                                Tail.Prev,
                                                Head
                                            }, VariableAccess.Set);
                                        }
                                    }
                                    break;
                            }
                        }
                    }
                }

                //for (int i = 0; i < Keys.Count; i++)
                //{
                //    var Key = Keys[i];
                //    var Track = TrackerRef.Tracks[Key].TrackRef;

                //    string GetCodeLine = TrackerRef.Tracks[Keys[i]].Assembly;

                //    if (Track is TVariable)
                //    {
                //        var Variable = (TVariable)Track;

                //        var Get = TrackerRef.QueryMethodVariable(Variable.VariableName);
                //        if (Get == "" && Variable.VariableName.Length > 0)
                //        {
                //            var GlobalVariable = ParentCls.QueryGlobalVariable(Variable.VariableName);
                //            if (GlobalVariable == null)
                //            {
                //                var AutoVariable = ParentCls.QueryAutoGlobalVariable(Variable.VariableName);

                //                if (AutoVariable == null)
                //                {
                //                    TempVariables.Add(new TempVariable(Variable.VariableName, i));
                //                }
                //                else
                //                {
                                  
                //                }
                //            }
                //            else
                //            {
                               
                //            }
                //        }
                //        else
                //        { 
                        
                //        }
                //    }
                //    else
                //    if (Track is AsmCall)
                //    {

                //        //callmethod GetSize ::aaa_RDOPreventedActorsList_var ::temp269

                //        //While iSelect<aaa_RDOPreventedActorsList.GetSize() ;

                //        //callmethod SetMenuDialogStartIndex self::nonevar ::temp269
                //        //Self.SetMenuDialogStartIndex(ActorsListIndex[iSelect]) ; 
                //        //SetMenuDialogStartIndex(temp269);

                //        string PscCode = "";
                //        var Function = (AsmCall)Track;
                //        if (Function.Links.HaveValue())
                //        {
                //            if (Function.OPCode.Equals("callmethod"))
                //            {
                //                //int Index = 0;
                //                //bool Self = false;
                //                //string Params = "";

                //                //Function.Links.ForEachForward(new Action<AsmLink>((LinkItem) =>
                //                //{
                //                //    if (LinkItem.Value.StartsWith("::"))
                //                //    {
                //                //        if (!Self)
                //                //        {
                //                //            if (!LinkItem.IsNull())
                //                //            {
                //                //                if (!LinkItem.IsVar())
                //                //                {
                //                //                    PscCode = Function.Call + "." + LinkItem.GetValue() + "()";
                //                //                }
                //                //                else
                //                //                {
                //                //                    PscCode = LinkItem.GetValue() + "." + Function.Call + "()";
                //                //                }

                //                //                if ((LinkItem.Prev != null && !LinkItem.Prev.IsNull()) && PscCode.Length > 0)
                //                //                {
                //                //                    PscCode = LinkItem.Value + "&" + PscCode;
                //                //                }

                //                //                if (Style == CodeGenStyle.CSharp)
                //                //                {
                //                //                    PscCode += ";";
                //                //                }
                //                //            }
                //                //        }
                //                //        else
                //                //        {
                //                //            if (!LinkItem.IsNull())
                //                //            {
                //                //                Params += LinkItem.GetValue() + ",";
                //                //            }
                //                //        }
                //                //    }

                //                //    if (LinkItem.IsSelf())
                //                //    {
                //                //        Self = true;
                //                //    }

                //                //    Index++;
                //                //}));

                //                //if (Params.Length > 0)
                //                //{
                //                //    if (Params.EndsWith(","))
                //                //    {
                //                //        Params = Params.Substring(0, Params.Length - ",".Length);
                //                //    }

                //                //    PscCode = Function.Call + "(" + Params + ")";

                //                //    if (Style == CodeGenStyle.CSharp)
                //                //    {
                //                //        PscCode += ";";
                //                //    }
                //                //}

                //                //Function.PSCCode = PscCode;
                //            }
                //        }
                //        else
                //        { 
                        
                //        }
                //    }
                //    else
                //    if (Track is TProp)
                //    {

                //    }
                //    else
                //    if (Track is TStrcat)
                //    {

                //    }
                //    else
                //    if (Track is TOperator)
                //    {

                //    }
                //    else
                //    if (Track is TJump)
                //    {

                //    }
                //    else
                //    if (Track is TValIncrease)
                //    {

                //    }
                //    else
                //    if (Track is TReturn)
                //    {

                //    }
                //    else
                //    if (Track is TArrayOP)
                //    {

                //    }
                //    else
                //    if (Track is TVariableSetter)
                //    {

                //    }
                //}
            }
          
        }
    }
}
