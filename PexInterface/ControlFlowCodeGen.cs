using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using static PexInterface.PexReader;

namespace PexInterface
{
    /// <summary>
    /// Reconstructs structured control flow independently from source rendering.
    /// </summary>
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

        private static string BuildCondition(
            DecompileTracker Tracker,
            int LineIndex,
            List<PexString> TempStrings)
        {
            var Head = Tracker.Lines[LineIndex].Links?.GetHead();
            string Left = ResolveTemp(Tracker, LineIndex, Head?.Next?.GetValue() ?? "?", TempStrings);
            string Right = ResolveTemp(Tracker, LineIndex, Head?.Next?.Next?.GetValue() ?? "?", TempStrings);
            string OPCode = Tracker.Lines[LineIndex].OPCode?.Value ?? "";
            return string.Format("{0} {1} {2}", Left, CmpOp(OPCode), Right);
        }
        /// <summary>
        /// Resolves a temporary variable to the expression that produced it.
        /// </summary>
        /// <param name="Tracker">The analyzed instruction sequence.</param>
        /// <param name="FromLine">The line before which definitions are searched.</param>
        /// <param name="Name">The temporary variable or literal expression.</param>
        /// <param name="TempStrings">The detached PEX string table.</param>
        /// <returns>The resolved expression, or <paramref name="Name"/> when it is not a temporary.</returns>
        public static string ResolveTemp(
        DecompileTracker Tracker,
        int FromLine,
        string Name,
        List<PexString> TempStrings)
        {
            if (!Name.StartsWith("temp")) return Name;

            for (int j = FromLine - 1; j >= 0; j--)
            {
                var Line = Tracker.Lines[j];
                string LineOP = Line.OPCode?.Value ?? "";
                var LineHead = Line.Links?.GetHead();
                if (LineHead == null) continue;

                if (LineOP == "callmethod")
                {
                    var CallerNode = LineHead.Next;
                    var RetNode = CallerNode?.Next;
                    if (RetNode == null || RetNode.GetValue() != Name) continue;

                    Line.PSCCode = "";

                    string FuncName = LineHead.GetValue();
                    string CallerRaw = CallerNode?.GetValue() ?? "Self";
                    string Caller = (CallerNode != null && CallerNode.IsSelf())
                                           ? "Self"
                                           : ResolveTemp(Tracker, j, CallerRaw, TempStrings);
                    if (Caller.EndsWith("_var"))
                        Caller = Caller.Substring(0, Caller.Length - "_var".Length);

                    var ParamList = BuildResolvedParams(Tracker, j, RetNode.Next?.Next, TempStrings);
                    return string.Format("{0}.{1}({2})", Caller, FuncName, string.Join(", ", ParamList));
                }

                if (LineOP == "callstatic")
                {
                    var FuncNode = LineHead.Next;
                    var RetNode = FuncNode?.Next;
                    if (RetNode == null || RetNode.GetValue() != Name) continue;

                    Line.PSCCode = "";

                    string ClassName = PapyrusAsmDecoder.CapitalizeFirst(LineHead.GetValue());
                    string FuncName = FuncNode?.GetValue() ?? "?";

                    var ParamList = BuildResolvedParams(Tracker, j, RetNode.Next?.Next, TempStrings);
                    return string.Format("{0}.{1}({2})", ClassName, FuncName, string.Join(", ", ParamList));
                }

                if (LineOP == "strcat")
                {
                    if (LineHead.GetValue() != Name) continue;
                    Line.PSCCode = "";

                    string Left = ResolveTemp(Tracker, j, LineHead.Next?.GetValue() ?? "?", TempStrings);
                    string Right = ResolveTemp(Tracker, j, LineHead.Next?.Next?.GetValue() ?? "?", TempStrings);
                    return string.Format("{0} + {1}", Left, Right);
                }

                if (LineOP == "cast")
                {
                    if (LineHead.GetValue() != Name) continue;
                    Line.PSCCode = "";
                    return ResolveTemp(Tracker, j, LineHead.Next?.GetValue() ?? "?", TempStrings);
                }

                if (LineOP == "propget")
                {
                    var ObjNode = LineHead.Next;
                    var DestNode = ObjNode?.Next;
                    if (DestNode == null || DestNode.GetValue() != Name) continue;
                    Line.PSCCode = "";

                    string PropName = LineHead.GetValue();
                    string Obj = (ObjNode?.GetValue() ?? "Self").ToLower() == "self"
                                          ? "Self" : ObjNode?.GetValue() ?? "Self";
                    return string.Format("{0}.{1}", Obj, PropName);
                }

                if (LineOP == "array_getelement")
                {
                    if (LineHead.GetValue() != Name) continue;
                    Line.PSCCode = "";

                    string Array = ResolveTemp(Tracker, j, LineHead.Next?.GetValue() ?? "?", TempStrings);
                    string Index = ResolveTemp(Tracker, j, LineHead.Next?.Next?.GetValue() ?? "?", TempStrings);
                    return string.Format("{0}[{1}]", Array, Index);
                }

                if (LineOP == "assign")
                {
                    if (LineHead.GetValue() != Name) continue;
                    Line.PSCCode = "";
                    return ResolveTemp(Tracker, j, LineHead.Next?.GetValue() ?? "?", TempStrings);
                }

                if (LineOP == "not")
                {
                    if (LineHead.GetValue() != Name) continue;
                    Line.PSCCode = "";
                    string Operand = LineHead.Next?.GetValue() ?? "?";
                    return string.Format("!{0}", Operand);
                }

                if (LineOP == "cmp_eq" || LineOP == "cmp_lt" || LineOP == "cmp_le" ||
                    LineOP == "cmp_gt" || LineOP == "cmp_ge")
                {
                    if (LineHead.GetValue() != Name) continue;
                    Line.PSCCode = "";

                    string Left = ResolveTemp(Tracker, j, LineHead.Next?.GetValue() ?? "?", TempStrings);
                    string Right = ResolveTemp(Tracker, j, LineHead.Next?.Next?.GetValue() ?? "?", TempStrings);
                    string Op;
                    switch (LineOP)
                    {
                        case "cmp_eq": Op = "=="; break;
                        case "cmp_lt": Op = "<"; break;
                        case "cmp_le": Op = "<="; break;
                        case "cmp_gt": Op = ">"; break;
                        case "cmp_ge": Op = ">="; break;
                        default: Op = "?"; break;
                    }
                    return string.Format("{0} {1} {2}", Left, Op, Right);
                }
            }

            return Name;
        }


        private static List<string> BuildResolvedParams(
            DecompileTracker Tracker,
            int FromLine,
            AsmLink StartNode,
            List<PexString> TempStrings)
        {
            var List = new List<string>();
            var Node = StartNode;
            while (Node != null)
            {
                if (!Node.IsNull())
                    List.Add(ResolveTemp(Tracker, FromLine, Node.GetValue(), TempStrings));
                Node = Node.Next;
            }
            return List;
        }

        /// <summary>
        /// Applies control-flow events, temporary resolution, and indentation in a single linear pass.
        /// </summary>
        /// <param name="Tracker">The analyzed instruction sequence to update.</param>
        /// <param name="TempStrings">The detached PEX string table.</param>
        /// <param name="Style">The syntax used for structured control-flow statements.</param>
        public static void ApplyControlFlow(
            DecompileTracker Tracker,
            List<PexString> TempStrings,
            ICodeStyle Style)
        {
            var Lines = Tracker.Lines;
            int Length = Lines.Count;

            var CFA = ControlFlowAnalyzer.Analyze(
                            Lines,
                            LineIndex => BuildCondition(Tracker, LineIndex, TempStrings), Tracker, TempStrings);

            int Depth = 0;
            bool NextLineIsSingleIfBody = false;

            for (int i = 0; i < Length; i++)
            {
                var AsmLine = Lines[i];
                string OPCode = AsmLine.OPCode?.Value ?? "";
                var Events = CFA.GetEvents(i);

                var NStringBuilder = new StringBuilder();

                // Emit structural statements before translating the body line.
                if (Events != null)
                {
                    foreach (var Event in Events)
                    {
                        switch (Event.Kind)
                        {
                            case CfEventKind.EndIf:
                                Depth = Math.Max(0, Depth - 1);
                                NStringBuilder.AppendLine(Style.Indent(Depth) + Style.EndIf());
                                break;

                            case CfEventKind.EndWhile:
                                Depth = Math.Max(0, Depth - 1);
                                NStringBuilder.AppendLine(Style.Indent(Depth) + Style.EndWhile());
                                AsmLine.PSCCode = "";
                                break;

                            case CfEventKind.ElseBegin:
                                Depth = Math.Max(0, Depth - 1);
                                NStringBuilder.AppendLine(Style.Indent(Depth) + Style.Else());
                                Depth++;
                                break;

                            case CfEventKind.ElseIfBegin:
                                Depth = Math.Max(0, Depth - 1);
                                NStringBuilder.AppendLine(Style.Indent(Depth) + Style.ElseIf(Event.Condition));
                                Depth++;
                                AsmLine.PSCCode = "";
                                break;

                            case CfEventKind.IfBegin:
                                if (Event.IsSingleLine)
                                {
                                    // No brace — depth stays the same; body gets Depth+1
                                    NStringBuilder.AppendLine(Style.Indent(Depth) + Style.IfSingleLine(Event.Condition));
                                    NextLineIsSingleIfBody = true;
                                }
                                else
                                {
                                    NStringBuilder.AppendLine(Style.Indent(Depth) + Style.If(Event.Condition));
                                    Depth++;
                                }
                                AsmLine.PSCCode = "";
                                break;

                            case CfEventKind.WhileBegin:
                                NStringBuilder.AppendLine(Style.Indent(Depth) + Style.While(Event.Condition));
                                Depth++;
                                AsmLine.PSCCode = "";
                                break;
                        }
                    }
                }

                // Jump opcodes are fully consumed by control-flow events
                if (OPCode == "jmpf" || OPCode == "jmpt" || OPCode == "jmp")
                    AsmLine.PSCCode = "";

                // Inline remaining temporaries after control-flow expressions have consumed them.
                if (!string.IsNullOrEmpty(AsmLine.PSCCode))
                {
                    string Code = AsmLine.PSCCode;
                    int EqPos = Code.IndexOf('=');

                    if (EqPos >= 0)
                    {
                        string LhsRaw = Code.Substring(0, EqPos + 1);
                        string LhsResolved = Regex.Replace(
                            LhsRaw,
                            @"\btemp\d+\b",
                            Match => ResolveTemp(Tracker, i, Match.Value, TempStrings));

                        string Rhs = Regex.Replace(
                            Code.Substring(EqPos + 1),
                            @"\btemp\d+\b",
                            Match => ResolveTemp(Tracker, i, Match.Value, TempStrings));

                        Code = LhsResolved + Rhs;
                    }
                    else
                    {
                        Code = Regex.Replace(
                            Code,
                            @"\btemp\d+\b",
                            Match => ResolveTemp(Tracker, i, Match.Value, TempStrings));
                    }

                    AsmLine.PSCCode = Code;
                }

                // Apply the final nesting depth to the body line.
                if (!string.IsNullOrEmpty(AsmLine.PSCCode))
                {
                    if (NextLineIsSingleIfBody)
                    {
                        // Indent one extra level; do not change Depth permanently
                        NStringBuilder.Append(Style.Indent(Depth + 1) + AsmLine.PSCCode.Trim());
                        NextLineIsSingleIfBody = false;
                    }
                    else
                    {
                        NStringBuilder.Append(Style.Indent(Depth) + AsmLine.PSCCode.Trim());
                    }
                }

                AsmLine.PSCCode = NStringBuilder.ToString().TrimEnd('\r', '\n');
                AsmLine.SpaceCount = 0;
            }
        }
    }
}
