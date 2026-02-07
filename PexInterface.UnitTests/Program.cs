

using System;
using Newtonsoft.Json;

namespace PexInterface.UnitTests
{
    public class Program
    {
        public void TestLoadPex(string PexPath)
        {
            PexReader Reader = new PexReader();
            Reader.LoadPex(PexPath);
            string PexContentJson = JsonConvert.SerializeObject(Reader);

            PapyrusAsmDecoder Decoder = new PapyrusAsmDecoder(Reader, PapyrusAsmDecoder.CodeGenStyle.CSharp);
            string DecodeJson = JsonConvert.SerializeObject(Decoder);
            var GetPsc = Decoder.Decompile();

            Console.Write(PexContentJson+"\n---------------------"+DecodeJson);
        }
        static void Main(string[] args)
        {
        }
    }
}
