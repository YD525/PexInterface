using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Text.RegularExpressions;
using static PexInterface.PexHeuristicAnalysis;
using static PexInterface.PexReader;

namespace PexInterface
{
    // Copyright (c) 2026 YD525
    // Licensed under the LGPL3.0 License.
    // See LICENSE file in the project root for full license information.

    public class PexAnalysisPipeline
    {
        public PexReader Reader = new PexReader();
        public PexHeuristicAnalysis HeuristicCore = null;
        public PexAnalysisPipeline(PexHeuristicAnalysis HeuristicCore)
        {
            this.HeuristicCore = HeuristicCore;
        }
        public PexAnalysisPipeline ReadStrings()
        {
            this.HeuristicCore.ReadStrings();
            return this;
        }

        public object FileReadLocker = new object();
        public PexAnalysisPipeline LoadPex(string Path)
        {
            lock (FileReadLocker)
            {
                Close();
                Reader.LoadPex(Path);
                this.HeuristicCore.AsmDecoder.Reader = Reader;
                this.HeuristicCore.CurrentCls = this.HeuristicCore.AsmDecoder.Decompile();
                this.HeuristicCore.Init();
                return this;
            }
          
        }
        public object FileSaveLocker = new object();
        public PexAnalysisPipeline SavePex(string Path,out int SaveState)
        {
            lock (FileSaveLocker)
            {
                SaveState = this.HeuristicCore.SaveAll(Path);
            }
            return this;
        }

        public PexAnalysisPipeline GetReaderPointer(out IntPtr Pointer)
        {
            Pointer = Reader.GetHandle();
            return this;
        }
        public PexAnalysisPipeline GetStrings(out List<PexStringItem> StringsRef, string Type = "")
        {
            StringsRef = this.HeuristicCore.GetStrings(Type);
            return this;
        }
        public PexAnalysisPipeline AnalysisStrings()
        {
            HeuristicCore.AnalysisStrings();
            return this;
        }
        public PexAnalysisPipeline GetPsc(out string Psc, bool ShowNode, CodeGenStyle Style)
        {
            Psc = HeuristicCore.GetPsc(ShowNode, Style);
            return this;
        }
        public PexAnalysisPipeline Close()
        {
            Reader.Close();
            HeuristicCore.Close();
            return this;
        }
    }
    public class PexHeuristicAnalysis
    {
        public static string Version = "1.0.5 Beta";

        //https://ck.uesp.net/wiki/Category:Papyrus Game Api Doc


        //public static List<string> UserDefinedSafeFuncs = new List<string>() {
        //"Notify", "notify",
        //"Log", "log",
        //"Msg", "msg", "MSG",
        //"Message", "message",
        //"ShowMessage", "showMessage", "ShowMsg", "showMsg",
        //"Display", "display",
        //"Print", "print",
        //"Alert", "alert",
        //"Popup", "popup", "PopUp", "popUp",
        //"Toast", "toast",
        //"DebugLog", "debugLog", "Debug", "debug",
        //"WriteLine", "writeline", "Write", "write",
        //"ShowNotification", "showNotification", "Notification", "notification",
        //"ShowInfo", "showInfo", "Info", "info",
        //"ShowText", "showText",
        //"ConsoleLog", "consoleLog",
        //};

        public PexAnalysisPipeline Core = null;

        public PexHeuristicAnalysis()
        {
            Core = new PexAnalysisPipeline(this);
        }

        public PapyrusAsmDecoder AsmDecoder = new PapyrusAsmDecoder();


        public FuncRule FuncNameCheck = null;

        public Dictionary<string, List<PexStringItem>> Strings = new Dictionary<string, List<PexStringItem>>();
        public List<string> Types = new List<string>();

        public PscCls CurrentCls = new PscCls();

        public static string GenSpace(int Count)
        {
            string Space = "";
            for (int i = 0; i < Count; i++)
            {
                Space += "    ";
            }
            return Space;
        }

        public List<string> DangerFunctions = new List<string>() { "GetFormFromFile".ToLower(), "FromFile".ToLower(), "IsPluginInstalled".ToLower(), "GetModByName".ToLower(), "GetActorValue".ToLower() };
        public void Init()
        {
            FuncNameCheck = new FuncRule();

            //Safe
            FuncNameCheck.Add(new FunctionCheck("NotifyPlayer", 0, true, ApiType.FrameworkApi, 1, "DD Api"));
            FuncNameCheck.Add(new FunctionCheck("NotifyNPC", 0, true, ApiType.FrameworkApi, 1, "DD Api"));

            FuncNameCheck.Add(new FunctionCheck("AddMenuOption", 1, true, ApiType.FrameworkApi, 4, "SkyUI Api"));
            FuncNameCheck.Add(new FunctionCheck("AddMenuOptionST", 1, true, ApiType.FrameworkApi, 4, "SkyUI Api"));

            FuncNameCheck.Add(new FunctionCheck("AddSliderOption", 1, true, ApiType.FrameworkApi, 5, "SkyUI Api"));
            FuncNameCheck.Add(new FunctionCheck("AddSliderOptionST", 1, true, ApiType.FrameworkApi, 5, "SkyUI Api"));
            FuncNameCheck.Add(new FunctionCheck("AddSliderOptionST", 3, true, ApiType.FrameworkApi, 5, "SkyUI Api"));

            FuncNameCheck.Add(new FunctionCheck("AddSliderOption", 1, true, ApiType.FrameworkApi, 4, "SkyUI Api"));
            FuncNameCheck.Add(new FunctionCheck("AddSliderOptionST", 1, true, ApiType.FrameworkApi, 4, "SkyUI Api"));

            FuncNameCheck.Add(new FunctionCheck("AddTextOption", 1, true, ApiType.FrameworkApi, 4, "SkyUI Api"));
            FuncNameCheck.Add(new FunctionCheck("AddTextOptionST", 1, true, ApiType.FrameworkApi, 4, "SkyUI Api"));

            FuncNameCheck.Add(new FunctionCheck("AddToggleOption", 1, true, ApiType.FrameworkApi, 4, "SkyUI Api"));
            FuncNameCheck.Add(new FunctionCheck("AddToggleOptionST", 1, true, ApiType.FrameworkApi, 4, "SkyUI Api"));
            FuncNameCheck.Add(new FunctionCheck("AddTextOptionST", 2, true, ApiType.FrameworkApi, 4, "SkyUI Api"));

            FuncNameCheck.Add(new FunctionCheck("SetGameSettingString", 1, true, ApiType.UnknownAPI, -1, ""));
            FuncNameCheck.Add(new FunctionCheck("ShowMessage", 0, true, ApiType.FrameworkApi, 1, ""));
            FuncNameCheck.Add(new FunctionCheck("ShowMessage", 0, true, ApiType.FrameworkApi, 4, ""));
            FuncNameCheck.Add(new FunctionCheck("SetInfoText", 0, true, ApiType.FrameworkApi, 1, ""));
            FuncNameCheck.Add(new FunctionCheck("Logwarning", 0, true, ApiType.FrameworkApi, 1, "Din"));

            FuncNameCheck.Add(new FunctionCheck("Trace", 0, true, ApiType.NativeApi, 1, ""));
            FuncNameCheck.Add(new FunctionCheck("Trace", 0, true, ApiType.NativeApi, 2, ""));
            FuncNameCheck.Add(new FunctionCheck("Notification", 0, true, ApiType.NativeApi, 1, ""));

            FuncNameCheck.Add(new FunctionCheck("NotifyActor", 0, true, ApiType.FrameworkApi, 3, "DD"));
            FuncNameCheck.Add(new FunctionCheck("NotifyPlayer", 0, true, ApiType.FrameworkApi, 2, "DD"));
            FuncNameCheck.Add(new FunctionCheck("Warn", 0, true, ApiType.FrameworkApi, 1, "DD"));
            FuncNameCheck.Add(new FunctionCheck("Log", 0, true, ApiType.FrameworkApi, 2, "DD"));


            FuncNameCheck.Add(new FunctionCheck("notify", 0, true, ApiType.FrameworkApi, 1, "DD"));
            FuncNameCheck.Add(new FunctionCheck("notify", 0, true, ApiType.FrameworkApi, 2, "DD"));



            FuncNameCheck.Add(new FunctionCheck("MessageBox", 0, true, ApiType.FrameworkApi, 1, ""));


            //Danger
            FuncNameCheck.Add(new FunctionCheck("DamageActorValue", 0, false, ApiType.NativeApi, -1, "Game Api", ""));
            FuncNameCheck.Add(new FunctionCheck("SendModEvent", 0, false, ApiType.NativeApi, -1, "Game Api", ""));
            FuncNameCheck.Add(new FunctionCheck("AnimSwitchKeyword", 0, false, ApiType.NativeApi, -1, "Game Api", ""));
            FuncNameCheck.Add(new FunctionCheck("StartThirdPersonAnimation", 0, false, ApiType.NativeApi, -1, "Game Api", ""));
            FuncNameCheck.Add(new FunctionCheck("SendAnimationEvent", 0, false, ApiType.NativeApi, -1, "Game Api", ""));
            FuncNameCheck.Add(new FunctionCheck("SetInstanceVolume", 0, false, ApiType.NativeApi, -1, "Game Api", ""));
            FuncNameCheck.Add(new FunctionCheck("Create", 0, false, ApiType.NativeApi, -1, "Game Api", ""));
            FuncNameCheck.Add(new FunctionCheck("RegisterForMenu", 0, false, ApiType.NativeApi, -1, "Game Api", ""));

            FuncNameCheck.Add(new FunctionCheck("UnSetFormValue", 0, false, ApiType.NativeApi, -1, "StorageUtil Api", ""));


            FuncNameCheck.Add(new FunctionCheck("AttrDrain", 0, false, ApiType.FrameworkApi, -1, "SkSE Api", ""));
            FuncNameCheck.Add(new FunctionCheck("RandomExpressionByTag", 0, false, ApiType.FrameworkApi, -1, "SexLab Api", ""));
            FuncNameCheck.Add(new FunctionCheck("SendDeviceEvent", 0, false, ApiType.FrameworkApi, -1, "DD Api", ""));
        }

        public class StringBuilderExtend
        {
            public StringBuilder Content = new StringBuilder();
            public int LineCount = 0;

            public void AppendLine(string Msg = "")
            {
                Content.AppendLine(Msg);

                LineCount += Msg.Count(c => c == '\n') + 1;
            }
        }
        public string GetPsc(bool ShowNote, CodeGenStyle Style = CodeGenStyle.CSharp)
        {
            StringBuilderExtend Content = new StringBuilderExtend();

            if (Style == CodeGenStyle.Papyrus)
            {
                Content.AppendLine(string.Format("ScriptName {0} Extends {1}", CurrentCls.ClassName, CurrentCls.Inherit));
            }
            else
            if (Style == CodeGenStyle.CSharp)
            {
                Content.AppendLine("public class " + CurrentCls.ClassName + " : " + CurrentCls.Inherit + " \n{");
            }

            if (this.CurrentCls.GlobalVariables.Count > 0)
            {
                if (Style == CodeGenStyle.Papyrus)
                {
                    Content.AppendLine(GenSpace(1) + ";GlobalVariables");
                }
                else
                {
                    Content.AppendLine(GenSpace(1) + "//GlobalVariables");
                }

                foreach (var GetFunc in this.CurrentCls.GlobalVariables)
                {
                    if (GetFunc.Value.Length == 0)
                    {
                        if (Style == CodeGenStyle.Papyrus)
                        {
                            Content.AppendLine(string.Format(GenSpace(1) + GetFunc.Type + " " + GetFunc.Name));
                        }
                        else
                         if (Style == CodeGenStyle.CSharp)
                        {
                            Content.AppendLine(string.Format(GenSpace(1) + GetFunc.Type + " " + GetFunc.Name + ";"));
                        }
                    }
                    else
                    {
                        if (GetFunc.Type.ToLower().Equals("string"))
                        {
                            if (Style == CodeGenStyle.Papyrus)
                            {
                                Content.AppendLine(string.Format(GenSpace(1) + GetFunc.Type + " " + GetFunc.Name + " = " + GetFunc.Value));
                            }
                            else
                            if (Style == CodeGenStyle.CSharp)
                            {
                                Content.AppendLine(string.Format(GenSpace(1) + GetFunc.Type + " " + GetFunc.Name + " = " + GetFunc.Value + ";"));
                            }
                        }
                    }

                }

                Content.AppendLine(string.Empty);
            }

            if (this.CurrentCls.AutoGlobalVariables.Count > 0)
            {
                if (Style == CodeGenStyle.Papyrus)
                {
                    Content.AppendLine(GenSpace(1) + ";Global Properties");
                }
                else
                {
                    Content.AppendLine(GenSpace(1) + "//Global Properties");
                }

                foreach (var GetFunc in this.CurrentCls.AutoGlobalVariables)
                {
                    string NodeStr = "";

                    if (GetFunc.DeValue.Length > 0)
                    {
                        NodeStr += "Value:" + GetFunc.DeValue;
                    }

                    if (Style == CodeGenStyle.Papyrus)
                    {
                        Content.AppendLine(string.Format(GenSpace(1) + GetFunc.Type + " Property " + GetFunc.Name + " Auto" + " ;" + NodeStr));
                    }
                    else
                    if (Style == CodeGenStyle.CSharp)
                    {
                        Content.AppendLine(GenSpace(1) + "[Property(Auto = true)]");
                        Content.AppendLine(string.Format(GenSpace(1) + GetFunc.Type + " " + GetFunc.Name + ";" + " //" + NodeStr));
                    }
                }

                Content.AppendLine(string.Empty);
            }

            if (this.CurrentCls.Functions.Count > 0)
            {
                Content.AppendLine("\n");

                //var GetFunc in this.CurrentCls.Functions
                for (int i= 0;i<this.CurrentCls.Functions.Count;i++)
                {
                    var GetFunc = this.CurrentCls.Functions[i];
                    string GenParams = "";

                    if (GetFunc.Params.Count > 0)
                    {
                        foreach (var GetParam in GetFunc.Params)
                        {
                            GenParams += string.Format("{0} {1},", GetParam.Type, GetParam.Name);
                        }

                        if (GenParams.EndsWith(","))
                        {
                            GenParams = GenParams.Substring(0, GenParams.Length - 1);
                        }
                    }

                    string GenLine = "";

                    string AutoStr = "";
                    if (GetFunc.StateName.Length > 0)
                    {
                        if (Style == CodeGenStyle.CSharp)
                        {
                            AutoStr += "public class " + GetFunc.StateName + "\n{";
                        }
                        else
                        {
                            AutoStr += string.Format("State {0}\n", GetFunc.StateName);
                        }

                        Content.AppendLine(AutoStr);
                    }

                    if (Style == CodeGenStyle.Papyrus)
                    {
                        GenLine = string.Format(GenSpace(1) + "{0} Function {1}({2})", GetFunc.ReturnType, GetFunc.FunctionName, GenParams);
                    }
                    else
                    if (Style == CodeGenStyle.CSharp)
                    {
                        var TempReturnType = GetFunc.ReturnType;
                        if (TempReturnType.Length == 0)
                        {
                            TempReturnType = "void";
                        }

                        if (GetFunc.IsGlobal)
                        {
                            GenLine = string.Format(GenSpace(1) + "public {0} {1}({2})\n", TempReturnType, GetFunc.FunctionName, GenParams) + GenSpace(1) + "{";
                        }
                        else
                        if (GetFunc.IsNative)
                        {
                            GenLine = string.Format(GenSpace(1) + "public {0} {1}({2})\n", TempReturnType, GetFunc.FunctionName, GenParams) + GenSpace(1) + "{";
                        }
                        else
                        {
                            GenLine = string.Format(GenSpace(1) + "private {0} {1}({2})\n", TempReturnType, GetFunc.FunctionName, GenParams) + GenSpace(1) + "{";
                        }
                    }
                    
                    Content.AppendLine(GenLine);

                    GetFunc.PscStartLineIndex = Content.LineCount;

                    //var GetLine in GetFunc.TrackerRef.Lines
                    for (int ir =0;ir<GetFunc.TrackerRef.Lines.Count;ir++)
                    {
                        AsmCode GetLine = GetFunc.TrackerRef.Lines[ir];
                        string SetCode = GetLine.PSCCode;
                        if (string.IsNullOrEmpty(SetCode)) continue;

                        string[] subLines = SetCode.Split(
                            new[] { "\r\n", "\n" },
                            StringSplitOptions.RemoveEmptyEntries);

                        for (int si = 0; si < subLines.Length; si++)
                        {
                            string sub = subLines[si];
                            if (string.IsNullOrWhiteSpace(sub)) continue;

                            // Basic indentation of function body (level 2 = 8 spaces)
                            string outputLine = PexHeuristicAnalysis.GenSpace(2) + sub;

                            if (ShowNote)
                            {
                                // Optional assembly comment on the last line
                                if (si == subLines.Length - 1)
                                    outputLine += GetLine.GetNote();
                            }

                            Content.AppendLine(outputLine);
                        }
                    }

                    if (Style == CodeGenStyle.Papyrus)
                    {
                        Content.AppendLine(GenSpace(1) + "EndFunction\n");
                    }
                    else
                    if (Style == CodeGenStyle.CSharp)
                    {
                        Content.AppendLine(GenSpace(1) + "}\n");
                    }

                    if (AutoStr.Length > 0)
                    {
                        if (Style == CodeGenStyle.CSharp)
                        {
                            Content.AppendLine("}\n");
                        }
                        else
                        {
                            Content.AppendLine("EndState\n");
                        }
                    }

                }
            }

            return Content.Content.ToString();
        }
        public void ReadStrings()
        {
            this.Types.Clear();
            this.Strings.Clear();

            HashSet<ushort> IDs = new HashSet<ushort>();
            foreach (var Function in this.CurrentCls.Functions)
            {
                if (!this.Strings.ContainsKey(Function.FunctionName))
                {
                    this.Strings.Add(Function.FunctionName, new List<PexStringItem>());
                }

                Regex PlaceholderRegex = new Regex(@"^\{\d+\}$");

                foreach (var GetString in Function.Strings)
                {
                    if (GetString.Value.Length > 0)
                    {
                        if (!PlaceholderRegex.IsMatch(GetString.Value))
                        {
                            if (!IDs.Contains(GetString.Index))
                            {
                                this.Strings[Function.FunctionName].Add(new PexStringItem(Function, GetString));
                                IDs.Add(GetString.Index);
                            }
                            else
                            { 
                            
                            }
                        }
                    }
                }

                if(!this.Types.Contains(Function.FunctionName))
                this.Types.Add(Function.FunctionName);
            }


            if (!this.Strings.ContainsKey("GlobalVariables"))
            {
                this.Strings.Add("GlobalVariables", new List<PexStringItem>());
            }


            foreach (var GlobalVar in this.CurrentCls.GlobalVariables)
            {
                if (GlobalVar.Value.StartsWith("\""))
                {
                    PexString NPexString = new PexString()
                    {
                        Index = (ushort)GlobalVar.Offset,
                        Value = GlobalVar.Value
                    };

                    this.Strings["GlobalVariables"].Add(new PexStringItem(null,new PexStringExtend(null, NPexString, null)));
                }
            }
        }

        public List<PexStringItem> GetStrings(string Type = "")
        {
            List<PexStringItem> Result = new List<PexStringItem>();

            if (Type == "")
            {
                foreach (var KV in Strings)
                {
                    Result.AddRange(KV.Value);
                }
            }
            else
            {
                if (Strings.ContainsKey(Type))
                {
                    Result.AddRange(Strings[Type]);
                }
            }

            Result.Sort((a, b) => b.Score.CompareTo(a.Score));

            return Result;
        }

        public void AnalysisStrings()
        {
            List<PexString> TempStrings = new List<PexString>();

            TempStrings.AddRange(this.AsmDecoder.Reader.StringTable);

            foreach (var GetKey in this.Strings.Keys)
            {
                if (GetKey != "GlobalVariables")
                {
                    var FuncStrs = this.Strings[GetKey];
                    Dictionary<string, int> FuncNames = new Dictionary<string, int>();
                    for (int i = 0; i < FuncStrs.Count; i++)
                    {
                        var SetFlow = FuncStrs[i].FunctionRef.StringFlower[FuncStrs[i].StringTableID];

                        var FuncIndex = 0;
                        var IFIndex = 0;

                        FuncStrs[i].Score = -1;

                        if (SetFlow.ConsumedByMethodName?.Length > 0)
                        {
                            string SetKey = SetFlow.ConsumedByCallerName + "_" + SetFlow.ConsumedByMethodName;
                            if (FuncNames.ContainsKey(SetKey))
                            {
                                FuncNames[SetKey]++;
                                FuncIndex = FuncNames[SetKey];
                            }
                            else
                            {
                                FuncNames.Add(SetKey, 0);
                            }
                        }

                        IFIndex = SetFlow.RelatedConditions.Count + SetFlow.LocalVariablesInvolved.Count + SetFlow.GlobalVariablesInvolved.Count;

                        if (FuncStrs[i].Original.StartsWith("$"))
                        {
                            FuncStrs[i].Score = -100;
                        }
                        else
                        if (DangerFunctions.Contains(SetFlow.ConsumedByMethodName?.ToLower()))
                        {
                            FuncStrs[i].Score = -100;
                        }
                        else
                        if (SetFlow.ConsumedByMethodName?.Length > 0)
                        {
                            FuncStrs[i].Score += FuncNameCheck.CheckFuncByName(SetFlow.CallInfo.MethodName, SetFlow.CallInfo.StringArgIndex, SetFlow.CallInfo.TotalArgCount);
                        }
                        else
                        {
                            if (SetFlow.RelatedConditions.Count > 0)
                            {
                                FuncStrs[i].Score -= 20;
                            }
                        }

                        FuncStrs[i].UniqueKey = PexStringItem.GenUniqueKey(FuncIndex + "_" + IFIndex + SetFlow.ArrayTarget, FuncStrs[i].Score, FuncStrs[i].FunctionRef, FuncStrs[i].PexStringItemRef);
                    }
                }
                else
                { 
                    var VarStrs = this.Strings[GetKey];
                    for (int i = 0; i < VarStrs.Count; i++)
                    {
                        var CurrentVar = VarStrs[i];
                        try 
                        { 
                            var VarInFo = TempStrings[CurrentVar.StringTableID];

                            
                        }
                        catch { }
                    }
                }
            }
        }

        public int EvaluationScore()
        {
            return 0;
        }

        public int SaveAll(string Output)
        {
            try
            {
                var LocalStrings = GetStrings();
                int ModifyCount = 0;
                for (int i = 0; i < LocalStrings.Count; i++)
                {
                    if (LocalStrings[i].IsCanTranslate())
                    {
                        if (LocalStrings[i].Translated.Length > 0)
                        {
                            Core.Reader.ModifyStringTable((ushort)LocalStrings[i].StringTableID, LocalStrings[i].Translated);
                            ModifyCount++;
                        }
                    }
                }
                if (ModifyCount > 0)
                {
                    return Core.Reader.SavePex(Output);
                }

                return 0;
            }
            catch { return -1; }
        }

        public void Close()
        {
            this.CurrentCls = null;
            this.Types.Clear();
            this.Strings.Clear();
            Core.Reader.Close();
            GC.SuppressFinalize(this);
        }

        public class PexStringExtend
        {
            public ushort Index { get; set; }
            public string Value { get; set; }
            public AsmLink Link { get; set; }

            public AsmCode Asm { get; set; }

            public PexStringExtend(AsmCode Asm,PexString Str, AsmLink Link)
            {
                this.Asm = Asm;

                this.Index = Str.Index;
                this.Value = Str.Value;
                this.Link = Link;
            }
        }

        public class PexStringItem
        {
            public string UniqueKey = "";
            public FunctionBlock FunctionRef = null;
            public PexStringExtend PexStringItemRef = null;
            public ushort StringTableID = 0;
            public int Score = 0;
            public string Original = "";
            public string Translated = "";

            public bool IsCanTranslate()
            {
                if (this.Score > 0)
                {
                    return true;
                }
                return false;
            }

            public bool IsSafe()
            {
                if (this.Score >= 9)
                {
                    return true;
                }
                return false;
            }

            public static string GenUniqueKey(string Sign,int Score, FunctionBlock Func, PexStringExtend StringItem)
            {
                var GetHead = StringItem.Link.GetHead();
                var GetPrev = StringItem.Link.Prev;
                var GetNext = StringItem.Link.Next;

                var GetTail = StringItem.Link.GetTail();

                string AutoMerge = string.Join("_", new[] {
                    GetHead?.GetSign(),
                    GetPrev?.GetSign(),
                    GetNext?.GetSign(),
                    GetTail?.GetSign()
                }.Where(S=> !string.IsNullOrEmpty(S)));

                //AutoMerge += "_" + StringItem.Index;

                string SetKey =Crc32Helper.ComputeCrc32(Score + "_" + Sign + "_" + Func.FunctionName + "_" + AutoMerge);
                return SetKey;
            }

            public PexStringItem(FunctionBlock FunctionRef, PexStringExtend Item)
            {
                this.FunctionRef = FunctionRef;
                this.PexStringItemRef = Item;

                this.StringTableID = Item.Index;
                this.Original = string.Copy(Item.Value);
                this.Translated = string.Empty;
            }
        }


        public class ScoreEvaluator
        {
            public static int Null = 0;
            public static int Safe = 30;
            public static int Normal = 10;
            public static int PotentialRisk = 0;
            public static int HighRisk = -1;
        }

        public enum ApiType
        {
            UnknownAPI = 0, NativeApi = 1, FrameworkApi = 2
        }

        public class ParamCheck
        {
            public int ParamAtIndex = 0;
            public bool IsReward = false;
            public string Note = "";
            public int ParamCount = -1;

            public ParamCheck(int ParamAtIndex, bool IsReward, string Note,int ParamCount)
            {
                this.ParamAtIndex = ParamAtIndex;
                this.IsReward = IsReward;
                this.Note = Note;
                this.ParamCount = ParamCount;
            }
        }
        public class FunctionCheck
        {
            public ApiType Type = ApiType.UnknownAPI;
            public string FunctionName = "";
            public Dictionary<string,ParamCheck> IndexChecks = new Dictionary<string, ParamCheck>();

            public string Note = "";

            public int CheckScore()
            {
                switch (this.Type)
                {
                    case ApiType.NativeApi:
                        return 20;
                    case ApiType.FrameworkApi:
                        return 10;
                    case ApiType.UnknownAPI:
                        return 5;
                }
                return 0;
            }
            public FunctionCheck(string FuncName, int ParamAtIndex, bool IsReward, ApiType Type, int ParamCount, string FuncNote = "", string ParamNote = "")
            {
                this.FunctionName = FuncName.ToLower();
                this.Type = Type;
                this.IndexChecks.Add(ParamAtIndex + "_" + ParamCount, new ParamCheck(ParamAtIndex, IsReward, Note,ParamCount));
                this.Note = FuncNote;
            }
        }
        public class FuncRule
        {
            public Dictionary<string, FunctionCheck> Checks = new Dictionary<string, FunctionCheck>();
            public void Add(FunctionCheck Func)
            {
                if (Checks.ContainsKey(Func.FunctionName))
                {
                    if (Func.IndexChecks.Count > 0)
                    {
                        var GetFrist = Func.IndexChecks.ToList()[0];

                        Checks[Func.FunctionName].IndexChecks.Add(GetFrist.Key, GetFrist.Value);
                    }
                }
                else
                {
                    Checks.Add(Func.FunctionName, Func);
                }
            }
            public int CheckFuncByName(string FuncName, int ParamAtIndex, int ParamCount)
            {
                FuncName = FuncName.ToLower();
                if (Checks.ContainsKey(FuncName))
                {
                    var GetRule = Checks[FuncName];

                    if (GetRule.IndexChecks.Count > 0)
                    {
                        var GetKey = ParamAtIndex + "_" + ParamCount;
                        if (GetRule.IndexChecks.ContainsKey(GetKey))
                        {
                            if (GetRule.IndexChecks[GetKey].ParamAtIndex == ParamAtIndex)
                            {
                                int Score = GetRule.CheckScore();
                                Score = GetRule.IndexChecks[GetKey].IsReward ? Math.Abs(Score) : -Math.Abs(Score);
                                return Score;
                            }
                        }

                        if (GetRule.IndexChecks.ContainsKey("0" + "_" + "-1"))
                        {
                            int Score = GetRule.CheckScore();
                            Score = GetRule.IndexChecks["0" + "_" + "-1"].IsReward ? Math.Abs(Score) : -Math.Abs(Score);
                            return Score;
                        }
                    }
                }

                return ScoreEvaluator.Null;
            }
        }

    }
}
