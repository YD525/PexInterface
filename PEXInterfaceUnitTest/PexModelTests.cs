using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PexInterface;

namespace PEXInterfaceUnitTest
{
    /// <summary>
    /// Verifies the managed analysis boundary without constructing a native reader.
    /// </summary>
    [TestClass]
    public sealed class PexModelTests
    {
        /// <summary>
        /// Verifies that decompilation consumes only the immutable managed snapshot.
        /// </summary>
        [TestMethod]
        public void DecompilesManagedModelWithoutNativeReader()
        {
            var model = CreateModel();
            var decoder = new PapyrusAsmDecoder();
            decoder.SetModel(model);

            PscCls result = decoder.Decompile();

            Assert.AreEqual("ObjectName", result.ClassName);
            Assert.AreEqual("ObjectName", result.Inherit);
            Assert.AreEqual(1, result.Functions.Count);
            Assert.AreEqual("FunctionName", result.Functions[0].FunctionName);
            Assert.AreEqual(2, result.Functions[0].TrackerRef.Lines.Count);
            Assert.AreEqual("\"Method\" = Variable.None(True);", result.Functions[0].TrackerRef.Lines[0].PSCCode);
            string source = PexSourceRenderer.Render(result, false, CodeGenStyle.Papyrus);
            StringAssert.Contains(source, "ScriptName ObjectName Extends ObjectName");
            StringAssert.Contains(source, "Int Function FunctionName(Int Variable)");
            PapyrusAsmDecoder.ObjType objectType = PapyrusAsmDecoder.ObjType.Null;
            object legacyVariable = decoder.QueryAnyByID(4, ref objectType);
            Assert.AreEqual(PapyrusAsmDecoder.ObjType.Variables, objectType);
            Assert.IsInstanceOfType(legacyVariable, typeof(PexReader.PexVariable));
        }

        private static PexDocumentModel CreateModel()
        {
            var strings = new[]
            {
                new PexModelString(0, "ObjectName"),
                new PexModelString(1, "Grüße 東京"),
                new PexModelString(2, "FunctionName"),
                new PexModelString(3, "Int"),
                new PexModelString(4, "Variable"),
                new PexModelString(5, "Method"),
                new PexModelString(6, "Flag")
            };
            var instructions = new[]
            {
                new PexModelInstruction("callmethod", new[]
                {
                    new PexModelInstructionArgument(0, null),
                    new PexModelInstructionArgument(1, (ushort)4),
                    new PexModelInstructionArgument(2, (ushort)5),
                    new PexModelInstructionArgument(3, 1),
                    new PexModelInstructionArgument(5, true)
                }),
                new PexModelInstruction("fadd", new[]
                {
                    new PexModelInstructionArgument(4, 1.5f),
                    new PexModelInstructionArgument(4, -2.0f),
                    new PexModelInstructionArgument(4, 0.5f)
                })
            };
            var function = new PexModelFunction(
                2,
                3,
                0,
                new[] { new PexModelFunctionParameter(4, 3) },
                instructions);
            var modelObject = new PexModelObject(
                0,
                0,
                1,
                new[] { new PexModelVariable(0, 4, 3, 7) },
                new[]
                {
                    new PexModelProperty(4, 3, 0),
                    new PexModelProperty(5, 3, 4)
                },
                new[] { new PexModelState(1, new[] { function }) });
            return new PexDocumentModel(strings, new[] { modelObject }, Array.Empty<ushort>());
        }
    }
}
