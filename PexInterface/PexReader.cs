using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace PexInterface
{
    // Copyright (c) 2025 YD525, Cutleast
    // Licensed under the LGPL3.0 License.

    /// <summary>
    /// Preserves the legacy low-level PEX API while forwarding every call to the validated internal interop boundary.
    /// </summary>
    public static class PexInterop
    {
        /// <summary>Stores the detected native product version for compatibility with existing callers.</summary>
        public static string Version = "";

        #region Legacy native compatibility surface

        /// <summary>Returns the borrowed native product-version pointer.</summary>
        /// <returns>A borrowed UTF-8 pointer.</returns>
        public static IntPtr C_GetVersion()
        {
            PexNativeContract.EnsureCompatible();
            return PexNativeMethods.C_GetVersion();
        }

        /// <summary>Returns the native product-version payload length.</summary>
        /// <returns>The byte length excluding the null terminator.</returns>
        public static int C_GetVersionLength()
        {
            PexNativeContract.EnsureCompatible();
            return PexNativeMethods.C_GetVersionLength();
        }

        /// <summary>Creates an owned native PEX reader instance.</summary>
        /// <returns>An owned pointer-sized handle, or zero on failure.</returns>
        public static IntPtr C_CreateInstance()
        {
            PexNativeContract.EnsureCompatible();
            return PexNativeMethods.C_CreateInstance();
        }

        /// <summary>Destroys an owned native PEX reader instance.</summary>
        /// <param name="handle">The nullable owned handle.</param>
        public static void C_DestroyInstance(IntPtr handle)
        {
            PexNativeContract.EnsureCompatible();
            PexNativeMethods.C_DestroyInstance(handle);
        }

        /// <summary>Loads a PEX file through the legacy raw-handle API.</summary>
        /// <param name="handle">The non-null borrowed handle.</param>
        /// <param name="pexPath">The non-null UTF-16 input path.</param>
        /// <returns>One on success; otherwise, zero.</returns>
        public static int C_ReadPex(IntPtr handle, string pexPath)
        {
            PexNativeContract.EnsureCompatible();
            return PexNativeMethods.C_ReadPex(handle, pexPath);
        }

        /// <summary>Replaces a PEX string through the legacy raw-pointer API.</summary>
        /// <param name="handle">The non-null borrowed handle.</param>
        /// <param name="index">The string-table index.</param>
        /// <param name="utf8Str">The non-null UTF-8 pointer.</param>
        /// <returns>One on success; otherwise, zero.</returns>
        public static int C_ModifyStringTable(IntPtr handle, ushort index, IntPtr utf8Str)
        {
            PexNativeContract.EnsureCompatible();
            return PexNativeMethods.C_ModifyStringTable(handle, index, utf8Str);
        }

        /// <summary>Saves PEX data through the legacy raw-handle API.</summary>
        /// <param name="handle">The non-null borrowed handle.</param>
        /// <param name="pexPath">The non-null UTF-16 output path.</param>
        /// <returns>One on success; otherwise, zero.</returns>
        public static int C_SavePex(IntPtr handle, string pexPath)
        {
            PexNativeContract.EnsureCompatible();
            return PexNativeMethods.C_SavePex(handle, pexPath);
        }

        /// <summary>Resets a native reader without ending its handle lifetime.</summary>
        /// <param name="handle">The nullable borrowed handle.</param>
        public static void C_Close(IntPtr handle)
        {
            PexNativeContract.EnsureCompatible();
            PexNativeMethods.C_Close(handle);
        }

        /// <summary>Returns the borrowed UTF-16 source-file name.</summary>
        /// <param name="handle">The non-null borrowed handle.</param>
        /// <returns>A borrowed UTF-16 pointer.</returns>
        public static IntPtr C_GetHeaderSourceFileName(IntPtr handle)
        {
            PexNativeContract.EnsureCompatible();
            return PexNativeMethods.C_GetHeaderSourceFileName(handle);
        }

        /// <summary>Returns the borrowed UTF-16 compiler username.</summary>
        /// <param name="handle">The non-null borrowed handle.</param>
        /// <returns>A borrowed UTF-16 pointer.</returns>
        public static IntPtr C_GetHeaderUsername(IntPtr handle)
        {
            PexNativeContract.EnsureCompatible();
            return PexNativeMethods.C_GetHeaderUsername(handle);
        }

        /// <summary>Returns the borrowed UTF-16 compiler machine name.</summary>
        /// <param name="handle">The non-null borrowed handle.</param>
        /// <returns>A borrowed UTF-16 pointer.</returns>
        public static IntPtr C_GetHeaderMachineName(IntPtr handle)
        {
            PexNativeContract.EnsureCompatible();
            return PexNativeMethods.C_GetHeaderMachineName(handle);
        }

        /// <summary>Returns the PEX header magic value.</summary>
        /// <param name="handle">The non-null borrowed handle.</param>
        /// <returns>The unsigned 32-bit magic value.</returns>
        public static uint C_GetHeaderMagic(IntPtr handle)
        {
            PexNativeContract.EnsureCompatible();
            return PexNativeMethods.C_GetHeaderMagic(handle);
        }

        /// <summary>Returns the PEX header major version.</summary>
        /// <param name="handle">The non-null borrowed handle.</param>
        /// <returns>The unsigned 8-bit major version.</returns>
        public static byte C_GetHeaderMajorVersion(IntPtr handle)
        {
            PexNativeContract.EnsureCompatible();
            return PexNativeMethods.C_GetHeaderMajorVersion(handle);
        }

        /// <summary>Returns the PEX header minor version.</summary>
        /// <param name="handle">The non-null borrowed handle.</param>
        /// <returns>The unsigned 8-bit minor version.</returns>
        public static byte C_GetHeaderMinorVersion(IntPtr handle)
        {
            PexNativeContract.EnsureCompatible();
            return PexNativeMethods.C_GetHeaderMinorVersion(handle);
        }

        /// <summary>Returns the PEX header game identifier.</summary>
        /// <param name="handle">The non-null borrowed handle.</param>
        /// <returns>The unsigned 16-bit game identifier.</returns>
        public static ushort C_GetHeaderGameId(IntPtr handle)
        {
            PexNativeContract.EnsureCompatible();
            return PexNativeMethods.C_GetHeaderGameId(handle);
        }

        /// <summary>Returns the PEX compilation timestamp.</summary>
        /// <param name="handle">The non-null borrowed handle.</param>
        /// <returns>The unsigned 64-bit timestamp.</returns>
        public static ulong C_GetHeaderCompilationTime(IntPtr handle)
        {
            PexNativeContract.EnsureCompatible();
            return PexNativeMethods.C_GetHeaderCompilationTime(handle);
        }

        /// <summary>Returns the native string-table count.</summary>
        /// <param name="handle">The non-null borrowed handle.</param>
        /// <returns>The unsigned 16-bit entry count.</returns>
        public static ushort C_GetStringTableCount(IntPtr handle)
        {
            PexNativeContract.EnsureCompatible();
            return PexNativeMethods.C_GetStringTableCount(handle);
        }

        /// <summary>Queries or copies a UTF-8 string-table entry.</summary>
        /// <param name="handle">The non-null borrowed handle.</param>
        /// <param name="index">The string-table index.</param>
        /// <param name="buffer">The optional caller-owned byte buffer.</param>
        /// <param name="bufferSize">The byte capacity of <paramref name="buffer"/>.</param>
        /// <returns>The payload length, or minus one on failure.</returns>
        public static int C_GetStringUtf8(IntPtr handle, ushort index, byte[] buffer, int bufferSize)
        {
            PexNativeContract.EnsureCompatible();
            return PexNativeMethods.C_GetStringUtf8(handle, index, buffer, bufferSize);
        }

        /// <summary>Queries or copies a UTF-16 string-table entry.</summary>
        /// <param name="handle">The non-null borrowed handle.</param>
        /// <param name="index">The string-table index.</param>
        /// <param name="buffer">The optional caller-owned character buffer.</param>
        /// <param name="bufferSize">The character capacity of <paramref name="buffer"/>.</param>
        /// <returns>The payload length, or minus one on failure.</returns>
        public static int C_GetStringWide(IntPtr handle, ushort index, char[] buffer, int bufferSize)
        {
            PexNativeContract.EnsureCompatible();
            return PexNativeMethods.C_GetStringWide(handle, index, buffer, bufferSize);
        }

        /// <summary>Reports whether debug information is available.</summary>
        /// <param name="handle">The non-null borrowed handle.</param>
        /// <returns>One when debug information exists; otherwise, zero.</returns>
        public static byte C_HasDebugInfo(IntPtr handle)
        {
            PexNativeContract.EnsureCompatible();
            return PexNativeMethods.C_HasDebugInfo(handle);
        }

        /// <summary>Returns the debug modification timestamp.</summary>
        /// <param name="handle">The non-null borrowed handle.</param>
        /// <returns>The unsigned 64-bit timestamp.</returns>
        public static ulong C_GetDebugModificationTime(IntPtr handle)
        {
            PexNativeContract.EnsureCompatible();
            return PexNativeMethods.C_GetDebugModificationTime(handle);
        }

        /// <summary>Returns the debug-function count.</summary>
        /// <param name="handle">The non-null borrowed handle.</param>
        /// <returns>The unsigned 16-bit function count.</returns>
        public static ushort C_GetDebugFunctionCount(IntPtr handle)
        {
            PexNativeContract.EnsureCompatible();
            return PexNativeMethods.C_GetDebugFunctionCount(handle);
        }

        /// <summary>Returns debug-function metadata and an owned native line-number array.</summary>
        /// <param name="handle">The non-null borrowed handle.</param>
        /// <param name="index">The debug-function index.</param>
        /// <param name="objectNameIndex">Receives the object-name string index.</param>
        /// <param name="stateNameIndex">Receives the state-name string index.</param>
        /// <param name="functionNameIndex">Receives the function-name string index.</param>
        /// <param name="functionType">Receives the function type.</param>
        /// <param name="lineNumbers">Receives an owned native array pointer.</param>
        /// <param name="lineCount">Receives the unsigned 16-bit element count.</param>
        /// <returns>One on success; otherwise, zero.</returns>
        public static int C_GetDebugFunctionInfo(
            IntPtr handle,
            ushort index,
            out ushort objectNameIndex,
            out ushort stateNameIndex,
            out ushort functionNameIndex,
            out byte functionType,
            out IntPtr lineNumbers,
            out int lineCount)
        {
            PexNativeContract.EnsureCompatible();
            return PexNativeMethods.C_GetDebugFunctionInfo(
                handle,
                index,
                out objectNameIndex,
                out stateNameIndex,
                out functionNameIndex,
                out functionType,
                out lineNumbers,
                out lineCount);
        }

        /// <summary>Returns the user-flag count.</summary>
        /// <param name="handle">The non-null borrowed handle.</param>
        /// <returns>The unsigned 16-bit flag count.</returns>
        public static ushort C_GetUserFlagCount(IntPtr handle)
        {
            PexNativeContract.EnsureCompatible();
            return PexNativeMethods.C_GetUserFlagCount(handle);
        }

        /// <summary>Returns one user-flag entry.</summary>
        /// <param name="handle">The non-null borrowed handle.</param>
        /// <param name="index">The user-flag index.</param>
        /// <param name="flagNameIndex">Receives the flag-name string index.</param>
        /// <param name="flagIndex">Receives the bit index.</param>
        /// <returns>One on success; otherwise, zero.</returns>
        public static int C_GetUserFlagInfo(
            IntPtr handle,
            ushort index,
            out ushort flagNameIndex,
            out byte flagIndex)
        {
            PexNativeContract.EnsureCompatible();
            return PexNativeMethods.C_GetUserFlagInfo(handle, index, out flagNameIndex, out flagIndex);
        }

        /// <summary>Returns the PEX object count.</summary>
        /// <param name="handle">The non-null borrowed handle.</param>
        /// <returns>The unsigned 16-bit object count.</returns>
        public static ushort C_GetObjectCount(IntPtr handle)
        {
            PexNativeContract.EnsureCompatible();
            return PexNativeMethods.C_GetObjectCount(handle);
        }

        /// <summary>Returns object-level metadata.</summary>
        /// <param name="handle">The non-null borrowed handle.</param>
        /// <param name="index">The object index.</param>
        /// <param name="nameIndex">Receives the name string index.</param>
        /// <param name="size">Receives the encoded object size.</param>
        /// <returns>One on success; otherwise, zero.</returns>
        public static int C_GetObjectInfo(IntPtr handle, ushort index, out ushort nameIndex, out uint size)
        {
            PexNativeContract.EnsureCompatible();
            return PexNativeMethods.C_GetObjectInfo(handle, index, out nameIndex, out size);
        }

        /// <summary>Returns object-body metadata.</summary>
        /// <param name="handle">The non-null borrowed handle.</param>
        /// <param name="objectIndex">The object index.</param>
        /// <param name="parentClassName">Receives the parent-class string index.</param>
        /// <param name="docString">Receives the documentation string index.</param>
        /// <param name="userFlags">Receives the user flags.</param>
        /// <param name="autoStateName">Receives the auto-state string index.</param>
        /// <returns>One on success; otherwise, zero.</returns>
        public static int C_GetObjectData(
            IntPtr handle,
            ushort objectIndex,
            out ushort parentClassName,
            out ushort docString,
            out uint userFlags,
            out ushort autoStateName)
        {
            PexNativeContract.EnsureCompatible();
            return PexNativeMethods.C_GetObjectData(
                handle,
                objectIndex,
                out parentClassName,
                out docString,
                out userFlags,
                out autoStateName);
        }

        /// <summary>Returns the variable count for an object.</summary>
        /// <param name="handle">The non-null borrowed handle.</param>
        /// <param name="objectIndex">The object index.</param>
        /// <returns>The unsigned 16-bit variable count.</returns>
        public static ushort C_GetVariableCount(IntPtr handle, ushort objectIndex)
        {
            PexNativeContract.EnsureCompatible();
            return PexNativeMethods.C_GetVariableCount(handle, objectIndex);
        }

        /// <summary>Returns variable metadata and an optional four-byte value.</summary>
        /// <param name="handle">The non-null borrowed handle.</param>
        /// <param name="objectIndex">The object index.</param>
        /// <param name="varIndex">The variable index.</param>
        /// <param name="name">Receives the name string index.</param>
        /// <param name="typeName">Receives the type-name string index.</param>
        /// <param name="userFlags">Receives the user flags.</param>
        /// <param name="dataType">Receives the PEX value type.</param>
        /// <param name="dataValue">Receives the optional caller-owned value pointer.</param>
        /// <returns>One on success; otherwise, zero.</returns>
        public static int C_GetVariableInfo(
            IntPtr handle,
            ushort objectIndex,
            ushort varIndex,
            out ushort name,
            out ushort typeName,
            out uint userFlags,
            out byte dataType,
            IntPtr dataValue)
        {
            PexNativeContract.EnsureCompatible();
            return PexNativeMethods.C_GetVariableInfo(
                handle,
                objectIndex,
                varIndex,
                out name,
                out typeName,
                out userFlags,
                out dataType,
                dataValue);
        }

        /// <summary>Returns the property count for an object.</summary>
        /// <param name="handle">The non-null borrowed handle.</param>
        /// <param name="objectIndex">The object index.</param>
        /// <returns>The unsigned 16-bit property count.</returns>
        public static ushort C_GetPropertyCount(IntPtr handle, ushort objectIndex)
        {
            PexNativeContract.EnsureCompatible();
            return PexNativeMethods.C_GetPropertyCount(handle, objectIndex);
        }

        /// <summary>Returns property metadata.</summary>
        /// <param name="handle">The non-null borrowed handle.</param>
        /// <param name="objectIndex">The object index.</param>
        /// <param name="propIndex">The property index.</param>
        /// <param name="name">Receives the name string index.</param>
        /// <param name="type">Receives the type string index.</param>
        /// <param name="docstring">Receives the documentation string index.</param>
        /// <param name="userFlags">Receives the user flags.</param>
        /// <param name="flags">Receives the property flags.</param>
        /// <param name="autoVarName">Receives the auto-variable string index.</param>
        /// <returns>One on success; otherwise, zero.</returns>
        public static int C_GetPropertyInfo(
            IntPtr handle,
            ushort objectIndex,
            ushort propIndex,
            out ushort name,
            out ushort type,
            out ushort docstring,
            out uint userFlags,
            out byte flags,
            out ushort autoVarName)
        {
            PexNativeContract.EnsureCompatible();
            return PexNativeMethods.C_GetPropertyInfo(
                handle,
                objectIndex,
                propIndex,
                out name,
                out type,
                out docstring,
                out userFlags,
                out flags,
                out autoVarName);
        }

        /// <summary>Returns the state count for an object.</summary>
        /// <param name="handle">The non-null borrowed handle.</param>
        /// <param name="objectIndex">The object index.</param>
        /// <returns>The unsigned 16-bit state count.</returns>
        public static ushort C_GetStateCount(IntPtr handle, ushort objectIndex)
        {
            PexNativeContract.EnsureCompatible();
            return PexNativeMethods.C_GetStateCount(handle, objectIndex);
        }

        /// <summary>Returns state metadata.</summary>
        /// <param name="handle">The non-null borrowed handle.</param>
        /// <param name="objectIndex">The object index.</param>
        /// <param name="stateIndex">The state index.</param>
        /// <param name="name">Receives the state-name string index.</param>
        /// <param name="numFunctions">Receives the function count.</param>
        /// <returns>One on success; otherwise, zero.</returns>
        public static int C_GetStateInfo(
            IntPtr handle,
            ushort objectIndex,
            ushort stateIndex,
            out ushort name,
            out ushort numFunctions)
        {
            PexNativeContract.EnsureCompatible();
            return PexNativeMethods.C_GetStateInfo(
                handle,
                objectIndex,
                stateIndex,
                out name,
                out numFunctions);
        }

        /// <summary>Returns state-function metadata.</summary>
        /// <param name="handle">The non-null borrowed handle.</param>
        /// <param name="objectIndex">The object index.</param>
        /// <param name="stateIndex">The state index.</param>
        /// <param name="funcIndex">The function index.</param>
        /// <param name="functionName">Receives the function-name string index.</param>
        /// <param name="returnType">Receives the return-type string index.</param>
        /// <param name="docString">Receives the documentation string index.</param>
        /// <param name="userFlags">Receives the user flags.</param>
        /// <param name="flags">Receives the function flags.</param>
        /// <param name="numParams">Receives the parameter count.</param>
        /// <param name="numLocals">Receives the local-variable count.</param>
        /// <param name="numInstructions">Receives the instruction count.</param>
        /// <returns>One on success; otherwise, zero.</returns>
        public static int C_GetStateFunctionInfo(
            IntPtr handle,
            ushort objectIndex,
            ushort stateIndex,
            ushort funcIndex,
            out ushort functionName,
            out ushort returnType,
            out ushort docString,
            out uint userFlags,
            out byte flags,
            out ushort numParams,
            out ushort numLocals,
            out ushort numInstructions)
        {
            PexNativeContract.EnsureCompatible();
            return PexNativeMethods.C_GetStateFunctionInfo(
                handle,
                objectIndex,
                stateIndex,
                funcIndex,
                out functionName,
                out returnType,
                out docString,
                out userFlags,
                out flags,
                out numParams,
                out numLocals,
                out numInstructions);
        }

        /// <summary>Returns a function's parameter count.</summary>
        /// <param name="handle">The non-null borrowed handle.</param>
        /// <param name="objectIndex">The object index.</param>
        /// <param name="stateIndex">The state index.</param>
        /// <param name="funcIndex">The function index.</param>
        /// <returns>The unsigned 16-bit parameter count.</returns>
        public static ushort C_GetFunctionParamCount(
            IntPtr handle,
            ushort objectIndex,
            ushort stateIndex,
            ushort funcIndex)
        {
            PexNativeContract.EnsureCompatible();
            return PexNativeMethods.C_GetFunctionParamCount(handle, objectIndex, stateIndex, funcIndex);
        }

        /// <summary>Returns function-parameter metadata.</summary>
        /// <param name="handle">The non-null borrowed handle.</param>
        /// <param name="objectIndex">The object index.</param>
        /// <param name="stateIndex">The state index.</param>
        /// <param name="funcIndex">The function index.</param>
        /// <param name="paramIndex">The parameter index.</param>
        /// <param name="name">Receives the name string index.</param>
        /// <param name="type">Receives the type string index.</param>
        /// <returns>One on success; otherwise, zero.</returns>
        public static int C_GetFunctionParamInfo(
            IntPtr handle,
            ushort objectIndex,
            ushort stateIndex,
            ushort funcIndex,
            ushort paramIndex,
            out ushort name,
            out ushort type)
        {
            PexNativeContract.EnsureCompatible();
            return PexNativeMethods.C_GetFunctionParamInfo(
                handle,
                objectIndex,
                stateIndex,
                funcIndex,
                paramIndex,
                out name,
                out type);
        }

        /// <summary>Returns a function's local-variable count.</summary>
        /// <param name="handle">The non-null borrowed handle.</param>
        /// <param name="objectIndex">The object index.</param>
        /// <param name="stateIndex">The state index.</param>
        /// <param name="funcIndex">The function index.</param>
        /// <returns>The unsigned 16-bit local-variable count.</returns>
        public static ushort C_GetFunctionLocalCount(
            IntPtr handle,
            ushort objectIndex,
            ushort stateIndex,
            ushort funcIndex)
        {
            PexNativeContract.EnsureCompatible();
            return PexNativeMethods.C_GetFunctionLocalCount(handle, objectIndex, stateIndex, funcIndex);
        }

        /// <summary>Returns function-local metadata.</summary>
        /// <param name="handle">The non-null borrowed handle.</param>
        /// <param name="objectIndex">The object index.</param>
        /// <param name="stateIndex">The state index.</param>
        /// <param name="funcIndex">The function index.</param>
        /// <param name="localIndex">The local-variable index.</param>
        /// <param name="name">Receives the name string index.</param>
        /// <param name="type">Receives the type string index.</param>
        /// <returns>One on success; otherwise, zero.</returns>
        public static int C_GetFunctionLocalInfo(
            IntPtr handle,
            ushort objectIndex,
            ushort stateIndex,
            ushort funcIndex,
            ushort localIndex,
            out ushort name,
            out ushort type)
        {
            PexNativeContract.EnsureCompatible();
            return PexNativeMethods.C_GetFunctionLocalInfo(
                handle,
                objectIndex,
                stateIndex,
                funcIndex,
                localIndex,
                out name,
                out type);
        }

        /// <summary>Returns instruction metadata.</summary>
        /// <param name="handle">The non-null borrowed handle.</param>
        /// <param name="objectIndex">The object index.</param>
        /// <param name="stateIndex">The state index.</param>
        /// <param name="funcIndex">The function index.</param>
        /// <param name="instrIndex">The instruction index.</param>
        /// <param name="opcode">Receives the opcode.</param>
        /// <param name="argCount">Receives the argument count.</param>
        /// <returns>One on success; otherwise, zero.</returns>
        public static int C_GetInstructionInfo(
            IntPtr handle,
            ushort objectIndex,
            ushort stateIndex,
            ushort funcIndex,
            ushort instrIndex,
            out byte opcode,
            out ushort argCount)
        {
            PexNativeContract.EnsureCompatible();
            return PexNativeMethods.C_GetInstructionInfo(
                handle,
                objectIndex,
                stateIndex,
                funcIndex,
                instrIndex,
                out opcode,
                out argCount);
        }

        /// <summary>Returns one instruction argument.</summary>
        /// <param name="handle">The non-null borrowed handle.</param>
        /// <param name="objectIndex">The object index.</param>
        /// <param name="stateIndex">The state index.</param>
        /// <param name="funcIndex">The function index.</param>
        /// <param name="instrIndex">The instruction index.</param>
        /// <param name="argIndex">The argument index.</param>
        /// <param name="type">Receives the PEX value type.</param>
        /// <param name="value">Receives the optional caller-owned value pointer.</param>
        /// <returns>One on success; otherwise, zero.</returns>
        public static int C_GetInstructionArgument(
            IntPtr handle,
            ushort objectIndex,
            ushort stateIndex,
            ushort funcIndex,
            ushort instrIndex,
            ushort argIndex,
            out byte type,
            IntPtr value)
        {
            PexNativeContract.EnsureCompatible();
            return PexNativeMethods.C_GetInstructionArgument(
                handle,
                objectIndex,
                stateIndex,
                funcIndex,
                instrIndex,
                argIndex,
                out type,
                value);
        }

        /// <summary>Releases a native line-number array with its producing allocator.</summary>
        /// <param name="buffer">The nullable owned native pointer.</param>
        public static void C_FreeBuffer(IntPtr buffer)
        {
            PexNativeContract.EnsureCompatible();
            PexNativeMethods.C_FreeBuffer(buffer);
        }

        #endregion

        /// <summary>
        /// Returns the validated native product version without exposing the borrowed version pointer.
        /// </summary>
        /// <returns>
        /// The product version, <c>Unknown</c>, or a compatibility error prefixed with <c>Error:</c>.
        /// </returns>
        public static string GetVersion()
        {
            try
            {
                return PexNativeContract.ReadProductVersion();
            }
            catch (Exception exception)
            {
                return "Error: " + exception.Message;
            }
        }

        static PexInterop()
        {
            Version = GetVersion();
        }
    }

    /// <summary>
    /// Reads, analyzes, modifies, and writes a PEX file through an owned native reader instance.
    /// </summary>
    /// <remarks>
    /// This type owns one native reader handle and is not thread-safe. Call <see cref="Dispose"/> when the
    /// reader is no longer needed. <see cref="Close"/> resets the reader to a new empty native instance.
    /// </remarks>
    public class PexReader : IDisposable
    {
        private PexInstanceSafeHandle _handle;
        private bool _disposed;

        public string PexPath { get; private set; } = "";
        public PexHeader Header { get; private set; } = new PexHeader();
        public List<PexString> StringTable { get; private set; } = new List<PexString>();
        public List<PexObject> Objects { get; private set; } = new List<PexObject>();
        public List<PexUserFlag> UserFlags { get; private set; } = new List<PexUserFlag>();
        public PexDebugInfo DebugInfo { get; private set; } = new PexDebugInfo();

        /// <summary>
        /// Creates a reader that owns a new native PEX instance.
        /// </summary>
        /// <exception cref="OutOfMemoryException">
        /// Thrown when the native module cannot allocate an instance.
        /// </exception>
        /// <exception cref="NotSupportedException">Thrown when the loaded native ABI is incompatible.</exception>
        public PexReader()
        {
            _handle = PexInstanceSafeHandle.Create();
        }

        /// <summary>
        /// Returns the borrowed native handle for compatibility with existing integrations.
        /// </summary>
        /// <returns>The native reader pointer owned by this instance.</returns>
        /// <remarks>
        /// The pointer remains valid only until <see cref="Close"/> or <see cref="Dispose"/> is called. The
        /// caller must not free, cache, or use it concurrently with other operations on this reader.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">Thrown after this reader has been disposed.</exception>
        public IntPtr GetHandle()
        {
            return NativeHandle;
        }

        /// <summary>
        /// Releases the owned native reader instance.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            _handle.Dispose();
            _disposed = true;
            GC.SuppressFinalize(this);
        }

        private void EnsureNotDisposed()
        {
            if (_disposed || _handle == null || _handle.IsClosed || _handle.IsInvalid)
                throw new ObjectDisposedException(nameof(PexReader));
        }

        private IntPtr NativeHandle
        {
            get
            {
                EnsureNotDisposed();
                return _handle.DangerousGetHandle();
            }
        }

        /// <summary>
        /// Releases the current native instance and resets this reader to a new empty instance.
        /// </summary>
        /// <remarks>
        /// Existing borrowed handles become invalid. The reader remains usable until <see cref="Dispose"/> is
        /// called.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">Thrown after this reader has been disposed.</exception>
        /// <exception cref="OutOfMemoryException">
        /// Thrown when the replacement native instance cannot be allocated.
        /// </exception>
        /// <exception cref="NotSupportedException">Thrown when the loaded native ABI is incompatible.</exception>
        public void Close()
        {
            EnsureNotDisposed();

            PexInstanceSafeHandle replacement = PexInstanceSafeHandle.Create();
            PexInstanceSafeHandle previous = _handle;
            _handle = replacement;
            previous.Dispose();

            PexPath = string.Empty;
            Header = new PexHeader();
            StringTable.Clear();
            Objects.Clear();
            UserFlags.Clear();
            DebugInfo = new PexDebugInfo();
        }

        /// <summary>
        /// Loads a PEX file and replaces the current managed model after native parsing succeeds.
        /// </summary>
        /// <param name="path">The path of the PEX file to load.</param>
        /// <remarks>
        /// A missing or malformed file leaves the previously loaded native and managed model unchanged.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">Thrown after this reader has been disposed.</exception>
        /// <exception cref="System.IO.FileNotFoundException">
        /// Thrown when <paramref name="path"/> does not exist.
        /// </exception>
        /// <exception cref="System.IO.InvalidDataException">Thrown when the native parser rejects the file.</exception>
        /// <exception cref="NotSupportedException">Thrown when the loaded native ABI is incompatible.</exception>
        public void LoadPex(string path)
        {
            EnsureNotDisposed();

            if (!System.IO.File.Exists(path))
                throw new System.IO.FileNotFoundException("PEX file not found.", path);

            int result = PexInterop.C_ReadPex(NativeHandle, path);
            if (result <= 0)
                throw PexNativeContract.CreateException("load the PEX file");

            Clear();
            PexPath = path;
            LoadHeader();
            LoadStringTable();
            LoadDebugInfo();
            LoadUserFlags();
            LoadObjects();
        }

        /// <summary>Saves the loaded PEX data to a file.</summary>
        /// <param name="outputPath">The UTF-16 output path.</param>
        /// <returns>One on success; otherwise, zero.</returns>
        /// <exception cref="ObjectDisposedException">Thrown after this reader has been disposed.</exception>
        /// <exception cref="NotSupportedException">Thrown when the loaded native ABI is incompatible.</exception>
        public int SavePex(string outputPath)
        {
            EnsureNotDisposed();
            return PexInterop.C_SavePex(NativeHandle, outputPath);
        }

        /// <summary>Replaces one string-table entry with a managed UTF-8 value.</summary>
        /// <param name="index">The string-table index.</param>
        /// <param name="str">The replacement value.</param>
        /// <returns>One on success; otherwise, zero.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="str"/> is null.</exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="str"/> contains an embedded null character.
        /// </exception>
        /// <exception cref="ObjectDisposedException">Thrown after this reader has been disposed.</exception>
        /// <exception cref="NotSupportedException">Thrown when the loaded native ABI is incompatible.</exception>
        public int ModifyStringTable(ushort index, string str)
        {
            EnsureNotDisposed();
            return PexNativeContract.WithUtf8String(
                str,
                pointer => PexInterop.C_ModifyStringTable(NativeHandle, index, pointer));
        }

        public void Clear()
        {
            PexPath = "";
            Header = new PexHeader();
            StringTable.Clear();
            Objects.Clear();
            UserFlags.Clear();
            DebugInfo = new PexDebugInfo();
        }

        private void LoadHeader()
        {
            Header = new PexHeader
            {
                Magic = PexInterop.C_GetHeaderMagic(NativeHandle),
                MajorVersion = PexInterop.C_GetHeaderMajorVersion(NativeHandle),
                MinorVersion = PexInterop.C_GetHeaderMinorVersion(NativeHandle),
                GameId = PexInterop.C_GetHeaderGameId(NativeHandle),
                CompilationTime = PexInterop.C_GetHeaderCompilationTime(NativeHandle),
                SourceFileName = PexNativeContract.ReadBorrowedUnicodeString(
                    PexInterop.C_GetHeaderSourceFileName(NativeHandle)),
                Username = PexNativeContract.ReadBorrowedUnicodeString(
                    PexInterop.C_GetHeaderUsername(NativeHandle)),
                MachineName = PexNativeContract.ReadBorrowedUnicodeString(
                    PexInterop.C_GetHeaderMachineName(NativeHandle)),
            };
        }

        private void LoadStringTable()
        {
            StringTable.Clear();
            ushort count = PexInterop.C_GetStringTableCount(NativeHandle);
            for (ushort i = 0; i < count; i++)
            {
                StringTable.Add(new PexString
                {
                    Index = i,
                    Value = GetStringUtf8(i)
                });
            }
        }

        private void LoadDebugInfo()
        {
            DebugInfo.HasDebugInfo = PexInterop.C_HasDebugInfo(NativeHandle) != 0;
            if (!DebugInfo.HasDebugInfo) return;

            DebugInfo.ModificationTime = PexInterop.C_GetDebugModificationTime(NativeHandle);
            DebugInfo.FunctionCount = PexInterop.C_GetDebugFunctionCount(NativeHandle);

            for (ushort i = 0; i < DebugInfo.FunctionCount; i++)
            {
                if (PexInterop.C_GetDebugFunctionInfo(NativeHandle, i,
                    out ushort objIdx, out ushort stateIdx, out ushort funcIdx,
                    out byte funcType, out IntPtr linePtr, out int lineCount) > 0)
                {
                    using (var lineNumbers = new PexLineNumberBufferSafeHandle(linePtr))
                    {
                        if (lineCount < 0 || lineCount > ushort.MaxValue)
                            throw new System.IO.InvalidDataException("The native line-number count is invalid.");

                        DebugInfo.Functions.Add(new PexDebugFunction
                        {
                            ObjectNameIndex = objIdx,
                            StateNameIndex = stateIdx,
                            FunctionNameIndex = funcIdx,
                            FunctionType = funcType,
                            InstructionCount = (ushort)lineCount,
                            LineNumbers = ReadUshortArray(lineNumbers.DangerousGetHandle(), lineCount)
                        });
                    }
                }
            }
        }

        private void LoadUserFlags()
        {
            UserFlags.Clear();
            ushort count = PexInterop.C_GetUserFlagCount(NativeHandle);
            for (ushort i = 0; i < count; i++)
            {
                if (PexInterop.C_GetUserFlagInfo(NativeHandle, i,
                    out ushort flagNameIndex, out byte flagIndex) > 0)
                {
                    UserFlags.Add(new PexUserFlag
                    {
                        FlagNameIndex = flagNameIndex,
                        FlagIndex = flagIndex
                    });
                }
            }
        }

        private void LoadObjects()
        {
            Objects.Clear();
            ushort count = PexInterop.C_GetObjectCount(NativeHandle);
            for (ushort i = 0; i < count; i++)
            {
                if (PexInterop.C_GetObjectInfo(NativeHandle, i, out ushort nameIndex, out uint size) > 0 &&
                    PexInterop.C_GetObjectData(NativeHandle, i, out ushort parentClass, out ushort docStr,
                        out uint userFlags, out ushort autoState) > 0)
                {
                    var obj = new PexObject
                    {
                        NameIndex = nameIndex,
                        Size = size,
                        ParentClassNameIndex = parentClass,
                        DocStringIndex = docStr,
                        UserFlags = userFlags,
                        AutoStateNameIndex = autoState
                    };

                    LoadObjectVariables(obj, i);
                    LoadObjectProperties(obj, i);
                    LoadObjectStates(obj, i);

                    Objects.Add(obj);
                }
            }
        }

        private void LoadObjectVariables(PexObject obj, ushort objectIndex)
        {
            ushort count = PexInterop.C_GetVariableCount(NativeHandle, objectIndex);
            for (ushort j = 0; j < count; j++)
            {
                if (PexInterop.C_GetVariableInfo(NativeHandle, objectIndex, j,
                    out ushort name, out ushort typeName,
                    out uint userFlags, out byte dataType, IntPtr.Zero) > 0)
                {
                    ushort realValueId = 0;
                    object value = GetVariableDataValue(dataType, objectIndex, j, ref realValueId);

                    obj.Variables.Add(new PexVariable
                    {
                        NameIndex = name,
                        TypeNameIndex = typeName,
                        UserFlags = userFlags,
                        DataType = dataType,
                        VarIndex = realValueId,
                        DataValue = value
                    });
                }
            }
        }

        private object GetVariableDataValue(
            byte dataType,
            ushort objectIndex,
            ushort variableIndex,
            ref ushort realValueId)
        {
            if (dataType == 0)
                return null;

            PexNativeValue value;
            int result = PexNativeContract.WithValueBuffer(
                pointer => PexInterop.C_GetVariableInfo(
                    NativeHandle,
                    objectIndex,
                    variableIndex,
                    out _,
                    out _,
                    out _,
                    out _,
                    pointer),
                out value);
            if (result <= 0)
                return null;

            switch (dataType)
            {
                case 1:
                case 2:
                    realValueId = value.StringTableIndex;
                    return GetString(realValueId);
                case 3:
                    return value.Integer;
                case 4:
                    return value.Real;
                case 5:
                    return value.Boolean != 0;
                default:
                    return null;
            }
        }

        private void LoadObjectProperties(PexObject obj, ushort objectIndex)
        {
            ushort count = PexInterop.C_GetPropertyCount(NativeHandle, objectIndex);
            for (ushort j = 0; j < count; j++)
            {
                if (PexInterop.C_GetPropertyInfo(NativeHandle, objectIndex, j,
                    out ushort name, out ushort type, out ushort docstring,
                    out uint userFlags, out byte flags, out ushort autoVarName) > 0)
                {
                    obj.Properties.Add(new PexProperty
                    {
                        NameIndex = name,
                        TypeIndex = type,
                        DocstringIndex = docstring,
                        UserFlags = userFlags,
                        Flags = flags,
                        AutoVarNameIndex = autoVarName
                    });
                }
            }
        }

        private void LoadObjectStates(PexObject obj, ushort objectIndex)
        {
            ushort count = PexInterop.C_GetStateCount(NativeHandle, objectIndex);
            for (ushort j = 0; j < count; j++)
            {
                if (PexInterop.C_GetStateInfo(NativeHandle, objectIndex, j,
                    out ushort name, out ushort numFunctions) > 0)
                {
                    var state = new PexState { NameIndex = name, NumFunctions = numFunctions };
                    LoadStateFunctions(state, objectIndex, j);
                    obj.States.Add(state);
                }
            }
        }

        private void LoadStateFunctions(PexState state, ushort objectIndex, ushort stateIndex)
        {
            for (ushort k = 0; k < state.NumFunctions; k++)
            {
                if (PexInterop.C_GetStateFunctionInfo(NativeHandle,
                    objectIndex, stateIndex, k,
                    out ushort funcName, out ushort returnType, out ushort docStr,
                    out uint userFlags, out byte flags,
                    out ushort numParams, out ushort numLocals, out ushort numInstr) > 0)
                {
                    var func = new PexFunction
                    {
                        FunctionNameIndex = funcName,
                        ReturnTypeIndex = returnType,
                        DocStringIndex = docStr,
                        UserFlags = userFlags,
                        Flags = flags,
                        NumParams = numParams,
                        NumLocals = numLocals,
                        NumInstructions = numInstr
                    };

                    LoadFunctionParameters(func, objectIndex, stateIndex, k);
                    LoadFunctionLocals(func, objectIndex, stateIndex, k);
                    LoadFunctionInstructions(func, objectIndex, stateIndex, k);

                    state.Functions.Add(func);
                }
            }
        }

        private void LoadFunctionParameters(PexFunction func,
            ushort objectIndex, ushort stateIndex, ushort funcIndex)
        {
            ushort count = PexInterop.C_GetFunctionParamCount(NativeHandle, objectIndex, stateIndex, funcIndex);
            for (ushort i = 0; i < count; i++)
            {
                if (PexInterop.C_GetFunctionParamInfo(NativeHandle,
                    objectIndex, stateIndex, funcIndex, i,
                    out ushort name, out ushort type) > 0)
                {
                    func.Parameters.Add(new PexFunctionParam { NameIndex = name, TypeIndex = type });
                }
            }
        }

        private void LoadFunctionLocals(PexFunction func,
            ushort objectIndex, ushort stateIndex, ushort funcIndex)
        {
            ushort count = PexInterop.C_GetFunctionLocalCount(NativeHandle, objectIndex, stateIndex, funcIndex);
            for (ushort i = 0; i < count; i++)
            {
                if (PexInterop.C_GetFunctionLocalInfo(NativeHandle,
                    objectIndex, stateIndex, funcIndex, i,
                    out ushort name, out ushort type) > 0)
                {
                    func.Locals.Add(new PexFunctionLocal { NameIndex = name, TypeIndex = type });
                }
            }
        }

        private void LoadFunctionInstructions(PexFunction func,
            ushort objectIndex, ushort stateIndex, ushort funcIndex)
        {
            for (ushort i = 0; i < func.NumInstructions; i++)
            {
                if (PexInterop.C_GetInstructionInfo(NativeHandle,
                    objectIndex, stateIndex, funcIndex, i,
                    out byte opcode, out ushort argCount) > 0)
                {
                    var instr = new PexInstruction { Opcode = opcode };

                    for (ushort argIdx = 0; argIdx < argCount; argIdx++)
                    {
                        byte argumentType = 0;
                        PexNativeValue value;
                        int argumentResult = PexNativeContract.WithValueBuffer(
                            pointer => PexInterop.C_GetInstructionArgument(
                                NativeHandle,
                                objectIndex,
                                stateIndex,
                                funcIndex,
                                i,
                                argIdx,
                                out argumentType,
                                pointer),
                            out value);
                        if (argumentResult > 0)
                        {
                            var argument = new PexInstructionArgument { Type = argumentType };
                            switch (argumentType)
                            {
                                case 0:
                                    argument.Value = null;
                                    break;
                                case 1:
                                case 2:
                                    argument.Value = value.StringTableIndex;
                                    break;
                                case 3:
                                    argument.Value = value.Integer;
                                    break;
                                case 4:
                                    argument.Value = value.Real;
                                    break;
                                case 5:
                                    argument.Value = value.Boolean != 0;
                                    break;
                                default:
                                    argument.Value = null;
                                    break;
                            }
                            instr.Arguments.Add(argument);
                        }
                    }

                    func.Instructions.Add(instr);
                }
            }
        }

        public string GetString(ushort index)
            => index < StringTable.Count ? StringTable[index].Value : "";

        private string GetStringUtf8(ushort index)
        {
            return PexNativeContract.ReadStringUtf8(NativeHandle, index);
        }

        private static ushort[] ReadUshortArray(IntPtr ptr, int count)
        {
            if (count < 0 || count > ushort.MaxValue)
                throw new System.IO.InvalidDataException("The native line-number count is invalid.");
            if (count == 0)
                return Array.Empty<ushort>();
            if (ptr == IntPtr.Zero)
                throw new System.IO.InvalidDataException("The native line-number pointer is null.");

            byte[] raw = new byte[count * 2];
            Marshal.Copy(ptr, raw, 0, raw.Length);
            ushort[] result = new ushort[count];
            for (int i = 0; i < count; i++)
                result[i] = BitConverter.ToUInt16(raw, i * 2);
            return result;
        }

        public string GetVariableValueAsString(PexVariable variable)
        {
            if (variable.DataValue == null) return "null";
            switch (variable.DataType)
            {
                case 0: return "null";
                case 1: case 2: return variable.DataValue.ToString() ?? "";
                case 3: return variable.DataValue.ToString() ?? "0";
                case 4: try { return ((float)variable.DataValue).ToString("F6"); } catch { return "0.000000"; }
                case 5: try { return ((bool)variable.DataValue) ? "true" : "false"; } catch { return "false"; }
                default: return variable.DataValue.ToString() ?? "";
            }
        }

        public PexObject FindObjectByName(string name)
        {
            foreach (var obj in Objects)
                if (string.Equals(obj.GetName(this), name, StringComparison.OrdinalIgnoreCase))
                    return obj;
            return null;
        }

        public class PexString { public ushort Index; public string Value = ""; }
        public class PexUserFlag { public ushort FlagNameIndex; public byte FlagIndex; public string GetFlagName(PexReader r) => r?.GetString(FlagNameIndex) ?? ""; }
        public class PexHeader { public uint Magic; public byte MajorVersion, MinorVersion; public ushort GameId; public ulong CompilationTime; public string SourceFileName = "", Username = "", MachineName = ""; }
        public class PexDebugFunction { public ushort ObjectNameIndex, StateNameIndex, FunctionNameIndex, InstructionCount; public byte FunctionType; public ushort[] LineNumbers = Array.Empty<ushort>(); public string GetObjectName(PexReader r) => r?.GetString(ObjectNameIndex) ?? ""; public string GetStateName(PexReader r) => r?.GetString(StateNameIndex) ?? ""; public string GetFunctionName(PexReader r) => r?.GetString(FunctionNameIndex) ?? ""; }
        public class PexDebugInfo { public bool HasDebugInfo; public ulong ModificationTime; public ushort FunctionCount; public List<PexDebugFunction> Functions = new List<PexDebugFunction>(); }
        public class PexVariable { public ushort VarIndex = 0; public ushort NameIndex, TypeNameIndex; public uint UserFlags; public byte DataType; public object DataValue = ""; public string GetName(PexReader r) => r?.GetString(NameIndex) ?? ""; public string GetTypeName(PexReader r) => r?.GetString(TypeNameIndex) ?? ""; }
        public class PexProperty { public ushort NameIndex, TypeIndex, DocstringIndex, AutoVarNameIndex; public uint UserFlags; public byte Flags; public string GetName(PexReader r) => r?.GetString(NameIndex) ?? ""; public string GetType(PexReader r) => r?.GetString(TypeIndex) ?? ""; public string GetDocstring(PexReader r) => r?.GetString(DocstringIndex) ?? ""; public string GetAutoVarName(PexReader r) => r?.GetString(AutoVarNameIndex) ?? ""; }
        public class PexState { public ushort NameIndex, NumFunctions; public List<PexFunction> Functions = new List<PexFunction>(); public string GetName(PexReader r) => r?.GetString(NameIndex) ?? ""; }
        public class PexFunctionParam { public ushort NameIndex, TypeIndex; public string GetName(PexReader r) => r?.GetString(NameIndex) ?? ""; public string GetTypeName(PexReader r) => r?.GetString(TypeIndex) ?? ""; }
        public class PexFunctionLocal { public ushort NameIndex, TypeIndex; public string GetName(PexReader r) => r?.GetString(NameIndex) ?? ""; public string GetTypeName(PexReader r) => r?.GetString(TypeIndex) ?? ""; }

        public class PexInstructionArgument
        {
            public byte Type; public object Value;
            public string GetValueAsString(PexReader r)
            {
                if (Value == null) return "null";
                switch (Type)
                {
                    case 0: return "null";
                    case 1: case 2: return Value is ushort si ? r?.GetString(si) ?? si.ToString() : Value.ToString() ?? "";
                    case 3: return Value.ToString() ?? "0";
                    case 4: return Value is float f ? f.ToString("F6") : "0.000000";
                    case 5: return (Value is bool b && b) ? "true" : "false";
                    default: return Value.ToString() ?? "";
                }
            }
        }

        public class PexInstruction
        {
            public byte Opcode; public List<PexInstructionArgument> Arguments = new List<PexInstructionArgument>();
            public string GetOpcodeName() { switch (Opcode) { case 0x00: return "nop"; case 0x01: return "iadd"; case 0x02: return "fadd"; case 0x03: return "isub"; case 0x04: return "fsub"; case 0x05: return "imul"; case 0x06: return "fmul"; case 0x07: return "idiv"; case 0x08: return "fdiv"; case 0x09: return "imod"; case 0x0A: return "not"; case 0x0B: return "ineg"; case 0x0C: return "fneg"; case 0x0D: return "assign"; case 0x0E: return "cast"; case 0x0F: return "cmp_eq"; case 0x10: return "cmp_lt"; case 0x11: return "cmp_le"; case 0x12: return "cmp_gt"; case 0x13: return "cmp_ge"; case 0x14: return "jmp"; case 0x15: return "jmpt"; case 0x16: return "jmpf"; case 0x17: return "callmethod"; case 0x18: return "callparent"; case 0x19: return "callstatic"; case 0x1A: return "return"; case 0x1B: return "strcat"; case 0x1C: return "propget"; case 0x1D: return "propset"; case 0x1E: return "array_create"; case 0x1F: return "array_length"; case 0x20: return "array_getelement"; case 0x21: return "array_setelement"; case 0x22: return "array_findelement"; case 0x23: return "array_rfindelement"; default: return $"0x{Opcode:X2}"; } }
        }

        public class PexFunction
        {
            public ushort FunctionNameIndex, ReturnTypeIndex, DocStringIndex, NumParams, NumLocals, NumInstructions;
            public uint UserFlags; public byte Flags;
            public List<PexFunctionParam> Parameters = new List<PexFunctionParam>();
            public List<PexFunctionLocal> Locals = new List<PexFunctionLocal>();
            public List<PexInstruction> Instructions = new List<PexInstruction>();
            public string GetFunctionName(PexReader r) => r?.GetString(FunctionNameIndex) ?? "";
            public string GetReturnType(PexReader r) => r?.GetString(ReturnTypeIndex) ?? "";
            public string GetDocString(PexReader r) => r?.GetString(DocStringIndex) ?? "";
        }

        public class PexObject
        {
            public ushort NameIndex, ParentClassNameIndex, DocStringIndex, AutoStateNameIndex;
            public uint Size, UserFlags;
            public List<PexVariable> Variables = new List<PexVariable>();
            public List<PexProperty> Properties = new List<PexProperty>();
            public List<PexState> States = new List<PexState>();
            public string GetName(PexReader r) => r?.GetString(NameIndex) ?? "";
            public string GetParentClassName(PexReader r) => r?.GetString(ParentClassNameIndex) ?? "";
            public string GetDocString(PexReader r) => r?.GetString(DocStringIndex) ?? "";
            public string GetAutoStateName(PexReader r) => r?.GetString(AutoStateNameIndex) ?? "";
        }
    }
}
