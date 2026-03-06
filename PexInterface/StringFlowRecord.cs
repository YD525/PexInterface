using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PexInterface
{
    // =========================================================================
    // StringFlowAnalyzer — heuristic analysis for string literal propagation
    // Only processes AsmCode where any argument starts with '"'
    // =========================================================================

    /// <summary>
    /// A single If/While condition expression that references the tracked variable
    /// or the original string literal.
    /// </summary>
    public class StringFlowCondition
    {
        /// <summary>
        /// The full condition expression as it appears in source,
        /// e.g. "Temp8 == \"cloaks.esp\""  or  "Temp8"  or  "!Temp8"
        /// </summary>
        public string ConditionExpression = "";

        /// <summary>
        /// The opcode that produced this condition: cmp_eq / cmp_lt / cmp_le /
        /// cmp_gt / cmp_ge, or "jmpf"/"jmpt" for bare bool jumps.
        /// </summary>
        public string CmpOpCode = "";

        /// <summary>
        /// Line index of the cmp_* (or bare jmpf/jmpt) instruction.
        /// </summary>
        public int CmpLineIndex = -1;

        /// <summary>
        /// True when this condition belongs to a While loop rather than an If.
        /// Determined by whether a back-jump inside the body targets back to
        /// this condition (same heuristic used by ControlFlowAnalyzer).
        /// </summary>
        public bool IsWhile = false;

        /// <summary>True when this condition belongs to an If / ElseIf block.</summary>
        public bool IsIf => !IsWhile;

        public override string ToString()
            => string.Format("{0} [{1}] @ line {2} -> {3}",
                ConditionExpression, CmpOpCode, CmpLineIndex,
                IsWhile ? "while" : "if");
    }

    /// <summary>
    /// Records the flow of a single string literal found in the instruction stream.
    /// Only emitted for instructions that contain at least one double-quote argument.
    /// </summary>
    public class StringFlowRecord
    {
        /// <summary>The original string literal value, e.g. "Hello"</summary>
        public string LiteralValue = "";

        /// <summary>Line index where this literal first appears.</summary>
        public int SourceLineIndex = -1;

        /// <summary>
        /// The chain of variable assignments the literal passes through,
        /// in order: [temp5, Temp333, Temp222, ...].
        /// Empty when the literal is passed directly into a call with no prior assign.
        /// </summary>
        public List<string> AssignmentChain = new List<string>();

        /// <summary>
        /// The final resolved variable name (last item in AssignmentChain),
        /// or the literal itself if used directly without assignment.
        /// </summary>
        public string FinalVariable => AssignmentChain.Count > 0
            ? AssignmentChain[AssignmentChain.Count - 1]
            : LiteralValue;

        /// <summary>
        /// The method call that consumes the final variable, if any.
        /// Format: "Caller.MethodName(arg0, arg1, ...)"
        /// </summary>
        public string ConsumedByCall = null;

        /// <summary>Line index of the consuming call instruction, or -1.</summary>
        public int ConsumedAtLine = -1;

        /// <summary>
        /// The method/function name that ultimately consumes this string value,
        /// e.g. "SetCosCloaks", "GetFormFromFile".
        /// </summary>
        public string ConsumedByMethodName = null;

        /// <summary>
        /// The caller/class of the consuming method (first-letter capitalised),
        /// e.g. "Self", "Game".
        /// </summary>
        public string ConsumedByCallerName = null;

        /// <summary>
        /// 0-based argument index the final variable occupies in the consuming call.
        /// -1 if the variable is the caller object itself.
        /// </summary>
        public int ConsumedAtArgIndex = -1;

        /// <summary>
        /// Every If / ElseIf / While condition that references any variable in
        /// AssignmentChain or the literal itself, found anywhere after SourceLineIndex.
        /// </summary>
        public List<StringFlowCondition> RelatedConditions = new List<StringFlowCondition>();

        /// <summary>
        /// Global variables (:: prefixed or found via ParentCls) that participate
        /// in the assignment chain alongside the literal.
        /// </summary>
        public List<string> GlobalVariablesInvolved = new List<string>();

        /// <summary>
        /// Local variables (non-temp, non-global) created within the same function
        /// that appear in the chain.
        /// </summary>
        public List<string> LocalVariablesInvolved = new List<string>();

        /// <summary>StringID of the AsmLink node that carries the literal value.</summary>
        public ushort StringID = 0;

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendFormat("[StringFlow] Literal={0}  SourceLine={1}\n",
                LiteralValue, SourceLineIndex);
            sb.AppendFormat("  Chain      : {0}\n",
                AssignmentChain.Count > 0 ? string.Join(" -> ", AssignmentChain) : "(direct)");
            sb.AppendFormat("  Final      : {0}\n", FinalVariable);
            sb.AppendFormat("  ConsumedBy : {0} @ line {1}\n",
                ConsumedByCall ?? "none", ConsumedAtLine);
            if (ConsumedByMethodName != null)
                sb.AppendFormat("  MethodName : {0}.{1}  ArgIndex={2}\n",
                    ConsumedByCallerName ?? "?", ConsumedByMethodName, ConsumedAtArgIndex);
            if (RelatedConditions.Count > 0)
            {
                sb.AppendLine("  Conditions :");
                foreach (var c in RelatedConditions)
                    sb.AppendFormat("    {0}\n", c);
            }
            if (GlobalVariablesInvolved.Count > 0)
                sb.AppendFormat("  Globals    : {0}\n", string.Join(", ", GlobalVariablesInvolved));
            if (LocalVariablesInvolved.Count > 0)
                sb.AppendFormat("  Locals     : {0}\n", string.Join(", ", LocalVariablesInvolved));
            return sb.ToString();
        }
    }

    public static class StringFlowAnalyzer
    {
        // ─────────────────────────────────────────────────────────────────────
        // cmp opcode → readable operator
        // ─────────────────────────────────────────────────────────────────────

        private static string CmpOperator(string op)
        {
            switch (op)
            {
                case "cmp_eq": return "==";
                case "cmp_lt": return "<";
                case "cmp_le": return "<=";
                case "cmp_gt": return ">";
                case "cmp_ge": return ">=";
                default: return "?";
            }
        }

        private static readonly HashSet<string> CmpOpcodes = new HashSet<string>
        {
            "cmp_eq", "cmp_lt", "cmp_le", "cmp_gt", "cmp_ge"
        };

        // ─────────────────────────────────────────────────────────────────────
        // Public entry point
        // ─────────────────────────────────────────────────────────────────────

        public static List<StringFlowRecord> Analyze(
            DecompileTracker tracker,
            PscCls parentCls,
            FunctionBlock func)
        {
            var results = new List<StringFlowRecord>();
            var lines = tracker.Lines;
            int n = lines.Count;

            for (int i = 0; i < n; i++)
            {
                var line = lines[i];
                if (line?.Links == null) continue;

                var strNodes = CollectStringNodes(line);
                if (strNodes.Count == 0) continue;

                foreach (var node in strNodes)
                {
                    var record = new StringFlowRecord
                    {
                        LiteralValue = node.GetValue(),
                        SourceLineIndex = i,
                        StringID = node.StringID
                    };

                    // ── Case A ────────────────────────────────────────────────
                    // Literal is a direct argument to a call on this line.
                    if (TryRecordDirectCallConsumer(line, node.GetValue(), i, record))
                    {
                        // Chain stays empty; literal flows straight into the call.
                    }
                    else
                    {
                        // ── Case B ────────────────────────────────────────────
                        // Literal is on the RHS of an assignment; trace forward.
                        string destVar = GetDestinationVariable(line);
                        if (!string.IsNullOrEmpty(destVar))
                        {
                            record.AssignmentChain.Add(destVar);
                            ForwardTrace(tracker, i + 1, n, destVar, record, parentCls, func);
                        }
                    }

                    // Scan the whole function body for If/While conditions that
                    // reference any variable in the chain or the literal itself.
                    ScanConditions(tracker, i, n, record);

                    ClassifyChainVariables(record, parentCls, func);
                    results.Add(record);
                }
            }

            return results;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Case A — literal used directly as a call argument on the same line
        // ─────────────────────────────────────────────────────────────────────

        private static bool TryRecordDirectCallConsumer(
            AsmCode line,
            string literal,
            int lineIndex,
            StringFlowRecord record)
        {
            string op = line.OPCode?.Value ?? "";
            var head = line.Links?.GetHead();
            if (head == null) return false;

            if (op == "callstatic")
            {
                // [className][funcName][retVar][skip][arg0][arg1]…
                var funcNode = head.Next;
                var retNode = funcNode?.Next;
                var firstArgNode = retNode?.Next?.Next;

                int argIdx = NodeListIndexOf(firstArgNode, literal);
                if (argIdx < 0) return false;

                string className = PapyrusAsmDecoder.CapitalizeFirst(head.GetValue());
                string funcName = PapyrusAsmDecoder.CapitalizeFirst(funcNode?.GetValue() ?? "?");
                var paramStrs = CollectNodeValuesCap(firstArgNode);

                record.ConsumedByCall = string.Format("{0}.{1}({2})",
                                                  className, funcName,
                                                  string.Join(", ", paramStrs));
                record.ConsumedAtLine = lineIndex;
                record.ConsumedByMethodName = funcName;
                record.ConsumedByCallerName = className;
                record.ConsumedAtArgIndex = argIdx;
                return true;
            }

            if (op == "callmethod")
            {
                // [funcName][caller][retVar][skip][arg0][arg1]…
                var callerNode = head.Next;
                var retNode = callerNode?.Next;
                var firstArgNode = retNode?.Next?.Next;

                int argIdx = NodeListIndexOf(firstArgNode, literal);
                if (argIdx < 0) return false;

                string funcName = PapyrusAsmDecoder.CapitalizeFirst(head.GetValue());
                string caller = (callerNode != null && callerNode.IsSelf())
                                      ? "Self"
                                      : PapyrusAsmDecoder.CapitalizeFirst(
                                            callerNode?.GetValue() ?? "Self");
                var paramStrs = CollectNodeValuesCap(firstArgNode);

                record.ConsumedByCall = string.Format("{0}.{1}({2})",
                                                  caller, funcName,
                                                  string.Join(", ", paramStrs));
                record.ConsumedAtLine = lineIndex;
                record.ConsumedByMethodName = funcName;
                record.ConsumedByCallerName = caller;
                record.ConsumedAtArgIndex = argIdx;
                return true;
            }

            return false;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Scan for If / While conditions that mention the tracked variable(s)
        // or the literal itself anywhere after the source line.
        //
        // Three sub-cases handled:
        //
        //  1. cmp_* [dest] [left] [right]  (followed by jmpf/jmpt)
        //     Matches when left or right is in watched set.
        //     The jmpf/jmpt that follows is intentionally NOT processed again
        //     in sub-case 2 to avoid duplicate records.
        //
        //  2. Bare jmpf / jmpt  (NOT preceded by a cmp_* on the line above)
        //     condVar = the single operand of jmpf/jmpt.
        //     Matches when condVar equals the FinalVariable OR the literal.
        //     → covers:  if (tempXX)  where tempXX IS the final traced variable
        //     → covers:  if ("xxx")   where the literal itself is the branch var
        //       (unusual but syntactically possible in some compilers)
        //
        // IsWhile detection:
        //   Scan forward from the jmpf/jmpt to falseTarget; if any unconditional
        //   jmp in that range jumps back to <= the condition line it is a while.
        // ─────────────────────────────────────────────────────────────────────

        private static void ScanConditions(
            DecompileTracker tracker,
            int fromLine,
            int n,
            StringFlowRecord record)
        {
            var lines = tracker.Lines;

            // watched = all chain variables + the literal value itself
            var watched = new HashSet<string>(record.AssignmentChain);
            watched.Add(record.LiteralValue);

            // finalVar is the last variable in the chain (or the literal if no chain).
            // Bare jmpf/jmpt is only matched against finalVar or the literal —
            // intermediate temps like temp5 are usually not used directly in a
            // bare-bool branch.
            string finalVar = record.FinalVariable;

            for (int i = fromLine; i < n; i++)
            {
                var line = lines[i];
                if (line?.Links == null) continue;

                string op = line.OPCode?.Value ?? "";
                var head = line.Links?.GetHead();
                if (head == null) continue;

                // ── Sub-case 1: cmp_* ────────────────────────────────────────
                if (CmpOpcodes.Contains(op))
                {
                    // Layout: [dest] [left] [right]
                    string left = head.Next?.GetValue() ?? "";
                    string right = head.Next?.Next?.GetValue() ?? "";

                    if (!watched.Contains(left) && !watched.Contains(right))
                    {
                        // Even if this cmp didn't match, add its dest to watched
                        // so that a later jmpf on that dest can still be caught.
                        // (No — only add if the cmp relates to our values.)
                        continue;
                    }

                    string leftCap = ShouldCapitalize(left)
                                          ? PapyrusAsmDecoder.CapitalizeFirst(left) : left;
                    string rightCap = ShouldCapitalize(right)
                                          ? PapyrusAsmDecoder.CapitalizeFirst(right) : right;
                    string expr = string.Format("{0} {1} {2}",
                                          leftCap, CmpOperator(op), rightCap);

                    bool isWhile = DetectWhileFromConditionLine(lines, i, n);

                    record.RelatedConditions.Add(new StringFlowCondition
                    {
                        ConditionExpression = expr,
                        CmpOpCode = op,
                        CmpLineIndex = i,
                        IsWhile = isWhile
                    });

                    // Add cmp dest to watched so a jmpf(dest) is also tracked.
                    string cmpDest = head.GetValue();
                    if (!string.IsNullOrEmpty(cmpDest))
                        watched.Add(cmpDest);

                    // Skip the jmpf/jmpt on the next line — already represented
                    // by the cmp_* entry we just added above.
                    if (i + 1 < n)
                    {
                        string nextOp = lines[i + 1].OPCode?.Value ?? "";
                        if (nextOp == "jmpf" || nextOp == "jmpt")
                            i++; // consume the paired jump so sub-case 2 never sees it
                    }

                    continue;
                }

                // ── Sub-case 2: bare jmpf / jmpt ────────────────────────────
                // Only handle when the previous instruction was NOT a cmp_*
                // (if it was, the cmp branch above already consumed the jump).
                if (op == "jmpf" || op == "jmpt")
                {
                    string condVar = head.GetValue();

                    // Match only on FinalVariable or the literal itself.
                    // This covers:
                    //   if (tempXX)      ← tempXX is the last variable in chain
                    //   if ("someStr")   ← bare string literal used as condition
                    bool matchesFinal = condVar == finalVar;
                    bool matchesLiteral = condVar == record.LiteralValue;

                    if (!matchesFinal && !matchesLiteral)
                        continue;

                    // jmpf  → jumps when false  → body executes when true  → "if (condVar)"
                    // jmpt  → jumps when true   → body executes when false → "if (!condVar)"
                    string expr = (op == "jmpt")
                        ? "!" + (ShouldCapitalize(condVar)
                                     ? PapyrusAsmDecoder.CapitalizeFirst(condVar)
                                     : condVar)
                        : (ShouldCapitalize(condVar)
                               ? PapyrusAsmDecoder.CapitalizeFirst(condVar)
                               : condVar);

                    bool isWhile = DetectWhileFromJumpLine(lines, i, n);

                    record.RelatedConditions.Add(new StringFlowCondition
                    {
                        ConditionExpression = expr,
                        CmpOpCode = op,
                        CmpLineIndex = i,
                        IsWhile = isWhile
                    });
                }
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // While detection helpers
        //
        // A condition at line C is a while-condition when, between C+1 and the
        // false-target of its associated jump, there exists an unconditional jmp
        // whose target is <= C (i.e. it jumps back to the condition).
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Detect while for a cmp_* at <paramref name="cmpLine"/>.
        /// The paired jmpf/jmpt is expected at cmpLine+1.
        /// </summary>
        private static bool DetectWhileFromConditionLine(
            List<AsmCode> lines,
            int cmpLine,
            int n)
        {
            int jmpLine = cmpLine + 1;
            if (jmpLine >= n) return false;

            string jmpOp = lines[jmpLine].OPCode?.Value ?? "";
            if (jmpOp != "jmpf" && jmpOp != "jmpt") return false;

            return HasBackJump(lines, jmpLine, cmpLine, n);
        }

        /// <summary>
        /// Detect while for a bare jmpf/jmpt at <paramref name="jmpLine"/>.
        /// </summary>
        private static bool DetectWhileFromJumpLine(
            List<AsmCode> lines,
            int jmpLine,
            int n)
        {
            return HasBackJump(lines, jmpLine, jmpLine, n);
        }

        /// <summary>
        /// Scans from jmpLine+1 to falseTarget-1; returns true if any
        /// unconditional jmp in that range targets back to conditionLine or earlier.
        /// </summary>
        private static bool HasBackJump(
            List<AsmCode> lines,
            int jmpLine,
            int conditionLine,
            int n)
        {
            int falseTarget = jmpLine + 1 + lines[jmpLine].GetJumpTarget();

            for (int k = jmpLine + 1; k < falseTarget && k < n; k++)
            {
                if (lines[k].OPCode?.Value == "jmp")
                {
                    int backTarget = k + 1 + lines[k].GetJumpTarget();
                    if (backTarget <= conditionLine)
                        return true;
                }
            }

            return false;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Collect argument nodes whose value starts with '"'
        // ─────────────────────────────────────────────────────────────────────

        private static List<AsmLink> CollectStringNodes(AsmCode line)
        {
            var result = new List<AsmLink>();
            var node = line.Links?.GetHead();
            while (node != null)
            {
                string v = node.GetValue();
                if (!string.IsNullOrEmpty(v) && v.StartsWith("\""))
                    result.Add(node);
                node = node.Next;
            }
            return result;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Get the variable that this instruction writes into (dest operand).
        // Only used for Case B.
        // ─────────────────────────────────────────────────────────────────────

        private static string GetDestinationVariable(AsmCode line)
        {
            string op = line.OPCode?.Value ?? "";
            var head = line.Links?.GetHead();
            if (head == null) return "";

            switch (op)
            {
                case "assign":
                case "cast":
                case "not":
                case "strcat":
                case "iadd":
                case "isub":
                case "array_getelement":
                case "array_length":
                    return head.GetValue();

                case "callmethod":
                    {
                        var retNode = head.Next?.Next;
                        return (retNode != null && !retNode.IsNull()) ? retNode.GetValue() : "";
                    }

                case "callstatic":
                    {
                        var retNode = head.Next?.Next;
                        return (retNode != null && !retNode.IsNull()) ? retNode.GetValue() : "";
                    }

                case "propget":
                    {
                        var dest = head.Next?.Next;
                        return dest != null ? dest.GetValue() : "";
                    }

                default:
                    return "";
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Forward-trace: follow currentVar through assigns until consumed
        // ─────────────────────────────────────────────────────────────────────

        private static void ForwardTrace(
            DecompileTracker tracker,
            int fromLine,
            int n,
            string currentVar,
            StringFlowRecord record,
            PscCls parentCls,
            FunctionBlock func)
        {
            var lines = tracker.Lines;
            const int MaxDepth = 64;
            int depth = 0;
            int i = fromLine;

            while (i < n && depth < MaxDepth)
            {
                var line = lines[i];
                if (line?.Links == null) { i++; continue; }

                string op = line.OPCode?.Value ?? "";
                var head = line.Links.GetHead();

                // ── assign / cast / strcat ───────────────────────────────────
                if (op == "assign" || op == "cast" || op == "strcat")
                {
                    if (head.GetValue() == currentVar)
                        break; // overwritten — stop tracing

                    if (NodeListContains(head.Next, currentVar))
                    {
                        string newDest = head.GetValue();
                        if (!record.AssignmentChain.Contains(newDest))
                            record.AssignmentChain.Add(newDest);
                        currentVar = newDest;
                        depth++;
                    }
                }

                // ── callmethod ───────────────────────────────────────────────
                else if (op == "callmethod")
                {
                    string funcName = PapyrusAsmDecoder.CapitalizeFirst(head.GetValue());
                    var callerNode = head.Next;
                    var retNode = callerNode?.Next;
                    var firstArgNode = retNode?.Next?.Next;

                    int argIdx = NodeListIndexOf(firstArgNode, currentVar);
                    bool isCallerItself = callerNode != null
                                         && callerNode.GetValue() == currentVar;

                    if (argIdx >= 0 || isCallerItself)
                    {
                        string caller = (callerNode != null && callerNode.IsSelf())
                            ? "Self"
                            : PapyrusAsmDecoder.CapitalizeFirst(callerNode?.GetValue() ?? "Self");

                        var paramStrs = CollectNodeValuesCap(firstArgNode);

                        record.ConsumedByCall = string.Format("{0}.{1}({2})",
                                                          caller, funcName,
                                                          string.Join(", ", paramStrs));
                        record.ConsumedAtLine = i;
                        record.ConsumedByMethodName = funcName;
                        record.ConsumedByCallerName = caller;
                        record.ConsumedAtArgIndex = isCallerItself ? -1 : argIdx;

                        if (retNode != null && !retNode.IsNull())
                        {
                            string newDest = retNode.GetValue();
                            if (!record.AssignmentChain.Contains(newDest))
                                record.AssignmentChain.Add(newDest);
                            currentVar = newDest;
                            depth++;
                        }
                        else
                        {
                            break;
                        }
                    }
                    else if (retNode != null && !retNode.IsNull()
                             && retNode.GetValue() == currentVar)
                    {
                        break; // overwritten by return value
                    }
                }

                // ── callstatic ───────────────────────────────────────────────
                else if (op == "callstatic")
                {
                    var funcNode = head.Next;
                    var retNode = funcNode?.Next;
                    var firstArgNode = retNode?.Next?.Next;

                    string className = PapyrusAsmDecoder.CapitalizeFirst(head.GetValue());
                    string funcName = PapyrusAsmDecoder.CapitalizeFirst(funcNode?.GetValue() ?? "?");

                    int argIdx = NodeListIndexOf(firstArgNode, currentVar);
                    if (argIdx >= 0)
                    {
                        var paramStrs = CollectNodeValuesCap(firstArgNode);

                        record.ConsumedByCall = string.Format("{0}.{1}({2})",
                                                          className, funcName,
                                                          string.Join(", ", paramStrs));
                        record.ConsumedAtLine = i;
                        record.ConsumedByMethodName = funcName;
                        record.ConsumedByCallerName = className;
                        record.ConsumedAtArgIndex = argIdx;
                        break;
                    }
                    else if (retNode != null && !retNode.IsNull()
                             && retNode.GetValue() == currentVar)
                    {
                        break;
                    }
                }

                // ── propset: [propName][obj][value] ─────────────────────────
                else if (op == "propset")
                {
                    var objNode = head.Next;
                    var valueNode = objNode?.Next;

                    if (valueNode != null && valueNode.GetValue() == currentVar)
                    {
                        string propName = PapyrusAsmDecoder.CapitalizeFirst(head.GetValue());
                        string obj = (objNode?.GetValue() ?? "Self").ToLower() == "self"
                                              ? "Self"
                                              : PapyrusAsmDecoder.CapitalizeFirst(
                                                    objNode?.GetValue() ?? "Self");

                        record.ConsumedByCall = string.Format("{0}.{1} = {2}",
                                                          obj, propName, currentVar);
                        record.ConsumedAtLine = i;
                        record.ConsumedByMethodName = propName;
                        record.ConsumedByCallerName = obj;
                        record.ConsumedAtArgIndex = 0;
                        break;
                    }
                }

                i++;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Classify chain variables as global / local
        // ─────────────────────────────────────────────────────────────────────

        private static void ClassifyChainVariables(
            StringFlowRecord record,
            PscCls parentCls,
            FunctionBlock func)
        {
            foreach (string varName in record.AssignmentChain)
            {
                if (IsGlobal(varName, parentCls))
                {
                    if (!record.GlobalVariablesInvolved.Contains(varName))
                        record.GlobalVariablesInvolved.Add(varName);
                }
                else if (!varName.StartsWith("temp") && !varName.StartsWith("::"))
                {
                    bool isParam = false;
                    foreach (var p in func.Params)
                        if (p.Name == varName) { isParam = true; break; }

                    if (!isParam && !record.LocalVariablesInvolved.Contains(varName))
                        record.LocalVariablesInvolved.Add(varName);
                }
            }
        }

        private static bool IsGlobal(string varName, PscCls parentCls)
        {
            if (varName.StartsWith("::")) return true;
            if (parentCls.QueryGlobalVariable(varName) != null) return true;
            if (parentCls.QueryAutoGlobalVariable(varName) != null) return true;
            return false;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Linked-list helpers
        // ─────────────────────────────────────────────────────────────────────

        private static int NodeListIndexOf(AsmLink start, string target)
        {
            int idx = 0;
            AsmLink node = start;
            while (node != null)
            {
                if (!node.IsNull())
                {
                    if (node.GetValue() == target) return idx;
                    idx++;
                }
                node = node.Next;
            }
            return -1;
        }

        private static bool NodeListContains(AsmLink start, string target)
            => NodeListIndexOf(start, target) >= 0;

        private static List<string> CollectNodeValuesCap(AsmLink start)
        {
            var list = new List<string>();
            AsmLink node = start;
            while (node != null)
            {
                if (!node.IsNull())
                {
                    string v = node.GetValue();
                    list.Add(ShouldCapitalize(v)
                        ? PapyrusAsmDecoder.CapitalizeFirst(v)
                        : v);
                }
                node = node.Next;
            }
            return list;
        }

        private static bool ShouldCapitalize(string v)
        {
            if (string.IsNullOrEmpty(v)) return false;
            if (v[0] == '"') return false;
            if (v[0] == '-') return false;
            if (char.IsDigit(v[0])) return false;
            if (v == "True" || v == "False") return false;
            if (v == "None") return false;
            return true;
        }
    }
}