using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PexInterface;
using static PexInterface.PexHeuristicAnalysis;

namespace PEXInterfaceUnitTest
{
    /// <summary>
    /// Verifies the managed PEX integration against a pinned native reader release.
    /// </summary>
    [TestClass]
    public sealed class PexInterfaceTests
    {
        private const string FixtureName = "skyrim-se-3.2-unicode.pex.hex";

        /// <summary>
        /// Verifies that the restored native dependency has the pinned release version.
        /// </summary>
        [TestMethod]
        public void NativeDependencyMatchesPinnedRelease()
        {
            Assert.AreEqual("1.0.1.5", PexInterop.GetVersion());
        }

        /// <summary>
        /// Verifies that parsing exposes header, string, debug, object, and instruction data.
        /// </summary>
        [TestMethod]
        public void LoadsHeaderStringsAndObjectModel()
        {
            using (var directory = new TemporaryDirectory())
            using (var reader = new PexReader())
            {
                string fixturePath = WriteFixture(directory.Path, "input.pex");
                reader.LoadPex(fixturePath);

                Assert.AreEqual(0xFA57C0DEu, reader.Header.Magic);
                Assert.AreEqual((byte)3, reader.Header.MajorVersion);
                Assert.AreEqual((byte)2, reader.Header.MinorVersion);
                Assert.AreEqual((ushort)1, reader.Header.GameId);
                Assert.AreEqual("Fixture.psc", reader.Header.SourceFileName);
                Assert.AreEqual("Tester", reader.Header.Username);
                Assert.AreEqual("BuildHost", reader.Header.MachineName);

                Assert.AreEqual(7, reader.StringTable.Count);
                Assert.AreEqual("ObjectName", reader.StringTable[0].Value);
                Assert.AreEqual("Grüße 東京", reader.StringTable[1].Value);
                Assert.AreEqual("FunctionName", reader.StringTable[2].Value);

                Assert.IsTrue(reader.DebugInfo.HasDebugInfo);
                Assert.AreEqual((ushort)1, reader.DebugInfo.FunctionCount);
                Assert.AreEqual(1, reader.DebugInfo.Functions.Count);
                CollectionAssert.AreEqual(
                    new ushort[] { 42, 43 },
                    reader.DebugInfo.Functions[0].LineNumbers);

                Assert.AreEqual(1, reader.UserFlags.Count);
                Assert.AreEqual(1, reader.Objects.Count);
                Assert.AreEqual(1, reader.Objects[0].Variables.Count);
                Assert.AreEqual(2, reader.Objects[0].Properties.Count);
                Assert.AreEqual(1, reader.Objects[0].States.Count);
                Assert.AreEqual(1, reader.Objects[0].States[0].Functions.Count);
                Assert.AreEqual(2, reader.Objects[0].States[0].Functions[0].Instructions.Count);
            }
        }

        /// <summary>
        /// Verifies that the analysis pipeline decompiles the parsed function and extracts its strings.
        /// </summary>
        [TestMethod]
        public void DecompilesAndExtractsStringsThroughAnalysisPipeline()
        {
            using (var directory = new TemporaryDirectory())
            {
                string fixturePath = WriteFixture(directory.Path, "analysis.pex");
                var analysis = new PexHeuristicAnalysis();
                try
                {
                    analysis.Core
                        .LoadPex(fixturePath)
                        .GetPsc(out string source, false, CodeGenStyle.CSharp)
                        .ReadStrings()
                        .AnalysisStrings()
                        .GetStrings(out List<PexStringItem> strings);

                    StringAssert.Contains(source, "ObjectName");
                    StringAssert.Contains(source, "FunctionName");
                    Assert.IsTrue(strings.Any(item => item.Original == "Method"));
                }
                finally
                {
                    analysis.Core.Reader.Dispose();
                }
            }
        }

        /// <summary>
        /// Verifies that a Unicode string modification survives save and reload without changing structure.
        /// </summary>
        [TestMethod]
        public void ModifiesSavesAndReloadsUnicodeString()
        {
            using (var directory = new TemporaryDirectory())
            {
                string inputPath = WriteFixture(directory.Path, "input.pex");
                string outputPath = System.IO.Path.Combine(directory.Path, "output.pex");
                const string replacement = "Neu: Καλημέρα 東京";

                using (var reader = new PexReader())
                {
                    reader.LoadPex(inputPath);
                    Assert.AreEqual(1, reader.ModifyStringTable(1, replacement));
                    Assert.AreEqual(1, reader.SavePex(outputPath));
                }

                using (var reloaded = new PexReader())
                {
                    reloaded.LoadPex(outputPath);
                    Assert.AreEqual(replacement, reloaded.StringTable[1].Value);
                    Assert.AreEqual("ObjectName", reloaded.StringTable[0].Value);
                    Assert.AreEqual("FunctionName", reloaded.StringTable[2].Value);
                    Assert.AreEqual(1, reloaded.Objects.Count);
                    Assert.AreEqual(2, reloaded.Objects[0].States[0].Functions[0].Instructions.Count);
                }
            }
        }

        /// <summary>
        /// Verifies that truncated input is rejected and does not replace a previously loaded model.
        /// </summary>
        [TestMethod]
        public void RejectsMalformedInputWithoutReplacingValidModel()
        {
            using (var directory = new TemporaryDirectory())
            using (var reader = new PexReader())
            {
                string validPath = WriteFixture(directory.Path, "valid.pex");
                byte[] malformedBytes = ReadFixtureBytes();
                Array.Resize(ref malformedBytes, malformedBytes.Length - 1);
                string malformedPath = System.IO.Path.Combine(directory.Path, "truncated.pex");
                File.WriteAllBytes(malformedPath, malformedBytes);

                reader.LoadPex(validPath);
                AssertThrows<Exception>(() => reader.LoadPex(malformedPath));

                Assert.AreEqual(7, reader.StringTable.Count);
                Assert.AreEqual("ObjectName", reader.StringTable[0].Value);
                Assert.AreEqual(1, reader.Objects.Count);
            }
        }

        /// <summary>
        /// Verifies that disposing a reader is repeatable and prevents later native access.
        /// </summary>
        [TestMethod]
        public void DisposeIsIdempotentAndRejectsLaterAccess()
        {
            var reader = new PexReader();
            Assert.AreNotEqual(IntPtr.Zero, reader.GetHandle());

            reader.Dispose();
            reader.Dispose();

            AssertThrows<ObjectDisposedException>(() => reader.GetHandle());
            AssertThrows<ObjectDisposedException>(() => reader.Close());
        }

        /// <summary>
        /// Verifies repeated parse and disposal cycles through the managed and native ownership boundary.
        /// </summary>
        [TestMethod]
        public void RepeatedParseAndDisposeCyclesRemainValid()
        {
            using (var directory = new TemporaryDirectory())
            {
                string fixturePath = WriteFixture(directory.Path, "lifecycle.pex");
                for (int iteration = 0; iteration < 512; iteration++)
                {
                    using (var reader = new PexReader())
                    {
                        reader.LoadPex(fixturePath);
                        Assert.AreEqual(7, reader.StringTable.Count);
                        Assert.AreEqual(2, reader.DebugInfo.Functions[0].LineNumbers.Length);
                    }
                }
            }
        }

        private static string WriteFixture(string directory, string fileName)
        {
            string path = System.IO.Path.Combine(directory, fileName);
            File.WriteAllBytes(path, ReadFixtureBytes());
            return path;
        }

        private static byte[] ReadFixtureBytes()
        {
            string resourceName = typeof(PexInterfaceTests).Namespace + ".Fixtures." + FixtureName;
            string hex;
            using (Stream stream = typeof(PexInterfaceTests).Assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                    throw new InvalidOperationException("The documented PEX fixture was not embedded.");
                using (var reader = new StreamReader(stream))
                    hex = reader.ReadToEnd();
            }
            var bytes = new List<byte>();
            int highNibble = -1;
            foreach (char character in hex)
            {
                if (char.IsWhiteSpace(character))
                    continue;

                int value = HexValue(character);
                if (value < 0)
                    throw new InvalidDataException("The documented PEX fixture contains a non-hexadecimal character.");

                if (highNibble < 0)
                    highNibble = value;
                else
                {
                    bytes.Add((byte)((highNibble << 4) | value));
                    highNibble = -1;
                }
            }

            if (highNibble >= 0 || bytes.Count == 0)
                throw new InvalidDataException("The documented PEX fixture contains an incomplete byte.");
            return bytes.ToArray();
        }

        private static int HexValue(char character)
        {
            if (character >= '0' && character <= '9')
                return character - '0';
            if (character >= 'A' && character <= 'F')
                return character - 'A' + 10;
            if (character >= 'a' && character <= 'f')
                return character - 'a' + 10;
            return -1;
        }

        private static void AssertThrows<TException>(Action action)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }

            Assert.Fail(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Expected {0}.",
                    typeof(TException).Name));
        }

        private sealed class TemporaryDirectory : IDisposable
        {
            private bool _disposed;

            public TemporaryDirectory()
            {
                Path = System.IO.Path.Combine(
                    System.IO.Path.GetTempPath(),
                    "PexInterfaceTests-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(Path);
            }

            public string Path { get; }

            public void Dispose()
            {
                if (_disposed)
                    return;

                if (Directory.Exists(Path))
                    Directory.Delete(Path, true);
                _disposed = true;
            }
        }
    }
}
