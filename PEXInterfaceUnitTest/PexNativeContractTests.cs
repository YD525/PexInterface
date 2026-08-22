using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PexInterface;

namespace PEXInterfaceUnitTest
{
    /// <summary>
    /// Verifies the exact managed projection of the canonical PEX native ABI.
    /// </summary>
    [TestClass]
    public sealed class PexNativeContractTests
    {
        /// <summary>
        /// Verifies version negotiation and clear rejection of unsupported ABI versions.
        /// </summary>
        [TestMethod]
        public void NegotiatesAndRejectsAbiVersionsExplicitly()
        {
            Assert.AreEqual(PexNativeContract.SupportedAbiVersion, PexNativeContract.AbiVersion);

            NotSupportedException missingVersion = AssertThrows<NotSupportedException>(
                () => PexNativeContract.ValidateAbiVersion(0));
            StringAssert.Contains(missingVersion.Message, "ABI version 0");

            NotSupportedException futureVersion = AssertThrows<NotSupportedException>(
                () => PexNativeContract.ValidateAbiVersion(2));
            StringAssert.Contains(futureVersion.Message, "ABI version 2");
        }

        /// <summary>
        /// Verifies value layout, all signatures, and cdecl metadata against <c>PexReaderApi.h</c>.
        /// </summary>
        [TestMethod]
        public void NativeDeclarationsMatchCanonicalHeader()
        {
            Assert.AreEqual(typeof(int), Enum.GetUnderlyingType(typeof(PexNativeStatus)));
            Assert.AreEqual(4, Marshal.SizeOf(typeof(PexNativeValue)));
            Assert.AreEqual(LayoutKind.Explicit, typeof(PexNativeValue).StructLayoutAttribute.Value);
            Assert.AreEqual(4, typeof(PexNativeValue).StructLayoutAttribute.Pack);
            Assert.AreEqual(4, typeof(PexNativeValue).StructLayoutAttribute.Size);
            Assert.AreEqual(IntPtr.Zero, Marshal.OffsetOf(typeof(PexNativeValue), "StringTableIndex"));
            Assert.AreEqual(IntPtr.Zero, Marshal.OffsetOf(typeof(PexNativeValue), "Integer"));
            Assert.AreEqual(IntPtr.Zero, Marshal.OffsetOf(typeof(PexNativeValue), "Real"));
            Assert.AreEqual(IntPtr.Zero, Marshal.OffsetOf(typeof(PexNativeValue), "Boolean"));

            Type byRefUShort = typeof(ushort).MakeByRefType();
            Type byRefByte = typeof(byte).MakeByRefType();
            Type byRefUInt = typeof(uint).MakeByRefType();
            Type byRefInt = typeof(int).MakeByRefType();
            Type byRefIntPtr = typeof(IntPtr).MakeByRefType();

            AssertSignature("C_GetAbiVersion", typeof(uint));
            AssertSignature("C_GetLastStatus", typeof(PexNativeStatus));
            AssertSignature("C_GetLastErrorUtf8", typeof(int), typeof(byte[]), typeof(int));
            AssertSignature("C_GetVersion", typeof(IntPtr));
            AssertSignature("C_GetVersionLength", typeof(int));
            AssertSignature("C_CreateInstance", typeof(IntPtr));
            AssertSignature("C_DestroyInstance", typeof(void), typeof(IntPtr));
            AssertSignature("C_ReadPex", typeof(int), typeof(IntPtr), typeof(string));
            AssertSignature("C_ModifyStringTable", typeof(int), typeof(IntPtr), typeof(ushort), typeof(IntPtr));
            AssertSignature("C_SavePex", typeof(int), typeof(IntPtr), typeof(string));
            AssertSignature("C_Close", typeof(void), typeof(IntPtr));
            AssertSignature("C_GetHeaderSourceFileName", typeof(IntPtr), typeof(IntPtr));
            AssertSignature("C_GetHeaderUsername", typeof(IntPtr), typeof(IntPtr));
            AssertSignature("C_GetHeaderMachineName", typeof(IntPtr), typeof(IntPtr));
            AssertSignature("C_GetHeaderMagic", typeof(uint), typeof(IntPtr));
            AssertSignature("C_GetHeaderMajorVersion", typeof(byte), typeof(IntPtr));
            AssertSignature("C_GetHeaderMinorVersion", typeof(byte), typeof(IntPtr));
            AssertSignature("C_GetHeaderGameId", typeof(ushort), typeof(IntPtr));
            AssertSignature("C_GetHeaderCompilationTime", typeof(ulong), typeof(IntPtr));
            AssertSignature("C_GetStringTableCount", typeof(ushort), typeof(IntPtr));
            AssertSignature(
                "C_GetStringUtf8",
                typeof(int),
                typeof(IntPtr),
                typeof(ushort),
                typeof(byte[]),
                typeof(int));
            AssertSignature(
                "C_GetStringWide",
                typeof(int),
                typeof(IntPtr),
                typeof(ushort),
                typeof(char[]),
                typeof(int));
            AssertSignature("C_HasDebugInfo", typeof(byte), typeof(IntPtr));
            AssertSignature("C_GetDebugModificationTime", typeof(ulong), typeof(IntPtr));
            AssertSignature("C_GetDebugFunctionCount", typeof(ushort), typeof(IntPtr));
            AssertSignature(
                "C_GetDebugFunctionInfo",
                typeof(int),
                typeof(IntPtr),
                typeof(ushort),
                byRefUShort,
                byRefUShort,
                byRefUShort,
                byRefByte,
                byRefIntPtr,
                byRefInt);
            AssertSignature("C_GetUserFlagCount", typeof(ushort), typeof(IntPtr));
            AssertSignature("C_GetUserFlagInfo", typeof(int), typeof(IntPtr), typeof(ushort), byRefUShort, byRefByte);
            AssertSignature("C_GetObjectCount", typeof(ushort), typeof(IntPtr));
            AssertSignature("C_GetObjectInfo", typeof(int), typeof(IntPtr), typeof(ushort), byRefUShort, byRefUInt);
            AssertSignature(
                "C_GetObjectData",
                typeof(int),
                typeof(IntPtr),
                typeof(ushort),
                byRefUShort,
                byRefUShort,
                byRefUInt,
                byRefUShort);
            AssertSignature("C_GetVariableCount", typeof(ushort), typeof(IntPtr), typeof(ushort));
            AssertSignature(
                "C_GetVariableInfo",
                typeof(int),
                typeof(IntPtr),
                typeof(ushort),
                typeof(ushort),
                byRefUShort,
                byRefUShort,
                byRefUInt,
                byRefByte,
                typeof(IntPtr));
            AssertSignature("C_GetPropertyCount", typeof(ushort), typeof(IntPtr), typeof(ushort));
            AssertSignature(
                "C_GetPropertyInfo",
                typeof(int),
                typeof(IntPtr),
                typeof(ushort),
                typeof(ushort),
                byRefUShort,
                byRefUShort,
                byRefUShort,
                byRefUInt,
                byRefByte,
                byRefUShort);
            AssertSignature("C_GetStateCount", typeof(ushort), typeof(IntPtr), typeof(ushort));
            AssertSignature(
                "C_GetStateInfo",
                typeof(int),
                typeof(IntPtr),
                typeof(ushort),
                typeof(ushort),
                byRefUShort,
                byRefUShort);
            AssertSignature(
                "C_GetStateFunctionInfo",
                typeof(int),
                typeof(IntPtr),
                typeof(ushort),
                typeof(ushort),
                typeof(ushort),
                byRefUShort,
                byRefUShort,
                byRefUShort,
                byRefUInt,
                byRefByte,
                byRefUShort,
                byRefUShort,
                byRefUShort);
            AssertSignature(
                "C_GetInstructionInfo",
                typeof(int),
                typeof(IntPtr),
                typeof(ushort),
                typeof(ushort),
                typeof(ushort),
                typeof(ushort),
                byRefByte,
                byRefUShort);
            AssertSignature(
                "C_GetInstructionArgument",
                typeof(int),
                typeof(IntPtr),
                typeof(ushort),
                typeof(ushort),
                typeof(ushort),
                typeof(ushort),
                typeof(ushort),
                byRefByte,
                typeof(IntPtr));
            AssertSignature(
                "C_GetFunctionParamCount",
                typeof(ushort),
                typeof(IntPtr),
                typeof(ushort),
                typeof(ushort),
                typeof(ushort));
            AssertSignature(
                "C_GetFunctionParamInfo",
                typeof(int),
                typeof(IntPtr),
                typeof(ushort),
                typeof(ushort),
                typeof(ushort),
                typeof(ushort),
                byRefUShort,
                byRefUShort);
            AssertSignature(
                "C_GetFunctionLocalCount",
                typeof(ushort),
                typeof(IntPtr),
                typeof(ushort),
                typeof(ushort),
                typeof(ushort));
            AssertSignature(
                "C_GetFunctionLocalInfo",
                typeof(int),
                typeof(IntPtr),
                typeof(ushort),
                typeof(ushort),
                typeof(ushort),
                typeof(ushort),
                byRefUShort,
                byRefUShort);
            AssertSignature("C_FreeBuffer", typeof(void), typeof(IntPtr));

            MethodInfo[] imports = typeof(PexNativeMethods)
                .GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
                .Where(method => method.GetCustomAttribute<DllImportAttribute>() != null)
                .ToArray();
            Assert.AreEqual(45, imports.Length);
            foreach (MethodInfo method in imports)
            {
                DllImportAttribute import = method.GetCustomAttribute<DllImportAttribute>();
                Assert.AreEqual(PexNativeMethods.DllName, import.Value, method.Name);
                Assert.AreEqual(CallingConvention.Cdecl, import.CallingConvention, method.Name);
                Assert.IsTrue(import.ExactSpelling, method.Name);
            }

            Assert.AreEqual(CharSet.Unicode, GetImport("C_ReadPex").CharSet);
            Assert.AreEqual(CharSet.Unicode, GetImport("C_SavePex").CharSet);
            Assert.AreEqual(CharSet.Unicode, GetImport("C_GetStringWide").CharSet);
            Assert.AreEqual(
                UnmanagedType.LPWStr,
                GetMethod("C_ReadPex").GetParameters()[1].GetCustomAttribute<MarshalAsAttribute>().Value);
            Assert.AreEqual(
                UnmanagedType.LPWStr,
                GetMethod("C_SavePex").GetParameters()[1].GetCustomAttribute<MarshalAsAttribute>().Value);
            Assert.IsNotNull(
                GetMethod("C_GetLastErrorUtf8").GetParameters()[0].GetCustomAttribute<OutAttribute>());
            Assert.IsNotNull(
                GetMethod("C_GetStringUtf8").GetParameters()[2].GetCustomAttribute<OutAttribute>());
            Assert.IsNotNull(
                GetMethod("C_GetStringWide").GetParameters()[2].GetCustomAttribute<OutAttribute>());

            bool publicFacadeContainsImports = typeof(PexInterop)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Any(method => method.GetCustomAttribute<DllImportAttribute>() != null);
            Assert.IsFalse(publicFacadeContainsImports);

            MethodInfo[] legacyFacadeMethods = typeof(PexInterop)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(method => method.Name.StartsWith("C_", StringComparison.Ordinal))
                .ToArray();
            Assert.AreEqual(42, legacyFacadeMethods.Length);
            foreach (MethodInfo nativeMethod in imports.Where(method =>
                method.Name != "C_GetAbiVersion" &&
                method.Name != "C_GetLastStatus" &&
                method.Name != "C_GetLastErrorUtf8"))
            {
                Type[] parameterTypes = nativeMethod
                    .GetParameters()
                    .Select(parameter => parameter.ParameterType)
                    .ToArray();
                MethodInfo facadeMethod = typeof(PexInterop).GetMethod(
                    nativeMethod.Name,
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    parameterTypes,
                    null);
                Assert.IsNotNull(facadeMethod, nativeMethod.Name);
                Assert.AreEqual(nativeMethod.ReturnType, facadeMethod.ReturnType, nativeMethod.Name);
            }
        }

        /// <summary>
        /// Verifies native status and UTF-8 error translation without leaking implementation paths.
        /// </summary>
        [TestMethod]
        public void TranslatesNativeStatusAndErrorText()
        {
            Assert.AreEqual(0, PexNativeMethods.C_ReadPex(IntPtr.Zero, "ignored.pex"));
            Assert.AreEqual(PexNativeStatus.InvalidArgument, PexNativeMethods.C_GetLastStatus());

            int length = PexNativeMethods.C_GetLastErrorUtf8(null, 0);
            Assert.IsTrue(length > 0);
            byte[] buffer = new byte[length + 1];
            Assert.AreEqual(length, PexNativeMethods.C_GetLastErrorUtf8(buffer, buffer.Length));
            Assert.AreEqual(0, buffer[length]);

            Exception translated = PexNativeContract.CreateException("load the PEX file");
            Assert.IsInstanceOfType(translated, typeof(ArgumentException));
            StringAssert.Contains(translated.Message, "handle is null");
            Assert.IsFalse(translated.Message.Contains(":\\"));
        }

        /// <summary>
        /// Verifies that embedded null characters cannot silently truncate native UTF-8 input.
        /// </summary>
        [TestMethod]
        public void RejectsEmbeddedNullInUtf8Input()
        {
            bool actionCalled = false;
            AssertThrows<ArgumentException>(
                () => PexNativeContract.WithUtf8String(
                    "prefix\0suffix",
                    pointer =>
                    {
                        actionCalled = true;
                        return pointer == IntPtr.Zero ? 0 : 1;
                    }));
            Assert.IsFalse(actionCalled);
        }

        /// <summary>
        /// Verifies that the public reader stores native ownership only in a safe handle.
        /// </summary>
        [TestMethod]
        public void ReaderUsesSafeHandleInsteadOfRawPointerOwnership()
        {
            FieldInfo[] fields = typeof(PexReader).GetFields(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsFalse(fields.Any(field => field.FieldType == typeof(IntPtr)));
            Assert.AreEqual(1, fields.Count(field => field.FieldType == typeof(PexInstanceSafeHandle)));
        }

        private static void AssertSignature(string name, Type returnType, params Type[] parameterTypes)
        {
            MethodInfo method = GetMethod(name);
            Assert.AreEqual(returnType, method.ReturnType, name);
            CollectionAssert.AreEqual(
                parameterTypes,
                method.GetParameters().Select(parameter => parameter.ParameterType).ToArray(),
                name);
        }

        private static MethodInfo GetMethod(string name)
        {
            MethodInfo method = typeof(PexNativeMethods).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(method, name);
            return method;
        }

        private static DllImportAttribute GetImport(string name)
        {
            DllImportAttribute import = GetMethod(name).GetCustomAttribute<DllImportAttribute>();
            Assert.IsNotNull(import, name);
            return import;
        }

        private static TException AssertThrows<TException>(Action action)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException exception)
            {
                return exception;
            }

            Assert.Fail("Expected " + typeof(TException).Name + ".");
            return null;
        }
    }
}
