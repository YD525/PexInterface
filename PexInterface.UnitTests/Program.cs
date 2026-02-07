

using System;
using Newtonsoft.Json;

namespace PexInterface.UnitTests
{
    public class Program
    {
        public static PexReader Reader = new PexReader();
        public static PapyrusAsmDecoder AsmDecoder = null;
        public static void LoadPex(string PexPath)
        {
            Reader.LoadPex(PexPath);
            string PexContentJson = JsonConvert.SerializeObject(Reader);

            AsmDecoder = new PapyrusAsmDecoder(Reader, PapyrusAsmDecoder.CodeGenStyle.CSharp);
            string DecodeJson = JsonConvert.SerializeObject(AsmDecoder);
            var GetPsc = AsmDecoder.Decompile();

            Console.Write(PexContentJson+"\n---------------------"+DecodeJson);
        }
        static void Main(string[] args)
        {
            LoadPex("C:\\Users\\52508\\Desktop\\TestPex\\rdo_mcmconfig.pex");

            foreach (var Get in Reader.StringTable)
            { 
            
            }
        }
    }
}
