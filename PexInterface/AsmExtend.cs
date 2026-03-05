using System;
using System.Collections.Generic;
using System.Text;
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
            public TempVariable(string Variable, int LineIndex)
            {
                this.Variable = Variable;
                this.LinkLineIndex = LineIndex;
            }
        }

        public static void DeFunctionCode(CodeGenStyle Style, List<PexString> TempStrings, PscCls ParentCls, FunctionBlock Func, DecompileTracker TrackerRef, bool CanSkipPscDeCode)
        {
            if (Func.FunctionName == "RDO_ModCheck")
            {
                string ASMCode = "";
                for (int i = 0; i < TrackerRef.Lines.Count; i++)
                    ASMCode += TrackerRef.Lines[i].GetAsmCode() + "\n";
                GC.Collect();
            }

            if (CanSkipPscDeCode) return;

            int n = TrackerRef.Lines.Count;

            // Pass 1: translate each opcode to PSCCode; control-flow opcodes left empty
            for (int i = 0; i < n; i++)
            {
                var AsmLine = TrackerRef.Lines[i];
                if (AsmLine == null || AsmLine.Links == null) continue;

                var Head = AsmLine.Links.GetHead();

                switch (AsmLine.OPCode.Value)
                {
                    case "assign":
                        {
                            string VarName = Head.GetValue();
                            bool IsFirstAssign = !TrackerRef.Variables.IsCreated(VarName);
                            TrackerRef.Variables.Add(new AsmOffset(i, 0), VarName, 0, null, VariableAccess.None);

                            if (Head.Next == null)
                            {
                                AsmLine.PSCCode = string.Format("{0} = 0", VarName);
                            }
                            else
                            {
                                string RhsRaw = Head.Next.GetValue();

                                // fold preceding iadd into += when rhs is a temp
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

                                        AsmLine.PSCCode = Src == VarName
                                            ? string.Format("{0} += {1};", VarName, Amount)
                                            : string.Format("{0} = {1} + {2};", VarName, Src, Amount);
                                        goto AssignDone;
                                    }
                                }

                                // Initial assignment: Attempt to infer the type from the RHS and write the __TYPE__ placeholder.
                                if (IsFirstAssign && !VarName.StartsWith("::"))
                                {
                                    AsmLine.PSCCode = string.Format("__TYPE__{0} = {1}", VarName, RhsRaw);
                                }
                                else
                                {
                                    AsmLine.PSCCode = string.Format("{0} = {1}", VarName, RhsRaw);
                                }
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

                            // [3] is NumParams (integer literal) — skip it, params start at [4]
                            var Params = new List<string>();
                            var Node = ReturnNode?.Next?.Next;
                            while (Node != null)
                            {
                                if (!Node.IsNull()) Params.Add(Node.GetValue());
                                Node = Node.Next;
                            }
                            string ParamStr = string.Join(", ", Params);
                            string CallExpr = string.Format("{0}.{1}({2})", Caller, FuncName, ParamStr);

                            AsmLine.PSCCode = (ReturnNode == null || ReturnNode.IsNull())
                                ? CallExpr + ";"
                                : string.Format("{0} = {1};", ReturnVar, CallExpr);

                            TrackerRef.Variables.Add(new AsmOffset(i, 2), ReturnVar, 0,
                                new List<AsmLink> { CallerNode, Head }, VariableAccess.Set);
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
                        AsmLine.PSCCode = Head.IsNull()
                            ? "Return"
                            : string.Format("return {0};", Head.GetValue());
                        break;

                    case "not":
                        {
                            string RetVar = Head.GetValue();
                            string Operand = Head.Next?.GetValue() ?? "?";
                            AsmLine.PSCCode = string.Format("{0} = !{1}", RetVar, Operand);
                            break;
                        }

                    // callstatic layout: <ClassName> <FunctionName> <ReturnVar> <NumParams> [params...]
                    case "callstatic":
                        {
                            string ClassName = Head.GetValue();
                            var FuncNode = Head.Next;
                            string FuncName = FuncNode?.GetValue() ?? "?";
                            var RetNode = FuncNode?.Next;
                            string ReturnVar = RetNode?.GetValue() ?? "";

                            // [3] is NumParams — skip it, params start at [4]
                            var Params = new List<string>();
                            var Node = RetNode?.Next?.Next;
                            while (Node != null)
                            {
                                if (!Node.IsNull()) Params.Add(Node.GetValue());
                                Node = Node.Next;
                            }
                            string ParamStr = string.Join(", ", Params);
                            string CallExpr = string.Format("{0}.{1}({2})", ClassName, FuncName, ParamStr);

                            AsmLine.PSCCode = (RetNode == null || RetNode.IsNull())
                                ? CallExpr + ";"
                                : string.Format("{0} = {1};", ReturnVar, CallExpr);
                            break;
                        }

                    // cast layout: <dest> <src>  →  dest = src  (Papyrus cast is an assignment)
                    case "cast":
                        {
                            string Dest = Head.GetValue();
                            string Src = Head.Next?.GetValue() ?? "?";
                            AsmLine.PSCCode = string.Format("{0} = {1};", Dest, Src);
                            break;
                        }

                    // array_create layout: <dest> <size>
                    case "array_create":
                        {
                            string Dest = Head.GetValue();
                            string Size = Head.Next?.GetValue() ?? "?";
                            AsmLine.PSCCode = string.Format("{0} = new __ARRAY_TYPE__[{1}];", Dest, Size);
                            break;
                        }

                    // array_setelement layout: <array> <index> <value>
                    case "array_setelement":
                        {
                            string Arr = Head.GetValue();
                            string Index = Head.Next?.GetValue() ?? "?";
                            string Value = Head.Next?.Next?.GetValue() ?? "?";
                            AsmLine.PSCCode = string.Format("{0}[{1}] = {2};", Arr, Index, Value);
                            break;
                        }

                    // array_getelement layout: <dest> <array> <index>
                    case "array_getelement":
                        {
                            string Dest = Head.GetValue();
                            string Arr = Head.Next?.GetValue() ?? "?";
                            string Index = Head.Next?.Next?.GetValue() ?? "?";
                            AsmLine.PSCCode = string.Format("{0} = {1}[{2}];", Dest, Arr, Index);
                            break;
                        }

                    // array_length layout: <dest> <array>
                    case "array_length":
                        {
                            string Dest = Head.GetValue();
                            string Arr = Head.Next?.GetValue() ?? "?";
                            AsmLine.PSCCode = string.Format("{0} = {1}.Length;", Dest, Arr);
                            break;
                        }

                    // control-flow opcodes: handled entirely in pass 2
                    case "cmp_lt":
                    case "cmp_le":
                    case "cmp_gt":
                    case "cmp_ge":
                    case "cmp_eq":
                    case "jmpf":
                    case "jmpt":
                    case "jmp":
                        AsmLine.PSCCode = "";
                        break;

                    default:
                        AsmLine.PSCCode = AsmLine.GetAsmCode();
                        break;
                }
            }

            // Scan backwards for the first matching array_setelement, and infer the array element type based on the value type.
            for (int i = 0; i < n; i++)
            {
                var AsmLine = TrackerRef.Lines[i];
                if (AsmLine.OPCode.Value != "array_create") continue;
                if (!AsmLine.PSCCode.Contains("__ARRAY_TYPE__")) continue;

                string ArrayName = AsmLine.Links.GetHead().GetValue();
                string InferredType = "var"; 

                for (int j = i + 1; j < n; j++)
                {
                    var ScanLine = TrackerRef.Lines[j];
                    if (ScanLine.OPCode.Value != "array_setelement") continue;

                    var ScanHead = ScanLine.Links.GetHead();
                    string ScanArr = ScanHead.GetValue();
                    if (ScanArr != ArrayName) continue;

                    // array_setelement layout: [array] [index] [value]
                    var ValueNode = ScanHead.Next?.Next;
                    if (ValueNode == null) break;

                    string Val = ValueNode.GetValue();

                    // Inferring from the primitive type markers (strings with Type==2 are enclosed in quotes, Type==3 is an integer, etc.)
                    // It's more accurate to check the original Type field of the OPCode parameter.
                    if (ScanLine.OPCode.Arguments.Count >= 3)
                    {
                        var RawArg = ScanLine.OPCode.Arguments[2];
                        switch (RawArg.Type)
                        {
                            case 2: // string literal
                                InferredType = "String";
                                break;
                            case 3: // integer literal
                                InferredType = "Int";
                                break;
                            case 4: // float literal
                                InferredType = "Float";
                                break;
                            case 5: // bool literal
                                InferredType = "Bool";
                                break;
                            case 1: // identifier — Further determination based on value content
                                {
                                    // Try to find the type from GlobalVariable / AutoGlobalVariable
                                    var GlobalVar = ParentCls.QueryGlobalVariable(Val);
                                    if (GlobalVar != null && GlobalVar.Type.Length > 0)
                                    {
                                        InferredType = GlobalVar.Type;
                                    }
                                    else
                                    {
                                        var AutoVar = ParentCls.QueryAutoGlobalVariable(Val);
                                        if (AutoVar != null && AutoVar.Type.Length > 0)
                                            InferredType = AutoVar.Type;
                                        else
                                        {
                                            if (Val.StartsWith("$"))
                                                InferredType = "String";
                                        }
                                    }
                                    break;
                                }
                            case 0: // var
                                InferredType = "var";
                                break;
                            default:
                                // Finally, guess based on the value literal
                                if (Val.StartsWith("\"") || Val.StartsWith("$"))
                                    InferredType = "String";
                                else if (int.TryParse(Val, out _))
                                    InferredType = "Int";
                                else if (float.TryParse(Val, System.Globalization.NumberStyles.Float,
                                             System.Globalization.CultureInfo.InvariantCulture, out _))
                                    InferredType = "Float";
                                else if (Val.ToLower() == "true" || Val.ToLower() == "false")
                                    InferredType = "Bool";
                                break;
                        }
                    }
                    else
                    {
                        // Fallback to literal judgment when parameters are insufficient
                        if (Val.StartsWith("\"") || Val.StartsWith("$"))
                            InferredType = "String";
                        else if (int.TryParse(Val, out _))
                            InferredType = "Int";
                        else if (float.TryParse(Val, System.Globalization.NumberStyles.Float,
                                     System.Globalization.CultureInfo.InvariantCulture, out _))
                            InferredType = "Float";
                        else if (Val.ToLower() == "true" || Val.ToLower() == "false")
                            InferredType = "Bool";
                    }

                    break; 
                }

                AsmLine.PSCCode = AsmLine.PSCCode.Replace("__ARRAY_TYPE__", InferredType);
            }

            for (int i = 0; i < n; i++)
            {
                var AsmLine = TrackerRef.Lines[i];
                if (!AsmLine.PSCCode.StartsWith("__TYPE__")) continue;

                string Code = AsmLine.PSCCode.Substring("__TYPE__".Length);
                int EqIdx = Code.IndexOf('=');
                if (EqIdx < 0) { AsmLine.PSCCode = Code; continue; }

                string VarName = Code.Substring(0, EqIdx).Trim();
                string Rhs = Code.Substring(EqIdx + 1).Trim();

                string InferredType = InferTypeFromRhs(TrackerRef, i, Rhs, ParentCls, Func, TempStrings);

                if (InferredType.Length > 0)
                    AsmLine.PSCCode = string.Format("{0} {1} = {2};", InferredType, VarName, Rhs);
                else
                    AsmLine.PSCCode = string.Format("{0} = {1};", VarName, Rhs);
            }

            // Pass 2: control-flow analysis + emit indented keywords into PSCCode
            ControlFlowCodeGen.ApplyControlFlow(TrackerRef, TempStrings);
        }

        /// <summary>
        /// The variable type string is inferred from the RHS (which may be temp or a literal).
        /// Scan forward to find the source instruction that generated temp, and extract the return type from it.
        /// </summary>
        private static string InferTypeFromRhs(
            DecompileTracker Tracker,
            int FromLine,
            string Rhs,
            PscCls ParentCls,
            FunctionBlock Func,
            List<PexString> TempStrings)
        {
            // ── Direct judgment based on literal meaning
            if (Rhs.StartsWith("\"") || Rhs.StartsWith("$"))
                return "String";
            if (Rhs == "True" || Rhs == "False")
                return "Bool";
            if (int.TryParse(Rhs, out _))
                return "Int";
            if (float.TryParse(Rhs, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out _))
                return "Float";
            if (Rhs == "None")
                return "";

            // -- Non-temp identifiers: look up global variables 
            if (!Rhs.StartsWith("temp"))
            {
                var GV = ParentCls.QueryGlobalVariable(Rhs);
                if (GV != null && GV.Type.Length > 0) return GV.Type;
                var AV = ParentCls.QueryAutoGlobalVariable(Rhs);
                if (AV != null && AV.Type.Length > 0) return AV.Type;
                foreach (var P in Func.Params)
                    if (P.Name == Rhs) return P.Type;
                return "";
            }

            // ── temp: Tracing the source forward
            for (int J = FromLine - 1; J >= 0; J--)
            {
                var L = Tracker.Lines[J];
                string Lop = L.OPCode?.Value ?? "";
                var Lh = L.Links?.GetHead();
                if (Lh == null) continue;

                // callmethod: [FuncName] [Caller] [ReturnVar] [NumParams] [params...]
                if (Lop == "callmethod")
                {
                    var RetNode = Lh.Next?.Next;
                    if (RetNode == null || RetNode.GetValue() != Rhs) continue;
                    // Find the return type of a function with the same name within the same script
                    string CalledFuncName = Lh.GetValue();
                    foreach (var F in ParentCls.Functions)
                    {
                        if (F.FunctionName == CalledFuncName && F.ReturnType.Length > 0)
                            return F.ReturnType;
                    }
                    // If the native/external function cannot be found, ReturnType will be returned as null here, without declaration.
                    return "var";
                }

                // callstatic: [ClassName] [FuncName] [ReturnVar] [NumParams] [params...]
                if (Lop == "callstatic")
                {
                    var FuncNode = Lh.Next;
                    var RetNode = FuncNode?.Next;
                    if (RetNode == null || RetNode.GetValue() != Rhs) continue;
                    string CalledFuncName = FuncNode?.GetValue() ?? "";
                    foreach (var F in ParentCls.Functions)
                    {
                        if (F.FunctionName == CalledFuncName && F.ReturnType.Length > 0)
                            return F.ReturnType;
                    }
                    return "var";
                }

                // cast: [dest] [src] — recursively infers the type of src
                if (Lop == "cast")
                {
                    if (Lh.GetValue() != Rhs) continue;
                    return InferTypeFromRhs(Tracker, J, Lh.Next?.GetValue() ?? "", ParentCls, Func, TempStrings);
                }

                // assign: [dest] [src]
                if (Lop == "assign")
                {
                    if (Lh.GetValue() != Rhs) continue;
                    return InferTypeFromRhs(Tracker, J, Lh.Next?.GetValue() ?? "", ParentCls, Func, TempStrings);
                }
            }

            return "";
        }

    }

    // ─────────────────────────────────────────────────────────────────────
    // Control-flow event types
    // ─────────────────────────────────────────────────────────────────────
    public enum CfEventKind
    {
        IfBegin,
        ElseIfBegin,
        ElseBegin,
        EndIf,
        WhileBegin,
        EndWhile,
    }

    public class CfEvent
    {
        public CfEventKind Kind;
        public string Condition; // non-empty for If / ElseIf / While
        public CfEvent(CfEventKind kind, string cond = "")
        {
            Kind = kind;
            Condition = cond;
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // Single-pass control-flow analyzer.
    // ─────────────────────────────────────────────────────────────────────
    public class ControlFlowAnalyzer
    {
        private Dictionary<int, List<CfEvent>> _events = new Dictionary<int, List<CfEvent>>();

        public void AddEvent(int lineIndex, CfEvent ev)
        {
            if (!_events.ContainsKey(lineIndex))
                _events[lineIndex] = new List<CfEvent>();
            _events[lineIndex].Add(ev);
        }

        public List<CfEvent> GetEvents(int lineIndex)
        {
            _events.TryGetValue(lineIndex, out var list);
            return list;
        }

        // Papyrus jump offset is relative to the instruction after the jump:
        //   absoluteTarget = jmpIndex + 1 + offset
        private static int AbsTarget(List<AsmCode> lines, int jmpIdx)
            => jmpIdx + 1 + lines[jmpIdx].GetJumpTarget();

        public static bool IsCmp(string op)
            => op == "cmp_eq" || op == "cmp_lt" || op == "cmp_le" ||
               op == "cmp_gt" || op == "cmp_ge";

        private struct IfSeg { public int CmpLine, JmpfLine, FalseTarget; }

        private static List<IfSeg> CollectChain(List<AsmCode> lines, int startCmp, int n)
        {
            var chain = new List<IfSeg>();
            int cur = startCmp;

            while (cur < n && IsCmp(lines[cur].OPCode?.Value ?? ""))
            {
                int jf = cur + 1;
                if (jf >= n || lines[jf].OPCode?.Value != "jmpf") break;

                int ft = AbsTarget(lines, jf);
                chain.Add(new IfSeg { CmpLine = cur, JmpfLine = jf, FalseTarget = ft });

                int prev = ft - 1;
                if (prev < 0 || prev >= n || lines[prev].OPCode?.Value != "jmp") break;

                cur = ft;
                if (cur >= n || !IsCmp(lines[cur].OPCode?.Value ?? "")) break;
            }

            return chain;
        }

        public static ControlFlowAnalyzer Analyze(List<AsmCode> lines, Func<int, string> buildCondition)
        {
            var cfa = new ControlFlowAnalyzer();
            int n = lines.Count;
            bool[] handled = new bool[n];

            int i = 0;
            while (i < n)
            {
                if (handled[i]) { i++; continue; }

                string op = lines[i].OPCode?.Value ?? "";

                if (!IsCmp(op) || i + 1 >= n || lines[i + 1].OPCode?.Value != "jmpf")
                {
                    i++;
                    continue;
                }

                int cmpLine = i;
                int jmpfLine = i + 1;
                int falseTarget = AbsTarget(lines, jmpfLine);

                // Detect while: a back-jump inside the body that targets <= cmpLine
                bool isWhile = false;
                int whileJmpLine = -1;

                for (int k = jmpfLine + 1; k < falseTarget && k < n; k++)
                {
                    if (lines[k].OPCode?.Value == "jmp" && AbsTarget(lines, k) <= cmpLine)
                    {
                        isWhile = true;
                        whileJmpLine = k;
                        break;
                    }
                }

                if (isWhile)
                {
                    cfa.AddEvent(cmpLine, new CfEvent(CfEventKind.WhileBegin, buildCondition(cmpLine)));
                    cfa.AddEvent(whileJmpLine, new CfEvent(CfEventKind.EndWhile));

                    handled[cmpLine] = true;
                    handled[jmpfLine] = true;
                    handled[whileJmpLine] = true;
                    i = cmpLine + 1;
                    continue;
                }

                // If / ElseIf chain
                var chain = CollectChain(lines, cmpLine, n);

                if (chain.Count <= 1)
                {
                    // Simple if, possibly with else
                    cfa.AddEvent(cmpLine, new CfEvent(CfEventKind.IfBegin, buildCondition(cmpLine)));
                    handled[cmpLine] = true;
                    handled[jmpfLine] = true;

                    int prevFalse = falseTarget - 1;
                    if (prevFalse >= 0 && prevFalse < n && lines[prevFalse].OPCode?.Value == "jmp")
                    {
                        int elseEnd = Math.Min(AbsTarget(lines, prevFalse) - 1, n - 1);
                        cfa.AddEvent(falseTarget, new CfEvent(CfEventKind.ElseBegin));
                        cfa.AddEvent(elseEnd, new CfEvent(CfEventKind.EndIf));
                        handled[prevFalse] = true;
                    }
                    else
                    {
                        int endIfLine = Math.Max(0, Math.Min(falseTarget - 1, n - 1));
                        cfa.AddEvent(endIfLine, new CfEvent(CfEventKind.EndIf));
                    }
                }
                else
                {
                    // First segment → If
                    var first = chain[0];
                    cfa.AddEvent(first.CmpLine, new CfEvent(CfEventKind.IfBegin, buildCondition(first.CmpLine)));
                    handled[first.CmpLine] = true;
                    handled[first.JmpfLine] = true;

                    // Remaining segments → ElseIf
                    for (int ci = 1; ci < chain.Count; ci++)
                    {
                        var seg = chain[ci];
                        cfa.AddEvent(seg.CmpLine, new CfEvent(CfEventKind.ElseIfBegin, buildCondition(seg.CmpLine)));
                        handled[seg.CmpLine] = true;
                        handled[seg.JmpfLine] = true;
                    }

                    // All branches share a common exit
                    int commonEnd = -1;
                    int pf0 = chain[0].FalseTarget - 1;
                    if (pf0 >= 0 && pf0 < n && lines[pf0].OPCode?.Value == "jmp")
                    {
                        commonEnd = AbsTarget(lines, pf0) - 1;
                        handled[pf0] = true;
                    }
                    for (int ci = 1; ci < chain.Count; ci++)
                    {
                        int pfN = chain[ci].FalseTarget - 1;
                        if (pfN >= 0 && pfN < n && lines[pfN].OPCode?.Value == "jmp")
                        {
                            handled[pfN] = true;
                            if (commonEnd < 0) commonEnd = AbsTarget(lines, pfN) - 1;
                        }
                    }
                    if (commonEnd < 0)
                        commonEnd = chain[chain.Count - 1].FalseTarget - 1;

                    commonEnd = Math.Max(0, Math.Min(commonEnd, n - 1));
                    cfa.AddEvent(commonEnd, new CfEvent(CfEventKind.EndIf));
                }

                i = cmpLine + 1;
            }

            return cfa;
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // Merges control-flow keywords + indentation directly into PSCCode.
    // ─────────────────────────────────────────────────────────────────────
    public static class ControlFlowCodeGen
    {
        private static string CmpOp(string op)
        {
            switch (op)
            {
                case "cmp_lt": return "<";
                case "cmp_le": return "<=";
                case "cmp_gt": return ">";
                case "cmp_ge": return ">=";
                case "cmp_eq": return "==";
                default: return "?";
            }
        }

        private static string BuildCondition(DecompileTracker tracker, int lineIndex, List<PexString> tempStrings)
        {
            var line = tracker.Lines[lineIndex];
            string op = line.OPCode?.Value ?? "";
            var head = line.Links?.GetHead();

            // cmp argument layout: [dest] [left] [right]
            string left = ResolveTemp(tracker, lineIndex, head?.Next?.GetValue() ?? "?", tempStrings);
            string right = ResolveTemp(tracker, lineIndex, head?.Next?.Next?.GetValue() ?? "?", tempStrings);

            return string.Format("{0} {1} {2}", left, CmpOp(op), right);
        }

        // Walk backwards from fromLine to substitute a temp with its producing expression
        public static string ResolveTemp(DecompileTracker tracker, int fromLine, string name, List<PexString> tempStrings)
        {
            if (!name.StartsWith("temp")) return name;

            for (int j = fromLine - 1; j >= 0; j--)
            {
                var L = tracker.Lines[j];
                string lop = L.OPCode?.Value ?? "";

                if (lop == "callmethod")
                {
                    var lh = L.Links.GetHead();
                    var callerNode = lh?.Next;
                    var retNode = callerNode?.Next;
                    if (retNode == null || retNode.GetValue() != name) continue;

                    L.PSCCode = ""; // consumed: this line no longer emits standalone code

                    string funcName = lh.GetValue();
                    string callerRaw = callerNode?.GetValue() ?? "Self";
                    string caller = (callerNode != null && callerNode.IsSelf())
                                       ? "Self"
                                       : ResolveTemp(tracker, j, callerRaw, tempStrings);
                    if (caller.EndsWith("_var"))
                        caller = caller.Substring(0, caller.Length - "_var".Length);

                    var paramList = new List<string>();
                    // [3] is NumParams — skip it, params start at [4]
                    var node = retNode.Next?.Next;
                    while (node != null)
                    {
                        if (!node.IsNull())
                            paramList.Add(ResolveTemp(tracker, j, node.GetValue(), tempStrings));
                        node = node.Next;
                    }
                    return string.Format("{0}.{1}({2})", caller, funcName, string.Join(", ", paramList));
                }

                if (lop == "callstatic")
                {
                    var lh = L.Links.GetHead();
                    var FuncNode = lh?.Next;
                    var RetNode = FuncNode?.Next;
                    if (RetNode == null || RetNode.GetValue() != name) continue;

                    L.PSCCode = ""; // consumed

                    string ClassName = lh.GetValue();
                    string FuncName = FuncNode?.GetValue() ?? "?";

                    var ParamList = new List<string>();
                    // [3] is NumParams — skip it, params start at [4]
                    var Node = RetNode.Next?.Next;
                    while (Node != null)
                    {
                        if (!Node.IsNull())
                            ParamList.Add(ResolveTemp(tracker, j, Node.GetValue(), tempStrings));
                        Node = Node.Next;
                    }
                    return string.Format("{0}.{1}({2})", ClassName, FuncName, string.Join(", ", ParamList));
                }

                // cast: dest = src — inline src as the expression for dest
                if (lop == "cast")
                {
                    var lh = L.Links.GetHead();
                    if (lh.GetValue() != name) continue;
                    L.PSCCode = ""; // consumed
                    return ResolveTemp(tracker, j, lh.Next?.GetValue() ?? "?", tempStrings);
                }

                if (lop == "array_getelement")
                {
                    var lh = L.Links.GetHead();
                    if (lh.GetValue() != name) continue;
                    L.PSCCode = "";
                    string arr = ResolveTemp(tracker, j, lh.Next?.GetValue() ?? "?", tempStrings);
                    string idx = ResolveTemp(tracker, j, lh.Next?.Next?.GetValue() ?? "?", tempStrings);
                    return string.Format("{0}[{1}]", arr, idx);
                }

                if (lop == "assign")
                {
                    var lh = L.Links.GetHead();
                    if (lh.GetValue() != name) continue;
                    return ResolveTemp(tracker, j, lh.Next?.GetValue() ?? "?", tempStrings);
                }
            }
            return name;
        }

        /// <summary>
        /// Single linear pass: prepend control-flow keywords (with indentation)
        /// to PSCCode, then inline any remaining temp references in body text.
        /// After this runs, SpaceCount is 0 on every line.
        /// </summary>
        public static void ApplyControlFlow(DecompileTracker tracker, List<PexString> tempStrings)
        {
            var lines = tracker.Lines;
            int n = lines.Count;

            var cfa = ControlFlowAnalyzer.Analyze(lines, li => BuildCondition(tracker, li, tempStrings));
            int depth = 0;

            for (int i = 0; i < n; i++)
            {
                var asmLine = lines[i];
                string op = asmLine.OPCode?.Value ?? "";
                var events = cfa.GetEvents(i);

                var sb = new StringBuilder();

                if (events != null)
                {
                    foreach (var ev in events)
                    {
                        switch (ev.Kind)
                        {
                            case CfEventKind.EndIf:
                                depth = Math.Max(0, depth - 1);
                                sb.AppendLine(Indent(depth) + "}");
                                break;

                            case CfEventKind.EndWhile:
                                depth = Math.Max(0, depth - 1);
                                sb.AppendLine(Indent(depth) + "}");
                                asmLine.PSCCode = ""; // back-jump jmp line emits nothing
                                break;

                            case CfEventKind.ElseBegin:
                                depth = Math.Max(0, depth - 1);
                                sb.AppendLine(Indent(depth) + "Else");
                                depth++;
                                break;

                            case CfEventKind.ElseIfBegin:
                                depth = Math.Max(0, depth - 1);
                                sb.AppendLine(Indent(depth) + "Else\nIf " + ev.Condition);
                                depth++;
                                asmLine.PSCCode = ""; // cmp instruction itself emits nothing
                                break;

                            case CfEventKind.IfBegin:
                                sb.AppendLine(Indent(depth) + "If (" + ev.Condition + "){");
                                depth++;
                                asmLine.PSCCode = "";
                                break;

                            case CfEventKind.WhileBegin:
                                sb.AppendLine(Indent(depth) + "While (" + ev.Condition + "){");
                                depth++;
                                asmLine.PSCCode = "";
                                break;
                        }
                    }
                }

                // Jump opcodes are fully absorbed by control-flow events
                if (op == "jmpf" || op == "jmpt" || op == "jmp")
                    asmLine.PSCCode = "";

                // Inline remaining temp references in the body expression
                if (!string.IsNullOrEmpty(asmLine.PSCCode))
                {
                    string code = asmLine.PSCCode;
                    int eqPos = code.IndexOf('=');
                    if (eqPos >= 0)
                    {
                        string lhs = code.Substring(0, eqPos + 1);
                        string rhs = Regex.Replace(code.Substring(eqPos + 1), @"\btemp\d+\b",
                                         m => ResolveTemp(tracker, i, m.Value, tempStrings));
                        code = lhs + rhs;
                    }
                    else
                    {
                        code = Regex.Replace(code, @"\btemp\d+\b",
                                   m => ResolveTemp(tracker, i, m.Value, tempStrings));
                    }
                    asmLine.PSCCode = code;
                }

                // Append the body line at the current depth
                if (!string.IsNullOrEmpty(asmLine.PSCCode))
                    sb.Append(Indent(depth) + asmLine.PSCCode.Trim());

                // Write back: indentation is now embedded in PSCCode
                asmLine.PSCCode = sb.ToString().TrimEnd('\r', '\n');
                asmLine.SpaceCount = 0;
            }
        }

        private static string Indent(int depth) => new string(' ', depth * 4);
    }
}