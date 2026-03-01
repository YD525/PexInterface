

using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using static PapyrusAsmDecoder;

namespace PexInterface
{
    // Copyright (c) 2026 YD525
    // Licensed under the LGPL3.0 License.
    // See LICENSE file in the project root for full license information.
    public class PexHeuristicAnalysis
    {
        public static string Version = "1.0.0";

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
        }
        public string GetPsc(CodeGenStyle Style)
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
                                Content.AppendLine(string.Format(GenSpace(1) + GetFunc.Type + " " + GetFunc.Name+ " = " + GetFunc.Value));
                            }
                            else
                            if (Style == CodeGenStyle.CSharp)
                            {
                                Content.AppendLine(string.Format(GenSpace(1) + GetFunc.Type + " " + GetFunc.Name + " = " + GetFunc.Value + ";"));
                            }
                        }
                    }
                   
                }
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
                        Content.AppendLine(string.Format(GenSpace(1)  + GetFunc.Type + " " + GetFunc.Name + ";" + " //" + NodeStr));
                    }
                }
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

                    for (int i = 0; i < GetFunc.TracksRef.Keys.Count; i++)
                    {
                        string SetLine = "";

                        if (GetFunc.TracksRef[i].Code.Length > 0)
                        {
                            SetLine = PexHeuristicAnalysis.GenSpace(SpaceCount + GetFunc.TracksRef[i].SpaceCount) + GetFunc.TracksRef[i].Code + GetFunc.TracksRef[i].GetNote(Style) + "\n";
                        }
                        else
                        {
                            if (GetFunc.TracksRef[i].TrackRef == null)
                            {
                                SetLine = PexHeuristicAnalysis.GenSpace(SpaceCount + GetFunc.TracksRef[i].SpaceCount) + GetFunc.TracksRef[i].Assembly + "\n";
                            }
                            else
                            {
                                SetLine = PexHeuristicAnalysis.GenSpace(SpaceCount) + GetFunc.TracksRef[i].GetNote(Style) + "\n";
                            }
                        }

                        if (SetLine.EndsWith("\n"))
                        {
                            SetLine = SetLine.Substring(0, SetLine.Length - "\n".Length);
                        }

                        Content.AppendLine(SetLine);
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
    }


  
}
