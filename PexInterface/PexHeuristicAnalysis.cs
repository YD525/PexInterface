using System;
using System.Collections.Generic;
using System.Text;

namespace PexInterface
{
    // Copyright (c) 2026 YD525
    // Licensed under the LGPL3.0 License.
    // See LICENSE file in the project root for full license information.
    public class PexHeuristicAnalysis
    {
        public static string Version = "1.0.0 Alpha";

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


        public FuncRule FuncNameCheck = null;

        public List<PexStringItem> Strings = new List<PexStringItem>();
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

        public PexHeuristicAnalysis(PscCls SetClass)
        {
            Strings.Clear();
            CurrentCls = SetClass;
            Init();
        }

        public void Init()
        {
            FuncNameCheck = new FuncRule();

            //Safe
            FuncNameCheck.Add(new FunctionCheck("NotifyPlayer",0,true,ApiType.FrameworkApi,-1, "DD Api"));
            FuncNameCheck.Add(new FunctionCheck("NotifyNPC",0,true,ApiType.FrameworkApi, -1, "DD Api"));

            FuncNameCheck.Add(new FunctionCheck("AddSliderOption",0,true,ApiType.FrameworkApi, -1, "SkyUI Api"));
            FuncNameCheck.Add(new FunctionCheck("AddSliderOptionST", 1, true, ApiType.FrameworkApi, -1, "SkyUI Api"));

            FuncNameCheck.Add(new FunctionCheck("AddTextOption", 0, true, ApiType.FrameworkApi, -1, "SkyUI Api"));
            FuncNameCheck.Add(new FunctionCheck("AddTextOptionST", 1, true, ApiType.FrameworkApi, -1, "SkyUI Api"));

            FuncNameCheck.Add(new FunctionCheck("AddToggleOption", 0, true, ApiType.FrameworkApi, -1, "SkyUI Api"));
            FuncNameCheck.Add(new FunctionCheck("AddToggleOptionST", 1, true, ApiType.FrameworkApi, -1, "SkyUI Api"));


            FuncNameCheck.Add(new FunctionCheck("SetGameSettingString", 1, true, ApiType.UnknownAPI, -1, ""));
            FuncNameCheck.Add(new FunctionCheck("ShowMessage", 0, true, ApiType.FrameworkApi, 1, ""));
    
            //Danger
            FuncNameCheck.Add(new FunctionCheck("DamageActorValue", 0, false, ApiType.NativeApi, -1, "Game Api","",true));
            FuncNameCheck.Add(new FunctionCheck("SendModEvent", 0, false, ApiType.NativeApi, -1, "Game Api", "", true));
            FuncNameCheck.Add(new FunctionCheck("AnimSwitchKeyword", 0, false, ApiType.NativeApi, -1, "Game Api", "", true));
            FuncNameCheck.Add(new FunctionCheck("StartThirdPersonAnimation", 0, false, ApiType.NativeApi, -1, "Game Api", "", true));
            FuncNameCheck.Add(new FunctionCheck("SendAnimationEvent", 0, false, ApiType.NativeApi, -1, "Game Api", "", true));
            FuncNameCheck.Add(new FunctionCheck("SetInstanceVolume", 0, false, ApiType.NativeApi, -1, "Game Api", "", true));
            FuncNameCheck.Add(new FunctionCheck("Create", 0, false, ApiType.NativeApi, -1, "Game Api", "", true));

            FuncNameCheck.Add(new FunctionCheck("UnSetFormValue", 0, false, ApiType.NativeApi, -1, "StorageUtil Api", "", true));
        

            FuncNameCheck.Add(new FunctionCheck("AttrDrain", 0, false, ApiType.FrameworkApi, -1, "SkSE Api", "", true));
            FuncNameCheck.Add(new FunctionCheck("RandomExpressionByTag", 0, false, ApiType.FrameworkApi, -1, "SexLab Api", "", true));
            FuncNameCheck.Add(new FunctionCheck("SendDeviceEvent", 0, false, ApiType.FrameworkApi, -1, "DD Api", "", true));
        }
        public string GetPsc(CodeGenStyle Style = CodeGenStyle.CSharp)
        {
            StringBuilder Content = new StringBuilder();

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

                foreach (var GetFunc in this.CurrentCls.Functions)
                {
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
                        GenLine = string.Format(GenSpace(1) + "public {0} {1}({2})\n", TempReturnType, GetFunc.FunctionName, GenParams) + GenSpace(1) + "{";
                    }

                    Content.AppendLine(GenLine);

                    int SpaceCount = 2;

                    foreach (var GetLine in GetFunc.TracksRef.Lines)
                    {
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

                            // Optional assembly comment on the last line
                            if (si == subLines.Length - 1)
                                outputLine += GetLine.GetNote();

                            Content.AppendLine(outputLine);
                        }
                    }

                    if (Style == CodeGenStyle.Papyrus)
                    {
                        Content.AppendLine(GenSpace(1) + "EndFunction");
                    }
                    else
                    if (Style == CodeGenStyle.CSharp)
                    {
                        Content.AppendLine(GenSpace(1) + "}\n");
                    }

                }
            }

            return Content.ToString();
        }

        public void SaveAll(string Output)
        {
            int ModifyCount = 0;
            for (int i = 0; i < Strings.Count; i++)
            {
                if (Strings[i].Translated.Length > 0)
                {
                    PexInterop.ModifyStringTable((ushort)Strings[i].StringTableID, Strings[i].Translated);
                    ModifyCount++;
                }
            }
            if (ModifyCount > 0)
            {
                PexInterop.SavePex(Output);
            }
        }

        public void Close()
        {
            this.CurrentCls = null;
            this.Strings.Clear();
            GC.SuppressFinalize(this);
        }


        public class PexStringItem
        {
            public string UniqueKey = "";
            public object Link = new object();
            public int StringTableID = 0;
            public int Score = 0;
            public string Original = "";
            public string Translated = "";
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
            UnknownAPI = 0,NativeApi = 1,FrameworkApi = 2
        }

        public class ParamCheck
        {
            public int Index = 0;
            public bool IsReward = false;
            public string Note = "";

            public ParamCheck(int Index, bool IsReward, string Note)
            {
                this.Index = Index;
                this.IsReward = IsReward;
                this.Note = Note;
            }
        }
        public class FunctionCheck
        {
            public ApiType Type = ApiType.UnknownAPI;
            public bool FullCheck = false;
            public string FunctionName = "";
            public List<ParamCheck> IndexChecks = new List<ParamCheck>();
            public string Note = "";
            public int ParamCountLimit = -1;

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
            public FunctionCheck(string FuncName, int ParamIndex,bool IsReward, ApiType Type,int ParamCountLimit,string FuncNote ="", string ParamNote = "",bool FullCheck = false)
            {
                this.FunctionName = FuncName;
                this.Type = Type;
                this.FullCheck = FullCheck;
                this.IndexChecks = new List<ParamCheck>() { new ParamCheck(ParamIndex,IsReward, Note)};
                this.ParamCountLimit = ParamCountLimit;
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
                        Checks[Func.FunctionName].IndexChecks.Add(Func.IndexChecks[0]);
                    }
                }
                else
                {
                    Checks.Add(Func.FunctionName, Func);
                }
            }
            public int CheckFuncByName(string FuncName, int ParamIndex = 0,int ParamCount = -1)
            {
                if (Checks.ContainsKey(FuncName))
                {
                    var GetRule = Checks[FuncName];

                    if (!GetRule.FullCheck)
                    {
                        for (int i = 0; i < GetRule.IndexChecks.Count; i++)
                        {
                            if (GetRule.IndexChecks[i].Index == ParamIndex)
                            {
                                if (ParamCount == -1 || (GetRule.ParamCountLimit == ParamCount))
                                {
                                    int Score = GetRule.CheckScore();
                                    Score = GetRule.IndexChecks[i].IsReward ? Math.Abs(Score) : -Math.Abs(Score);
                                    return Score;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (GetRule.IndexChecks.Count > 0)
                        {
                            if (ParamCount == -1 || (GetRule.ParamCountLimit == ParamCount))
                            {
                                int Score = GetRule.CheckScore();
                                Score = GetRule.IndexChecks[0].IsReward ? Math.Abs(Score) : -Math.Abs(Score);
                                return Score;
                            }
                        }
                    }
                  
                }

                return ScoreEvaluator.Null;
            }
        }

    }
}
