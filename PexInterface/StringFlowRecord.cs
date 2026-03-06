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

    // ─────────────────────────────────────────────────────────────────────────
    // FunctionCallInfo — describes the call that consumes the string value
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Detailed information about the method / static call or property set
    /// that ultimately consumes the tracked string literal.
    /// </summary>
    public class FunctionCallInfo
    {
        /// <summary>
        /// Fully qualified call expression, e.g. "Self.SetCosCloaks(Temp5)"
        /// or "Game.GetFormFromFile(3431, \"cloaks.esp\")".
        /// </summary>
        public string CallExpression = "";

        /// <summary>
        /// The caller object or class name (first-letter capitalised).
        /// "Self" for instance calls on self, the class name for static calls.
        /// e.g. "Self", "Game", "Debug"
        /// </summary>
        public string CallerName = "";

        /// <summary>
        /// The method / function name (first-letter capitalised).
        /// e.g. "SetCosCloaks", "GetFormFromFile", "Trace"
        /// For propset this is the property name.
        /// </summary>
        public string MethodName = "";

        /// <summary>
        /// Whether this is a static call (callstatic), instance call (callmethod),
        /// or property assignment (propset).
        /// </summary>
        public CallKind Kind = CallKind.Method;

        /// <summary>
        /// Total number of arguments the call was invoked with (excluding the
        /// implicit caller/self). Derived from the actual argument nodes present
        /// in the instruction; may differ from the declared signature if the
        /// script was compiled with optional args omitted.
        /// </summary>
        public int TotalArgCount = 0;

        /// <summary>
        /// The 0-based index of the argument position that holds the tracked
        /// string value (or the variable it flowed into).
        /// -1 when the tracked value is the caller object itself (not an argument).
        /// </summary>
        public int StringArgIndex = -1;

        /// <summary>
        /// The actual argument values as they appear in the call, capitalised
        /// where appropriate.  e.g. ["3431", "\"cloaks.esp\""]
        /// </summary>
        public List<string> Arguments = new List<string>();

        /// <summary>Line index of the consuming instruction.</summary>
        public int LineIndex = -1;

        public override string ToString()
        {
            return string.Format(
                "[{0}] {1}  TotalArgs={2}  StringArgIndex={3}  @ line {4}",
                Kind, CallExpression, TotalArgCount, StringArgIndex, LineIndex);
        }
    }

    public enum CallKind
    {
        Method,   // callmethod
        Static,   // callstatic
        PropSet   // propset
    }

    // ─────────────────────────────────────────────────────────────────────────
    // StringFlowCondition
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// A single If / While condition expression that references the tracked
    /// variable or the original string literal.
    /// </summary>
    public class StringFlowCondition
    {
        /// <summary>
        /// The full condition expression, e.g. "Temp8 == \"cloaks.esp\""
        /// or "Temp8" or "!Temp8".
        /// </summary>
        public string ConditionExpression = "";

        /// <summary>
        /// The opcode that produced this condition: cmp_eq / cmp_lt / cmp_le /
        /// cmp_gt / cmp_ge, or "jmpf" / "jmpt" for bare bool jumps.
        /// </summary>
        public string CmpOpCode = "";

        /// <summary>Line index of the cmp_* (or bare jmpf/jmpt) instruction.</summary>
        public int CmpLineIndex = -1;

        /// <summary>
        /// True when this condition belongs to a While loop rather than an If.
        /// </summary>
        public bool IsWhile = false;

        /// <summary>True when this condition belongs to an If / ElseIf block.</summary>
        public bool IsIf => !IsWhile;

        public override string ToString()
            => string.Format("{0} [{1}] @ line {2} -> {3}",
                ConditionExpression, CmpOpCode, CmpLineIndex,
                IsWhile ? "while" : "if");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // StringFlowRecord
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Records the complete flow of a single string literal found in the
    /// instruction stream.
    /// </summary>
    public class StringFlowRecord
    {
        /// <summary>The original string literal value, e.g. "Hello"</summary>
        public string LiteralValue = "";

        /// <summary>Line index where this literal first appears.</summary>
        public int SourceLineIndex = -1;

        /// <summary>
        /// The chain of variable assignments the literal passes through,
        /// e.g. [temp5, Temp333, Temp222].
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

        // ── Legacy flat fields (kept for backward compat) ────────────────────

        /// <summary>Full call expression string. Same as CallInfo.CallExpression.</summary>
        public string ConsumedByCall => CallInfo?.CallExpression;

        /// <summary>Line index of the consuming call. Same as CallInfo.LineIndex.</summary>
        public int ConsumedAtLine => CallInfo?.LineIndex ?? -1;

        /// <summary>Method name. Same as CallInfo.MethodName.</summary>
        public string ConsumedByMethodName => CallInfo?.MethodName;

        /// <summary>Caller name. Same as CallInfo.CallerName.</summary>
        public string ConsumedByCallerName => CallInfo?.CallerName;

        /// <summary>Arg index. Same as CallInfo.StringArgIndex.</summary>
        public int ConsumedAtArgIndex => CallInfo?.StringArgIndex ?? -1;

        // ── Rich call information ────────────────────────────────────────────

        /// <summary>
        /// Detailed information about the call that consumes this string value.
        /// Null if the value is never passed to any method.
        /// </summary>
        public FunctionCallInfo CallInfo = null;

        // ── Condition associations ───────────────────────────────────────────

        /// <summary>
        /// Every If / ElseIf / While condition that references any variable in
        /// AssignmentChain or the literal itself.
        /// </summary>
        public List<StringFlowCondition> RelatedConditions = new List<StringFlowCondition>();

        // ── Variable classification ──────────────────────────────────────────

        /// <summary>Global variables (:: prefixed or class-level) in the chain.</summary>
        public List<string> GlobalVariablesInvolved = new List<string>();

        /// <summary>Non-temp, non-global local variables in the chain.</summary>
        public List<string> LocalVariablesInvolved = new List<string>();

        /// <summary>StringID of the AsmLink node that carries the literal value.</summary>
        public ushort StringID = 0;


        public string ArrayTarget = null;


        public string ArrayName = null;


        public int ArrayIndex = -1;

        public string FinalTarget => ArrayTarget ?? FinalVariable;

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendFormat("[StringFlow] Literal={0}  SourceLine={1}\n",
                LiteralValue, SourceLineIndex);
            sb.AppendFormat("  Chain      : {0}\n",
                AssignmentChain.Count > 0 ? string.Join(" -> ", AssignmentChain) : "(direct)");
            sb.AppendFormat("  Final      : {0}\n", FinalVariable);

            if (CallInfo != null)
                sb.AppendFormat("  Call       : {0}\n", CallInfo);
            else
                sb.AppendLine("  Call       : none");

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

    // =========================================================================
    // StringFlowAnalyzer
    // =========================================================================

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
                var firstArgNode = retNode?.Next?.Next;   // skip placeholder

                int argIdx = NodeListIndexOf(firstArgNode, literal);
                if (argIdx < 0) return false;

                string className = Cap(head.GetValue());
                string funcName = Cap(funcNode?.GetValue() ?? "?");
                var args = CollectArgsCap(firstArgNode);

                record.CallInfo = new FunctionCallInfo
                {
                    CallExpression = string.Format("{0}.{1}({2})",
                                         className, funcName,
                                         string.Join(", ", args)),
                    CallerName = className,
                    MethodName = funcName,
                    Kind = CallKind.Static,
                    TotalArgCount = args.Count,
                    StringArgIndex = argIdx,
                    Arguments = args,
                    LineIndex = lineIndex
                };
                return true;
            }

            if (op == "callmethod")
            {
                // [funcName][caller][retVar][skip][arg0][arg1]…
                var callerNode = head.Next;
                var retNode = callerNode?.Next;
                var firstArgNode = retNode?.Next?.Next;   // skip placeholder

                int argIdx = NodeListIndexOf(firstArgNode, literal);
                if (argIdx < 0) return false;

                string funcName = Cap(head.GetValue());
                string caller = NormaliseCaller(callerNode);
                var args = CollectArgsCap(firstArgNode);

                record.CallInfo = new FunctionCallInfo
                {
                    CallExpression = string.Format("{0}.{1}({2})",
                                         caller, funcName,
                                         string.Join(", ", args)),
                    CallerName = caller,
                    MethodName = funcName,
                    Kind = CallKind.Method,
                    TotalArgCount = args.Count,
                    StringArgIndex = argIdx,
                    Arguments = args,
                    LineIndex = lineIndex
                };
                return true;
            }

            return false;
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
                    string funcName = Cap(head.GetValue());
                    var callerNode = head.Next;
                    var retNode = callerNode?.Next;
                    var firstArgNode = retNode?.Next?.Next;

                    int argIdx = NodeListIndexOf(firstArgNode, currentVar);
                    bool isCallerItself = callerNode != null
                                         && callerNode.GetValue() == currentVar;

                    if (argIdx >= 0 || isCallerItself)
                    {
                        string caller = NormaliseCaller(callerNode);
                        var args = CollectArgsCap(firstArgNode);

                        record.CallInfo = new FunctionCallInfo
                        {
                            CallExpression = string.Format("{0}.{1}({2})",
                                                 caller, funcName,
                                                 string.Join(", ", args)),
                            CallerName = caller,
                            MethodName = funcName,
                            Kind = CallKind.Method,
                            TotalArgCount = args.Count,
                            StringArgIndex = isCallerItself ? -1 : argIdx,
                            Arguments = args,
                            LineIndex = i
                        };

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

                    string className = Cap(head.GetValue());
                    string funcName = Cap(funcNode?.GetValue() ?? "?");

                    int argIdx = NodeListIndexOf(firstArgNode, currentVar);
                    if (argIdx >= 0)
                    {
                        var args = CollectArgsCap(firstArgNode);

                        record.CallInfo = new FunctionCallInfo
                        {
                            CallExpression = string.Format("{0}.{1}({2})",
                                                 className, funcName,
                                                 string.Join(", ", args)),
                            CallerName = className,
                            MethodName = funcName,
                            Kind = CallKind.Static,
                            TotalArgCount = args.Count,
                            StringArgIndex = argIdx,
                            Arguments = args,
                            LineIndex = i
                        };
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
                        string propName = Cap(head.GetValue());
                        string obj = NormaliseObj(objNode?.GetValue() ?? "Self");

                        record.CallInfo = new FunctionCallInfo
                        {
                            // propset is expressed as  Obj.Prop = Value
                            CallExpression = string.Format("{0}.{1} = {2}",
                                                 obj, propName,
                                                 CapValue(currentVar)),
                            CallerName = obj,
                            MethodName = propName,
                            Kind = CallKind.PropSet,
                            TotalArgCount = 1,
                            StringArgIndex = 0,
                            Arguments = new List<string> { CapValue(currentVar) },
                            LineIndex = i
                        };
                        break;
                    }
                }

                else if (op == "array_setelement")
                {
                    var arrayNode = head;           // array variable
                    var indexNode = head.Next;      // index (integer literal or var)
                    var valueNode = indexNode?.Next; // value being stored

                    if (valueNode != null && valueNode.GetValue() == currentVar)
                    {
                        string arrayName = CapValue(arrayNode.GetValue());
                        string indexRaw = indexNode?.GetValue() ?? "?";

                        // 解析下标为整数（字面量如 "0","1","2"）
                        int arrayIdx = -1;
                        int.TryParse(indexRaw, out arrayIdx);

                        string arrayTarget = string.Format("{0}[{1}]", arrayName, indexRaw);

                        record.ArrayName = arrayName;
                        record.ArrayIndex = arrayIdx;
                        record.ArrayTarget = arrayTarget;

                        if (!record.AssignmentChain.Contains(arrayTarget))
                            record.AssignmentChain.Add(arrayTarget);

                        break; 
                    }
                }

                i++;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // ScanConditions — find If / While that reference tracked vars or literal
        // ─────────────────────────────────────────────────────────────────────

        private static void ScanConditions(
            DecompileTracker tracker,
            int fromLine,
            int n,
            StringFlowRecord record)
        {
            var lines = tracker.Lines;
            var watched = new HashSet<string>(record.AssignmentChain);
            watched.Add(record.LiteralValue);

            // For bare jmpf/jmpt we only match against the final variable or literal
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
                        continue;

                    string expr = string.Format("{0} {1} {2}",
                                      CapValue(left), CmpOperator(op), CapValue(right));

                    bool isWhile = DetectWhileFromConditionLine(lines, i, n);

                    record.RelatedConditions.Add(new StringFlowCondition
                    {
                        ConditionExpression = expr,
                        CmpOpCode = op,
                        CmpLineIndex = i,
                        IsWhile = isWhile
                    });

                    // Track the cmp dest so a later jmpf(dest) is caught too
                    string cmpDest = head.GetValue();
                    if (!string.IsNullOrEmpty(cmpDest))
                        watched.Add(cmpDest);

                    // Consume the paired jmpf/jmpt so sub-case 2 never duplicates it
                    if (i + 1 < n)
                    {
                        string nextOp = lines[i + 1].OPCode?.Value ?? "";
                        if (nextOp == "jmpf" || nextOp == "jmpt")
                            i++;
                    }

                    continue;
                }

                // ── Sub-case 2: bare jmpf / jmpt ────────────────────────────
                if (op == "jmpf" || op == "jmpt")
                {
                    string condVar = head.GetValue();

                    bool matchesFinal = condVar == finalVar;
                    bool matchesLiteral = condVar == record.LiteralValue;
                    if (!matchesFinal && !matchesLiteral)
                        continue;

                    // jmpf → body runs when true  → "if (condVar)"
                    // jmpt → body runs when false → "if (!condVar)"
                    string expr = (op == "jmpt")
                        ? "!" + CapValue(condVar)
                        : CapValue(condVar);

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
        // While detection
        // ─────────────────────────────────────────────────────────────────────

        private static bool DetectWhileFromConditionLine(
            List<AsmCode> lines, int cmpLine, int n)
        {
            int jmpLine = cmpLine + 1;
            if (jmpLine >= n) return false;
            string jmpOp = lines[jmpLine].OPCode?.Value ?? "";
            if (jmpOp != "jmpf" && jmpOp != "jmpt") return false;
            return HasBackJump(lines, jmpLine, cmpLine, n);
        }

        private static bool DetectWhileFromJumpLine(
            List<AsmCode> lines, int jmpLine, int n)
            => HasBackJump(lines, jmpLine, jmpLine, n);

        private static bool HasBackJump(
            List<AsmCode> lines, int jmpLine, int conditionLine, int n)
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
        // GetDestinationVariable — for Case B only
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
        // Capitalisation helpers
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Capitalise a raw identifier (method name, variable name, class name).
        /// Delegates to PapyrusAsmDecoder.CapitalizeFirst.
        /// </summary>
        private static string Cap(string v)
            => string.IsNullOrEmpty(v) ? v : PapyrusAsmDecoder.CapitalizeFirst(v);

        /// <summary>
        /// Capitalise a value that could be an identifier OR a literal / number.
        /// Literals, numbers, booleans, and None are returned unchanged.
        /// </summary>
        private static string CapValue(string v)
        {
            if (string.IsNullOrEmpty(v)) return v;
            if (v[0] == '"') return v; // string literal
            if (v[0] == '-') return v; // negative number
            if (char.IsDigit(v[0])) return v; // number
            if (v == "True" || v == "False") return v; // bool literal
            if (v == "None") return v; // null literal
            return PapyrusAsmDecoder.CapitalizeFirst(v);
        }

        /// <summary>
        /// Normalise a caller node to a display string.
        /// Self nodes → "Self", everything else → CapValue.
        /// </summary>
        private static string NormaliseCaller(AsmLink callerNode)
        {
            if (callerNode == null) return "Self";
            if (callerNode.IsSelf()) return "Self";
            return CapValue(callerNode.GetValue() ?? "Self");
        }

        /// <summary>Normalise an object name (for propget/propset).</summary>
        private static string NormaliseObj(string obj)
            => (obj ?? "Self").ToLower() == "self" ? "Self" : CapValue(obj);

        // ─────────────────────────────────────────────────────────────────────
        // Collect argument list as capitalised strings + count non-null entries
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Walks the argument linked-list starting at <paramref name="start"/>,
        /// applies <see cref="CapValue"/> to every identifier, and returns the
        /// result as a List&lt;string&gt;.  The list length equals TotalArgCount.
        /// </summary>
        private static List<string> CollectArgsCap(AsmLink start)
        {
            var list = new List<string>();
            AsmLink node = start;
            while (node != null)
            {
                if (!node.IsNull())
                    list.Add(CapValue(node.GetValue()));
                node = node.Next;
            }
            return list;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Linked-list search helpers
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
    }
}