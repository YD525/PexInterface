using System;

namespace PexInterface
{
    /// <summary>
    /// Renders an analyzed PEX class without accessing parsing or native interop.
    /// </summary>
    internal static class PexSourceRenderer
    {
        private static string GenSpace(int count)
        {
            return PexHeuristicAnalysis.GenSpace(count);
        }

        internal static string Render(PscCls currentClass, bool ShowNote, CodeGenStyle Style)
        {
            PexHeuristicAnalysis.StringBuilderExtend Content = new PexHeuristicAnalysis.StringBuilderExtend();

            if (Style == CodeGenStyle.Papyrus)
            {
                Content.AppendLine(string.Format("ScriptName {0} Extends {1}", currentClass.ClassName, currentClass.Inherit));
            }
            else
            if (Style == CodeGenStyle.CSharp)
            {
                Content.AppendLine("public class " + currentClass.ClassName + " : " + currentClass.Inherit + " \n{");
            }

            if (currentClass.GlobalVariables.Count > 0)
            {
                if (Style == CodeGenStyle.Papyrus)
                {
                    Content.AppendLine(GenSpace(1) + ";GlobalVariables");
                }
                else
                {
                    Content.AppendLine(GenSpace(1) + "//GlobalVariables");
                }

                foreach (var GetFunc in currentClass.GlobalVariables)
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

            if (currentClass.AutoGlobalVariables.Count > 0)
            {
                if (Style == CodeGenStyle.Papyrus)
                {
                    Content.AppendLine(GenSpace(1) + ";Global Properties");
                }
                else
                {
                    Content.AppendLine(GenSpace(1) + "//Global Properties");
                }

                foreach (var GetFunc in currentClass.AutoGlobalVariables)
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

            if (currentClass.Functions.Count > 0)
            {
                Content.AppendLine("\n");

                for (int i= 0;i<currentClass.Functions.Count;i++)
                {
                    var GetFunc = currentClass.Functions[i];
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
    }
}
