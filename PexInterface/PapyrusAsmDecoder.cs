using System.Collections.Generic;
using System;
using PexInterface;
using System.Linq;
using static PexInterface.PexReader;
using System.Text.RegularExpressions;
using static PapyrusAsmDecoder;
using System.Diagnostics;
using System.Text;

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

    public class AsmInFo
    {
        public PexVariable Variable = null;
        public PexFunction Function = null;
        public PexProperty Property = null;

    }
    public class AsmOrder
    {
        public string Value = "";
        public AsmInFo InFo = null;

        public AsmOrder(string Value)
        {
          this.Value = Value.Trim();
        }
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
                        AsmOPCode OPCode = new AsmOPCode();
                        OPCode.Value = GetInstruction.GetOpcodeName();
                        OPCode.Arguments = GetInstruction.Arguments;

                        if (OPCode.Value.StartsWith("jmp"))
                        { 
                        
                        }

                        List<AsmOrder> Orders = new List<AsmOrder>();
                        PexFunction PexFunc = null;

                        foreach (var GetArg in GetInstruction.Arguments)
                        {
                            AsmOrder Order = null;
                            var GetIndex = ObjToInt(GetArg.Value);
                            if (GetIndex > 0)
                            {
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

                                    Order = new AsmOrder(GetObj.Value.Trim());

                                    ObjType VariableType = ObjType.Null;
                                    var GetVariable = QueryAnyByID(GetObj.Index, ref VariableType);
                                    if (GetVariable != null)
                                    {
                                        if (VariableType == ObjType.Variables)
                                        {
                                            PexVariable Variable = GetVariable as PexVariable;
                                            Order.InFo = new AsmInFo();
                                            Order.InFo.Variable = Variable;
                                        }
                                       
                                        if (VariableType == ObjType.Functions)
                                        {
                                            PexFunction Func = GetVariable as PexFunction;
                                            Order.InFo = new AsmInFo();
                                            Order.InFo.Function = Func;
                                        }
                                        
                                        if (VariableType == ObjType.Properties)
                                        {
                                            PexProperty Property = GetVariable as PexProperty;
                                            Order.InFo = new AsmInFo();
                                            Order.InFo.Property = Property;
                                        }
                                    }
                                }

                                if (Order != null)
                                {
                                    if (GetArg.Type == 2)
                                    {
                                        Order.Value = "\"" + Order.Value + "\"";
                                    }

                                    Orders.Add(Order);
                                }
                            }
                        }
                        Tracker.CheckCode(LineIndex,OPCode, Orders);
                        LineIndex++;
                    }

                    NFunctionBlock.FunctionCode = FunctionCode;
                    NFunctionBlock.TracksRef = Tracker;

                    AsmExtend.DeFunctionCode(CodeGenStyle.CSharp,TempStrings,ParentCls, NFunctionBlock, Tracker, CanSkipPscDeCode);

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
    public AsmInFo InFo = null;
    public string UPDateValue = null;
    private AsmLink Head = null;
    public AsmLink Next = null;
    public AsmLink Prev = null;   
    private AsmLink Tail = null;

    public string GetValue(AsmValueType Type = AsmValueType.Null)
    {
        if (Value == null) return string.Empty;

        string SetValue = this.Value.Trim();

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
    public AsmLink GetTail()
    {
        return Tail ?? this;
    }
    public AsmLink GetHead()
    {
        return Head ?? this;
    }
    public void Remove()
    {
        var Head = GetHead();

        if (Prev != null)
            Prev.Next = Next;

        if (Next != null)
            Next.Prev = Prev;

        if (this == Head)
        {
            if (Next != null)
            {
                Next.Tail = this.Tail;
                Next.Head = null;        

                var Node = Next.Next; 
                while (Node != null)
                {
                    Node.Head = Next;
                    Node = Node.Next;
                }
            }
        }
        else
        if (this == Head.Tail)
        {
            Head.Tail = Prev;
        }

        Head = null;
        Tail = null;
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
    public void SetValue(string Value, AsmInFo InFo, TokenSeparator Separator)
    {
        if (this.Value == null)
        {
            this.Value = Value;
            this.InFo = InFo;
            this.Separator = Separator;
            Tail = this;
            Head = null;
        }
        else
        {
            var Head = GetHead();

            var NewNode = new AsmLink
            {
                Value = Value,
                InFo =  InFo,
                Separator = Separator,
                Prev = Head.Tail,
                Head = Head
            };

            Head.Tail.Next = NewNode;
            Head.Tail = NewNode;
        }
    }
    public string GetValueByIndex(int Index)
    {
        var Node = GetHead();
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
        var Node = GetHead().Tail;
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
        var Node = GetHead();
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

    public static void ParseLink(ref AsmLink Links, List<AsmOrder> Orders)
    {
        TokenSeparator PendingSeparator = TokenSeparator.Null;

        foreach (var Order in Orders)
        {
            if (Order.Value.StartsWith("::"))
            {
                PendingSeparator = TokenSeparator.DoubleColon;
                Links.SetValue(Order.Value.Substring("::".Length),Order.InFo,PendingSeparator);
            }
            else
            {
                PendingSeparator = TokenSeparator.Space;
                Links.SetValue(Order.Value,Order.InFo,PendingSeparator);
            }
        }
    }
}


[Flags]
public enum VariableAccess
{
    None = 0,        
    Created = 1 << 0,   
    Get = 1 << 1,
    Set = 1 << 2
}

public class AsmOffset
{
    public int LineIndex = 0;
    public int Offset = 0;

    public AsmOffset(int LineIndex, int Offset)
    {
        this.LineIndex = LineIndex;
        this.Offset = Offset;
    }
}

public class AsmAction
{
    public VariableAccess Access;
    public int State = 0;
    public List<AsmLink> Links;
    public AsmAction(VariableAccess Access,int State,List<AsmLink> Links)
    {
        this.Access = Access;
        this.State = State;
        this.Links = Links;
    }
}

public enum AsmVariableType
{
    Null = 0,  
    Int,         
    Float,       
    Double,     
    String,       
    Bool,       
    Char,         
    Object,      
    Array,
    Function,
    Pointer
}
public class AsmVariable
{
    public string Name = null;
    public AsmVariableType Type = AsmVariableType.Null;
    public bool IsTemp = false;
    public Dictionary<AsmOffset, AsmAction> Actions = null;
    public static bool CheckTemp(string Variable)
    {
        Variable = Variable.Trim();

        if (Regex.IsMatch(Variable, @"^temp\d+$"))
        {
           return true;
        }

        return false;
    }
    public void SetType(AsmVariableType Type)
    {
        this.Type = Type;
    }
    public bool IsFunction()
    {
        if (this.Type == AsmVariableType.Function)
        {
            return true;
        }
        return false;
    }
}
public class VariableTracker
{
    private Dictionary<string,AsmVariable> _Variables = new Dictionary<string,AsmVariable>();
    public bool IsCreated(string Variable)
    {
        Variable = Variable.Trim();
        return _Variables.ContainsKey(Variable);
    }
    public void Add(AsmOffset Offset,string Variable,int State,List<AsmLink> Links,VariableAccess Access)
    {
        if (IsCreated(Variable))
        {
            if (_Variables[Variable].Actions.ContainsKey(Offset))
            {
                _Variables[Variable].Actions[Offset] = new AsmAction(Access,State,Links);
            }
            else
            {
                _Variables[Variable].Actions.Add(Offset, new AsmAction(Access,State,Links));
            }
        }
        else
        {
            AsmVariable Var = new AsmVariable();
            Var.Actions = new Dictionary<AsmOffset, AsmAction>();
            Var.Name = Variable;

            if (AsmVariable.CheckTemp(Variable))
            {
                Var.IsTemp = true;
            }

            Var.Actions.Add(Offset,new AsmAction(VariableAccess.Created|Access,State,Links));

            this._Variables.Add(Variable, Var);
        }
    }
    public void SetType(DecompileTracker Tracker, string Variable, AsmVariableType Type)
    {
        if (_Variables.ContainsKey(Variable))
        {
            _Variables[Variable].Type = Type;
            int ID = GetStartByVarCreated(Variable, 0);
            if (ID >= 0)
            {
                Tracker.Lines[ID].PSCCode = Type.ToString() + " " + Tracker.Lines[ID].PSCCode;
            }
        }
    }

    public int GetStartByVarCreated(string Variable, int Offset)
    {
        if (!_Variables.TryGetValue(Variable, out var VarObj))
            return -1;

        foreach (var Kvp in VarObj.Actions)
        {
            if (Kvp.Key.Offset == Offset)
            {
                var Action = Kvp.Value;
                if ((Action.Access & VariableAccess.Created) == VariableAccess.Created)
                    return Kvp.Key.LineIndex;
            }
        }

        return -1;
    }
}

#region ControlFlow

public enum ControlBlockType
{
    If,
    Else,
    While
}

public class ControlBlock
{
    public ControlBlockType Type;
    public int StartLine;
    public int? EndLine;
    public AsmLink Links;
    public ControlBlock Parent;
    public List<ControlBlock> Children = new List<ControlBlock>();

    public ControlBlock(ControlBlockType type, int startLine, AsmLink links, ControlBlock parent = null)
    {
        Type = type;
        StartLine = startLine;
        Links = links;
        Parent = parent;
        EndLine = null;
    }

    public int Depth => Parent != null ? Parent.Depth + 1 : 0;
}

public class ElseRange
{
    public int StartLine;
    public int EndLine;
    public int JmpLine;

    public ElseRange(int start, int end, int jmp)
    {
        StartLine = start;
        EndLine = end;
        JmpLine = jmp;
    }
}

public class ControlFlowTracker
{
    private Stack<ControlBlock> _BlockStack = new Stack<ControlBlock>();
    private List<ControlBlock> _AllBlocks = new List<ControlBlock>();
    public List<ElseRange> ElseRanges = new List<ElseRange>();

    public void BeginBlock(ControlBlockType type, int startLine, AsmLink link)
    {
        var parent = _BlockStack.Count > 0 ? _BlockStack.Peek() : null;
        var block = new ControlBlock(type, startLine, link, parent);

        parent?.Children.Add(block);
        _BlockStack.Push(block);
        _AllBlocks.Add(block);
    }

    public void EndBlock(int endLine)
    {
        if (_BlockStack.Count == 0) throw new InvalidOperationException("No block to end");
        var block = _BlockStack.Pop();
        block.EndLine = endLine;
    }

    public ControlBlock CurrentBlock => _BlockStack.Count > 0 ? _BlockStack.Peek() : null;

    public void RegisterElse(int start, int end, int jmp)
    {
        ElseRanges.Add(new ElseRange(start, end, jmp));
    }

    public static void IdentifyControlFlow(DecompileTracker tracker)
    {
        for (int i = 0; i < tracker.Lines.Count; i++)
        {
            var line = tracker.Lines[i];
            string op = line.OPCode?.Value ?? "";


            if (IsCompare(op) && i + 1 < tracker.Lines.Count)
            {
                var next = tracker.Lines[i + 1];
                if (next.OPCode.Value != "jmpf") continue;

                int falseTarget = (next.LineIndex + next.GetJumpTarget())-1;

                bool isWhile = false;
                for (int j = i + 2; j < falseTarget && j < tracker.Lines.Count; j++)
                {
                    var check = tracker.Lines[j];
                    if (check.OPCode.Value == "jmp" && (check.LineIndex + check.GetJumpTarget())-1 <= i)
                    {
                        isWhile = true;
                        break;
                    }
                }

                if (isWhile)
                {
                    tracker.ControlFlows.BeginBlock(ControlBlockType.While, i, line.Links);
                }
                else
                {
                    tracker.ControlFlows.BeginBlock(ControlBlockType.If, i, line.Links);

                    int prevTarget = falseTarget;
                    if (prevTarget >= 0 && prevTarget < tracker.Lines.Count &&
                        tracker.Lines[prevTarget].OPCode.Value == "jmp")
                    {
                        int elseEndTarget = (tracker.Lines[prevTarget].LineIndex + tracker.Lines[prevTarget].GetJumpTarget())-1;
                        int actualElseEnd = Math.Min(elseEndTarget, tracker.Lines.Count - 1);

                        tracker.ControlFlows.EndBlock(prevTarget);
                        tracker.ControlFlows.RegisterElse(falseTarget, actualElseEnd, prevTarget);
                        tracker.ControlFlows.BeginBlock(ControlBlockType.Else, falseTarget, null);
                    }
                    else
                    {
                        tracker.ControlFlows.EndBlock(falseTarget);
                    }
                }
            }

            if (op == "jmp")
            {
                int target = (line.LineIndex + line.GetJumpTarget())-1;
                var current = tracker.ControlFlows.CurrentBlock;
                if (current != null && current.Type == ControlBlockType.While && target <= current.StartLine)
                {
                    tracker.ControlFlows.EndBlock(i);
                }
            }
        }
    }

    public static void MarkControlFlowOutput(DecompileTracker tracker)
    {
        foreach (var e in tracker.ControlFlows.ElseRanges)
        {
            if (e.StartLine >= 0 && e.StartLine < tracker.Lines.Count)
            {
                var line = tracker.Lines[e.StartLine];
                if (line.ControlFlow.Length > 0)
                {
                    line.ControlFlow += "\n" + "Else";
                }
                else
                {
                    line.ControlFlow = "Else";
                }
            }
        }

        foreach (var block in tracker.ControlFlows.GetAllBlocks())
        {
            if (!block.EndLine.HasValue) continue;
            int endIdx = block.EndLine.Value;
            if (endIdx < 0 || endIdx >= tracker.Lines.Count) continue;

            string endCode = block.Type == ControlBlockType.While ? "EndWhile" : "EndIf";
            var line = tracker.Lines[endIdx];
            if (line.ControlFlow.Length > 0)
            {
                line.ControlFlow += "\n" + endCode;
            }
            else
            {
                line.ControlFlow = endCode;
            }
           
        }
    }

    private static bool IsCompare(string op)
    {
        return op == "cmp_lt" || op == "cmp_le" ||
               op == "cmp_gt" || op == "cmp_ge" || op == "cmp_eq";
    }

    public List<ControlBlock> GetAllBlocks() => _AllBlocks;
}

#endregion

public class AsmOPCode
{
    public string Value = "";
    public List<PexInstructionArgument> Arguments = new List<PexInstructionArgument>();
}

public class AsmBase
{
    public AsmOPCode OPCode = null;
    public string PSCCode = "";
    public string ControlFlow = "";
    public int LineIndex = 0;
    public int SpaceCount = 0;
    public AsmLink Links = new AsmLink();

    protected void ParseLink(List<AsmOrder> Orders)
    {
        AsmLink.ParseLink(ref Links, Orders);
    }
}

public class AsmCode:AsmBase
{
    public int GetJumpTarget()
    {
        if (OPCode == null || OPCode.Arguments == null) return -1;

        string Op = OPCode.Value;

        if (Op == "jmp")
            return ReadJumpArgument(0);   
        else if (Op == "jmpt" || Op == "jmpf")
            return ReadJumpArgument(1);   

        return -1;
    }
    private int ReadJumpArgument(int Index)
    {
        int JumpID = 0;
        if (OPCode.Arguments.Count <= Index)
            JumpID = -1;

        var Arg = OPCode.Arguments[Index];

        if (Arg.Value == null)
            JumpID = -1;

        if (Arg.Value is int IntValue)
            JumpID = IntValue;

        int Parsed;
        if (int.TryParse(Arg.Value.ToString(), out Parsed))
            JumpID = Parsed;

        Debug.WriteLine(JumpID);
        return JumpID;
    }
    public AsmCode(int LineIndex)
    { 
       this.LineIndex = LineIndex;
    }
    public void Parse(AsmOPCode OPCode, List<AsmOrder> Orders)
    {
        this.OPCode = OPCode;
        this.ParseLink(Orders);
    }

    public string GetAsmCode()
    {
        var SetStr = string.Join(" ",
            new[]
            {
                this.OPCode.Value,
                this.Links?.GetAsmCode().Trim()
            }
            .Where(Str => !string.IsNullOrWhiteSpace(Str))
        );
       
        return SetStr;
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
    public VariableTracker Variables = new VariableTracker();
    public ControlFlowTracker ControlFlows = new ControlFlowTracker();

    public List<AsmCode> Lines = new List<AsmCode>();
    public void CheckCode(int LineIndex,AsmOPCode OPCode,List<AsmOrder> Orders)
    {
        AsmCode NewLine = new AsmCode(LineIndex);
        NewLine.Parse(OPCode,Orders);
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