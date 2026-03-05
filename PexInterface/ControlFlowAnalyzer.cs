using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using static PexInterface.PexReader;

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
    public CfEvent(CfEventKind Kind, string Cond = "")
    {
        this.Kind = Kind;
        this.Condition = Cond;
    }
}

public class ControlFlowAnalyzer
{
    private Dictionary<int, List<CfEvent>> Events = new Dictionary<int, List<CfEvent>>();

    public void AddEvent(int LineIndex, CfEvent Ev)
    {
        if (!Events.ContainsKey(LineIndex))
            Events[LineIndex] = new List<CfEvent>();
        Events[LineIndex].Add(Ev);
    }

    public List<CfEvent> GetEvents(int LineIndex)
    {
        Events.TryGetValue(LineIndex, out var List);
        return List;
    }

    // Papyrus jump offset is relative to the instruction after the jump:
    //   absoluteTarget = JmpIndex + 1 + offset
    private static int AbsTarget(List<AsmCode> Lines, int JmpIndex)
    {
        int Offset = Lines[JmpIndex].GetJumpTarget();
        return JmpIndex + 1 + Offset;
    }

    public static bool IsCmp(string Op)
    {
        return Op == "cmp_eq" || Op == "cmp_lt" || Op == "cmp_le" ||
               Op == "cmp_gt" || Op == "cmp_ge";
    }

    private struct IfSegment
    {
        public int CmpLine;
        public int JmpfLine;
        public int FalseTarget;
    }

    // Collect consecutive if/elseif segments starting at StartCmp.
    // A segment is part of a chain only when its body ends with a jmp
    // (the exit jump that skips over remaining branches to the common end).
    private static List<IfSegment> CollectIfChain(List<AsmCode> Lines, int StartCmp, int N)
    {
        var Chain = new List<IfSegment>();
        int Cur = StartCmp;

        while (Cur < N && IsCmp(Lines[Cur].OPCode?.Value ?? ""))
        {
            int Jf = Cur + 1;
            if (Jf >= N || Lines[Jf].OPCode?.Value != "jmpf") break;

            int FalseTarget = AbsTarget(Lines, Jf);
            Chain.Add(new IfSegment { CmpLine = Cur, JmpfLine = Jf, FalseTarget = FalseTarget });

            int PrevFalse = FalseTarget - 1;
            bool HasExitJmp = PrevFalse >= 0 && PrevFalse < N &&
                               Lines[PrevFalse].OPCode?.Value == "jmp";
            if (!HasExitJmp) break;

            Cur = FalseTarget;
            if (Cur >= N || !IsCmp(Lines[Cur].OPCode?.Value ?? "")) break;
        }

        return Chain;
    }

    public static ControlFlowAnalyzer Analyze(List<AsmCode> Lines, Func<int, string> ResolveCondition)
    {
        var CFA = new ControlFlowAnalyzer();
        int N = Lines.Count;
        bool[] Handled = new bool[N];

        int I = 0;
        while (I < N)
        {
            if (Handled[I]) { I++; continue; }

            string Op = Lines[I].OPCode?.Value ?? "";

            if (!IsCmp(Op) || I + 1 >= N || Lines[I + 1].OPCode?.Value != "jmpf")
            {
                I++;
                continue;
            }

            int CmpLine = I;
            int JmpfLine = I + 1;
            int FalseTarget = AbsTarget(Lines, JmpfLine);

            // Detect while: a back-jump inside the body that targets <= CmpLine
            bool IsWhile = false;
            int WhileBackJmpLine = -1;

            for (int K = JmpfLine + 1; K < FalseTarget && K < N; K++)
            {
                if (Lines[K].OPCode?.Value == "jmp" && AbsTarget(Lines, K) <= CmpLine)
                {
                    IsWhile = true;
                    WhileBackJmpLine = K;
                    break;
                }
            }

            if (IsWhile)
            {
                CFA.AddEvent(CmpLine, new CfEvent(CfEventKind.WhileBegin, ResolveCondition(CmpLine)));
                CFA.AddEvent(WhileBackJmpLine, new CfEvent(CfEventKind.EndWhile));

                Handled[CmpLine] = true;
                Handled[JmpfLine] = true;
                Handled[WhileBackJmpLine] = true;
                I = CmpLine + 1;
                continue;
            }

            var Chain = CollectIfChain(Lines, CmpLine, N);

            if (Chain.Count == 1)
            {
                // Simple if, possibly with else
                var Seg = Chain[0];
                CFA.AddEvent(Seg.CmpLine, new CfEvent(CfEventKind.IfBegin, ResolveCondition(Seg.CmpLine)));
                Handled[Seg.CmpLine] = true;
                Handled[Seg.JmpfLine] = true;

                int PrevFalse = Seg.FalseTarget - 1;
                if (PrevFalse >= 0 && PrevFalse < N && Lines[PrevFalse].OPCode?.Value == "jmp")
                {
                    // jmp at end of if-body skips the else block
                    int ElseEnd = Math.Min(AbsTarget(Lines, PrevFalse) - 1, N - 1);
                    CFA.AddEvent(Seg.FalseTarget, new CfEvent(CfEventKind.ElseBegin));
                    CFA.AddEvent(ElseEnd, new CfEvent(CfEventKind.EndIf));
                    Handled[PrevFalse] = true;
                }
                else
                {
                    int EndIfLine = Math.Max(0, Math.Min(Seg.FalseTarget - 1, N - 1));
                    CFA.AddEvent(EndIfLine, new CfEvent(CfEventKind.EndIf));
                }
            }
            else
            {
                // First segment → If
                var First = Chain[0];
                CFA.AddEvent(First.CmpLine, new CfEvent(CfEventKind.IfBegin, ResolveCondition(First.CmpLine)));
                Handled[First.CmpLine] = true;
                Handled[First.JmpfLine] = true;

                // Remaining segments → ElseIf
                for (int Ci = 1; Ci < Chain.Count; Ci++)
                {
                    var Seg = Chain[Ci];
                    CFA.AddEvent(Seg.CmpLine, new CfEvent(CfEventKind.ElseIfBegin, ResolveCondition(Seg.CmpLine)));
                    Handled[Seg.CmpLine] = true;
                    Handled[Seg.JmpfLine] = true;
                }

                // All branches share a common exit; find it via the jmp at the end of each body
                int CommonEnd = -1;
                int Pf0 = Chain[0].FalseTarget - 1;
                if (Pf0 >= 0 && Pf0 < N && Lines[Pf0].OPCode?.Value == "jmp")
                {
                    CommonEnd = AbsTarget(Lines, Pf0) - 1;
                    Handled[Pf0] = true;
                }
                for (int Ci = 1; Ci < Chain.Count; Ci++)
                {
                    int PfN = Chain[Ci].FalseTarget - 1;
                    if (PfN >= 0 && PfN < N && Lines[PfN].OPCode?.Value == "jmp")
                    {
                        Handled[PfN] = true;
                        if (CommonEnd < 0) CommonEnd = AbsTarget(Lines, PfN) - 1;
                    }
                }
                if (CommonEnd < 0)
                    CommonEnd = Chain[Chain.Count - 1].FalseTarget - 1;

                CommonEnd = Math.Max(0, Math.Min(CommonEnd, N - 1));
                CFA.AddEvent(CommonEnd, new CfEvent(CfEventKind.EndIf));
            }

            I = CmpLine + 1;
        }

        return CFA;
    }
}

public static class ControlFlowCodeGen
{
    private static string CmpOp(string Op)
    {
        switch (Op)
        {
            case "cmp_lt": return "<";
            case "cmp_le": return "<=";
            case "cmp_gt": return ">";
            case "cmp_ge": return ">=";
            case "cmp_eq": return "==";
            default: return "?";
        }
    }

    // Build the condition string for a cmp instruction, inlining any temps
    private static string BuildCondition(DecompileTracker Tracker, int LineIndex, List<PexString> TempStrings)
    {
        var Line = Tracker.Lines[LineIndex];
        string Op = Line.OPCode?.Value ?? "";
        var Head = Line.Links?.GetHead();

        // cmp argument layout: [dest] [left] [right]
        string Left = ResolveTemp(Tracker, LineIndex, Head?.Next?.GetValue() ?? "?", TempStrings);
        string Right = ResolveTemp(Tracker, LineIndex, Head?.Next?.Next?.GetValue() ?? "?", TempStrings);

        return string.Format("{0} {1} {2}", Left, CmpOp(Op), Right);
    }

    // Walk backwards from FromLine to substitute a temp with its producing expression
    public static string ResolveTemp(DecompileTracker Tracker, int FromLine, string Name, List<PexString> TempStrings)
    {
        if (!Name.StartsWith("temp")) return Name;

        for (int J = FromLine - 1; J >= 0; J--)
        {
            var L = Tracker.Lines[J];
            string Lop = L.OPCode?.Value ?? "";

            if (Lop == "callmethod")
            {
                var Lh = L.Links.GetHead();
                var CallerNode = Lh?.Next;
                var RetNode = CallerNode?.Next;
                if (RetNode == null || RetNode.GetValue() != Name) continue;

                L.PSCCode = ""; // consumed: this line no longer emits standalone code

                string FuncName = Lh.GetValue();
                string CallerRaw = CallerNode?.GetValue() ?? "Self";
                string Caller = (CallerNode != null && CallerNode.IsSelf())
                                    ? "Self"
                                    : ResolveTemp(Tracker, J, CallerRaw, TempStrings);
                if (Caller.EndsWith("_var"))
                    Caller = Caller.Substring(0, Caller.Length - "_var".Length);

                var ParamList = new List<string>();
                var Node = RetNode.Next;
                while (Node != null)
                {
                    if (!Node.IsNull())
                        ParamList.Add(ResolveTemp(Tracker, J, Node.GetValue(), TempStrings));
                    Node = Node.Next;
                }
                return string.Format("{0}.{1}({2})", Caller, FuncName, string.Join(", ", ParamList));
            }

            if (Lop == "array_getelement")
            {
                var Lh = L.Links.GetHead();
                if (Lh.GetValue() != Name) continue;
                L.PSCCode = "";
                string Arr = ResolveTemp(Tracker, J, Lh.Next?.GetValue() ?? "?", TempStrings);
                string Idx = ResolveTemp(Tracker, J, Lh.Next?.Next?.GetValue() ?? "?", TempStrings);
                return string.Format("{0}[{1}]", Arr, Idx);
            }

            if (Lop == "assign")
            {
                var Lh = L.Links.GetHead();
                if (Lh.GetValue() != Name) continue;
                return ResolveTemp(Tracker, J, Lh.Next?.GetValue() ?? "?", TempStrings);
            }
        }
        return Name;
    }

    /// <summary>
    /// Single linear pass: prepend control-flow keywords (with indentation)
    /// to PSCCode, then inline any remaining temp references in body text.
    /// Indentation is embedded directly in PSCCode; SpaceCount is zeroed.
    /// </summary>
    public static void ApplyControlFlow(DecompileTracker Tracker, List<PexString> TempStrings)
    {
        var Lines = Tracker.Lines;
        int N = Lines.Count;

        var Cfa = ControlFlowAnalyzer.Analyze(Lines, Li => BuildCondition(Tracker, Li, TempStrings));
        int Depth = 0;

        for (int I = 0; I < N; I++)
        {
            var AsmLine = Lines[I];
            string OP = AsmLine.OPCode?.Value ?? "";
            var Events = Cfa.GetEvents(I);

            var NStringBuilder = new StringBuilder();

            if (Events != null)
            {
                foreach (var Event in Events)
                {
                    switch (Event.Kind)
                    {
                        case CfEventKind.EndIf:
                        case CfEventKind.EndWhile:
                            Depth = Math.Max(0, Depth - 1);
                            NStringBuilder.AppendLine(Indent(Depth) + (Event.Kind == CfEventKind.EndIf ? "EndIf" : "EndWhile"));
                            AsmLine.PSCCode = ""; // back-jump jmp / closing line emits nothing
                            break;

                        case CfEventKind.ElseBegin:
                            Depth = Math.Max(0, Depth - 1);
                            NStringBuilder.AppendLine(Indent(Depth) + "Else");
                            Depth++;
                            break;

                        case CfEventKind.ElseIfBegin:
                            Depth = Math.Max(0, Depth - 1);
                            NStringBuilder.AppendLine(Indent(Depth) + "ElseIf " + Event.Condition);
                            Depth++;
                            AsmLine.PSCCode = ""; // cmp instruction itself emits nothing
                            break;

                        case CfEventKind.IfBegin:
                            NStringBuilder.AppendLine(Indent(Depth) + "If " + Event.Condition);
                            Depth++;
                            AsmLine.PSCCode = "";
                            break;

                        case CfEventKind.WhileBegin:
                            NStringBuilder.AppendLine(Indent(Depth) + "While " + Event.Condition);
                            Depth++;
                            AsmLine.PSCCode = "";
                            break;
                    }
                }
            }

            // Jump opcodes are fully absorbed by control-flow events
            if (OP == "jmpf" || OP == "jmpt" || OP == "jmp")
                AsmLine.PSCCode = "";

            // Inline remaining temp references in the body expression
            if (!string.IsNullOrEmpty(AsmLine.PSCCode))
            {
                string Code = AsmLine.PSCCode;
                int EqPos = Code.IndexOf('=');
                if (EqPos >= 0)
                {
                    string Lhs = Code.Substring(0, EqPos + 1);
                    string Rhs = Regex.Replace(Code.Substring(EqPos + 1), @"\btemp\d+\b",
                                     M => ResolveTemp(Tracker, I, M.Value, TempStrings));
                    Code = Lhs + Rhs;
                }
                else
                {
                    Code = Regex.Replace(Code, @"\btemp\d+\b",
                               M => ResolveTemp(Tracker, I, M.Value, TempStrings));
                }
                AsmLine.PSCCode = Code;
            }

            // Append the body line at the current depth
            if (!string.IsNullOrEmpty(AsmLine.PSCCode))
                NStringBuilder.Append(Indent(Depth) + AsmLine.PSCCode.Trim());

            AsmLine.PSCCode = NStringBuilder.ToString().TrimEnd('\r', '\n');
            AsmLine.SpaceCount = 0;
        }
    }

    private static string Indent(int Depth) => new string(' ', Depth * 4);
}