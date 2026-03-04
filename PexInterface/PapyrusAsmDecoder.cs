using System.Collections.Generic;
using System;
using PexInterface;
using System.Linq;
using static PexInterface.PexReader;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;

// Copyright (c) 2026 YD525
// Licensed under the LGPL3.0 License.
// See LICENSE file in the project root for full license information.

#region Extend
public enum CodeGenStyle
{
    Null = 0, Papyrus = 1, CSharp = 2, Python = 3
}

#endregion
public class PapyrusAsmDecoder
{
    public static string ObjToStr(object Item)
    {
        string GetConvertStr = string.Empty;
        if (Item == null == false)
        {
            GetConvertStr = Item.ToString();
        }
        return GetConvertStr;
    }
    public static int ObjToInt(object Item)
    {
        int Number = -1;
        if (Item == null == false)
        {
            int.TryParse(Item.ToString(), out Number);
        }
        return Number;
    }

    public static string Version = "1.0.2 Alpha";
    public PexReader Reader;

    public PapyrusAsmDecoder(PexReader CurrentReader)
    {
        this.Reader = CurrentReader;
    }
    public enum ObjType
    {
        Null = 0, Variables = 1, Properties = 2, Functions = 3, DebugInfo = 5
    }
    public object QueryAnyByID(int ID, ref ObjType Type)
    {
        foreach (var GetObj in Reader.Objects)
        {
            foreach (var GetItem in GetObj.Variables)
            {
                if (GetItem.NameIndex.Equals((ushort)ID))
                {
                    Type = ObjType.Variables;
                    return GetItem;
                }
            }

            foreach (var GetItem in GetObj.Properties)
            {
                if (GetItem.NameIndex.Equals((ushort)ID))
                {
                    Type = ObjType.Properties;
                    return GetItem;
                }
            }

            List<PexFunction> Functions = new List<PexFunction>();

            foreach (var GetItem in GetObj.States)
            {
                foreach (var GetFunc in GetItem.Functions)
                {
                    if (GetFunc.FunctionNameIndex.Equals((ushort)ID))
                    {
                        Type = ObjType.Functions;
                        Functions.Add(GetFunc);
                    }
                }
            }

            if (Functions.Count > 0)
            {
                return Functions;
            }

            foreach (var GetDebugFunc in Reader.DebugInfo.Functions)
            {
                if (GetDebugFunc.FunctionNameIndex.Equals(ID))
                {
                    Type = ObjType.DebugInfo;
                    return GetDebugFunc;
                }
            }
        }

        return null;
    }
    public PscCls DeClass(List<PexString> TempStrings)
    {
        PscCls CreateCls = new PscCls();

        if (Reader.Objects.Count > 0)
        {
            string ScriptName = TempStrings[Reader.Objects[0].NameIndex].Value;
            string ParentClass = TempStrings[Reader.Objects[0].ParentClassNameIndex].Value;

            CreateCls.ClassName = ScriptName;
            CreateCls.Inherit = ParentClass;
        }

        return CreateCls;
    }
    public List<AutoGlobalVariable> DeAutoGlobalVariables(List<PexString> TempStrings)
    {
        List<AutoGlobalVariable> AutoGlobalVariables = new List<AutoGlobalVariable>();

        for (int i = 0; i < TempStrings.Count; i++)
        {
            var Item = TempStrings[i];
            ObjType CheckType = ObjType.Null;

            var TempValue = QueryAnyByID(Item.Index, ref CheckType);

            if (CheckType == ObjType.Properties)
            {
                PexProperty Property = TempValue as PexProperty;

                string GetVariableType = TempStrings[Property.TypeIndex].Value;
                string CheckVarName = TempStrings[Property.AutoVarNameIndex].Value;

                if (CheckVarName.StartsWith("::"))
                {
                    var RealValue = QueryAnyByID(Property.AutoVarNameIndex, ref CheckType);
                    string GetRealValue = "";
                    if (CheckType == ObjType.Variables)
                    {
                        GetRealValue = ObjToStr((RealValue as PexVariable).DataValue);
                    }

                    AutoGlobalVariable NAutoGlobalVariable = new AutoGlobalVariable();
                    NAutoGlobalVariable.Name = Item.Value;
                    NAutoGlobalVariable.Type = GetVariableType;
                    NAutoGlobalVariable.DeValue = GetRealValue;

                    AutoGlobalVariables.Add(NAutoGlobalVariable);
                }

            }
        }

        return AutoGlobalVariables;
    }
    public List<GlobalVariable> DeGlobalVariables(List<PexString> TempStrings)
    {
        List<GlobalVariable> GlobalVariables = new List<GlobalVariable>();

        for (int i = 0; i < TempStrings.Count; i++)
        {
            var Item = TempStrings[i];
            ObjType CheckType = ObjType.Null;
            var TempValue = QueryAnyByID(Item.Index, ref CheckType);

            if (CheckType == ObjType.Variables)
            {
                PexVariable Variable = TempValue as PexVariable;
                if (!Item.Value.StartsWith("::"))
                {
                    string GetVariableType = TempStrings[Variable.TypeNameIndex].Value;
                    string TryGetValue = ObjToStr(Variable.DataValue);
                    if (TryGetValue.Length == 0)
                    {
                        GlobalVariable NGlobalVariable = new GlobalVariable();
                        NGlobalVariable.Type = GetVariableType;
                        NGlobalVariable.Name = Item.Value;
                        NGlobalVariable.Value = "";

                        GlobalVariables.Add(NGlobalVariable);
                    }
                    else
                    {
                        GlobalVariable NGlobalVariable = new GlobalVariable();
                        NGlobalVariable.Type = GetVariableType;
                        NGlobalVariable.Name = Item.Value;

                        if (GetVariableType.ToLower().Equals("string"))
                        {
                            NGlobalVariable.Value = "\"" + TryGetValue + "\"";
                            GlobalVariables.Add(NGlobalVariable);
                        }
                        else
                        {
                            NGlobalVariable.Value = TryGetValue;
                            GlobalVariables.Add(NGlobalVariable);
                        }
                    }
                }
            }
        }

        return GlobalVariables;
    }

    public List<FunctionBlock> DeFunction(PscCls ParentCls,List<PexString> TempStrings,bool CanSkipPscDeCode)
    {
        List<FunctionBlock> FunctionBlocks = new List<FunctionBlock>();
        for (int i = 0; i < TempStrings.Count; i++)
        {
            var Item = TempStrings[i];

            ObjType CheckType = ObjType.Null;

            var GetFunc = QueryAnyByID(Item.Index, ref CheckType);
            if (CheckType == ObjType.Functions)
            {
                List<PexFunction> Function = GetFunc as List<PexFunction>;

                if (Function.Count == 1)
                {
                    FunctionBlock NFunctionBlock = new FunctionBlock();
                    var GetFunc1st = Function[0];

                    string ReturnType = TempStrings[GetFunc1st.ReturnTypeIndex].Value;

                    if (ReturnType == "None")
                    {
                        ReturnType = string.Empty;
                    }
                    else
                    {
                        if (ReturnType.Length > 0)
                        {
                            ReturnType += " ";
                        }
                    }

                    ReturnType = ReturnType.Trim();

                    NFunctionBlock.FunctionName = Item.Value;
                    NFunctionBlock.ReturnType = ReturnType;

                    if (Item.Value == "OnMenuOpenST")
                    { 
                    
                    }

                    List<LocalVariable> LocalVariables = new List<LocalVariable>();

                    for (int ir = 0; ir < GetFunc1st.NumParams; ir++)
                    {
                        if (GetFunc1st.Parameters.Count > ir)
                        {
                            var GetParam = GetFunc1st.Parameters[ir];
                            string GetType = TempStrings[GetFunc1st.Parameters[ir].TypeIndex].Value;
                            string GetParamName = TempStrings[GetParam.NameIndex].Value;

                            LocalVariable NLocalVariable = new LocalVariable();
                            NLocalVariable.Name = GetParamName;
                            NLocalVariable.Type = GetType;

                            LocalVariables.Add(NLocalVariable);
                        }
                    }

                    NFunctionBlock.Params = LocalVariables;

                    string FunctionCode = "";
                    int LineIndex = 0;
                    DecompileTracker Tracker = new DecompileTracker(Item.Value);

                    foreach (var GetInstruction in GetFunc1st.Instructions)
                    {
                        string GetOPName = GetInstruction.GetOpcodeName();

                        List<int> IntValues = new List<int>();

                        string CurrentLine = "";
                        PexFunction PexFunc = null;

                        foreach (var GetArg in GetInstruction.Arguments)
                        {
                            var GetIndex = ObjToInt(GetArg.Value);
                            if (GetIndex > 0)
                            {
                                string GetValue = "";


                                IntValues.Add(GetArg.Type);
                                if (GetArg.Type == 3)
                                {

                                }
                                else
                                {
                                    var GetObj = TempStrings[GetIndex];

                                    var ChildType = ObjType.Null;
                                    var GetChildFunc = QueryAnyByID(GetObj.Index, ref ChildType);

                                    if (ChildType == ObjType.Functions)
                                    {
                                        var Functions = GetChildFunc as List<PexFunction>;
                                        if (Functions.Count > 0)
                                        {
                                            PexFunc = Functions[0];
                                        }
                                    }
                                    else
                                    {

                                    }

                                    GetValue = GetObj.Value;

                                    ObjType VariableType = ObjType.Null;
                                    var GetVariableTypes = QueryAnyByID(GetObj.Index, ref VariableType);
                                }

                                if (GetArg.Type == 2)
                                {
                                    CurrentLine += "\"" + GetValue + "\"" + " ";
                                }
                                else
                                {
                                    CurrentLine += GetValue + " ";
                                }
                            }
                        }

                        CurrentLine = CurrentLine.Trim();
                        FunctionCode += GetOPName + " " + CurrentLine + "\n";
                        Tracker.CheckCode(LineIndex, GetOPName,CurrentLine);
                        LineIndex++;
                    }

                    NFunctionBlock.FunctionCode = FunctionCode;
                    NFunctionBlock.TracksRef = Tracker.Tracks;

                    AsmExtend.DeFunctionCode(ParentCls, NFunctionBlock, Tracker, CanSkipPscDeCode);

                    FunctionBlocks.Add(NFunctionBlock);
                }
            }
        }
        return FunctionBlocks;
    }
    public void Decompile(out PexHeuristicAnalysis Analyst, bool CanSkipPscDeCode = false)
    {
        List<PexString> TempStrings = new List<PexString>();

        TempStrings.AddRange(Reader.StringTable);
    
        var GenPsc = DeClass(TempStrings);

        GenPsc.GlobalVariables = DeGlobalVariables(TempStrings);

        GenPsc.AutoGlobalVariables = DeAutoGlobalVariables(TempStrings);

        GenPsc.Functions = DeFunction(GenPsc,TempStrings, CanSkipPscDeCode);

        Analyst = new PexHeuristicAnalysis(GenPsc);
    }
}

public class LocalVariable
{
    public string Name = "";
    public string Type = "";
    public string Value = "";
}
public class GlobalVariable
{
    public string Type = "";
    public string Name = "";
    public string Value = "";
}

public class AutoGlobalVariable
{
    public string Type = "";
    public string Name = "";
    public string DeValue = "";
}

public class PscCls
{
    public string ClassName = "";
    public string Inherit = "";

    public List<GlobalVariable> GlobalVariables = new List<GlobalVariable>();
    public List<AutoGlobalVariable> AutoGlobalVariables = new List<AutoGlobalVariable>();
    public List<FunctionBlock> Functions = new List<FunctionBlock>();

    public GlobalVariable QueryGlobalVariable(string Name)
    {
        foreach (var Get in GlobalVariables)
        {
            if (Get.Name.Equals(Name))
            {
                return Get;
            }
        }
        return null;
    }
    public AutoGlobalVariable QueryAutoGlobalVariable(string Name)
    {
        foreach (var Get in AutoGlobalVariables)
        {
            if (Get.Name.Equals(Name))
            {
                return Get;
            }
        }
        return null;
    }

}
public class FunctionBlock
{
    public string FunctionName = "";
    public List<LocalVariable> Params = new List<LocalVariable>();
    public string ReturnType = "";

    public string FunctionCode = "";

    public int StartIndex = 0;

    public Dictionary<int, AssemblyLine> TracksRef = null;
}

public class TVariable
{
    public int LineIndex = 0;

    public string Tag = "";
    public string VariableName = "";
}

public class CastLink
{
    public int LineIndex = 0;

    public List<string> Links = new List<string>();

    public void AddLinks(List<string> SetLinks)
    {
        foreach (var GetLink in SetLinks)
        {
            if (!this.Links.Contains(GetLink))
            {
                this.Links.Add(GetLink);
            }
        }
    }
    public bool Find(string Name)
    {
        foreach (var Get in Links)
        {
            if (Get.Equals(Name))
            {
                return true;
            }
        }
        return false;
    }
}


public class AsmLink
{
    public string Value = null;
    public AsmLink Next = null;
    public AsmLink Prev = null;   
    private AsmLink Tail = null;

    public bool HaveValue()
    {
        if (Tail != null)
        {
            return true;
        }
        return false;
    }

    public void SetValue(string value)
    {
        if (Value == null)
        {
            Value = value;
            Tail = this;
        }
        else
        {
            var NewNode = new AsmLink { Value = value, Prev = Tail };
            Tail.Next = NewNode;
            Tail = NewNode;
        }
    }

    public string GetValue(int Index)
    {
        var Node = this;
        int i = 0;
        while (Node != null)
        {
            if (i == Index)
                return Node.Value;
            Node = Node.Next;
            i++;
        }
        return null;
    }

    public string GetValueFromTail(int IndexFromTail)
    {
        var Node = Tail;
        int i = 0;
        while (Node != null)
        {
            if (i == IndexFromTail)
            {
                return Node.Value;
            } 
            Node = Node.Prev;
            i++;
        }
        return null;
    }
    public int Count()
    {
        int Count = 0;
        var Node = this;
        while (Node != null)
        {
            Count++;
            Node = Node.Next;
        }
        return Count;
    }
    public void ForEachForward(Action<AsmLink> Action)
    {
        var Node = this;
        while (Node != null)
        {
            Action(Node);
            Node = Node.Next;
        }
    }
    public void ForEachBackward(Action<AsmLink> Action)
    {
        var Node = Tail;
        while (Node != null)
        {
            Action(Node);
            Node = Node.Prev;
        }
    }
}
public class AsmBase
{
    public string OPCode = "";
    public string AsmCode = "";
    public AsmLink Links = new AsmLink();
    public void ParseLink(string Str)
    {
        var Tokens = new List<string>();

        var Matches = Regex.Matches(Str, @"(::\w+)|(""[^""]*"")|(\S+)");

        foreach (Match m in Matches)
        {
            Tokens.Add(m.Value);
        }

        Tokens.Reverse();

        foreach (var Token in Tokens)
        {
            this.Links.SetValue(Token);
        }
    }
}

public class TProp
{
    public int LineIndex = 0;

    public List<string> Fronts = new List<string>();
    public string PropName = "";
    public string LinkVariable = "";

    public int IsGetOrSet = 0;
    public bool Self = false;
}

public class AsmCall:AsmBase
{
    public int LineIndex = 0;
    public string Call = "";

    public void Parse(string Line)
    {
        if (Line.Contains(" "))
        {
            this.Call = Line.Split(' ')[0];
            this.ParseLink(Line.Split(' ')[1]);
        }

        this.ParseLink(Line);
    }
}

public class TStrcat
{
    public int LineIndex = 0;

    public string LinkVariable = "";

    public string Value = "";

    public string MergeStr = "";

    public bool IsLeft = false;
}

public class TOperator
{
    public int LineIndex = 0;

    public string Operator = "";

    public List<string> Variables = new List<string>();
}

public class TReturn
{
    public int LineIndex = 0;

    public List<string> ReturnVariables = new List<string>();
    public TReturn(int LineIndex, List<string> Params)
    {
        this.LineIndex = LineIndex;
        this.ReturnVariables = Params;
    }
}

public class TJump
{
    public int LineIndex = 0;

    public string Jump = "";
    public string Variable = "";
}

public class TArrayOP
{
    public string ArrayOP = "";
    public List<string> Variables = new List<string>();
}

public class TValIncrease
{
    public string Variable = "";
    public string Increase = "";
}
public class TVariableSetter
{
    public int LineIndex = 0;

    public string Parent = "";
    public string Child = "";
}

public class AssemblyLine
{
    public string Assembly = "";
    public string Code = "";
    public int SpaceCount = 0;
    public int Score = 0;
    public object TrackRef;
    public AssemblyLine(string Assembly, object Track)
    {
        this.Assembly = Assembly;
        this.TrackRef = Track;
    }

    public AssemblyLine(string Assembly)
    {
        this.Assembly = Assembly;
        this.TrackRef = null;
    }

    public string GetNote(CodeGenStyle Style)
    {
        return "//" + this.Assembly;
    }
}

public class DecompileTracker
{
    public string FuncName = "";

    public Dictionary<int, AssemblyLine> Tracks = new Dictionary<int, AssemblyLine>();

    public List<TVariable> _PexVariables = new List<TVariable>();
    public List<AsmCall> AsmCalls = new List<AsmCall>();
    public List<CastLink> _CastLinks = new List<CastLink>();
    public List<TProp> _Props = new List<TProp>();
    public List<TStrcat> _StrMerges = new List<TStrcat>();
    public List<TOperator> _Operators = new List<TOperator>();
    public List<TJump> _Jumps = new List<TJump>();
    public List<TValIncrease> _TIncreases =new List<TValIncrease>();
    public List<TReturn> _Returns = new List<TReturn>();
    public List<TArrayOP> _Arrays = new List<TArrayOP>();
    public List<TVariableSetter> _VariableSetters = new List<TVariableSetter>();

    public string QueryMethodVariable(string TempName)
    {
        TempName = TempName.Trim();
        foreach (var Get in this._CastLinks)
        {
            if (Get.Find(TempName))
            {
                foreach (var GetLinkValue in Get.Links)
                {
                    foreach (var GetVariable in _PexVariables)
                    {
                        if (GetVariable.Tag.Equals(GetLinkValue))
                        {
                            return GetVariable.VariableName.Trim();
                        }
                    }
                }
                break;
            }
        }

        foreach (var GetVariable in _PexVariables)
        {
            if (GetVariable.Tag.Equals(TempName))
            {
                return GetVariable.VariableName.Trim();
            }
        }

        return string.Empty;
    }
    public DecompileTracker(string FuncName)
    {
        this.FuncName = FuncName;
    }
    public List<string> CreateParams(string Line)
    {
        List<string> Params = new List<string>();
        foreach (var Get in Line.Split(new[] { "::" }, StringSplitOptions.None))
        {
            if (Get.Trim().Length > 0)
            {
                Params.Add(Get);
            }
        }
        return Params;
    }

    public string FormatVar(string VariableName)
    {
        VariableName = VariableName.Trim();
        if (VariableName.EndsWith("_Var"))
        {
            VariableName = VariableName.Substring(0, VariableName.Length - "_Var".Length);
        }
        else
        if (VariableName.EndsWith("_var"))
        {
            VariableName = VariableName.Substring(0, VariableName.Length - "_var".Length);
        }
        return VariableName;
    }
    public void CheckCode(int LineIndex, string OPCode, string Line)
    {
        List<string> GetParams = CreateParams(Line);
        string DefLine = OPCode + " " + Line;

        if (OPCode == "return")
        {
            var SetReturn = new TReturn(LineIndex, GetParams);
            _Returns.Add(SetReturn);
            Tracks.Add(LineIndex, new AssemblyLine(DefLine, SetReturn));
        }
        else
        if (OPCode == "callmethod" || OPCode == "callparent" || OPCode == "callstatic")
        {
            AsmCall NTFunction = new AsmCall();
            NTFunction.LineIndex = LineIndex;
            NTFunction.OPCode = OPCode;
            NTFunction.AsmCode = OPCode + " " + Line;
            NTFunction.Call = Line.Split(' ')[0].Trim();
            NTFunction.Parse(Line);

            AsmCalls.Add(NTFunction);
            Tracks.Add(LineIndex, new AssemblyLine(DefLine, NTFunction));
        }
        else
        if (OPCode == "assign")
        {
            if (GetParams.Count == 2)
            {
                TVariable NTVariable = new TVariable();
                NTVariable.LineIndex = LineIndex;
                NTVariable.Tag = GetParams[1];
                NTVariable.VariableName = GetParams[0];

                _PexVariables.Add(NTVariable);
                Tracks.Add(LineIndex, new AssemblyLine(DefLine, NTVariable));
            }
            else
            {
                if (Line.Split(' ').Length == 2)
                {
                    TVariableSetter NVariableSetter = new TVariableSetter();
                    NVariableSetter.LineIndex = LineIndex;
                    NVariableSetter.Parent = Line.Split(' ')[0];
                    NVariableSetter.Child = Line.Split(' ')[1];
                    _VariableSetters.Add(NVariableSetter);

                    Tracks.Add(LineIndex, new AssemblyLine(DefLine, NVariableSetter));
                }
                else
                {
                    TVariable NTVariable = new TVariable();
                    NTVariable.VariableName = Line;
                    NTVariable.LineIndex = LineIndex;

                    _PexVariables.Add(NTVariable);
                    Tracks.Add(LineIndex, new AssemblyLine(DefLine, NTVariable));
                }
            }
        }
        else
        if (OPCode == "cast")
        {
            bool FindLink = false;
            int SetOffset = -1;

            for (int i = 0; i < this._CastLinks.Count; i++)
            {
                foreach (var CheckLink in GetParams)
                {
                    if (this._CastLinks[i].Find(CheckLink))
                    {
                        SetOffset = i;
                        FindLink = true;
                        goto SetLink;
                    }
                }
            }

        SetLink:
            if (FindLink)
            {
                this._CastLinks[SetOffset].AddLinks(GetParams);
            }
            else
            {
                CastLink NCastLink = new CastLink();
                NCastLink.LineIndex = LineIndex;
                NCastLink.AddLinks(GetParams);

                this._CastLinks.Add(NCastLink);
            }
        }
        else
        if (OPCode == "propget" || OPCode == "propset")
        {
            TProp NTProp = new TProp();
            NTProp.LineIndex = LineIndex;

            if (OPCode == "propset")
            {
                NTProp.IsGetOrSet = 1;
            }

            if (GetParams.Count == 2)
            {
                string GetPropName = GetParams[0];
                GetPropName = GetPropName.Trim();

                if (GetPropName.Contains(" "))
                {
                    var GetPropNames = GetPropName.Split(' ');
                    if (GetPropNames.Length >= 2)
                    {
                        NTProp.PropName = GetPropNames[0];

                        if (GetPropNames[1] == "self")
                        {
                            NTProp.Self = true;
                        }
                    }

                    NTProp.LinkVariable = GetParams[1];
                }
                else
                {
                    NTProp.PropName = GetPropName;
                    NTProp.LinkVariable = GetParams[1];
                }
            }
            else
            {
                List<string> GetFronts = new List<string>();

                foreach (var Get in GetParams)
                {
                    if (Get.Trim() != GetParams[GetParams.Count - 1].Trim() && Get.Trim() != GetParams[0].Trim())
                    {
                        GetFronts.Add(Get.Trim());
                    }
                }

                NTProp.PropName = GetParams[0].Trim();
                NTProp.LinkVariable = GetParams[GetParams.Count - 1].Trim();
                NTProp.Fronts = GetFronts;
            }

            _Props.Add(NTProp);
            Tracks.Add(LineIndex, new AssemblyLine(DefLine, NTProp));
        }
        else
        if (OPCode == "strcat")
        {
            if (GetParams.Count > 0)
            {
                string StrValue = "";
                List<string> StrcatParams = new List<string>();

                if (GetParams.Count > 0 && GetParams.Count < 2)
                {
                    foreach (var Get in GetParams[0].Split(' '))
                    {
                        if (Get.Trim().StartsWith("\""))
                        {
                            StrValue = Get.Trim();
                        }
                        else
                        if (Get.Trim().Length > 0)
                        {
                            StrcatParams.Add(Get.Trim());
                        }
                    }
                }
                else
                if (GetParams.Count >= 2)
                {
                    StrcatParams = GetParams;
                }

                TStrcat NTStrcat = new TStrcat();
                NTStrcat.LineIndex = LineIndex;
                NTStrcat.LinkVariable = StrcatParams[0].Trim();
                if (StrValue.Trim().Length > 0)
                {
                    NTStrcat.Value = StrcatParams[StrcatParams.Count - 1];
                    NTStrcat.MergeStr = StrValue;
                }
                else
                {
                    NTStrcat.MergeStr = StrcatParams[StrcatParams.Count - 1];
                }

                NTStrcat.MergeStr = NTStrcat.MergeStr.Trim();
                if (NTStrcat.MergeStr.StartsWith(NTStrcat.LinkVariable))
                {
                    NTStrcat.MergeStr = NTStrcat.MergeStr.Substring(NTStrcat.LinkVariable.Length).Trim();
                    NTStrcat.IsLeft = true;
                }
                if (NTStrcat.MergeStr.EndsWith(NTStrcat.LinkVariable))
                {
                    NTStrcat.MergeStr = NTStrcat.MergeStr.Substring(0, NTStrcat.MergeStr.Length - NTStrcat.LinkVariable.Length).Trim();
                    NTStrcat.IsLeft = false;
                }
                _StrMerges.Add(NTStrcat);
                Tracks.Add(LineIndex, new AssemblyLine(DefLine, NTStrcat));
            }
        }
        else
        if (OPCode == "cmp_eq" || OPCode == "cmp_lt" || OPCode == "cmp_le" || OPCode == "cmp_gt" || OPCode == "cmp_ge" || OPCode == "not" || OPCode == "isub")
        {
            if (GetParams.Count > 0)
            {
                TOperator NTOperator = new TOperator();
                NTOperator.LineIndex = LineIndex;
                NTOperator.Operator = OPCode;
                NTOperator.Variables = GetParams;

                _Operators.Add(NTOperator);
                Tracks.Add(LineIndex, new AssemblyLine(DefLine, NTOperator));
            }
            else
            {
                throw new Exception("cmp parse failed.");
            }
        }
        else
        if (OPCode == "jmpt" || OPCode == "jmpf" || OPCode == "jmp")
        {
            var SetJump = new TJump();
            SetJump.LineIndex = LineIndex;
            SetJump.Jump = OPCode;

            if (GetParams.Count > 0)
            {
                SetJump.Variable = GetParams[0].Trim();
            }

            _Jumps.Add(SetJump);
            Tracks.Add(LineIndex, new AssemblyLine(DefLine, SetJump));
        }
        else
        if (OPCode == "iadd")
        {
            var SetIncrease = new TValIncrease();
            if (GetParams.Count > 0)
            {
                if (GetParams[0].Contains(" "))
                {
                    SetIncrease.Variable = GetParams[0].Split(' ')[1].Trim();
                    SetIncrease.Increase = GetParams[0].Split(' ')[0].Trim();
                }
                else
                {
                    SetIncrease.Increase = GetParams[0].Trim();
                }
            }

            _TIncreases.Add(SetIncrease);
            Tracks.Add(LineIndex, new AssemblyLine(DefLine, SetIncrease));
        }
        else
        if (OPCode == "array_create" || OPCode == "array_setelement" || OPCode == "array_getelement" || OPCode == "array_length")
        {
            TArrayOP NTArray = new TArrayOP();
            NTArray.ArrayOP = OPCode;
            NTArray.Variables = GetParams;
            _Arrays.Add(NTArray);
            Tracks.Add(LineIndex, new AssemblyLine(DefLine, NTArray));
        }
        else
        {
            Tracks.Add(LineIndex, new AssemblyLine(DefLine));
        }
    }
}