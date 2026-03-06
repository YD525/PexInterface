using System;
using System.Collections.Generic;
using PexInterface;
using static PexInterface.PexHeuristicAnalysis;

namespace PEXInterfaceUnitTest
{
    internal class Program
    {
        public static PexHeuristicAnalysis LoadPex(string PexPath)
        {
            PexHeuristicAnalysis Analysis = new PexHeuristicAnalysis();
            Analysis.Core.LoadPex(PexPath).GetPsc(out string Psc, false, CodeGenStyle.CSharp)
            .ReadStrings().GetStrings(out List<PexStringItem> Strings);

            foreach (var GetStr in Strings)
            {
                Console.WriteLine(string.Format("Key:{0},Value:{1}",GetStr.UniqueKey,GetStr.Original));
            }

            return Analysis;
            //Console.WriteLine(Psc);
        }
        static void Main(string[] args)
        {
            //Multi-file reading test
            PexHeuristicAnalysis Analysis1st = new PexHeuristicAnalysis();
            Analysis1st.Core.LoadPex("C:\\Users\\52508\\Desktop\\TestPex\\din_Config.pex").GetPsc(out string Psc, false, CodeGenStyle.CSharp)
            .ReadStrings().GetStrings(out List<PexStringItem> Strings).GetReaderPointer(out IntPtr Ptr1st);

            PexHeuristicAnalysis Analysis2nd = new PexHeuristicAnalysis();
            Analysis2nd.Core.LoadPex("C:\\Users\\52508\\Desktop\\TestPex\\_wetquestscript.pex").GetPsc(out string PscA, false, CodeGenStyle.CSharp)
            .ReadStrings().GetStrings(out List<PexStringItem> StringsA).GetReaderPointer(out IntPtr Ptr2nd);

            Console.WriteLine($"Ptr1st = 0x{Ptr1st.ToInt64():X}");
            Console.WriteLine($"Ptr2nd = 0x{Ptr2nd.ToInt64():X}");

            //foreach (var GetStr in Strings)
            //{
            //    Console.WriteLine(string.Format("Key:{0},Value:{1}", GetStr.UniqueKey, GetStr.Original));
            //}

            ////_wetquestscript.pex
            ////LoadPex("C:\\Users\\52508\\Desktop\\TestPex\\_wetquestscript.pex");
            //var GetAnalysis = LoadPex("C:\\Users\\52508\\Desktop\\TestPex\\din_Config.pex");
            Console.ReadKey();
           
        }
    }
}
