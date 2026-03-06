using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using static PexInterface.PexReader;

namespace PexInterface
{
    public interface ICodeStyle
    {
        string If(string condition);
        string IfSingleLine(string condition);
        string ElseIf(string condition);
        string Else();
        string EndIf();
        string While(string condition);
        string EndWhile();
        string Indent(int depth);
    }

    public class PapyrusStyle : ICodeStyle
    {
        public static readonly PapyrusStyle Instance = new PapyrusStyle();
        public string If(string condition) => "If " + condition;
        public string IfSingleLine(string condition) => "If " + condition;
        public string ElseIf(string condition) => "ElseIf " + condition;
        public string Else() => "Else";
        public string EndIf() => "EndIf";
        public string While(string condition) => "While " + condition;
        public string EndWhile() => "EndWhile";
        public string Indent(int depth) => new string(' ', depth * 4);
    }

    public class CSharpStyle : ICodeStyle
    {
        public static readonly CSharpStyle Instance = new CSharpStyle();
        public string If(string condition) => "if (" + condition + ") {";
        public string IfSingleLine(string condition) => "if (" + condition + ")";
        public string ElseIf(string condition) => "} else if (" + condition + ") {";
        public string Else() => "} else {";
        public string EndIf() => "}";
        public string While(string condition) => "while (" + condition + ") {";
        public string EndWhile() => "}";
        public string Indent(int depth) => new string(' ', depth * 4);
    }

    public class AsmExtend
    {
        public class TempVariable
        {
            public string Type = "";
            public string Variable = "";
            public int LinkLineIndex = 0;
            public TempVariable(string variable, int lineIndex)
            {
                Variable = variable;
                LinkLineIndex = lineIndex;
            }
        }

        public static void DeFunctionCode(
            CodeGenStyle Style,
            List<PexString> TempStrings,
            PscCls ParentCls,
            FunctionBlock Func,
            DecompileTracker TrackerRef,
            bool CanSkipPscDeCode,
            ICodeStyle CodeStyle = null)
        {
            if (CanSkipPscDeCode) return;

            if (CodeStyle == null)
            {
                if (Style == CodeGenStyle.Papyrus)
                    CodeStyle = PapyrusStyle.Instance;
                else
                    CodeStyle = CSharpStyle.Instance;
            }

            int Length = TrackerRef.Lines.Count;

            Pass1_TranslateOpcodes(TrackerRef, ParentCls, Func, Length);
            Pass2_InferArrayTypes(TrackerRef, ParentCls, Length);
            Pass3_ResolveTypePlaceholders(TrackerRef, ParentCls, Func, TempStrings, Length);
            Pass4_ControlFlow(TrackerRef, TempStrings, CodeStyle);
            Pass5_DeclareResidualTemps(TrackerRef, ParentCls, Func);

            Func.StringFlower = new Dictionary<ushort, StringFlowRecord>();
            foreach (var GetFlow in StringFlowAnalyzer.Analyze(TrackerRef, ParentCls, Func))
            {
                if (!Func.StringFlower.ContainsKey(GetFlow.StringID))
                    Func.StringFlower.Add(GetFlow.StringID, GetFlow);
            }
        }

        private static void Pass1_TranslateOpcodes(
            DecompileTracker TrackerRef,
            PscCls ParentCls,
            FunctionBlock Func,
            int Length)
        {
            for (int i = 0; i < Length; i++)
            {
                var AsmLine = TrackerRef.Lines[i];
                if (AsmLine == null || AsmLine.Links == null) continue;

                var Head = AsmLine.Links.GetHead();

                switch (AsmLine.OPCode.Value)
                {
                    case "assign": EmitAssign(AsmLine, Head, TrackerRef, i); break;
                    case "callmethod": EmitCallMethod(AsmLine, Head, TrackerRef, i); break;
                    case "callstatic": EmitCallStatic(AsmLine, Head, TrackerRef, i); break;
                    case "iadd": EmitIAdd(AsmLine, Head, TrackerRef, i); break;
                    case "isub": EmitISub(AsmLine, Head); break;
                    case "return": EmitReturn(AsmLine, Head); break;
                    case "not": EmitNot(AsmLine, Head, TrackerRef, i); break;
                    case "strcat": EmitStrcat(AsmLine, Head, TrackerRef, i); break;
                    case "cast": EmitCast(AsmLine, Head, TrackerRef, i); break;
                    case "propget": EmitPropGet(AsmLine, Head, TrackerRef, i); break;
                    case "propset": EmitPropSet(AsmLine, Head); break;
                    case "array_create": EmitArrayCreate(AsmLine, Head); break;
                    case "array_setelement": EmitArraySetElement(AsmLine, Head); break;
                    case "array_getelement": EmitArrayGetElement(AsmLine, Head, TrackerRef, i); break;
                    case "array_length": EmitArrayLength(AsmLine, Head); break;

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
        }

        private static void Pass2_InferArrayTypes(
            DecompileTracker TrackerRef,
            PscCls ParentCls,
            int Length)
        {
            for (int i = 0; i < Length; i++)
            {
                var AsmLine = TrackerRef.Lines[i];
                if (AsmLine.OPCode.Value != "array_create") continue;
                if (!AsmLine.PSCCode.Contains("__ARRAY_TYPE__")) continue;

                string ArrayName = AsmLine.Links.GetHead().GetValue();
                string InferredType = "var";

                for (int j = i + 1; j < Length; j++)
                {
                    var ScanLine = TrackerRef.Lines[j];
                    if (ScanLine.OPCode.Value != "array_setelement") continue;

                    var ScanHead = ScanLine.Links.GetHead();
                    if (ScanHead.GetValue() != ArrayName) continue;

                    var ValueNode = ScanHead.Next?.Next;
                    if (ValueNode == null) break;

                    InferredType = InferTypeFromArgument(ScanLine, ValueNode.GetValue(), ParentCls, ArgIndex: 2);
                    break;
                }

                AsmLine.PSCCode = AsmLine.PSCCode.Replace("__ARRAY_TYPE__", InferredType);
            }
        }

        private static void Pass3_ResolveTypePlaceholders(
            DecompileTracker TrackerRef,
            PscCls ParentCls,
            FunctionBlock Func,
            List<PexString> TempStrings,
            int Length)
        {
            for (int i = 0; i < Length; i++)
            {
                var AsmLine = TrackerRef.Lines[i];
                if (!AsmLine.PSCCode.StartsWith("__TYPE__")) continue;

                string Code = AsmLine.PSCCode.Substring("__TYPE__".Length);
                int EqualsIndex = Code.IndexOf('=');
                if (EqualsIndex < 0) { AsmLine.PSCCode = Code; continue; }

                string VarName = Code.Substring(0, EqualsIndex).Trim();
                string Rhs = Code.Substring(EqualsIndex + 1).Trim();
                string InferredType = InferTypeFromRhs(TrackerRef, i, Rhs, ParentCls, Func, TempStrings);

                AsmLine.PSCCode = InferredType.Length > 0
                    ? string.Format("{0} {1} = {2};", InferredType, VarName, Rhs)
                    : string.Format("{0} = {1};", VarName, Rhs);
            }
        }

        private static void Pass4_ControlFlow(
            DecompileTracker TrackerRef,
            List<PexString> TempStrings,
            ICodeStyle CodeStyle)
        {
            ControlFlowCodeGen.ApplyControlFlow(TrackerRef, TempStrings, CodeStyle);
        }

        private static void Pass5_DeclareResidualTemps(
            DecompileTracker TrackerRef,
            PscCls ParentCls,
            FunctionBlock Func)
        {
            var TempLhsRx = new Regex(@"^(\s*)(\w+)\s*=");
            var DeclaredInOutput = new HashSet<string>(StringComparer.Ordinal);

            foreach (var AsmLine in TrackerRef.Lines)
            {
                if (string.IsNullOrEmpty(AsmLine.PSCCode)) continue;

                string[] Parts = AsmLine.PSCCode.Split('\n');
                bool Changed = false;

                for (int p = 0; p < Parts.Length; p++)
                {
                    var M = TempLhsRx.Match(Parts[p]);
                    if (!M.Success) continue;

                    string VarName = M.Groups[2].Value;

                    if (DeclaredInOutput.Contains(VarName)) continue;
                    if (TrackerRef.Variables.IsCreated(VarName)) continue;

                    // Skip global variables (bare name or decorated form ::Name_var)
                    if (ParentCls.QueryGlobalVariable(VarName) != null) continue;
                    if (ParentCls.QueryGlobalVariable("::" + VarName + "_var") != null) continue;

                    // Skip auto-property variables (bare name or decorated form ::Name_var)
                    if (ParentCls.QueryAutoGlobalVariable(VarName) != null) continue;
                    if (ParentCls.QueryAutoGlobalVariable("::" + VarName + "_var") != null) continue;

                    bool IsParam = false;
                    foreach (var Param in Func.Params)
                        if (Param.Name == VarName) { IsParam = true; break; }
                    if (IsParam) continue;

                    DeclaredInOutput.Add(VarName);
                    Parts[p] = M.Groups[1].Value + "var " + Parts[p].TrimStart();
                    Changed = true;
                }

                if (Changed)
                    AsmLine.PSCCode = string.Join("\n", Parts);
            }
        }

        // =========================================================================
        // Opcode emitters
        // =========================================================================

        private static void EmitAssign(AsmCode AsmLine, AsmLink Head,
            DecompileTracker TrackerRef, int i)
        {
            string VarName = Head.GetValue();
            bool IsFirstAssign = !TrackerRef.Variables.IsCreated(VarName);
            TrackerRef.Variables.Add(new AsmOffset(i, 0), VarName, 0, null, VariableAccess.None);

            if (Head.Next == null)
            {
                AsmLine.PSCCode = string.Format("{0} = 0;", VarName);
                return;
            }

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

                    AsmLine.PSCCode = Src == VarName
                        ? string.Format("{0} += {1};", VarName, Amount)
                        : string.Format("{0} = {1} + {2};", VarName, Src, Amount);
                    return;
                }
            }

            if (IsFirstAssign && !VarName.StartsWith("::"))
                AsmLine.PSCCode = string.Format("__TYPE__{0} = {1}", VarName, RhsRaw);
            else
                AsmLine.PSCCode = string.Format("{0} = {1}", VarName, RhsRaw);
        }

        private static void EmitCallMethod(AsmCode AsmLine, AsmLink Head,
            DecompileTracker TrackerRef, int i)
        {
            string FuncName = Head.GetValue();
            var CallerNode = Head.Next;
            string Caller = (CallerNode != null && CallerNode.IsSelf())
                                     ? "Self" : (CallerNode?.GetValue() ?? "Self");
            var ReturnNode = CallerNode?.Next;
            string ReturnVar = ReturnNode?.GetValue() ?? "";

            var ParamsList = BuildParamList(ReturnNode?.Next?.Next);
            string CallExpr = string.Format("{0}.{1}({2})", Caller, FuncName, string.Join(", ", ParamsList));

            bool DiscardReturn = ReturnNode == null
                              || ReturnNode.IsNull()
                              || IsTempNeverRead(TrackerRef, i, ReturnVar, TrackerRef.Lines.Count);

            AsmLine.PSCCode = DiscardReturn
                ? CallExpr + ";"
                : string.Format("{0} = {1};", ReturnVar, CallExpr);

            if (!DiscardReturn && !string.IsNullOrEmpty(ReturnVar))
                TrackerRef.Variables.Add(new AsmOffset(i, 2), ReturnVar, 0,
                    new List<AsmLink> { CallerNode, Head }, VariableAccess.Set);
        }

        // ─────────────────────────────────────────────────────────────────────
        // CRITICAL: reads must be checked BEFORE overwrites.
        // If the same temp is read and then overwritten in a later instruction,
        // we must not discard the producing assignment.
        // ─────────────────────────────────────────────────────────────────────
        private static bool IsTempNeverRead(
            DecompileTracker TrackerRef,
            int FromLine,
            string TempName,
            int Length)
        {
            if (string.IsNullOrEmpty(TempName) || !TempName.StartsWith("temp"))
                return false;

            for (int k = FromLine + 1; k < Length; k++)
            {
                var ScanLine = TrackerRef.Lines[k];
                if (ScanLine == null) continue;

                string ScanOP = ScanLine.OPCode?.Value ?? "";
                var Head = ScanLine.Links?.GetHead();
                if (Head == null) continue;

                // ── 1. Check reads FIRST ──────────────────────────────────────
                if (ScanOP == "callmethod" || ScanOP == "callstatic")
                {
                    // Index 2 is the return slot (write). All others are reads.
                    var Node = Head;
                    int ArgIdx = 0;
                    while (Node != null)
                    {
                        if (ArgIdx != 2 && Node.GetValue() == TempName)
                            return false; // Read as caller or parameter
                        Node = Node.Next;
                        ArgIdx++;
                    }
                }
                else
                {
                    // For all other ops, index 0 is destination (write); rest are reads.
                    var Node = Head.Next;
                    while (Node != null)
                    {
                        if (Node.GetValue() == TempName)
                            return false; // Read here
                        Node = Node.Next;
                    }
                }

                // ── 2. Check overwrites AFTER reads ──────────────────────────
                bool IsDestOp = ScanOP == "assign" || ScanOP == "not" || ScanOP == "cast" ||
                                ScanOP == "strcat" || ScanOP == "iadd" || ScanOP == "isub" ||
                                ScanOP == "array_getelement" || ScanOP == "array_length";
                if (IsDestOp && Head.GetValue() == TempName)
                    return true; // Overwritten before any further read

                if ((ScanOP == "callmethod" || ScanOP == "callstatic") &&
                    Head.Next?.Next?.GetValue() == TempName)
                    return true; // Return slot overwrites TempName

                if (ScanOP == "propget" && Head.Next?.Next?.GetValue() == TempName)
                    return true; // propget destination overwrites TempName
            }

            return true; // Reached end of function without any read
        }

        private static void EmitCallStatic(AsmCode AsmLine, AsmLink Head,
            DecompileTracker TrackerRef, int i)
        {
            string ClassName = PapyrusAsmDecoder.CapitalizeFirst(Head.GetValue());
            var FuncNode = Head.Next;
            string FuncName = FuncNode?.GetValue() ?? "?";
            var RetNode = FuncNode?.Next;
            string ReturnVar = RetNode?.GetValue() ?? "";

            var ParamsList = BuildParamList(RetNode?.Next?.Next);
            string CallExpr = string.Format("{0}.{1}({2})", ClassName, FuncName, string.Join(", ", ParamsList));

            bool DiscardReturn = RetNode == null
                              || RetNode.IsNull()
                              || IsTempNeverRead(TrackerRef, i, ReturnVar, TrackerRef.Lines.Count);

            AsmLine.PSCCode = DiscardReturn
                ? CallExpr + ";"
                : string.Format("{0} = {1};", ReturnVar, CallExpr);

            if (!DiscardReturn && !string.IsNullOrEmpty(ReturnVar))
                TrackerRef.Variables.Add(new AsmOffset(i, 2), ReturnVar, 0, null, VariableAccess.Set);
        }

        private static void EmitIAdd(AsmCode AsmLine, AsmLink Head,
            DecompileTracker TrackerRef, int i)
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
        }

        private static void EmitISub(AsmCode AsmLine, AsmLink Head)
        {
            string VarName = Head.GetValue();
            string Operand = Head.Next != null ? Head.Next.GetValue() : "1";
            AsmLine.PSCCode = string.Format("{0} -= {1};", VarName, Operand);
        }

        private static void EmitReturn(AsmCode AsmLine, AsmLink Head)
        {
            AsmLine.PSCCode = Head.IsNull()
                ? "Return;"
                : string.Format("Return {0};", Head.GetValue());
        }

        private static void EmitNot(AsmCode AsmLine, AsmLink Head,
            DecompileTracker TrackerRef, int i)
        {
            string RetVar = Head.GetValue();
            string Operand = Head.Next?.GetValue() ?? "?";
            AsmLine.PSCCode = string.Format("{0} = !{1};", RetVar, Operand);

            if (!TrackerRef.Variables.IsCreated(RetVar))
                TrackerRef.Variables.Add(new AsmOffset(i, 0), RetVar, 0, null, VariableAccess.Set);
        }

        private static void EmitStrcat(AsmCode AsmLine, AsmLink Head,
            DecompileTracker TrackerRef, int i)
        {
            string Dest = Head.GetValue();
            string Left = Head.Next?.GetValue() ?? "?";
            string Right = Head.Next?.Next?.GetValue() ?? "?";
            AsmLine.PSCCode = string.Format("{0} = {1} + {2};", Dest, Left, Right);

            if (!TrackerRef.Variables.IsCreated(Dest))
                TrackerRef.Variables.Add(new AsmOffset(i, 0), Dest, 0, null, VariableAccess.Set);
        }

        private static void EmitCast(AsmCode AsmLine, AsmLink Head,
            DecompileTracker TrackerRef, int i)
        {
            string Dest = Head.GetValue();
            string Src = Head.Next?.GetValue() ?? "?";
            AsmLine.PSCCode = string.Format("{0} = {1};", Dest, Src);

            if (!TrackerRef.Variables.IsCreated(Dest))
                TrackerRef.Variables.Add(new AsmOffset(i, 0), Dest, 0, null, VariableAccess.Set);
        }

        private static void EmitPropGet(AsmCode AsmLine, AsmLink Head,
            DecompileTracker TrackerRef, int i)
        {
            string PropName = Head.GetValue();
            string Obj = NormalizeObj(Head.Next?.GetValue() ?? "Self");
            string Dest = Head.Next?.Next?.GetValue() ?? "?";
            AsmLine.PSCCode = string.Format("{0} = {1}.{2};", Dest, Obj, PropName);

            if (!TrackerRef.Variables.IsCreated(Dest))
                TrackerRef.Variables.Add(new AsmOffset(i, 0), Dest, 0, null, VariableAccess.Set);
        }

        private static void EmitPropSet(AsmCode AsmLine, AsmLink Head)
        {
            string PropName = Head.GetValue();
            string Obj = NormalizeObj(Head.Next?.GetValue() ?? "Self");
            string Value = Head.Next?.Next?.GetValue() ?? "?";
            AsmLine.PSCCode = string.Format("{0}.{1} = {2};", Obj, PropName, Value);
        }

        private static void EmitArrayCreate(AsmCode AsmLine, AsmLink Head)
        {
            string Dest = Head.GetValue();
            string Size = Head.Next?.GetValue() ?? "?";
            AsmLine.PSCCode = string.Format("{0} = new __ARRAY_TYPE__[{1}];", Dest, Size);
        }

        private static void EmitArraySetElement(AsmCode AsmLine, AsmLink Head)
        {
            string Arr = Head.GetValue();
            string Index = Head.Next?.GetValue() ?? "?";
            string Value = Head.Next?.Next?.GetValue() ?? "?";
            AsmLine.PSCCode = string.Format("{0}[{1}] = {2};", Arr, Index, Value);
        }

        private static void EmitArrayGetElement(AsmCode AsmLine, AsmLink Head,
            DecompileTracker TrackerRef, int i)
        {
            string Dest = Head.GetValue();
            string Array = Head.Next?.GetValue() ?? "?";
            string Index = Head.Next?.Next?.GetValue() ?? "?";
            AsmLine.PSCCode = string.Format("{0} = {1}[{2}];", Dest, Array, Index);

            if (!TrackerRef.Variables.IsCreated(Dest))
                TrackerRef.Variables.Add(new AsmOffset(i, 0), Dest, 0, null, VariableAccess.Set);
        }

        private static void EmitArrayLength(AsmCode AsmLine, AsmLink Head)
        {
            string Dest = Head.GetValue();
            string Array = Head.Next?.GetValue() ?? "?";
            AsmLine.PSCCode = string.Format("{0} = {1}.Length;", Dest, Array);
        }

        // =========================================================================
        // Type inference helpers
        // =========================================================================

        private static string InferTypeFromArgument(
            AsmCode ScanLine,
            string Value,
            PscCls ParentCls,
            int ArgIndex)
        {
            if (ScanLine.OPCode.Arguments.Count > ArgIndex)
            {
                var RawArg = ScanLine.OPCode.Arguments[ArgIndex];
                switch (RawArg.Type)
                {
                    case 2: return "String";
                    case 3: return "Int";
                    case 4: return "Float";
                    case 5: return "Bool";
                    case 0: return "var";
                    case 1:
                        {
                            var Global = ParentCls.QueryGlobalVariable(Value);
                            if (Global != null && Global.Type.Length > 0) return Global.Type;
                            var Auto = ParentCls.QueryAutoGlobalVariable(Value);
                            if (Auto != null && Auto.Type.Length > 0) return Auto.Type;
                            if (Value.StartsWith("$")) return "String";
                            return "var";
                        }
                }
            }
            return InferTypeFromLiteral(Value);
        }

        private static string InferTypeFromLiteral(string Value)
        {
            if (Value.StartsWith("\"") || Value.StartsWith("$")) return "String";
            if (int.TryParse(Value, out _)) return "Int";
            if (float.TryParse(Value, NumberStyles.Float, CultureInfo.InvariantCulture, out _)) return "Float";
            if (Value.ToLower() == "true" || Value.ToLower() == "false") return "Bool";
            return "var";
        }

        private static string InferTypeFromRhs(
            DecompileTracker Tracker,
            int FromLine,
            string Rhs,
            PscCls ParentCls,
            FunctionBlock Func,
            List<PexString> TempStrings)
        {
            if (Rhs.StartsWith("\"") || Rhs.StartsWith("$")) return "String";
            if (Rhs == "True" || Rhs == "False") return "Bool";
            if (int.TryParse(Rhs, out _)) return "Int";
            if (float.TryParse(Rhs, NumberStyles.Float, CultureInfo.InvariantCulture, out _)) return "Float";
            if (Rhs == "None") return "";

            if (!Rhs.StartsWith("temp"))
            {
                var Global = ParentCls.QueryGlobalVariable(Rhs);
                if (Global != null && Global.Type.Length > 0) return Global.Type;
                var Auto = ParentCls.QueryAutoGlobalVariable(Rhs);
                if (Auto != null && Auto.Type.Length > 0) return Auto.Type;
                foreach (var Param in Func.Params)
                    if (Param.Name == Rhs) return Param.Type;
                return "";
            }

            for (int j = FromLine - 1; j >= 0; j--)
            {
                var Line = Tracker.Lines[j];
                string LineOP = Line.OPCode?.Value ?? "";
                var LineHead = Line.Links?.GetHead();
                if (LineHead == null) continue;

                if (LineOP == "callmethod")
                {
                    var RetNode = LineHead.Next?.Next;
                    if (RetNode == null || RetNode.GetValue() != Rhs) continue;
                    string CalledFunc = LineHead.GetValue();
                    foreach (var Function in ParentCls.Functions)
                        if (Function.FunctionName == CalledFunc && Function.ReturnType.Length > 0) return Function.ReturnType;
                    return "var";
                }

                if (LineOP == "callstatic")
                {
                    var FuncNode = LineHead.Next;
                    var RetNode = FuncNode?.Next;
                    if (RetNode == null || RetNode.GetValue() != Rhs) continue;
                    string CalledFunc = FuncNode?.GetValue() ?? "";
                    foreach (var Function in ParentCls.Functions)
                        if (Function.FunctionName == CalledFunc && Function.ReturnType.Length > 0) return Function.ReturnType;
                    return "var";
                }

                if (LineOP == "cast")
                {
                    if (LineHead.GetValue() != Rhs) continue;
                    return InferTypeFromRhs(Tracker, j, LineHead.Next?.GetValue() ?? "", ParentCls, Func, TempStrings);
                }

                if (LineOP == "propget")
                {
                    var DestNode = LineHead.Next?.Next;
                    if (DestNode == null || DestNode.GetValue() != Rhs) continue;
                    return "var";
                }

                if (LineOP == "assign")
                {
                    if (LineHead.GetValue() != Rhs) continue;
                    return InferTypeFromRhs(Tracker, j, LineHead.Next?.GetValue() ?? "", ParentCls, Func, TempStrings);
                }
            }

            return "";
        }

        private static string NormalizeObj(string Obj)
            => Obj.ToLower() == "self" ? "Self" : Obj;

        private static List<string> BuildParamList(AsmLink StartNode)
        {
            var List = new List<string>();
            var Node = StartNode;
            while (Node != null)
            {
                if (!Node.IsNull()) List.Add(Node.GetValue());
                Node = Node.Next;
            }
            return List;
        }
    }

    // =========================================================================
    // Control-flow event types
    // =========================================================================

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
        public string Condition;
        public bool IsSingleLine; // True = no braces, body is one instruction

        public CfEvent(CfEventKind kind, string cond = "", bool singleLine = false)
        {
            Kind = kind;
            Condition = cond;
            IsSingleLine = singleLine;
        }
    }

    // =========================================================================
    // Single-pass control-flow analyzer
    // =========================================================================

    public class ControlFlowAnalyzer
    {
        private readonly Dictionary<int, List<CfEvent>> _Events =
            new Dictionary<int, List<CfEvent>>();

        public void AddEvent(int LineIndex, CfEvent Event)
        {
            if (!_Events.ContainsKey(LineIndex))
                _Events[LineIndex] = new List<CfEvent>();
            _Events[LineIndex].Add(Event);
        }

        public List<CfEvent> GetEvents(int LineIndex)
        {
            _Events.TryGetValue(LineIndex, out var List);
            return List;
        }

        private static int AbsTarget(List<AsmCode> Lines, int JmpIndex)
            => JmpIndex + 1 + Lines[JmpIndex].GetJumpTarget();

        public static bool IsCmp(string OPCode)
            => OPCode == "cmp_eq" || OPCode == "cmp_lt" || OPCode == "cmp_le" ||
               OPCode == "cmp_gt" || OPCode == "cmp_ge";

        private struct IfSeg { public int CmpLine, JmpfLine, FalseTarget; }

        private static List<IfSeg> CollectChain(List<AsmCode> Lines, int StartCmp, int Length)
        {
            var Chain = new List<IfSeg>();
            int Cur = StartCmp;

            while (Cur < Length && IsCmp(Lines[Cur].OPCode?.Value ?? ""))
            {
                int JMPFLine = Cur + 1;
                if (JMPFLine >= Length || Lines[JMPFLine].OPCode?.Value != "jmpf") break;

                int FalseTarget = AbsTarget(Lines, JMPFLine);
                Chain.Add(new IfSeg { CmpLine = Cur, JmpfLine = JMPFLine, FalseTarget = FalseTarget });

                int Prev = FalseTarget - 1;
                if (Prev < 0 || Prev >= Length || Lines[Prev].OPCode?.Value != "jmp") break;

                Cur = FalseTarget;
                if (Cur >= Length || !IsCmp(Lines[Cur].OPCode?.Value ?? "")) break;
            }

            return Chain;
        }

        // Count executable (non control-flow) instructions in [BodyStart, BodyEnd)
        private static int CountBodyInstructions(List<AsmCode> Lines, int BodyStart, int BodyEnd, int Length)
        {
            int Count = 0;
            for (int k = BodyStart; k < BodyEnd && k < Length; k++)
            {
                string Op = Lines[k].OPCode?.Value ?? "";
                if (Op == "jmp" || Op == "jmpf" || Op == "jmpt" ||
                    Op == "cmp_eq" || Op == "cmp_lt" || Op == "cmp_le" ||
                    Op == "cmp_gt" || Op == "cmp_ge")
                    continue;
                Count++;
            }
            return Count;
        }

        public static ControlFlowAnalyzer Analyze(
            List<AsmCode> Lines,
            Func<int, string> BuildCondition)
        {
            var CFA = new ControlFlowAnalyzer();
            int Length = Lines.Count;
            bool[] Handled = new bool[Length];

            int i = 0;
            while (i < Length)
            {
                if (Handled[i]) { i++; continue; }

                string OPCode = Lines[i].OPCode?.Value ?? "";

                if (IsCmp(OPCode) && i + 1 < Length && Lines[i + 1].OPCode?.Value == "jmpf")
                {
                    i = AnalyzeIfChain(CFA, Lines, Length, Handled, i, BuildCondition);
                    continue;
                }

                if (OPCode == "jmpf" || OPCode == "jmpt")
                {
                    i = AnalyzeBareJump(CFA, Lines, Length, Handled, i, OPCode);
                    continue;
                }

                i++;
            }

            return CFA;
        }

        private static int AnalyzeIfChain(
            ControlFlowAnalyzer CFA,
            List<AsmCode> Lines,
            int Length,
            bool[] Handled,
            int CmpLine,
            Func<int, string> BuildCondition)
        {
            int JMPFLine = CmpLine + 1;
            int FalseTarget = AbsTarget(Lines, JMPFLine);

            if (TryDetectWhile(CFA, Lines, Length, Handled, CmpLine, JMPFLine, FalseTarget, BuildCondition))
                return CmpLine + 1;

            var Chain = CollectChain(Lines, CmpLine, Length);

            if (Chain.Count <= 1)
            {
                // Detect whether there is an else branch
                bool HasElse = false;
                int PrevFalse = FalseTarget - 1;
                if (PrevFalse >= 0 && PrevFalse < Length
                    && Lines[PrevFalse].OPCode?.Value == "jmp"
                    && AbsTarget(Lines, PrevFalse) > FalseTarget)
                    HasElse = true;

                // Single-line optimisation only when there is no else
                bool SingleLine = false;
                if (!HasElse)
                {
                    int BodyCount = CountBodyInstructions(Lines, JMPFLine + 1, FalseTarget, Length);
                    SingleLine = (BodyCount == 1);
                }

                CFA.AddEvent(CmpLine, new CfEvent(CfEventKind.IfBegin, BuildCondition(CmpLine), SingleLine));
                Handled[CmpLine] = true;
                Handled[JMPFLine] = true;

                if (!SingleLine)
                    EmitSimpleIfEnd(CFA, Lines, Length, Handled, FalseTarget);
                // If SingleLine: no EndIf event — depth never increased
            }
            else
            {
                // ElseIf chain — always multi-line
                var First = Chain[0];
                CFA.AddEvent(First.CmpLine, new CfEvent(CfEventKind.IfBegin, BuildCondition(First.CmpLine)));
                Handled[First.CmpLine] = true;
                Handled[First.JmpfLine] = true;

                for (int ci = 1; ci < Chain.Count; ci++)
                {
                    var Seg = Chain[ci];
                    CFA.AddEvent(Seg.CmpLine, new CfEvent(CfEventKind.ElseIfBegin, BuildCondition(Seg.CmpLine)));
                    Handled[Seg.CmpLine] = true;
                    Handled[Seg.JmpfLine] = true;
                }

                int CommonEnd = ResolveChainEnd(CFA, Lines, Length, Handled, Chain);
                CFA.AddEvent(CommonEnd, new CfEvent(CfEventKind.EndIf));
            }

            return CmpLine + 1;
        }

        private static int AnalyzeBareJump(
            ControlFlowAnalyzer CFA,
            List<AsmCode> Lines,
            int Length,
            bool[] Handled,
            int JMPLine,
            string OPCode)
        {
            string CondVar = Lines[JMPLine].Links?.GetHead()?.GetValue() ?? "?";
            string Condition = (OPCode == "jmpt") ? ("!" + CondVar) : CondVar;
            int FalseTarget = AbsTarget(Lines, JMPLine);

            // While detection
            int WhileJmpLine = -1;
            for (int k = JMPLine + 1; k < FalseTarget && k < Length; k++)
            {
                if (Lines[k].OPCode?.Value == "jmp" && AbsTarget(Lines, k) <= JMPLine)
                { WhileJmpLine = k; break; }
            }

            if (WhileJmpLine >= 0)
            {
                CFA.AddEvent(JMPLine, new CfEvent(CfEventKind.WhileBegin, Condition));
                CFA.AddEvent(WhileJmpLine, new CfEvent(CfEventKind.EndWhile));
                Handled[JMPLine] = true;
                Handled[WhileJmpLine] = true;
                return JMPLine + 1;
            }

            int PrevFalse = FalseTarget - 1;
            bool HasElse = PrevFalse > JMPLine
                             && PrevFalse < Length
                             && Lines[PrevFalse].OPCode?.Value == "jmp"
                             && AbsTarget(Lines, PrevFalse) > FalseTarget;

            bool SingleLine = false;
            if (!HasElse)
            {
                int BodyCount = CountBodyInstructions(Lines, JMPLine + 1, FalseTarget, Length);
                SingleLine = (BodyCount == 1);
            }

            CFA.AddEvent(JMPLine, new CfEvent(CfEventKind.IfBegin, Condition, SingleLine));
            Handled[JMPLine] = true;

            if (SingleLine)
            {
                // No EndIf — brace-free
            }
            else if (HasElse && FalseTarget < Length)
            {
                int ElseEnd = Math.Min(AbsTarget(Lines, PrevFalse) - 1, Length - 1);
                CFA.AddEvent(FalseTarget, new CfEvent(CfEventKind.ElseBegin));
                CFA.AddEvent(ElseEnd, new CfEvent(CfEventKind.EndIf));
                Handled[PrevFalse] = true;
            }
            else
            {
                int EndIfLine = Math.Max(0, Math.Min(FalseTarget - 1, Length - 1));
                CFA.AddEvent(EndIfLine, new CfEvent(CfEventKind.EndIf));
            }

            return JMPLine + 1;
        }

        private static bool TryDetectWhile(
            ControlFlowAnalyzer CFA,
            List<AsmCode> Lines,
            int Length,
            bool[] Handled,
            int CMPLine,
            int JMPFLine,
            int FalseTarget,
            Func<int, string> BuildCondition)
        {
            for (int k = JMPFLine + 1; k < FalseTarget && k < Length; k++)
            {
                if (Lines[k].OPCode?.Value == "jmp" && AbsTarget(Lines, k) <= CMPLine)
                {
                    CFA.AddEvent(CMPLine, new CfEvent(CfEventKind.WhileBegin, BuildCondition(CMPLine)));
                    CFA.AddEvent(k, new CfEvent(CfEventKind.EndWhile));
                    Handled[CMPLine] = true;
                    Handled[JMPFLine] = true;
                    Handled[k] = true;
                    return true;
                }
            }
            return false;
        }

        private static void EmitSimpleIfEnd(
            ControlFlowAnalyzer CFA,
            List<AsmCode> Lines,
            int Length,
            bool[] Handled,
            int FalseTarget)
        {
            int PrevFalse = FalseTarget - 1;
            if (PrevFalse >= 0 && PrevFalse < Length
                && Lines[PrevFalse].OPCode?.Value == "jmp"
                && AbsTarget(Lines, PrevFalse) > FalseTarget)
            {
                int ElseEnd = Math.Min(AbsTarget(Lines, PrevFalse) - 1, Length - 1);
                CFA.AddEvent(FalseTarget, new CfEvent(CfEventKind.ElseBegin));
                CFA.AddEvent(ElseEnd, new CfEvent(CfEventKind.EndIf));
                Handled[PrevFalse] = true;
            }
            else
            {
                int EndIfLine = Math.Max(0, Math.Min(FalseTarget - 1, Length - 1));
                CFA.AddEvent(EndIfLine, new CfEvent(CfEventKind.EndIf));
            }
        }

        private static int ResolveChainEnd(
            ControlFlowAnalyzer CFA,
            List<AsmCode> Lines,
            int Length,
            bool[] Handled,
            List<IfSeg> Chain)
        {
            int CommonEnd = -1;

            int PF0 = Chain[0].FalseTarget - 1;
            if (PF0 >= 0 && PF0 < Length && Lines[PF0].OPCode?.Value == "jmp")
            {
                CommonEnd = AbsTarget(Lines, PF0) - 1;
                Handled[PF0] = true;
            }

            for (int ci = 1; ci < Chain.Count; ci++)
            {
                int PFN = Chain[ci].FalseTarget - 1;
                if (PFN >= 0 && PFN < Length && Lines[PFN].OPCode?.Value == "jmp")
                {
                    Handled[PFN] = true;
                    if (CommonEnd < 0) CommonEnd = AbsTarget(Lines, PFN) - 1;
                }
            }

            if (CommonEnd < 0)
                CommonEnd = Chain[Chain.Count - 1].FalseTarget - 1;

            return Math.Max(0, Math.Min(CommonEnd, Length - 1));
        }
    }

    // =========================================================================
    // ControlFlowCodeGen
    // =========================================================================

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

                switch (LineOP)
                {
                    case "callmethod":
                        {
                            var LineHead = Line.Links.GetHead();
                            var CallerNode = LineHead?.Next;
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

                    case "callstatic":
                        {
                            var LineHead = Line.Links.GetHead();
                            var FuncNode = LineHead?.Next;
                            var RetNode = FuncNode?.Next;
                            if (RetNode == null || RetNode.GetValue() != Name) continue;

                            Line.PSCCode = "";

                            string ClassName = PapyrusAsmDecoder.CapitalizeFirst(LineHead.GetValue());
                            string FuncName = FuncNode?.GetValue() ?? "?";

                            var ParamList = BuildResolvedParams(Tracker, j, RetNode.Next?.Next, TempStrings);
                            return string.Format("{0}.{1}({2})", ClassName, FuncName, string.Join(", ", ParamList));
                        }

                    case "strcat":
                        {
                            var LineHead = Line.Links.GetHead();
                            if (LineHead.GetValue() != Name) continue;
                            Line.PSCCode = "";

                            string Left = ResolveTemp(Tracker, j, LineHead.Next?.GetValue() ?? "?", TempStrings);
                            string Right = ResolveTemp(Tracker, j, LineHead.Next?.Next?.GetValue() ?? "?", TempStrings);
                            return string.Format("{0} + {1}", Left, Right);
                        }

                    case "cast":
                        {
                            var LineHead = Line.Links.GetHead();
                            if (LineHead.GetValue() != Name) continue;
                            Line.PSCCode = "";
                            return ResolveTemp(Tracker, j, LineHead.Next?.GetValue() ?? "?", TempStrings);
                        }

                    case "propget":
                        {
                            var LineHead = Line.Links.GetHead();
                            var ObjNode = LineHead.Next;
                            var DestNode = ObjNode?.Next;
                            if (DestNode == null || DestNode.GetValue() != Name) continue;
                            Line.PSCCode = "";

                            string PropName = LineHead.GetValue();
                            string Obj = (ObjNode?.GetValue() ?? "Self").ToLower() == "self"
                                                  ? "Self" : ObjNode?.GetValue() ?? "Self";
                            return string.Format("{0}.{1}", Obj, PropName);
                        }

                    case "array_getelement":
                        {
                            var LineHead = Line.Links.GetHead();
                            if (LineHead.GetValue() != Name) continue;
                            Line.PSCCode = "";

                            string Array = ResolveTemp(Tracker, j, LineHead.Next?.GetValue() ?? "?", TempStrings);
                            string Index = ResolveTemp(Tracker, j, LineHead.Next?.Next?.GetValue() ?? "?", TempStrings);
                            return string.Format("{0}[{1}]", Array, Index);
                        }

                    case "assign":
                        {
                            var LineHead = Line.Links.GetHead();
                            if (LineHead.GetValue() != Name) continue;
                            Line.PSCCode = "";
                            return ResolveTemp(Tracker, j, LineHead.Next?.GetValue() ?? "?", TempStrings);
                        }

                    case "not":
                        {
                            var LineHead = Line.Links.GetHead();
                            if (LineHead.GetValue() != Name) continue;
                            Line.PSCCode = "";
                            string Operand = LineHead.Next?.GetValue() ?? "?";
                            return string.Format("!{0}", Operand);
                        }
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
        /// Single linear pass:
        ///   1. Emit control-flow keywords.
        ///   2. Inline remaining temp references.
        ///   3. Apply indentation.
        ///
        /// IsSingleLine if bodies are emitted without braces:
        ///     if (cond)
        ///         body;
        /// </summary>
        public static void ApplyControlFlow(
            DecompileTracker Tracker,
            List<PexString> TempStrings,
            ICodeStyle Style)
        {
            var Lines = Tracker.Lines;
            int Length = Lines.Count;

            var CFA = ControlFlowAnalyzer.Analyze(
                            Lines,
                            LineIndex => BuildCondition(Tracker, LineIndex, TempStrings));

            int Depth = 0;
            bool NextLineIsSingleIfBody = false;

            for (int i = 0; i < Length; i++)
            {
                var AsmLine = Lines[i];
                string OPCode = AsmLine.OPCode?.Value ?? "";
                var Events = CFA.GetEvents(i);

                var NStringBuilder = new StringBuilder();

                // ── 1. Emit control-flow keywords ─────────────────────────────
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

                // ── 2. Inline remaining temp references ───────────────────────
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

                // ── 3. Append body line ───────────────────────────────────────
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