using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using static PexInterface.PexReader;

namespace PexInterface
{
    // =========================================================================
    // Code-style abstraction
    // =========================================================================

    /// <summary>
    /// Defines how control-flow keywords and brackets are emitted.
    /// Implement this interface to add a new output language style.
    /// </summary>
    public interface ICodeStyle
    {
        string If(string condition);
        string ElseIf(string condition);
        string Else();
        string EndIf();
        string While(string condition);
        string EndWhile();
        string Indent(int depth);
    }

    /// <summary>
    /// Original Papyrus scripting language style.
    /// </summary>
    public class PapyrusStyle : ICodeStyle
    {
        public static readonly PapyrusStyle Instance = new PapyrusStyle();

        public string If(string condition) => "If " + condition;
        public string ElseIf(string condition) => "ElseIf " + condition;
        public string Else() => "Else";
        public string EndIf() => "EndIf";
        public string While(string condition) => "While " + condition;
        public string EndWhile() => "EndWhile";
        public string Indent(int depth) => new string(' ', depth * 4);
    }

    /// <summary>
    /// C#-style output with braces.
    /// </summary>
    public class CSharpStyle : ICodeStyle
    {
        public static readonly CSharpStyle Instance = new CSharpStyle();

        public string If(string condition) => "if (" + condition + ") {";
        public string ElseIf(string condition) => "} else if (" + condition + ") {";
        public string Else() => "} else {";
        public string EndIf() => "}";
        public string While(string condition) => "while (" + condition + ") {";
        public string EndWhile() => "}";
        public string Indent(int depth) => new string(' ', depth * 4);
    }


    // =========================================================================
    // AsmExtend — Pass 1 (opcode → PSCCode) + orchestration
    // =========================================================================

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

        // ─────────────────────────────────────────────────────────────────────
        // Public entry point
        // ─────────────────────────────────────────────────────────────────────

        public static void DeFunctionCode(
            CodeGenStyle Style,
            List<PexString> TempStrings,
            PscCls ParentCls,
            FunctionBlock Func,
            DecompileTracker TrackerRef,
            bool CanSkipPscDeCode,
            ICodeStyle CodeStyle = null)
        {
            // Debug hook — keep for breakpoint inspection
            //if (Func.FunctionName == "IsFeatureEnabled")
            //{
            //    string AsmCode = "";
            //    for (int i = 0; i < TrackerRef.Lines.Count; i++)
            //        AsmCode += TrackerRef.Lines[i].GetAsmCode() + "\n";
            //    GC.Collect();
            //}

            if (CanSkipPscDeCode) return;

            // Default to Papyrus style when none is specified
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
            Pass5_DeclareResidualTemps(TrackerRef);

            Func.StringFlower = new Dictionary<ushort, StringFlowRecord>();
            foreach (var GetFlow in StringFlowAnalyzer.Analyze(TrackerRef, ParentCls, Func))
            {
                if (!Func.StringFlower.ContainsKey(GetFlow.StringID))
                    Func.StringFlower.Add(GetFlow.StringID, GetFlow);
            }
        }


        // ─────────────────────────────────────────────────────────────────────
        // Pass 1 — translate each opcode to PSCCode
        // Control-flow opcodes are intentionally left empty (handled in Pass 4).
        // ─────────────────────────────────────────────────────────────────────

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

        // ─────────────────────────────────────────────────────────────────────
        // Pass 2 — infer array element type for every array_create instruction
        // ─────────────────────────────────────────────────────────────────────

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

        // ─────────────────────────────────────────────────────────────────────
        // Pass 3 — resolve __TYPE__ placeholders left by Pass 1
        // ─────────────────────────────────────────────────────────────────────

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

        // ─────────────────────────────────────────────────────────────────────
        // Pass 4 — control-flow analysis + styled keyword emission
        // ─────────────────────────────────────────────────────────────────────

        private static void Pass4_ControlFlow(
            DecompileTracker TrackerRef,
            List<PexString> TempStrings,
            ICodeStyle CodeStyle)
        {
            ControlFlowCodeGen.ApplyControlFlow(TrackerRef, TempStrings, CodeStyle);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Pass 5 — declare any temp variables that survived inlining
        // After Pass 4, temps that were consumed are blank. Any that remain
        // as the LHS of an assignment need a leading "var" declaration.
        // ─────────────────────────────────────────────────────────────────────

        private static void Pass5_DeclareResidualTemps(DecompileTracker TrackerRef)
        {
            // Matches leading whitespace + tempN on the left of an assignment
            var TempLhsRx = new Regex(@"^(\s*)(temp\d+)\s*=");           // temp 在 LHS
            var TempRhsRx = new Regex(@"\btemp\d+\b");                    // temp 在 RHS / 任意位置
            var DeclaredTemps = new HashSet<string>();

            foreach (var AsmLine in TrackerRef.Lines)
            {
                if (string.IsNullOrEmpty(AsmLine.PSCCode)) continue;

                string[] Parts = AsmLine.PSCCode.Split('\n');
                bool Changed = false;

                for (int p = 0; p < Parts.Length; p++)
                {
                    var M = TempLhsRx.Match(Parts[p]);
                    if (!M.Success) continue;

                    string TempName = M.Groups[2].Value;
                    if (DeclaredTemps.Add(TempName) && !TrackerRef.Variables.IsCreated(TempName))
                    {
                        TrackerRef.Variables.Add(
                            new AsmOffset(0, 0), TempName, 0, null, VariableAccess.None);
                        Parts[p] = M.Groups[1].Value + "var " + Parts[p].TrimStart();
                        Changed = true;
                    }
                }

                if (Changed)
                    AsmLine.PSCCode = string.Join("\n", Parts);
            }
        }


        // =========================================================================
        // Opcode emitters — one method per opcode
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

            // Fold a preceding iadd into += when the temp flows directly into this assign
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

            // First assignment → emit __TYPE__ placeholder for type inference in Pass 3
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

            AsmLine.PSCCode = (ReturnNode == null || ReturnNode.IsNull())
                ? CallExpr + ";"
                : string.Format("{0} = {1};", ReturnVar, CallExpr);

            TrackerRef.Variables.Add(new AsmOffset(i, 2), ReturnVar, 0,
                new List<AsmLink> { CallerNode, Head }, VariableAccess.Set);
        }

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

                // Check every argument link for a read of TempName
                var Node = ScanLine.Links?.GetHead();
                int ArgPos = 0;
                while (Node != null)
                {
                    // For callmethod/callstatic the return slot is arg index 2 — that's a write, not a read
                    bool IsReturnSlot = (ScanLine.OPCode?.Value == "callmethod" && ArgPos == 2)
                                     || (ScanLine.OPCode?.Value == "callstatic" && ArgPos == 2);
                    // For assign/not/cast/etc. arg index 0 is the destination — write, not a read
                    bool IsDestSlot = ArgPos == 0 && ScanLine.OPCode?.Value != "callmethod"
                                                  && ScanLine.OPCode?.Value != "callstatic";

                    if (!IsReturnSlot && !IsDestSlot && Node.GetValue() == TempName)
                        return false; // It is read here

                    Node = Node.Next;
                    ArgPos++;
                }

                // If this line overwrites TempName as destination, stop — it's redefined, original never read
                var Head = ScanLine.Links?.GetHead();
                if (Head != null && Head.GetValue() == TempName)
                    break;
            }

            return true; // Never read after FromLine
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

            // Suppress the assignment if the return value is never actually used
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

            // Register so that a subsequent assign to the same variable is not re-declared
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

        /// <summary>
        /// Infer element type from a specific argument of an array_setelement instruction.
        /// </summary>
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

        /// <summary>
        /// Infer the variable type from a RHS expression (literal, identifier, or temp).
        /// </summary>
        private static string InferTypeFromRhs(
            DecompileTracker Tracker,
            int FromLine,
            string Rhs,
            PscCls ParentCls,
            FunctionBlock Func,
            List<PexString> TempStrings)
        {
            // Direct literal checks
            if (Rhs.StartsWith("\"") || Rhs.StartsWith("$")) return "String";
            if (Rhs == "True" || Rhs == "False") return "Bool";
            if (int.TryParse(Rhs, out _)) return "Int";
            if (float.TryParse(Rhs, NumberStyles.Float, CultureInfo.InvariantCulture, out _)) return "Float";
            if (Rhs == "None") return "";

            // Non-temp: look up in class variables / function params
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

            // Temp: trace backwards to its producing instruction
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
                    return "var"; // Property type not available without an external type registry
                }

                if (LineOP == "assign")
                {
                    if (LineHead.GetValue() != Rhs) continue;
                    return InferTypeFromRhs(Tracker, j, LineHead.Next?.GetValue() ?? "", ParentCls, Func, TempStrings);
                }
            }

            return "";
        }


        // =========================================================================
        // Shared utilities
        // =========================================================================

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

        public CfEvent(CfEventKind kind, string cond = "")
        {
            Kind = kind;
            Condition = cond;
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

        // Papyrus jump offset is relative to the instruction *after* the jump:
        //   absoluteTarget = jmpIndex + 1 + offset
        private static int AbsTarget(List<AsmCode> Lines, int JmpIndex)
            => JmpIndex + 1 + Lines[JmpIndex].GetJumpTarget();

        public static bool IsCmp(string OPCode)
            => OPCode == "cmp_eq" || OPCode == "cmp_lt" || OPCode == "cmp_le" ||
               OPCode == "cmp_gt" || OPCode == "cmp_ge";

        // ─────────────────────────────────────────────────────────────────────
        // ElseIf chain detection
        // ─────────────────────────────────────────────────────────────────────

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

        // ─────────────────────────────────────────────────────────────────────
        // Main analysis entry
        // ─────────────────────────────────────────────────────────────────────

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

                // Path A: cmp_* immediately followed by jmpf
                if (IsCmp(OPCode) && i + 1 < Length && Lines[i + 1].OPCode?.Value == "jmpf")
                {
                    i = AnalyzeIfChain(CFA, Lines, Length, Handled, i, BuildCondition);
                    continue;
                }

                // Path B: bare jmpf / jmpt (Bool variable, no preceding cmp_*)
                if (OPCode == "jmpf" || OPCode == "jmpt")
                {
                    i = AnalyzeBareJump(CFA, Lines, Length, Handled, i, OPCode);
                    continue;
                }

                i++;
            }

            return CFA;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Path A — cmp_* + jmpf  (if / elseif / while)
        // ─────────────────────────────────────────────────────────────────────

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

            // While: a back-jump inside the body targets <= CmpLine
            if (TryDetectWhile(CFA, Lines, Length, Handled, CmpLine, JMPFLine, FalseTarget, BuildCondition))
                return CmpLine + 1;

            var Chain = CollectChain(Lines, CmpLine, Length);

            if (Chain.Count <= 1)
            {
                // Simple If [/ Else]
                CFA.AddEvent(CmpLine, new CfEvent(CfEventKind.IfBegin, BuildCondition(CmpLine)));
                Handled[CmpLine] = true;
                Handled[JMPFLine] = true;
                EmitSimpleIfEnd(CFA, Lines, Length, Handled, FalseTarget);
            }
            else
            {
                // ElseIf chain
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

        // ─────────────────────────────────────────────────────────────────────
        // Path B — bare jmpf / jmpt  (if / while on a Bool variable)
        // ─────────────────────────────────────────────────────────────────────

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

            // While detection: look for a back-jump inside the body
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

            // If [/ Else] — only emit Else when the instruction immediately before
            // FalseTarget is a jmp whose target is strictly past FalseTarget.
            // This prevents inner or unrelated jmps from being mistaken as else exits.
            CFA.AddEvent(JMPLine, new CfEvent(CfEventKind.IfBegin, Condition));
            Handled[JMPLine] = true;

            int PrevFalse = FalseTarget - 1;
            bool HasElse = PrevFalse > JMPLine
                             && PrevFalse < Length
                             && Lines[PrevFalse].OPCode?.Value == "jmp"
                             && AbsTarget(Lines, PrevFalse) > FalseTarget;

            if (HasElse && FalseTarget < Length)
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

        // ─────────────────────────────────────────────────────────────────────
        // Shared sub-helpers
        // ─────────────────────────────────────────────────────────────────────

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

        /// <summary>
        /// Emits EndIf (and optionally ElseBegin) for a simple single-segment if.
        /// Only treats the instruction at FalseTarget-1 as an else-exit jmp when its
        /// target is strictly past FalseTarget (i.e. it skips an else block).
        /// </summary>
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

        /// <summary>
        /// Resolves the common EndIf line index for an ElseIf chain
        /// and marks all inner exit-jumps as handled.
        /// </summary>
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
    // ControlFlowCodeGen — merges CfEvents + indentation into PSCCode
    // =========================================================================

    public static class ControlFlowCodeGen
    {
        // ─────────────────────────────────────────────────────────────────────
        // Condition builder
        // ─────────────────────────────────────────────────────────────────────

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

            // cmp layout: [dest] [left] [right]
            string Left = ResolveTemp(Tracker, LineIndex, Head?.Next?.GetValue() ?? "?", TempStrings);
            string Right = ResolveTemp(Tracker, LineIndex, Head?.Next?.Next?.GetValue() ?? "?", TempStrings);
            string OPCode = Tracker.Lines[LineIndex].OPCode?.Value ?? "";

            return string.Format("{0} {1} {2}", Left, CmpOp(OPCode), Right);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Temp resolver — walk backwards and inline the producing expression
        // ─────────────────────────────────────────────────────────────────────

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

                            Line.PSCCode = ""; // Consumed — suppress standalone emit

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

                            Line.PSCCode = ""; // Consumed — suppress standalone emit

                            string ClassName = PapyrusAsmDecoder.CapitalizeFirst(LineHead.GetValue());
                            string FuncName = FuncNode?.GetValue() ?? "?";

                            var ParamList = BuildResolvedParams(Tracker, j, RetNode.Next?.Next, TempStrings);
                            return string.Format("{0}.{1}({2})", ClassName, FuncName, string.Join(", ", ParamList));
                        }

                    case "strcat":
                        {
                            var LineHead = Line.Links.GetHead();
                            if (LineHead.GetValue() != Name) continue;
                            Line.PSCCode = ""; // Consumed

                            string Left = ResolveTemp(Tracker, j, LineHead.Next?.GetValue() ?? "?", TempStrings);
                            string Right = ResolveTemp(Tracker, j, LineHead.Next?.Next?.GetValue() ?? "?", TempStrings);
                            return string.Format("{0} + {1}", Left, Right);
                        }

                    case "cast":
                        {
                            var LineHead = Line.Links.GetHead();
                            if (LineHead.GetValue() != Name) continue;
                            Line.PSCCode = ""; // Consumed
                            return ResolveTemp(Tracker, j, LineHead.Next?.GetValue() ?? "?", TempStrings);
                        }

                    case "propget":
                        {
                            var LineHead = Line.Links.GetHead();
                            var ObjNode = LineHead.Next;
                            var DestNode = ObjNode?.Next;
                            if (DestNode == null || DestNode.GetValue() != Name) continue;
                            Line.PSCCode = ""; // Consumed

                            string PropName = LineHead.GetValue();
                            string Obj = (ObjNode?.GetValue() ?? "Self").ToLower() == "self"
                                                  ? "Self" : ObjNode?.GetValue() ?? "Self";
                            return string.Format("{0}.{1}", Obj, PropName);
                        }

                    case "array_getelement":
                        {
                            var LineHead = Line.Links.GetHead();
                            if (LineHead.GetValue() != Name) continue;
                            Line.PSCCode = ""; // Consumed

                            string Array = ResolveTemp(Tracker, j, LineHead.Next?.GetValue() ?? "?", TempStrings);
                            string Index = ResolveTemp(Tracker, j, LineHead.Next?.Next?.GetValue() ?? "?", TempStrings);
                            return string.Format("{0}[{1}]", Array, Index);
                        }

                    case "assign":
                        {
                            var LineHead = Line.Links.GetHead();
                            if (LineHead.GetValue() != Name) continue;
                            return ResolveTemp(Tracker, j, LineHead.Next?.GetValue() ?? "?", TempStrings);
                        }
                    case "not":
                        {
                            var LineHead = Line.Links.GetHead();
                            if (LineHead.GetValue() != Name) continue;
                            Line.PSCCode = ""; // Consumed
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

        // ─────────────────────────────────────────────────────────────────────
        // Main code-gen pass
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Single linear pass over all instructions:
        ///   1. Prepend styled control-flow keywords (If / Else / EndIf / While / EndWhile).
        ///   2. Inline any remaining temp references in body expressions.
        ///   3. Apply indentation.
        /// After this pass, SpaceCount is 0 on every line (indentation is embedded).
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
                                AsmLine.PSCCode = ""; // Back-jump jmp emits nothing
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
                                AsmLine.PSCCode = ""; // cmp instruction emits nothing
                                break;

                            case CfEventKind.IfBegin:
                                NStringBuilder.AppendLine(Style.Indent(Depth) + Style.If(Event.Condition));
                                Depth++;
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

                // Jump opcodes are fully consumed by control-flow events above
                if (OPCode == "jmpf" || OPCode == "jmpt" || OPCode == "jmp")
                    AsmLine.PSCCode = "";

                // ── 2. Inline remaining temp references in body expressions ───
                if (!string.IsNullOrEmpty(AsmLine.PSCCode))
                {
                    string Code = AsmLine.PSCCode;
                    int EqPos = Code.IndexOf('=');

                    if (EqPos >= 0)
                    {
                        string Lhs = Code.Substring(0, EqPos + 1);
                        string Rhs = Regex.Replace(
                            Code.Substring(EqPos + 1),
                            @"\btemp\d+\b",
                            Match => ResolveTemp(Tracker, i, Match.Value, TempStrings));
                        Code = Lhs + Rhs;
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

                // ── 3. Append body line at current depth ──────────────────────
                if (!string.IsNullOrEmpty(AsmLine.PSCCode))
                    NStringBuilder.Append(Style.Indent(Depth) + AsmLine.PSCCode.Trim());

                AsmLine.PSCCode = NStringBuilder.ToString().TrimEnd('\r', '\n');
                AsmLine.SpaceCount = 0;
            }
        }
    }
}