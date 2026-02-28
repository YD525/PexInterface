
using static PexInterface.PexReader;
using System.Collections.Generic;
using System.Text;
using System;
using PexInterface;
using static PapyrusAsmDecoder;
using System.Net.Http.Headers;
using System.Data.OleDb;

// Copyright (c) 2026 YD525
// Licensed under the LGPL3.0 License.
// See LICENSE file in the project root for full license information.
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

    #region Extend
    public enum CodeGenStyle
    {
        Null = 0, Papyrus = 1, CSharp = 2, Python = 3
    }

    #endregion

    public static string Version = "1.0.1 Alpha";
    public CodeGenStyle GenStyle = CodeGenStyle.Null;
    public PexReader Reader;
    string CodeSpace = "    ";

    public PapyrusAsmDecoder(PexReader CurrentReader, CodeGenStyle GenStyle = CodeGenStyle.Papyrus)
    {
        this.Reader = CurrentReader;
        this.GenStyle = GenStyle;
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

    public void DeClass(List<PexString> TempStrings, ref StringBuilder PscCode)
    {
        if (Reader.Objects.Count > 0)
        {
            string ScriptName = TempStrings[Reader.Objects[0].NameIndex].Value;
            string ParentClass = TempStrings[Reader.Objects[0].ParentClassNameIndex].Value;

            if (this.GenStyle == CodeGenStyle.Papyrus)
            {
                PscCode.AppendLine(string.Format("ScriptName {0} Extends {1}", ScriptName, ParentClass));
            }
            else
            if (this.GenStyle == CodeGenStyle.CSharp)
            {
                PscCode.AppendLine("public class " + ScriptName.Trim() + " : " + ParentClass.Trim() + " \n{");
            }
        }
    }

    public void DeGlobalVariables(List<PexString> TempStrings, ref StringBuilder PscCode)
    {
        if (GenStyle == CodeGenStyle.Papyrus)
        {
            PscCode.AppendLine(";GlobalVariables");
        }
        else
        {
            PscCode.AppendLine(CodeSpace + "//GlobalVariables");
        }

        for (int i = 0; i < TempStrings.Count; i++)
        {
            var Item = TempStrings[i];
            ObjType CheckType = ObjType.Null;
            var TempValue = QueryAnyByID(Item.Index, ref CheckType);

            //if (Item.Value.Equals("KeysList"))
            //{ 

            //}

            if (CheckType == ObjType.Variables)
            {
                PexVariable Variable = TempValue as PexVariable;
                if (!Item.Value.StartsWith("::"))
                {
                    string GetVariableType = TempStrings[Variable.TypeNameIndex].Value;
                    string TryGetValue = ObjToStr(Variable.DataValue);
                    if (TryGetValue.Length == 0)
                    {
                        if (this.GenStyle == CodeGenStyle.Papyrus)
                        {
                            PscCode.AppendLine(string.Format(GetVariableType + " " + Item.Value));
                        }
                        else
                        if (this.GenStyle == CodeGenStyle.CSharp)
                        {
                            PscCode.AppendLine(string.Format(CodeSpace + GetVariableType + " " + Item.Value + ";"));
                        }
                    }
                    else
                    {
                        if (GetVariableType.ToLower().Equals("string"))
                        {
                            if (this.GenStyle == CodeGenStyle.Papyrus)
                            {
                                PscCode.AppendLine(string.Format(GetVariableType + " " + Item.Value
                                + " = " + "\"" + TryGetValue + "\""));
                            }
                            else
                            if (this.GenStyle == CodeGenStyle.CSharp)
                            {
                                PscCode.AppendLine(string.Format(CodeSpace + GetVariableType + " " + Item.Value
                                + " = " + "\"" + TryGetValue + "\";"));
                            }
                        }
                        else
                        {
                            if (this.GenStyle == CodeGenStyle.Papyrus)
                            {
                                PscCode.AppendLine(string.Format(GetVariableType + " " + Item.Value + " = " + TryGetValue));
                            }
                            else
                            if (this.GenStyle == CodeGenStyle.CSharp)
                            {
                                PscCode.AppendLine(string.Format(CodeSpace + GetVariableType + " " + Item.Value + " = " + TryGetValue + ";"));
                            }
                        }
                    }
                }
            }
        }
    }

    public void DeAutoGlobalVariables(List<PexString> TempStrings, ref StringBuilder PscCode)
    {
        if (GenStyle == CodeGenStyle.Papyrus)
        {
            PscCode.AppendLine(";Global Properties");
        }
        else
        {
            PscCode.AppendLine(CodeSpace + "//Global Properties");
        }

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
                    if (GetVariableType.Equals("globalvariable"))
                    {
                        GetVariableType = "GetVariableType";
                    }

                    string NodeStr = "";

                    if (this.GenStyle == CodeGenStyle.Papyrus)
                    {
                        NodeStr = " ;";
                    }
                    else
                    if (this.GenStyle == CodeGenStyle.CSharp)
                    {
                        NodeStr = " //";
                    }

                    var RealValue = QueryAnyByID(Property.AutoVarNameIndex, ref CheckType);
                    if (CheckType == ObjType.Variables)
                    {
                        string GetRealValue = ObjToStr((RealValue as PexVariable).DataValue);
                        if (GetRealValue.Length > 0)
                        {
                            NodeStr += "Value:" + GetRealValue;
                        }
                        else
                        {
                            NodeStr = string.Empty;
                        }
                    }
                    else
                    {
                        NodeStr = string.Empty;
                    }

                    if (this.GenStyle == CodeGenStyle.Papyrus)
                    {
                        PscCode.AppendLine(string.Format(GetVariableType + " Property " + Item.Value + " Auto" + NodeStr));
                    }
                    else
                    if (this.GenStyle == CodeGenStyle.CSharp)
                    {
                        PscCode.AppendLine(CodeSpace + "[Property(Auto = true)]");
                        PscCode.AppendLine(string.Format(CodeSpace + GetVariableType + " " + Item.Value + ";" + NodeStr));
                    }
                }

            }
        }
    }

    public string GenSpace(int Count)
    {
        string Space = "";
        for (int i = 0; i < Count; i++)
        {
            Space += CodeSpace;
        }
        return Space;
    }

    public void DeFunction(List<PexString> TempStrings, ref StringBuilder PscCode)
    {
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
                    var GetFunc1st = Function[0];

                    string GenParams = "";

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

                    for (int ir = 0; ir < GetFunc1st.NumParams; ir++)
                    {
                        if (GetFunc1st.Parameters.Count > ir)
                        {
                            var GetParam = GetFunc1st.Parameters[ir];
                            string GetType = TempStrings[GetFunc1st.Parameters[ir].TypeIndex].Value;
                            string GetParamName = TempStrings[GetParam.NameIndex].Value;

                            GenParams += string.Format("{0} {1},", GetType, GetParamName);
                        }
                    }

                    if (GenParams.EndsWith(","))
                    {
                        GenParams = GenParams.Substring(0, GenParams.Length - 1);
                    }

                    string GenLine = "";

                    if (GenStyle == CodeGenStyle.Papyrus)
                    {
                        GenLine = string.Format("{0} Function {1}({2})", ReturnType, Item.Value, GenParams);
                    }
                    else
                    if (GenStyle == CodeGenStyle.CSharp)
                    {
                        var TempType = ReturnType;
                        if (TempType.Length == 0)
                        {
                            TempType = "void";
                        }
                        GenLine = string.Format(CodeSpace + "public {0} {1}({2})\n", TempType, Item.Value, GenParams) + CodeSpace + "{";
                    }

                    PscCode.AppendLine(GenLine);

                    int LineIndex = 0;
                    string TempBlock = "";
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
                                    var NextGet = TempStrings[GetObj.Index];

                                    if (GetValue.Equals("NextIndex"))
                                    {

                                    }
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
                        TempBlock += GenSpace(2) + Tracker.CheckCode(LineIndex, GetOPName,CurrentLine) + "\n";
                        LineIndex++;
                    }

                    if (TempBlock.EndsWith("\n"))
                    {
                        TempBlock = TempBlock.Substring(0, TempBlock.Length - "\n".Length);
                    }

                    PscCode.AppendLine(TempBlock);

                    if (GenStyle == CodeGenStyle.Papyrus)
                    {
                        PscCode.AppendLine("EndFunction");
                    }
                    else
                    if (GenStyle == CodeGenStyle.CSharp)
                    {
                        PscCode.AppendLine(CodeSpace + "}\n");
                    }
                }
            }
        }
    }

    public List<PexString> CurrentStrings = new List<PexString>();
    public string Decompile()
    {
        StringBuilder PscCode = new StringBuilder();
        List<PexString> TempStrings = new List<PexString>();

        TempStrings.AddRange(Reader.StringTable);

        CurrentStrings = TempStrings;

        DeClass(TempStrings, ref PscCode);

        DeGlobalVariables(TempStrings, ref PscCode);
        PscCode.Append("\n");
        DeAutoGlobalVariables(TempStrings, ref PscCode);
        PscCode.Append("\n");
        //Function XXX() EndFunction
        DeFunction(TempStrings, ref PscCode);


        if (this.GenStyle == CodeGenStyle.CSharp)
        {
            PscCode.AppendLine("}");
        }

        var GetCode = PscCode.ToString();

        return GetCode;
    }

}

public class Function
{
    public int LineIndex = 0;

    public string ParentFunction = "";
    public string SelfVariable = "";
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

public class TProp
{
    public int LineIndex = 0;

    public List<string> Fronts = new List<string>();
    public string PropName = "";
    public string LinkVariable = "";

    public int IsGetOrSet = 0;
    public bool Self = false;
}

public class TFunction
{
    public int LineIndex = 0;

    public List<string> Fronts = new List<string>();
    public string FunctionName = "";
    public List<string> Params = new List<string>();

    public bool Self = false;

    public string LinkVariable = "";
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
    public string ValueA = "";
    public string ValueB = "";

    public string LinkVariable = "";
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

public class VariableSetter
{
    public int LineIndex = 0;

    public string Parent = "";
    public string Child = "";
}

public class AssemblyLine
{
    public string AssemblyNote = "";
    public object TrackRef;
    public AssemblyLine(string AssemblyNote, object Track)
    {
        this.AssemblyNote = AssemblyNote;
        this.TrackRef = Track;
    }
}

public class DecompileTracker
{
    public string FuncName = "";

    public Dictionary<int, AssemblyLine> Tracks = new Dictionary<int, AssemblyLine>();

    public List<TVariable> _PexVariables = new List<TVariable>();
    public List<TFunction> _PexFunctions = new List<TFunction>();
    public List<CastLink> _CastLinks = new List<CastLink>();
    public List<TProp> _Props = new List<TProp>();
    public List<TStrcat> _StrMerges = new List<TStrcat>();
    public List<TOperator> _Operators = new List<TOperator>();
    public List<TJump> _Jumps = new List<TJump>();
    public List<TReturn> _Returns = new List<TReturn>();
    public List<VariableSetter> _VariableSetters = new List<VariableSetter>();

    public string QueryVariables(string TempName)
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
    public List<string> CreatParams(string Line)
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
    public string CheckCode(int LineIndex, string OPCode, string Line)
    {
        if (Tracks.ContainsKey(LineIndex))
        {
            throw new Exception();
        }
        List<string> GetParams = CreatParams(Line);
        string Note = "//" + OPCode + " " + Line;

        if (OPCode == "return")
        {
            var SetReturn = new TReturn(LineIndex, GetParams);
            _Returns.Add(SetReturn);
            Tracks.Add(LineIndex, new AssemblyLine(Note, SetReturn));
            return Note;
        }
        else
        if (OPCode == "callmethod" || OPCode == "callparent" || OPCode == "callstatic")
        {
            TFunction NTFunction = new TFunction();
            NTFunction.LineIndex = LineIndex;

            bool NoParams = false;

            string FristValue = "";

            List<string> AParams = new List<string>();

            if (GetParams.Count >= 2)
            {
                GetParams[1] = GetParams[1].Trim();

                bool Nonevar = false;
                if (GetParams[1].Contains(" "))
                {
                    var GetStaticValue = GetParams[1].Split(' ');

                    for (int i = 0; i < GetStaticValue.Length; i++)
                    {
                        if (GetStaticValue[i] == "nonevar")
                        {
                            Nonevar = true;
                        }
                        else
                        if (GetStaticValue[i].Trim().Length > 0)
                        {
                            AParams.Add(GetStaticValue[i]);
                        }
                    }
                }
                if (Nonevar)
                {
                    GetParams[1] = "nonevar";
                }
                if (GetParams[1] == "nonevar")
                {
                    NoParams = true;
                }
            }

            if (GetParams.Count > 0)
            {
                FristValue = GetParams[0].Trim();
            }

            if (GetParams.Count > 2)
            {
                for (int i = 2; i < GetParams.Count; i++)
                {
                    GetParams[i] = GetParams[i].Trim();

                    if (GetParams[i].Contains(" "))
                    {
                        foreach (var Get in GetParams[i].Split(' '))
                        {
                            if (Get.Trim().Length > 0)
                            {
                                AParams.Add(Get);
                            }
                        }
                    }
                    else
                    {
                        AParams.Add(GetParams[i]);
                    }
                }
            }

            var NextParams = FristValue.Split(' ');
            List<string> Fronts = new List<string>();
            List<string> Params = new List<string>();

            if (AParams.Count > 0)
            {
                Params.AddRange(AParams);
            }

            if (FristValue.Contains(" "))
            {
                for (int i = 1; i < NextParams.Length; i++)
                {
                    if (NextParams[i] != "self")
                    {
                        Fronts.Add(NextParams[i]);
                    }
                }
            }

            if (NoParams)
            {
                if (NextParams.Length >= 2)
                {
                    if (NextParams[1].Equals("self"))
                    {
                        NTFunction.Self = true;
                        NTFunction.FunctionName = NextParams[0];

                        NTFunction.Fronts = Fronts;
                        NTFunction.Params = Params;

                        _PexFunctions.Add(NTFunction);
                        Tracks.Add(LineIndex,new AssemblyLine(Note,NTFunction));
                    }
                }
                else
                if (NextParams.Length > 0)
                {
                    NTFunction.FunctionName = NextParams[0];

                    NTFunction.Fronts = Fronts;
                    NTFunction.Params = Params;

                    _PexFunctions.Add(NTFunction);
                    Tracks.Add(LineIndex,new AssemblyLine(Note,NTFunction));
                }
            }
            else
            {
                if (NextParams.Length >= 2)
                {
                    if (NextParams[1].Equals("self"))
                    {
                        NTFunction.Self = true;
                        NTFunction.FunctionName = NextParams[0];

                        NTFunction.Fronts = Fronts;
                        NTFunction.Params = Params;

                        _PexFunctions.Add(NTFunction);
                        Tracks.Add(LineIndex,new AssemblyLine(Note,NTFunction));
                    }
                }
                else
                if (NextParams.Length > 0)
                {
                    NTFunction.FunctionName = NextParams[0];

                    NTFunction.Fronts = Fronts;
                    NTFunction.Params = Params;

                    _PexFunctions.Add(NTFunction);
                    Tracks.Add(LineIndex,new AssemblyLine(Note,NTFunction));
                }
            }

            return Note;
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
                Tracks.Add(LineIndex, new AssemblyLine(Note,NTVariable));

                return Note;
            }
            else
            {
                if (Line.Split(' ').Length == 2)
                {
                    VariableSetter NVariableSetter = new VariableSetter();
                    NVariableSetter.LineIndex = LineIndex;
                    NVariableSetter.Parent = Line.Split(' ')[0];
                    NVariableSetter.Child = Line.Split(' ')[1];
                    _VariableSetters.Add(NVariableSetter);

                    Tracks.Add(LineIndex, new AssemblyLine(Note, NVariableSetter));

                    return Note;
                }
                else
                {
                    TVariable NTVariable = new TVariable();
                    NTVariable.VariableName = Line;
                    NTVariable.LineIndex = LineIndex;

                    _PexVariables.Add(NTVariable);
                    Tracks.Add(LineIndex,new AssemblyLine(Note,NTVariable));
                    return Note;
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

            return Note;
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
                        else
                        {

                        }
                    }
                    else
                    {

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
            Tracks.Add(LineIndex, new AssemblyLine(Note, NTProp));
            return Note;
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
                Tracks.Add(LineIndex,new AssemblyLine(Note,NTStrcat));
            }

            return Note;
        }
        else
        if (OPCode == "cmp_eq" || OPCode == "cmp_lt" || OPCode == "cmp_le" || OPCode == "cmp_gt" || OPCode == "cmp_ge")
        {
            if (GetParams.Count > 0)
            {
                string Operator = "";
                switch (OPCode)
                {
                    case "cmp_eq":
                        {
                            Operator = "==";
                        }
                        break;
                    case "cmp_lt":
                        {
                            Operator = "<";
                        }
                        break;
                    case "cmp_le":
                        {
                            Operator = "<=";
                        }
                        break;
                    case "cmp_gt":
                        {
                            Operator = ">";
                        }
                        break;
                    case "cmp_ge":
                        {
                            Operator = ">=";
                        }
                        break;
                }

                TOperator NTOperator = new TOperator();
                NTOperator.LineIndex = LineIndex;
                NTOperator.Operator = Operator;
                NTOperator.LinkVariable = GetParams[0].Trim();

                _Operators.Add(NTOperator);
                Tracks.Add(LineIndex,new AssemblyLine(Note,NTOperator));

                return Note;
            }
            else
            {
                throw new Exception();
            }
        }
        else
        if (OPCode== "jmpt" || OPCode == "jmpf" || OPCode == "jmp")
        {
            var SetJump = new TJump();
            SetJump.LineIndex = LineIndex;
            SetJump.Jump = OPCode;

            if (GetParams.Count > 0)
            {
                SetJump.Variable = GetParams[0].Trim();
            }
            
            _Jumps.Add(SetJump);
            Tracks.Add(LineIndex,new AssemblyLine(Note,SetJump));

            return Note;
        }
        else
        {
            return OPCode + " " + Line;
        }
    }
}