using System;
using System.Runtime.InteropServices;

namespace PexInterface
{
    /// <summary>
    /// Identifies stable status values returned by the native PEX ABI.
    /// </summary>
    internal enum PexNativeStatus
    {
        /// <summary>The previous ABI call completed successfully.</summary>
        Ok = 0,

        /// <summary>An argument did not satisfy the native contract.</summary>
        InvalidArgument = 1,

        /// <summary>An index or value was outside its valid range.</summary>
        OutOfRange = 2,

        /// <summary>An output buffer could not hold the requested value.</summary>
        BufferTooSmall = 3,

        /// <summary>An input or output operation failed.</summary>
        IoError = 4,

        /// <summary>PEX input could not be parsed.</summary>
        ParseError = 5,

        /// <summary>The native module could not allocate required memory.</summary>
        OutOfMemory = 6,

        /// <summary>The native module encountered an unexpected failure.</summary>
        InternalError = 7
    }

    /// <summary>
    /// Mirrors the four-byte <c>PexReaderValue</c> union from the native ABI.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Pack = 4, Size = 4)]
    internal struct PexNativeValue
    {
        /// <summary>Stores a string-table index for PEX types 1 and 2.</summary>
        [FieldOffset(0)]
        internal ushort StringTableIndex;

        /// <summary>Stores a signed integer for PEX type 3.</summary>
        [FieldOffset(0)]
        internal int Integer;

        /// <summary>Stores a single-precision value for PEX type 4.</summary>
        [FieldOffset(0)]
        internal float Real;

        /// <summary>Stores a one-byte Boolean for PEX type 5.</summary>
        [FieldOffset(0)]
        internal byte Boolean;
    }

    /// <summary>
    /// Defines the only raw P/Invoke boundary for <c>Pex.Interop.dll</c>.
    /// </summary>
    /// <remarks>
    /// Every declaration uses the cdecl calling convention from <c>PexReaderApi.h</c>. Handles and returned
    /// pointers are represented as pointer-sized values. The owning wrappers in this assembly control their lifetimes.
    /// </remarks>
    internal static class PexNativeMethods
    {
        internal const string DllName = "Pex.Interop.dll";

        /// <summary>Returns the additive native ABI version.</summary>
        /// <returns>The native ABI version.</returns>
        [DllImport(DllName, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint C_GetAbiVersion();

        /// <summary>Returns the status of the previous native ABI call on this thread.</summary>
        /// <returns>A stable native status value.</returns>
        [DllImport(DllName, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        internal static extern PexNativeStatus C_GetLastStatus();

        /// <summary>Copies the previous thread-local native error as UTF-8.</summary>
        /// <param name="buffer">The optional caller-owned byte buffer.</param>
        /// <param name="bufferSize">The byte capacity of <paramref name="buffer"/>.</param>
        /// <returns>The payload length in bytes, excluding the null terminator.</returns>
        [DllImport(DllName, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int C_GetLastErrorUtf8([Out] byte[] buffer, int bufferSize);

        /// <summary>Returns the borrowed null-terminated product version.</summary>
        /// <returns>A borrowed ANSI-compatible UTF-8 pointer.</returns>
        [DllImport(DllName, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr C_GetVersion();

        /// <summary>Returns the product-version payload length.</summary>
        /// <returns>The length in bytes, excluding the null terminator.</returns>
        [DllImport(DllName, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int C_GetVersionLength();

        /// <summary>Creates an owned native reader instance.</summary>
        /// <returns>An owned pointer-sized handle, or zero on failure.</returns>
        [DllImport(DllName, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr C_CreateInstance();

        /// <summary>Destroys an owned native reader instance.</summary>
        /// <param name="handle">The nullable owned handle.</param>
        [DllImport(DllName, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void C_DestroyInstance(IntPtr handle);

        /// <summary>Loads a PEX file from a Windows UTF-16 path.</summary>
        /// <param name="handle">The non-null reader handle.</param>
        /// <param name="pexPath">The non-null UTF-16 input path.</param>
        /// <returns>One on success; otherwise, zero.</returns>
        [DllImport(
            DllName,
            ExactSpelling = true,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode)]
        internal static extern int C_ReadPex(
            IntPtr handle,
            [MarshalAs(UnmanagedType.LPWStr)] string pexPath);

        /// <summary>Replaces a string-table entry with null-terminated UTF-8.</summary>
        /// <param name="handle">The non-null reader handle.</param>
        /// <param name="index">The string-table index.</param>
        /// <param name="utf8String">The non-null UTF-8 pointer.</param>
        /// <returns>One on success; otherwise, zero.</returns>
        [DllImport(DllName, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int C_ModifyStringTable(IntPtr handle, ushort index, IntPtr utf8String);

        /// <summary>Saves loaded PEX data to a Windows UTF-16 path.</summary>
        /// <param name="handle">The non-null reader handle.</param>
        /// <param name="pexPath">The non-null UTF-16 output path.</param>
        /// <returns>One on success; otherwise, zero.</returns>
        [DllImport(
            DllName,
            ExactSpelling = true,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode)]
        internal static extern int C_SavePex(
            IntPtr handle,
            [MarshalAs(UnmanagedType.LPWStr)] string pexPath);

        /// <summary>Resets a native reader without ending its handle lifetime.</summary>
        /// <param name="handle">The nullable reader handle.</param>
        [DllImport(DllName, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void C_Close(IntPtr handle);

        /// <summary>Returns the borrowed UTF-16 source-file name.</summary>
        /// <param name="handle">The non-null reader handle.</param>
        /// <returns>A borrowed UTF-16 pointer.</returns>
        [DllImport(DllName, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr C_GetHeaderSourceFileName(IntPtr handle);

        /// <summary>Returns the borrowed UTF-16 compiler username.</summary>
        /// <param name="handle">The non-null reader handle.</param>
        /// <returns>A borrowed UTF-16 pointer.</returns>
        [DllImport(DllName, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr C_GetHeaderUsername(IntPtr handle);

        /// <summary>Returns the borrowed UTF-16 compiler machine name.</summary>
        /// <param name="handle">The non-null reader handle.</param>
        /// <returns>A borrowed UTF-16 pointer.</returns>
        [DllImport(DllName, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr C_GetHeaderMachineName(IntPtr handle);

        /// <summary>Returns the PEX magic value.</summary>
        /// <param name="handle">The non-null reader handle.</param>
        /// <returns>The unsigned 32-bit magic value.</returns>
        [DllImport(DllName, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint C_GetHeaderMagic(IntPtr handle);

        /// <summary>Returns the PEX major version.</summary>
        /// <param name="handle">The non-null reader handle.</param>
        /// <returns>The unsigned 8-bit major version.</returns>
        [DllImport(DllName, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        internal static extern byte C_GetHeaderMajorVersion(IntPtr handle);

        /// <summary>Returns the PEX minor version.</summary>
        /// <param name="handle">The non-null reader handle.</param>
        /// <returns>The unsigned 8-bit minor version.</returns>
        [DllImport(DllName, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        internal static extern byte C_GetHeaderMinorVersion(IntPtr handle);

        /// <summary>Returns the PEX game identifier.</summary>
        /// <param name="handle">The non-null reader handle.</param>
        /// <returns>The unsigned 16-bit game identifier.</returns>
        [DllImport(DllName, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        internal static extern ushort C_GetHeaderGameId(IntPtr handle);

        /// <summary>Returns the PEX compilation timestamp.</summary>
        /// <param name="handle">The non-null reader handle.</param>
        /// <returns>The unsigned 64-bit timestamp.</returns>
        [DllImport(DllName, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        internal static extern ulong C_GetHeaderCompilationTime(IntPtr handle);

        /// <summary>Returns the string-table count.</summary>
        /// <param name="handle">The non-null reader handle.</param>
        /// <returns>The unsigned 16-bit entry count.</returns>
        [DllImport(DllName, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        internal static extern ushort C_GetStringTableCount(IntPtr handle);

        /// <summary>Queries or copies a UTF-8 string-table entry.</summary>
        /// <param name="handle">The non-null reader handle.</param>
        /// <param name="index">The string-table index.</param>
        /// <param name="buffer">The optional caller-owned byte buffer.</param>
        /// <param name="bufferSize">The byte capacity of <paramref name="buffer"/>.</param>
        /// <returns>The payload length, or minus one on failure.</returns>
        [DllImport(DllName, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int C_GetStringUtf8(
            IntPtr handle,
            ushort index,
            [Out] byte[] buffer,
            int bufferSize);

        /// <summary>Queries or copies a UTF-16 string-table entry.</summary>
        /// <param name="handle">The non-null reader handle.</param>
        /// <param name="index">The string-table index.</param>
        /// <param name="buffer">The optional caller-owned UTF-16 buffer.</param>
        /// <param name="bufferSize">The character capacity of <paramref name="buffer"/>.</param>
        /// <returns>The payload length, or minus one on failure.</returns>
        [DllImport(
            DllName,
            ExactSpelling = true,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode)]
        internal static extern int C_GetStringWide(
            IntPtr handle,
            ushort index,
            [Out] char[] buffer,
            int bufferSize);

        /// <summary>Reports whether debug data is available.</summary>
        /// <param name="handle">The non-null reader handle.</param>
        /// <returns>One when debug data exists; otherwise, zero.</returns>
        [DllImport(DllName, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        internal static extern byte C_HasDebugInfo(IntPtr handle);

        /// <summary>Returns the debug modification timestamp.</summary>
        /// <param name="handle">The non-null reader handle.</param>
        /// <returns>The unsigned 64-bit timestamp.</returns>
        [DllImport(DllName, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        internal static extern ulong C_GetDebugModificationTime(IntPtr handle);

        /// <summary>Returns the debug-function count.</summary>
        /// <param name="handle">The non-null reader handle.</param>
        /// <returns>The unsigned 16-bit function count.</returns>
        [DllImport(DllName, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        internal static extern ushort C_GetDebugFunctionCount(IntPtr handle);

        /// <summary>Returns debug-function metadata and an owned line-number array.</summary>
        /// <param name="handle">The non-null reader handle.</param>
        /// <param name="index">The debug-function index.</param>
        /// <param name="objectNameIndex">Receives the object-name string index.</param>
        /// <param name="stateNameIndex">Receives the state-name string index.</param>
        /// <param name="functionNameIndex">Receives the function-name string index.</param>
        /// <param name="functionType">Receives the function type.</param>
        /// <param name="lineNumbers">Receives an owned native array pointer.</param>
        /// <param name="lineCount">Receives the number of unsigned 16-bit array elements.</param>
        /// <returns>One on success; otherwise, zero.</returns>
        [DllImport(DllName, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int C_GetDebugFunctionInfo(
            IntPtr handle,
            ushort index,
            out ushort objectNameIndex,
            out ushort stateNameIndex,
            out ushort functionNameIndex,
            out byte functionType,
            out IntPtr lineNumbers,
            out int lineCount);

        /// <summary>Returns the user-flag count.</summary>
        /// <param name="handle">The non-null reader handle.</param>
        /// <returns>The unsigned 16-bit flag count.</returns>
        [DllImport(DllName, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        internal static extern ushort C_GetUserFlagCount(IntPtr handle);

        /// <summary>Returns one user-flag entry.</summary>
        /// <param name="handle">The non-null reader handle.</param>
        /// <param name="index">The user-flag index.</param>
        /// <param name="flagNameIndex">Receives the flag-name string index.</param>
        /// <param name="flagIndex">Receives the bit index.</param>
        /// <returns>One on success; otherwise, zero.</returns>
        [DllImport(DllName, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int C_GetUserFlagInfo(
            IntPtr handle,
            ushort index,
            out ushort flagNameIndex,
            out byte flagIndex);

        /// <summary>Returns the object count.</summary>
        /// <param name="handle">The non-null reader handle.</param>
        /// <returns>The unsigned 16-bit object count.</returns>
        [DllImport(DllName, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        internal static extern ushort C_GetObjectCount(IntPtr handle);

        /// <summary>Returns object-level metadata.</summary>
        /// <param name="handle">The non-null reader handle.</param>
        /// <param name="index">The object index.</param>
        /// <param name="nameIndex">Receives the object-name string index.</param>
        /// <param name="size">Receives the encoded object size.</param>
        /// <returns>One on success; otherwise, zero.</returns>
        [DllImport(DllName, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int C_GetObjectInfo(
            IntPtr handle,
            ushort index,
            out ushort nameIndex,
            out uint size);

        /// <summary>Returns object-body metadata.</summary>
        /// <param name="handle">The non-null reader handle.</param>
        /// <param name="objectIndex">The object index.</param>
        /// <param name="parentClassName">Receives the parent-class string index.</param>
        /// <param name="docString">Receives the documentation string index.</param>
        /// <param name="userFlags">Receives the user flags.</param>
        /// <param name="autoStateName">Receives the auto-state string index.</param>
        /// <returns>One on success; otherwise, zero.</returns>
        [DllImport(DllName, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int C_GetObjectData(
            IntPtr handle,
            ushort objectIndex,
            out ushort parentClassName,
            out ushort docString,
            out uint userFlags,
            out ushort autoStateName);

        /// <summary>Returns the variable count for an object.</summary>
        /// <param name="handle">The non-null reader handle.</param>
        /// <param name="objectIndex">The object index.</param>
        /// <returns>The unsigned 16-bit variable count.</returns>
        [DllImport(DllName, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        internal static extern ushort C_GetVariableCount(IntPtr handle, ushort objectIndex);

        /// <summary>Returns variable metadata and an optional four-byte value.</summary>
        /// <param name="handle">The non-null reader handle.</param>
        /// <param name="objectIndex">The object index.</param>
        /// <param name="variableIndex">The variable index.</param>
        /// <param name="name">Receives the name string index.</param>
        /// <param name="typeName">Receives the type-name string index.</param>
        /// <param name="userFlags">Receives the user flags.</param>
        /// <param name="dataType">Receives the PEX value type.</param>
        /// <param name="dataValue">Receives the optional caller-owned value pointer.</param>
        /// <returns>One on success; otherwise, zero.</returns>
        [DllImport(DllName, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int C_GetVariableInfo(
            IntPtr handle,
            ushort objectIndex,
            ushort variableIndex,
            out ushort name,
            out ushort typeName,
            out uint userFlags,
            out byte dataType,
            IntPtr dataValue);

        /// <summary>Returns the property count for an object.</summary>
        /// <param name="handle">The non-null reader handle.</param>
        /// <param name="objectIndex">The object index.</param>
        /// <returns>The unsigned 16-bit property count.</returns>
        [DllImport(DllName, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        internal static extern ushort C_GetPropertyCount(IntPtr handle, ushort objectIndex);

        /// <summary>Returns property metadata.</summary>
        /// <param name="handle">The non-null reader handle.</param>
        /// <param name="objectIndex">The object index.</param>
        /// <param name="propertyIndex">The property index.</param>
        /// <param name="name">Receives the name string index.</param>
        /// <param name="type">Receives the type string index.</param>
        /// <param name="docString">Receives the documentation string index.</param>
        /// <param name="userFlags">Receives the user flags.</param>
        /// <param name="flags">Receives the property flags.</param>
        /// <param name="autoVariableName">Receives the auto-variable string index.</param>
        /// <returns>One on success; otherwise, zero.</returns>
        [DllImport(DllName, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int C_GetPropertyInfo(
            IntPtr handle,
            ushort objectIndex,
            ushort propertyIndex,
            out ushort name,
            out ushort type,
            out ushort docString,
            out uint userFlags,
            out byte flags,
            out ushort autoVariableName);

        /// <summary>Returns the state count for an object.</summary>
        /// <param name="handle">The non-null reader handle.</param>
        /// <param name="objectIndex">The object index.</param>
        /// <returns>The unsigned 16-bit state count.</returns>
        [DllImport(DllName, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        internal static extern ushort C_GetStateCount(IntPtr handle, ushort objectIndex);

        /// <summary>Returns state metadata.</summary>
        /// <param name="handle">The non-null reader handle.</param>
        /// <param name="objectIndex">The object index.</param>
        /// <param name="stateIndex">The state index.</param>
        /// <param name="name">Receives the state-name string index.</param>
        /// <param name="functionCount">Receives the function count.</param>
        /// <returns>One on success; otherwise, zero.</returns>
        [DllImport(DllName, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int C_GetStateInfo(
            IntPtr handle,
            ushort objectIndex,
            ushort stateIndex,
            out ushort name,
            out ushort functionCount);

        /// <summary>Returns state-function metadata.</summary>
        /// <param name="handle">The non-null reader handle.</param>
        /// <param name="objectIndex">The object index.</param>
        /// <param name="stateIndex">The state index.</param>
        /// <param name="functionIndex">The function index.</param>
        /// <param name="functionName">Receives the function-name string index.</param>
        /// <param name="returnType">Receives the return-type string index.</param>
        /// <param name="docString">Receives the documentation string index.</param>
        /// <param name="userFlags">Receives the user flags.</param>
        /// <param name="flags">Receives the function flags.</param>
        /// <param name="parameterCount">Receives the parameter count.</param>
        /// <param name="localCount">Receives the local-variable count.</param>
        /// <param name="instructionCount">Receives the instruction count.</param>
        /// <returns>One on success; otherwise, zero.</returns>
        [DllImport(DllName, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int C_GetStateFunctionInfo(
            IntPtr handle,
            ushort objectIndex,
            ushort stateIndex,
            ushort functionIndex,
            out ushort functionName,
            out ushort returnType,
            out ushort docString,
            out uint userFlags,
            out byte flags,
            out ushort parameterCount,
            out ushort localCount,
            out ushort instructionCount);

        /// <summary>Returns instruction metadata.</summary>
        /// <param name="handle">The non-null reader handle.</param>
        /// <param name="objectIndex">The object index.</param>
        /// <param name="stateIndex">The state index.</param>
        /// <param name="functionIndex">The function index.</param>
        /// <param name="instructionIndex">The instruction index.</param>
        /// <param name="opcode">Receives the opcode.</param>
        /// <param name="argumentCount">Receives the argument count.</param>
        /// <returns>One on success; otherwise, zero.</returns>
        [DllImport(DllName, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int C_GetInstructionInfo(
            IntPtr handle,
            ushort objectIndex,
            ushort stateIndex,
            ushort functionIndex,
            ushort instructionIndex,
            out byte opcode,
            out ushort argumentCount);

        /// <summary>Returns one instruction argument.</summary>
        /// <param name="handle">The non-null reader handle.</param>
        /// <param name="objectIndex">The object index.</param>
        /// <param name="stateIndex">The state index.</param>
        /// <param name="functionIndex">The function index.</param>
        /// <param name="instructionIndex">The instruction index.</param>
        /// <param name="argumentIndex">The argument index.</param>
        /// <param name="type">Receives the PEX value type.</param>
        /// <param name="value">Receives the optional caller-owned value pointer.</param>
        /// <returns>One on success; otherwise, zero.</returns>
        [DllImport(DllName, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int C_GetInstructionArgument(
            IntPtr handle,
            ushort objectIndex,
            ushort stateIndex,
            ushort functionIndex,
            ushort instructionIndex,
            ushort argumentIndex,
            out byte type,
            IntPtr value);

        /// <summary>Returns a function's parameter count.</summary>
        /// <param name="handle">The non-null reader handle.</param>
        /// <param name="objectIndex">The object index.</param>
        /// <param name="stateIndex">The state index.</param>
        /// <param name="functionIndex">The function index.</param>
        /// <returns>The unsigned 16-bit parameter count.</returns>
        [DllImport(DllName, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        internal static extern ushort C_GetFunctionParamCount(
            IntPtr handle,
            ushort objectIndex,
            ushort stateIndex,
            ushort functionIndex);

        /// <summary>Returns function-parameter metadata.</summary>
        /// <param name="handle">The non-null reader handle.</param>
        /// <param name="objectIndex">The object index.</param>
        /// <param name="stateIndex">The state index.</param>
        /// <param name="functionIndex">The function index.</param>
        /// <param name="parameterIndex">The parameter index.</param>
        /// <param name="name">Receives the name string index.</param>
        /// <param name="type">Receives the type string index.</param>
        /// <returns>One on success; otherwise, zero.</returns>
        [DllImport(DllName, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int C_GetFunctionParamInfo(
            IntPtr handle,
            ushort objectIndex,
            ushort stateIndex,
            ushort functionIndex,
            ushort parameterIndex,
            out ushort name,
            out ushort type);

        /// <summary>Returns a function's local-variable count.</summary>
        /// <param name="handle">The non-null reader handle.</param>
        /// <param name="objectIndex">The object index.</param>
        /// <param name="stateIndex">The state index.</param>
        /// <param name="functionIndex">The function index.</param>
        /// <returns>The unsigned 16-bit local-variable count.</returns>
        [DllImport(DllName, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        internal static extern ushort C_GetFunctionLocalCount(
            IntPtr handle,
            ushort objectIndex,
            ushort stateIndex,
            ushort functionIndex);

        /// <summary>Returns function-local metadata.</summary>
        /// <param name="handle">The non-null reader handle.</param>
        /// <param name="objectIndex">The object index.</param>
        /// <param name="stateIndex">The state index.</param>
        /// <param name="functionIndex">The function index.</param>
        /// <param name="localIndex">The local-variable index.</param>
        /// <param name="name">Receives the name string index.</param>
        /// <param name="type">Receives the type string index.</param>
        /// <returns>One on success; otherwise, zero.</returns>
        [DllImport(DllName, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int C_GetFunctionLocalInfo(
            IntPtr handle,
            ushort objectIndex,
            ushort stateIndex,
            ushort functionIndex,
            ushort localIndex,
            out ushort name,
            out ushort type);

        /// <summary>Releases a native line-number array with its producing allocator.</summary>
        /// <param name="buffer">The nullable owned array pointer.</param>
        [DllImport(DllName, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void C_FreeBuffer(IntPtr buffer);
    }
}
