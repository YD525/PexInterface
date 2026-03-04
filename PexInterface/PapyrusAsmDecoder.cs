using System.Collections.Generic;
using System;
using PexInterface;
using System.Linq;
using static PexInterface.PexReader;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Diagnostics.SymbolStore;

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
                    DecompileTracker Tracker = new DecompileTracker();

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
                        Tracker.CheckCode(GetOPName,FunctionCode);
                        LineIndex++;
                    }

                    NFunctionBlock.FunctionCode = FunctionCode;
                    NFunctionBlock.TracksRef = Tracker;

                    AsmExtend.DeFunctionCode(CodeGenStyle.CSharp,ParentCls, NFunctionBlock, Tracker, CanSkipPscDeCode);

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

    public DecompileTracker TracksRef = null;
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


public enum TokenSeparator
{
    Null = 0,
    Space = 1,
    DoubleColon = 2
}

public enum AsmValueType
{
    Null = 0,
    Original = 1
}

public class AsmLink
{
    private TokenSeparator Separator = TokenSeparator.Null;
    private string Value = null;
    public string UPDateValue = null;
    public AsmLink Next = null;
    public AsmLink Prev = null;   
    public AsmLink Tail = null;

    public string GetValue(AsmValueType Type = AsmValueType.Null)
    {
        string SetValue = Value.Trim();

        if (Type == AsmValueType.Null)
        {
            if (SetValue.EndsWith("_var"))
            {
                SetValue = SetValue.Substring(0, SetValue.Length - "_var".Length);
            }
            return SetValue;
        }
        else
        {
            if (this.Separator == TokenSeparator.DoubleColon)
            {
                return "::" + SetValue;
            }
            else
            if (this.Separator == TokenSeparator.Space)
            {
                return " " + SetValue;
            }
            else
            { 
               return SetValue;
            }
        }
    }
    public AsmLink GetHead()
    {
        var Node = this;
        while (Node.Prev != null)
            Node = Node.Prev;
        return Node;
    }

    public void Remove()
    {
        var Head = GetHead();

        if (Prev != null)
            Prev.Next = Next;

        if (Next != null)
            Next.Prev = Prev;

        if (this == Head.Tail)
            Head.Tail = Prev;

        Next = null;
        Prev = null;
    }
    public bool IsTemp()
    {
        if (IsNull())
        {
            return false;
        }
        else
        if (this.Value.Trim().StartsWith("::temp"))
        {
            return true;
        }
        return false;
    }
    public bool IsVar()
    {
        if (IsNull())
        {
            return false;
        }
        else
        if (this.Value.Trim().EndsWith("_var"))
        {
            return true;
        }

        return false;
    }
    public bool IsSelf()
    {
        if (IsNull())
        {
            return false;
        }
        else
        if (this.Value.Trim().ToLower().Equals("self"))
        {
            return true;
        }

        return false;
    }
    public bool IsNull()
    {
        if (this.Value == null)
        {
            return true;
        }
        else
        if (this.Value == string.Empty)
        {
            return true;
        }
        else
        if (this.Value == "::nonevar" || this.Value == "nonevar")
        {
            return true;
        }
        return false;
    }
    public bool HaveValue()
    {
        if (Tail != null)
        {
            return true;
        }
        return false;
    }
    public void SetValue(string Value, TokenSeparator Separator)
    {
        if (this.Value == null)
        {
            this.Value = Value;
            this.Separator = Separator;
            Tail = this;
        }
        else
        {
            var NewNode = new AsmLink
            {
                Value = Value,
                Separator = Separator,
                Prev = Tail
            };

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
        var Node = GetHead();
        while (Node != null)
        {
            if (Node != null)
            Action(Node);

            Node = Node.Next;
        }
    }
    public void ForEachBackward(Action<AsmLink> Action)
    {
        var Head = GetHead();
        var Node = Head.Tail;

        while (Node != null)
        {
            Action(Node);
            Node = Node.Prev;
        }
    }
    public List<AsmLink> GetNodesBefore()
    {
        var Result = new List<AsmLink>();
        var Node = Prev;
        while (Node != null)
        {
            Result.Insert(0,Node);
            Node = Node.Prev;
        }
        return Result;
    }
    public List<AsmLink> GetNodesAfter()
    {
        var Result = new List<AsmLink>();
        var Node = Next; 
        while (Node != null)
        {
            Result.Add(Node); 
            Node = Node.Next;
        }
        return Result;
    }

    public string GetAsmCode()
    {
        string Line = "";

        this.ForEachForward(new Action<AsmLink>((Word) => 
        {
            Line += Word.GetValue(AsmValueType.Original);
        }));

        return Line;
    }

    public void UPDate(string Value)
    { 
       this.UPDateValue = Value;
    }

    public string GetCode()
    {
        string Line = "";

        this.ForEachForward(new Action<AsmLink>((Word) =>
        {
            if (Word.UPDateValue != null)
            {
                Line += Word.UPDateValue;
            }
            else
            {
                Line += Word.GetValue();
            } 
        }));

        return Line;
    }

    public static void ParseLink(ref AsmLink Links, string Str)
    {
        var Matches = Regex.Matches(Str, @"(::)|(""[^""]*"")|(\S+)|(\s+)");

        TokenSeparator PendingSeparator = TokenSeparator.Null;

        foreach (Match M in Matches)
        {
            var Token = M.Value;

            if (Token == "::")
            {
                PendingSeparator = TokenSeparator.DoubleColon;
                continue;
            }

            if (string.IsNullOrWhiteSpace(Token))
            {
                PendingSeparator = TokenSeparator.Space;
                continue;
            }

            Links.SetValue(Token, PendingSeparator);
            PendingSeparator = TokenSeparator.Null;
        }
    }
}
public class AsmBase
{
    public string OPCode = "";
    public string PSCCode = "";
    public int LineIndex = 0;
    public int SpaceCount = 0;
    public AsmLink Links = new AsmLink();

    protected void ParseLink(string Str)
    {
        AsmLink.ParseLink(ref Links,Str);
    }
}

public class VariableTracker
{
   
}

public class AsmCode:AsmBase
{
    public string Param = "";
    public string VariableLink = "";

    public void Parse(string OPCode, string Line)
    {
        this.OPCode = OPCode.Trim().ToLower();
        Line = Line.Trim();
        if (Line.Contains(" "))
        {
            this.Param = Line.Split(' ')[0];
            string GetLinkStr = Line.Split(' ')[1];

            if (GetLinkStr.Length > 0)
            {
                this.ParseLink(GetLinkStr);
            }
        }
        else
        {
            if (Line.Length > 0)
            {
                this.ParseLink(Line);
            }
        }
    }

    public string GetAsmCode()
    {
        string Code = this.OPCode + " " + this.Param + " " + this.Links.GetAsmCode();

        return Code;
    }

    public string GetCode()
    {
        return this.PSCCode;
    }

    public string GetNote()
    {
        return "//" + GetAsmCode();
    }

    ///// <summary>
    ///// XXX = Func() I need to know if Temp has been created.If it weren't for this, it should be equal to.
    ///// </summary>
    //public void FindVariableLink()
    //{
    //    if (Call.Length > 0)
    //    {
    //        if (this.Links.Tail.IsTemp())
    //        {
    //            this.VariableLink = this.Links.Tail.GetValue();
    //            this.Links.Tail.Remove();
    //        }
    //    }
    //}
  
}



public class DecompileTracker
{
    public List<AsmCode> Lines = new List<AsmCode>();
    public void CheckCode(string OPCode,string Line)
    {
        AsmCode NewLine = new AsmCode();
        NewLine.Parse(OPCode,Line);
        Lines.Add(NewLine);

        //if (OPCode == "return")
        //{
           
        //}
        //else
        //if (OPCode == "callmethod" || OPCode == "callparent" || OPCode == "callstatic")
        //{
           
        //}
        //else
        //if (OPCode == "assign")
        //{
           
        //}
        //else
        //if (OPCode == "cast")
        //{
           
        //}
        //else
        //if (OPCode == "propget" || OPCode == "propset")
        //{
           
        //}
        //else
        //if (OPCode == "strcat")
        //{
           
        //}
        //else
        //if (OPCode == "cmp_eq" || OPCode == "cmp_lt" || OPCode == "cmp_le" || OPCode == "cmp_gt" || OPCode == "cmp_ge" || OPCode == "not" || OPCode == "isub")
        //{
           
        //}
        //else
        //if (OPCode == "jmpt" || OPCode == "jmpf" || OPCode == "jmp")
        //{
           
        //}
        //else
        //if (OPCode == "iadd")
        //{
            
        //}
        //else
        //if (OPCode == "array_create" || OPCode == "array_setelement" || OPCode == "array_getelement" || OPCode == "array_length")
        //{
          
        //}
    }
}