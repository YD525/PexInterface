using System.Collections.Generic;
using System;
using PexInterface;
using System.Linq;
using static PexInterface.PexReader;
using System.Text.RegularExpressions;
using static PapyrusAsmDecoder;
using static PexInterface.PexHeuristicAnalysis;

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
    public static string CapitalizeFirst(string s)
=> (s == null || s.Length == 0) ? s : char.ToUpper(s[0]) + s.Substring(1);
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

    public static string Version = "1.0.3 Beta";

    public PexReader Reader = null;

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
        public ushort Index = 0;
        public bool HasIndex = false;
        public string Value = "";
        public string DefValue = null;
        public AsmInFo InFo = null;

        public AsmOrder(ushort Index,string Value)
        {
            this.HasIndex = true;
            this.Index = Index;
            this.Value = Value.Trim();
        }
        public AsmOrder(string Value)
        {
            this.Value = Value.Trim();
        }
    }
    public List<FunctionBlock> DeFunction(
    PscCls parentCls,
    List<PexString> tempStrings,
    bool canSkipPscDeCode)
    {
        var functionBlocks = new List<FunctionBlock>();

        // Determine which state is the "Auto State" so we can tag it later.
        // The auto-state name is stored in the Object header.
        string autoStateName = "";
        if (Reader.Objects.Count > 0)
            autoStateName = tempStrings[Reader.Objects[0].AutoStateNameIndex].Value;

        foreach (var obj in Reader.Objects)
        {
            foreach (var state in obj.States)
            {
                // State name: "" is the empty/global state (functions not inside any State block)
                string stateName = tempStrings[state.NameIndex].Value;

                foreach (var pexFunc in state.Functions)
                {
                    var block = BuildFunctionBlock(
                        pexFunc, stateName, tempStrings, parentCls, canSkipPscDeCode,out DecompileTracker Tracker);

                    block.TrackerRef = Tracker;
                    functionBlocks.Add(block);
                }
            }
        }

        return functionBlocks;
    }

    // ── Helper: build one FunctionBlock from a PexFunction ───────────────────────

    private FunctionBlock BuildFunctionBlock(
        PexFunction pexFunc,
        string stateName,
        List<PexString> tempStrings,
        PscCls parentCls,
        bool canSkipPscDeCode,
        out DecompileTracker Tracker)
    {
        var block = new FunctionBlock();

        // Function identity
        block.FunctionName = tempStrings[pexFunc.FunctionNameIndex].Value;
        block.StateName = stateName;

        // Flags  (bit 0 = Global, bit 1 = Native)
        block.IsGlobal = (pexFunc.Flags & 0x01) != 0;
        block.IsNative = (pexFunc.Flags & 0x02) != 0;

        // Return type  ("None" → empty string in PSC)
        string retType = tempStrings[pexFunc.ReturnTypeIndex].Value;
        block.ReturnType = retType == "None" ? "" : retType.Trim();

        // Parameters
        var paramList = new List<LocalVariable>();
        for (int i = 0; i < pexFunc.NumParams && i < pexFunc.Parameters.Count; i++)
        {
            var p = pexFunc.Parameters[i];
            paramList.Add(new LocalVariable
            {
                Name = tempStrings[p.NameIndex].Value,
                Type = tempStrings[p.TypeIndex].Value
            });
        }
        block.Params = paramList;

        // Build instruction tracker
        Tracker = new DecompileTracker();
        int lineIdx = 0;

        foreach (var instr in pexFunc.Instructions)
        {
            var opCode = new AsmOPCode
            {
                Value = instr.GetOpcodeName(),
                Arguments = instr.Arguments
            };

            var orders = BuildOrders(out List<ushort> StrPos, block,instr, tempStrings);
            Tracker.CheckCode(block, StrPos, lineIdx++, opCode, orders);
            StrPos.Clear();
        }

        block.FunctionCode = "";

        // Decompile body
        AsmExtend.DeFunctionCode(
            CodeGenStyle.CSharp, tempStrings, parentCls,
            block, Tracker, canSkipPscDeCode);

        return block;
    }

    // ── Helper: convert one instruction's arguments to AsmOrder list ─────────────

    private List<AsmOrder> BuildOrders(
        out List<ushort> StrPos,
        FunctionBlock BlockRef,
        PexInstruction Instruct,
        List<PexString> tempStrings)
    {
        var Orders = new List<AsmOrder>();
        StrPos = new List<ushort>();

        foreach (var Arg in Instruct.Arguments)
        {
            switch (Arg.Type)
            {
                case 0: // null / None
                    Orders.Add(new AsmOrder("None"));
                    continue;

                case 3: // integer literal
                    Orders.Add(new AsmOrder(Arg.Value?.ToString() ?? "0"));
                    continue;

                case 4: // float literal
                    Orders.Add(new AsmOrder(Arg.Value?.ToString() ?? "0.0"));
                    continue;

                case 5: // bool literal
                    Orders.Add(new AsmOrder(
                        Arg.Value is bool b && b ? "True" : "False"));
                    continue;
            }

            // Types 1 (identifier) and 2 (string) — index into string table
            int Index = ObjToInt(Arg.Value);
            if (Index < 0) continue;

            var StrEntry = tempStrings[Index];
            //if (StrEntry.Index != Index)
            //{
            //    GC.Collect();
            //}

            var Order = new AsmOrder(StrEntry.Index,StrEntry.Value.Trim());

            // Attach metadata (variable / function / property info)
            ObjType infoType = ObjType.Null;
            var infoObj = QueryAnyByID(StrEntry.Index, ref infoType);
            if (infoObj != null)
            {
                Order.InFo = new AsmInFo();
                if (infoType == ObjType.Variables)
                    Order.InFo.Variable = infoObj as PexVariable;
                else if (infoType == ObjType.Functions)
                    Order.InFo.Function = (infoObj as List<PexFunction>)?[0];
                else if (infoType == ObjType.Properties)
                    Order.InFo.Property = infoObj as PexProperty;
            }

            
            // String literals get quotes
            if (Arg.Type == 2)
            {
                StrPos.Add(StrEntry.Index);
                Order.DefValue = Order.Value;
                Order.Value = "\"" + Order.Value + "\"";
            }   

            Orders.Add(Order);
        }

        return Orders;
    }
    public PscCls Decompile(bool CanSkipPscDeCode = false)
    {
        List<PexString> TempStrings = new List<PexString>();

        TempStrings.AddRange(Reader.StringTable);
    
        var GenPsc = DeClass(TempStrings);

        GenPsc.GlobalVariables = DeGlobalVariables(TempStrings);

        GenPsc.AutoGlobalVariables = DeAutoGlobalVariables(TempStrings);

        GenPsc.Functions = DeFunction(GenPsc,TempStrings, CanSkipPscDeCode);

        return GenPsc;
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
    public DecompileTracker TrackerRef = new DecompileTracker();
    public string FunctionName = "";
    public List<LocalVariable> Params = new List<LocalVariable>();
    public string ReturnType = "";
  
    public string FunctionCode = "";
    public string StateName = "";

    public int StartIndex = 0;

    public bool IsGlobal = false;
    public bool IsNative = false;

    public List<PexStringExtend> Strings = new List<PexStringExtend>();
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

    public bool IsNull()
    {
        if (this.Value == null)
            return true;
        if (this.Value == string.Empty)
            return true;
        // Case-insensitive match for all nonevar variants
        string lower = this.Value.Trim().ToLower();
        if (lower == "nonevar" || lower == "::nonevar" || lower == "none")
            return true;
        return false;
    }
    public string GetValue(AsmValueType Type = AsmValueType.Null)
    {
        if (Value == null) return string.Empty;

        string SetValue = this.Value.Trim();

        if (Type == AsmValueType.Null)
        {
            SetValue = SetValue.ToLower();

            if (SetValue.EndsWith("_var"))
            {
                SetValue = SetValue.Substring(0, SetValue.Length - "_var".Length);
            }
            if (SetValue.Equals("none"))
            {
                SetValue = "null";
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
    public bool HaveValue()
    {
        if (Tail != null)
        {
            return true;
        }
        return false;
    }
    public AsmLink SetValue(string Value, AsmInFo InFo, TokenSeparator Separator)
    {
        if (this.Value == null)
        {
            this.Value = Value;
            this.InFo = InFo;
            this.Separator = Separator;
            Tail = this;
            Head = null;

            return this;   
        }
        else
        {
            var Head = GetHead();

            var NewNode = new AsmLink
            {
                Value = Value,
                InFo = InFo,
                Separator = Separator,
                Prev = Head.Tail,
                Head = Head
            };

            Head.Tail.Next = NewNode;
            Head.Tail = NewNode;

            return NewNode;   
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

    public static void ParseLink(FunctionBlock BlockRef, List<ushort> StrPos,ref AsmLink Links, List<AsmOrder> Orders)
    {
        TokenSeparator PendingSeparator = TokenSeparator.Null;

        foreach (var Order in Orders)
        {
            if (Order.Value.StartsWith("::"))
            {
                PendingSeparator = TokenSeparator.DoubleColon;
                var CurrentLink = Links.SetValue(Order.Value.Substring("::".Length),Order.InFo,PendingSeparator);

                if(Order.HasIndex)
                if (StrPos.Contains(Order.Index))
                {
                    PexString NPexString = new PexString();
                    NPexString.Value = Order.DefValue;
                    NPexString.Index = Order.Index;
                    BlockRef.Strings.Add(new PexStringExtend(NPexString,CurrentLink));
                    StrPos.Remove(Order.Index);
                }
            }
            else
            {
                PendingSeparator = TokenSeparator.Space;
                var CurrentLink = Links.SetValue(Order.Value,Order.InFo,PendingSeparator);

                if (Order.HasIndex)
                if (StrPos.Contains(Order.Index))
                {
                    PexString NPexString = new PexString();
                    NPexString.Value = Order.DefValue;
                    NPexString.Index = Order.Index;
                    BlockRef.Strings.Add(new PexStringExtend(NPexString, CurrentLink));
                    StrPos.Remove(Order.Index);
                }
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

    protected void ParseLink(FunctionBlock BlockRef, List<ushort> StrPos, List<AsmOrder> Orders)
    {
        AsmLink.ParseLink(BlockRef,StrPos,ref Links, Orders);
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

        return JumpID;
    }
    public AsmCode(int LineIndex)
    { 
       this.LineIndex = LineIndex;
    }
    public void Parse(FunctionBlock BlockRef, List<ushort> StrPos,AsmOPCode OPCode, List<AsmOrder> Orders)
    {
        this.OPCode = OPCode;
        this.ParseLink(BlockRef,StrPos,Orders);
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
    public string GetNote()
    {
        return "//" + GetAsmCode();
    }


  
}
public class DecompileTracker
{
    public VariableTracker Variables = new VariableTracker();
    public List<AsmCode> Lines = new List<AsmCode>();
    public void CheckCode(FunctionBlock BlockRef, List<ushort> StrPos,int LineIndex,AsmOPCode OPCode,List<AsmOrder> Orders)
    {
        AsmCode NewLine = new AsmCode(LineIndex);
        NewLine.Parse(BlockRef,StrPos,OPCode,Orders);
        Lines.Add(NewLine);
    }
}