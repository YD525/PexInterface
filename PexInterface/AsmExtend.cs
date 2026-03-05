using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
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


        public static void DeFunctionCode(CodeGenStyle Style, List<PexString> TempStrings, PscCls ParentCls, FunctionBlock Func, DecompileTracker TrackerRef, bool CanSkipPscDeCode)
        {
            string ASMCode = "";
            if (Func.FunctionName == "OnMenuOpenST")
            {
                for (int i = 0; i < TrackerRef.Lines.Count; i++)
                {
                    ASMCode += TrackerRef.Lines[i].GetAsmCode() + "\n";
                }
                GC.Collect();
            }
            ControlFlowTracker.IdentifyControlFlow(TrackerRef);

            if (CanSkipPscDeCode) return;

            for (int i = 0; i < TrackerRef.Lines.Count; i++)
            {
                var AsmLine = TrackerRef.Lines[i];
                if (AsmLine == null || AsmLine.Links == null) continue;

                var Head = AsmLine.Links.GetHead();
                var Tail = AsmLine.Links.GetTail();

                if (AsmLine.PSCCode.Length > 0)
                { 
                
                }

                switch (AsmLine.OPCode.Value)
                {
                    case "assign":
                        {
                            string VarName = Head.GetValue();
                            TrackerRef.Variables.Add(new AsmOffset(i, 0), VarName, 0, null, VariableAccess.None);

                            if (Head.Next == null)
                            {
                                AsmLine.PSCCode = string.Format("{0} = 0", VarName);
                            }
                            else
                            {
                                string RhsRaw = Head.Next.GetValue();

                                if (RhsRaw.StartsWith("temp"))
                                {
                                    for (int j = i - 1; j >= 0; j--)
                                    {
                                        var PrevLine = TrackerRef.Lines[j];
                                        if (PrevLine.OPCode.Value != "iadd") continue;

                                        var PrevHead = PrevLine.Links.GetHead();
                                        if (PrevHead.GetValue() != RhsRaw) continue;

                                        string Src = PrevHead.Next?.GetValue() ?? "?";
                                        string Amount = PrevHead.Next?.Next?.GetValue() ?? "1";

                                        PrevLine.PSCCode = "";

                                        if (Src == VarName)
                                            AsmLine.PSCCode = string.Format("{0} += {1};", VarName, Amount);
                                        else
                                            AsmLine.PSCCode = string.Format("{0} = {1} + {2};", VarName, Src, Amount);
                                        goto AssignDone;
                                    }
                                }

                                AsmLine.PSCCode = string.Format("{0} = {1}", VarName, RhsRaw);
                                AssignDone:;
                            }
                            break;
                        }

                    case "callmethod":
                        {
                            string FuncName = Head.GetValue();
                            var CallerNode = Head.Next;
                            string Caller = (CallerNode != null && CallerNode.IsSelf())
                                                ? "Self" : (CallerNode?.GetValue() ?? "Self");

                            var ReturnNode = CallerNode?.Next;
                            string ReturnVar = ReturnNode?.GetValue() ?? "";

                            var Params = new List<string>();
                            var Node = ReturnNode?.Next;
                            while (Node != null)
                            {
                                if (!Node.IsNull()) Params.Add(Node.GetValue());
                                Node = Node.Next;
                            }
                            string ParamStr = string.Join(", ", Params);
                            string CallExpr = string.Format("{0}.{1}({2})", Caller, FuncName, ParamStr);

                            if (ReturnNode == null || ReturnNode.IsNull())
                                AsmLine.PSCCode = CallExpr + ";";
                            else
                                AsmLine.PSCCode = string.Format("{0} = {1};", ReturnVar, CallExpr);

                            TrackerRef.Variables.Add(new AsmOffset(i, 2), ReturnVar, 0,
                                new List<AsmLink>() { CallerNode, Head }, VariableAccess.Set);
                            break;
                        }

                    case "iadd":
                        {
                            string Dest = Head.GetValue();
                            string Src = Head.Next?.GetValue() ?? "?";
                            string Amount = Head.Next?.Next?.GetValue() ?? "1";

                            if (Dest == Src)
                            {
                                AsmLine.PSCCode = string.Format("{0} += {1};", Dest, Amount);

                                if (!TrackerRef.Variables.IsCreated(Dest))
                                    TrackerRef.Variables.Add(new AsmOffset(i, 0), Dest, 0, null, VariableAccess.Set);

                                TrackerRef.Variables.SetType(TrackerRef, Dest, AsmVariableType.Int);
                            }
                            else
                            {
                                AsmLine.PSCCode = string.Format("{0} = {1} + {2};", Dest, Src, Amount);

                                if (!TrackerRef.Variables.IsCreated(Dest))
                                TrackerRef.Variables.Add(new AsmOffset(i, 0), Src, 0, null, VariableAccess.Set);

                                TrackerRef.Variables.SetType(TrackerRef, Dest, AsmVariableType.Int);
                                TrackerRef.Variables.SetType(TrackerRef, Src, AsmVariableType.Int);
                            }

                            break;
                        }

                    case "isub":
                        {
                            string VarName = Head.GetValue();
                            string Operand = Head.Next != null ? Head.Next.GetValue() : "1";
                            AsmLine.PSCCode = string.Format("{0} -= {1}", VarName, Operand);
                            break;
                        }                  
                    case "return":
                        {
                            if (Head.IsNull())
                                AsmLine.PSCCode = "Return";
                            else
                                AsmLine.PSCCode = string.Format("Return {0}", Head.GetValue());
                            break;
                        }

                    case "not":
                        {
                            string RetVar = Head.GetValue();
                            string Operand = Head.Next?.GetValue() ?? "?";
                            AsmLine.PSCCode = string.Format("{0} = !{1}", RetVar, Operand);
                            break;
                        }
                    case "callstatic":
                        {
                            //utility IsInMenuMode::temp332//callstatic utility IsInMenuMode::temp332
                            AsmLine.PSCCode = AsmLine.GetAsmCode();
                        }
                        break;

                    case "cmp_lt":
                    case "cmp_le":
                    case "cmp_gt":
                    case "cmp_ge":
                    case "cmp_eq":
                    case "jmpf":
                    case "jmpt":
                    case "jmp":
                        break;
                    default:
                        AsmLine.PSCCode = AsmLine.GetAsmCode();
                    break;
                }
            }

            ControlFlowTracker.MarkControlFlowOutput(TrackerRef);

           

            for (int i = 0; i < TrackerRef.Lines.Count; i++)
            {
                var AsmLine = TrackerRef.Lines[i];

                if (AsmLine.PSCCode.Contains("ELSE"))
                {

                }

                if (AsmLine == null) continue;

                string Op = AsmLine.OPCode.Value;

                var WhileBlock = FindBlockStartAt(TrackerRef, i, ControlBlockType.While);
                if (WhileBlock != null)
                {
                    var Head = AsmLine.Links.GetHead();
                    string CondLeft = Head.Next?.GetValue() ?? "?";
                    string CondRight = TryResolveTemp(TrackerRef, i,
                                           Head.Next?.Next?.GetValue() ?? "?", TempStrings);
                    if (string.IsNullOrEmpty(AsmLine.PSCCode))
                    {
                        AsmLine.PSCCode = string.Format("While {0} {1} {2}", CondLeft, CmpOp(Op), CondRight);
                    }
                    if (i + 1 < TrackerRef.Lines.Count)
                        TrackerRef.Lines[i + 1].PSCCode = "";
                    continue;
                }

                if (Op == "jmp")
                {
                    var WBlock = FindWhileEndAt(TrackerRef, i);
                    if (WBlock != null)
                    {
                        AsmLine.PSCCode = "EndWhile";
                        continue;
                    }
                    AsmLine.PSCCode = "";
                    continue;
                }

                var IfBlock = FindBlockStartAt(TrackerRef, i, ControlBlockType.If);
                if (IfBlock != null)
                {
                    var Head = AsmLine.Links.GetHead();
                    string CondLeft = Head.Next?.GetValue() ?? "?";
                    string CondRight = TryResolveTemp(TrackerRef, i,
                                           Head.Next?.Next?.GetValue() ?? "?", TempStrings);
                    if (string.IsNullOrEmpty(AsmLine.PSCCode))
                    {
                        AsmLine.PSCCode = string.Format("If {0} {1} {2}", CondLeft, CmpOp(Op), CondRight);
                    }
                    if (i + 1 < TrackerRef.Lines.Count)
                        TrackerRef.Lines[i + 1].PSCCode = "";
                    continue;
                }
            }

            for (int i = 0; i < TrackerRef.Lines.Count; i++)
            {
                var AsmLine = TrackerRef.Lines[i];
                if (string.IsNullOrEmpty(AsmLine?.PSCCode)) continue;

                string Code = AsmLine.PSCCode;
                int EqPos = Code.IndexOf('=');

                if (EqPos >= 0)
                {
                    string Left = Code.Substring(0, EqPos + 1);
                    string Right = Code.Substring(EqPos + 1);
                    Right = Regex.Replace(Right, @"\btemp\d+\b", m =>
                        TryResolveTemp(TrackerRef, i, m.Value, TempStrings));
                    AsmLine.PSCCode = Left + Right;
                }
                else
                {
                    AsmLine.PSCCode = Regex.Replace(Code, @"\btemp\d+\b", m =>
                        TryResolveTemp(TrackerRef, i, m.Value, TempStrings));
                }
            }
        }

        static ControlBlock FindBlockStartAt(DecompileTracker Tracker, int LineIndex, ControlBlockType Type)
        {
            foreach (var Block in Tracker.ControlFlows.GetAllBlocks())
                if (Block.Type == Type && Block.StartLine == LineIndex)
                    return Block;
            return null;
        }

        static ControlBlock FindWhileEndAt(DecompileTracker Tracker, int LineIndex)
        {
            foreach (var Block in Tracker.ControlFlows.GetAllBlocks())
                if (Block.Type == ControlBlockType.While && Block.EndLine == LineIndex)
                    return Block;
            return null;
        }
        static string TryResolveTemp(DecompileTracker Tracker, int FromLine,
                                string TempName, List<PexString> TempStrings)
        {
            if (!TempName.StartsWith("temp")) return TempName;

            for (int j = FromLine - 1; j >= 0; j--)
            {
                var L = Tracker.Lines[j];

                if (L.OPCode.Value == "callmethod")
                {
                    var LHead = L.Links.GetHead();
                    var CallerNode = LHead.Next;
                    var ReturnNode = CallerNode?.Next;

                    if (ReturnNode == null) continue;
                    if (ReturnNode.GetValue() != TempName) continue;

                    L.PSCCode = "";

                    string FuncName = LHead.GetValue();
                    string CallerRaw = CallerNode?.GetValue() ?? "Self";
                    string Caller = (CallerNode != null && CallerNode.IsSelf())
                                       ? "Self"
                                       : TryResolveTemp(Tracker, j, CallerRaw, TempStrings);

                    if (Caller.EndsWith("_var"))
                        Caller = Caller.Substring(0, Caller.Length - "_var".Length);

                    var Params = new List<string>();
                    var Node = ReturnNode.Next;
                    while (Node != null)
                    {
                        if (!Node.IsNull())
                            Params.Add(TryResolveTemp(Tracker, j, Node.GetValue(), TempStrings));
                        Node = Node.Next;
                    }
                    string ParamStr = string.Join(", ", Params);
                    return string.Format("{0}.{1}({2})", Caller, FuncName, ParamStr);
                }

                if (L.OPCode.Value == "array_getelement")
                {
                    var LHead = L.Links.GetHead();
                    if (LHead.GetValue() != TempName) continue;

                    L.PSCCode = "";  

                    string ArrName = TryResolveTemp(Tracker, j, LHead.Next?.GetValue() ?? "?", TempStrings);
                    string ArrIdx = TryResolveTemp(Tracker, j, LHead.Next?.Next?.GetValue() ?? "?", TempStrings);

                    return string.Format("{0}[{1}]", ArrName, ArrIdx);
                }
            }
            return TempName;
        }
        static string CmpOp(string OpCode)
        {
            switch (OpCode)
            {
                case "cmp_lt": return "<";
                case "cmp_le": return "<=";
                case "cmp_gt": return ">";
                case "cmp_ge": return ">=";
                case "cmp_eq": return "==";
                default: return "?";
            }
        }
    }
}
