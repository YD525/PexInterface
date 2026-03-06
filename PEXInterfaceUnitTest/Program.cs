using System;
using System.Collections.Generic;
using PexInterface;
using static PexInterface.PexHeuristicAnalysis;

namespace PEXInterfaceUnitTest
{
    internal class Program
    {
        public static PexReader Reader = new PexReader();
        public static void LoadPex(string PexPath)
        {
            Reader.LoadPex(PexPath);
            new PapyrusAsmDecoder(Reader).Decompile(out PexHeuristicAnalysis Analysis);

            Analysis.Core.GetPsc(out string Psc,false).ReadStrings().GetStrings(out List<PexStringItem> Strings);
            Console.WriteLine(Psc);
        }
        static void Main(string[] args)
        {
            //_wetquestscript.pex
            //LoadPex("C:\\Users\\52508\\Desktop\\TestPex\\_wetquestscript.pex");
            LoadPex("C:\\Users\\52508\\Desktop\\TestPex\\rdo_mcmconfig.pex");
            Console.ReadKey();
            foreach (var Get in Reader.StringTable)
            {
                //...
            }
        }
    }
}
