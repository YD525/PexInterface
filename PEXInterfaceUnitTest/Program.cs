using System;
using PexInterface;

namespace PEXInterfaceUnitTest
{
    internal class Program
    {
        public static PexReader Reader = new PexReader();
        public static void LoadPex(string PexPath)
        {
            Reader.LoadPex(PexPath);
            new PapyrusAsmDecoder(Reader).Decompile(out PexHeuristicAnalysis Analyst);
            Console.Write(Analyst.GetPsc(CodeGenStyle.CSharp));
        }
        static void Main(string[] args)
        {
            //_wetquestscript.pex
            //LoadPex("C:\\Users\\52508\\Desktop\\TestPex\\_wetquestscript.pex");
            LoadPex("C:\\Users\\52508\\Desktop\\TestPex\\din_Config.pex");
            Console.ReadKey();
            foreach (var Get in Reader.StringTable)
            {
                //...
            }
        }
    }
}
