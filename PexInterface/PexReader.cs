using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace PexInterface
{
    // Copyright (c) 2025 YD525, Cutleast
    // Licensed under the LGPL3.0 License.

    /// <summary>
    /// 底层 P/Invoke 绑定。所有函数第一个参数均为 handle（由 C_CreateInstance 返回的指针）。
    /// </summary>
    public static class PexInterop
    {
        private const string DllName = "Pex.Interop.dll";

        public static string Version = "";

        #region P/Invoke Declarations

        // 版本
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr C_GetVersion();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int C_GetVersionLength();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr C_CreateInstance();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void C_DestroyInstance(IntPtr handle);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        public static extern int C_ReadPex(IntPtr handle, [MarshalAs(UnmanagedType.LPWStr)] string pexPath);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int C_ModifyStringTable(IntPtr handle, ushort index, IntPtr utf8Str);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        public static extern int C_SavePex(IntPtr handle, [MarshalAs(UnmanagedType.LPWStr)] string pexPath);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void C_Close(IntPtr handle);

        // ── Header ───────────────────────────────────────────
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        public static extern IntPtr C_GetHeaderSourceFileName(IntPtr handle);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        public static extern IntPtr C_GetHeaderUsername(IntPtr handle);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        public static extern IntPtr C_GetHeaderMachineName(IntPtr handle);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern uint C_GetHeaderMagic(IntPtr handle);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern byte C_GetHeaderMajorVersion(IntPtr handle);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern byte C_GetHeaderMinorVersion(IntPtr handle);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern ushort C_GetHeaderGameId(IntPtr handle);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern ulong C_GetHeaderCompilationTime(IntPtr handle);

        // ── String table ─────────────────────────────────────
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern ushort C_GetStringTableCount(IntPtr handle);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int C_GetStringUtf8(IntPtr handle, ushort index, byte[] buffer, int bufferSize);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        public static extern int C_GetStringWide(IntPtr handle, ushort index, char[] buffer, int bufferSize);

        // ── Debug info ───────────────────────────────────────
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern byte C_HasDebugInfo(IntPtr handle);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern ulong C_GetDebugModificationTime(IntPtr handle);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern ushort C_GetDebugFunctionCount(IntPtr handle);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int C_GetDebugFunctionInfo(IntPtr handle, ushort index,
            out ushort objectNameIndex, out ushort stateNameIndex, out ushort functionNameIndex,
            out byte functionType, out IntPtr lineNumbers, out int lineCount);

        // ── User flags ───────────────────────────────────────
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern ushort C_GetUserFlagCount(IntPtr handle);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int C_GetUserFlagInfo(IntPtr handle, ushort index,
            out ushort flagNameIndex, out byte flagIndex);

        // ── Objects ──────────────────────────────────────────
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern ushort C_GetObjectCount(IntPtr handle);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int C_GetObjectInfo(IntPtr handle, ushort index,
            out ushort nameIndex, out uint size);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int C_GetObjectData(IntPtr handle, ushort objectIndex,
            out ushort parentClassName, out ushort docString,
            out uint userFlags, out ushort autoStateName);

        // ── Variables ────────────────────────────────────────
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern ushort C_GetVariableCount(IntPtr handle, ushort objectIndex);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int C_GetVariableInfo(IntPtr handle, ushort objectIndex, ushort varIndex,
            out ushort name, out ushort typeName,
            out uint userFlags, out byte dataType, IntPtr dataValue);

        // ── Properties ───────────────────────────────────────
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern ushort C_GetPropertyCount(IntPtr handle, ushort objectIndex);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int C_GetPropertyInfo(IntPtr handle, ushort objectIndex, ushort propIndex,
            out ushort name, out ushort type, out ushort docstring,
            out uint userFlags, out byte flags, out ushort autoVarName);

        // ── States ───────────────────────────────────────────
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern ushort C_GetStateCount(IntPtr handle, ushort objectIndex);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int C_GetStateInfo(IntPtr handle, ushort objectIndex, ushort stateIndex,
            out ushort name, out ushort numFunctions);

        // ── Functions ────────────────────────────────────────
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int C_GetStateFunctionInfo(IntPtr handle,
            ushort objectIndex, ushort stateIndex, ushort funcIndex,
            out ushort functionName, out ushort returnType, out ushort docString,
            out uint userFlags, out byte flags,
            out ushort numParams, out ushort numLocals, out ushort numInstructions);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern ushort C_GetFunctionParamCount(IntPtr handle,
            ushort objectIndex, ushort stateIndex, ushort funcIndex);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int C_GetFunctionParamInfo(IntPtr handle,
            ushort objectIndex, ushort stateIndex, ushort funcIndex, ushort paramIndex,
            out ushort name, out ushort type);

        // ── Function locals ──────────────────────────────────
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern ushort C_GetFunctionLocalCount(IntPtr handle,
            ushort objectIndex, ushort stateIndex, ushort funcIndex);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int C_GetFunctionLocalInfo(IntPtr handle,
            ushort objectIndex, ushort stateIndex, ushort funcIndex, ushort localIndex,
            out ushort name, out ushort type);

        // ── Instructions ─────────────────────────────────────
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int C_GetInstructionInfo(IntPtr handle,
            ushort objectIndex, ushort stateIndex, ushort funcIndex, ushort instrIndex,
            out byte opcode, out ushort argCount);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int C_GetInstructionArgument(IntPtr handle,
            ushort objectIndex, ushort stateIndex, ushort funcIndex, ushort instrIndex,
            ushort argIndex, out byte type, IntPtr value);

        // ── Memory management ────────────────────────────────
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void C_FreeBuffer(IntPtr buffer);

        #endregion


        public static string GetVersion()
        {
            try
            {
                int length = C_GetVersionLength();
                if (length <= 0) return "Unknown";
                IntPtr ptr = C_GetVersion();
                return ptr == IntPtr.Zero ? "Unknown" : Marshal.PtrToStringAnsi(ptr, length);
            }
            catch (Exception ex) { return "Error: " + ex.Message; }
        }

        static PexInterop()
        {
            try { Version = GetVersion(); }
            catch (Exception ex) { Version = "Error: " + ex.Message; }
        }
    }

    public class PexReader : IDisposable
    {
        private IntPtr _Handle;
        private bool _Disposed = false;

        public string PexPath { get; private set; } = "";
        public PexHeader Header { get; private set; } = new PexHeader();
        public List<PexString> StringTable { get; private set; } = new List<PexString>();
        public List<PexObject> Objects { get; private set; } = new List<PexObject>();
        public List<PexUserFlag> UserFlags { get; private set; } = new List<PexUserFlag>();
        public PexDebugInfo DebugInfo { get; private set; } = new PexDebugInfo();

        public PexReader()
        {
            _Handle = PexInterop.C_CreateInstance();
            if (_Handle == IntPtr.Zero)
                throw new Exception("Failed to create PexData instance.");
        }

        public void Dispose()
        {
            if (!_Disposed)
            {
                if (_Handle != IntPtr.Zero)
                {
                    PexInterop.C_DestroyInstance(_Handle);
                    _Handle = IntPtr.Zero;
                }
                _Disposed = true;
            }
            GC.SuppressFinalize(this);
        }

        ~PexReader() { Dispose(); }

        private void EnsureNotDisposed()
        {
            if (_Disposed || _Handle == IntPtr.Zero)
                throw new ObjectDisposedException(nameof(PexReader));
        }

        public void Close()
        {
            PexInterop.C_Close(_Handle);
            PexInterop.C_DestroyInstance(_Handle);
            this._Handle = PexInterop.C_CreateInstance();
        }

        public void LoadPex(string path)
        {
            EnsureNotDisposed();
            Clear();

            if (!System.IO.File.Exists(path))
                throw new System.IO.FileNotFoundException("PEX file not found.", path);

            int result = PexInterop.C_ReadPex(_Handle, path);
            if (result <= 0)
                throw new Exception("Failed to load PEX file: " + path);

            PexPath = path;
            LoadHeader();
            LoadStringTable();
            LoadDebugInfo();
            LoadUserFlags();
            LoadObjects();
        }

        public int SavePex(string outputPath)
        {
            EnsureNotDisposed();
            return PexInterop.C_SavePex(_Handle, outputPath);
        }

        public int ModifyStringTable(ushort index, string str)
        {
            EnsureNotDisposed();
            byte[] bytes = Encoding.UTF8.GetBytes(str + "\0");
            IntPtr ptr = Marshal.AllocHGlobal(bytes.Length);
            try
            {
                Marshal.Copy(bytes, 0, ptr, bytes.Length);
                return PexInterop.C_ModifyStringTable(_Handle, index, ptr);
            }
            finally { Marshal.FreeHGlobal(ptr); }
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
                Magic = PexInterop.C_GetHeaderMagic(_Handle),
                MajorVersion = PexInterop.C_GetHeaderMajorVersion(_Handle),
                MinorVersion = PexInterop.C_GetHeaderMinorVersion(_Handle),
                GameId = PexInterop.C_GetHeaderGameId(_Handle),
                CompilationTime = PexInterop.C_GetHeaderCompilationTime(_Handle),
                SourceFileName = PtrToWideStr(PexInterop.C_GetHeaderSourceFileName(_Handle)),
                Username = PtrToWideStr(PexInterop.C_GetHeaderUsername(_Handle)),
                MachineName = PtrToWideStr(PexInterop.C_GetHeaderMachineName(_Handle)),
            };
        }

        private void LoadStringTable()
        {
            StringTable.Clear();
            ushort count = PexInterop.C_GetStringTableCount(_Handle);
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
            DebugInfo.HasDebugInfo = PexInterop.C_HasDebugInfo(_Handle) != 0;
            if (!DebugInfo.HasDebugInfo) return;

            DebugInfo.ModificationTime = PexInterop.C_GetDebugModificationTime(_Handle);
            DebugInfo.FunctionCount = PexInterop.C_GetDebugFunctionCount(_Handle);

            for (ushort i = 0; i < DebugInfo.FunctionCount; i++)
            {
                if (PexInterop.C_GetDebugFunctionInfo(_Handle, i,
                    out ushort objIdx, out ushort stateIdx, out ushort funcIdx,
                    out byte funcType, out IntPtr linePtr, out int lineCount) > 0)
                {
                    DebugInfo.Functions.Add(new PexDebugFunction
                    {
                        ObjectNameIndex = objIdx,
                        StateNameIndex = stateIdx,
                        FunctionNameIndex = funcIdx,
                        FunctionType = funcType,
                        InstructionCount = (ushort)lineCount,
                        LineNumbers = ReadUshortArray(linePtr, lineCount)
                    });

                    if (linePtr != IntPtr.Zero)
                        PexInterop.C_FreeBuffer(linePtr);
                }
            }
        }

        private void LoadUserFlags()
        {
            UserFlags.Clear();
            ushort count = PexInterop.C_GetUserFlagCount(_Handle);
            for (ushort i = 0; i < count; i++)
            {
                if (PexInterop.C_GetUserFlagInfo(_Handle, i,
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
            ushort count = PexInterop.C_GetObjectCount(_Handle);
            for (ushort i = 0; i < count; i++)
            {
                if (PexInterop.C_GetObjectInfo(_Handle, i, out ushort nameIndex, out uint size) > 0 &&
                    PexInterop.C_GetObjectData(_Handle, i, out ushort parentClass, out ushort docStr,
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
            ushort count = PexInterop.C_GetVariableCount(_Handle, objectIndex);
            for (ushort j = 0; j < count; j++)
            {
                if (PexInterop.C_GetVariableInfo(_Handle, objectIndex, j,
                    out ushort name, out ushort typeName,
                    out uint userFlags, out byte dataType, IntPtr.Zero) > 0)
                {
                    obj.Variables.Add(new PexVariable
                    {
                        NameIndex = name,
                        TypeNameIndex = typeName,
                        UserFlags = userFlags,
                        DataType = dataType,
                        DataValue = GetVariableDataValue(dataType, objectIndex, j)
                    });
                }
            }
        }

        private object GetVariableDataValue(byte dataType, ushort objectIndex, ushort varIndex)
        {
            try
            {
                switch (dataType)
                {
                    case 0: return null;

                    case 1:
                    case 2:
                        {
                            IntPtr ptr = Marshal.AllocHGlobal(sizeof(ushort));
                            try
                            {
                                if (PexInterop.C_GetVariableInfo(_Handle, objectIndex, varIndex,
                                    out _, out _, out _, out _, ptr) > 0)
                                    return GetString((ushort)Marshal.ReadInt16(ptr));
                            }
                            finally { Marshal.FreeHGlobal(ptr); }
                            return "";
                        }

                    case 3:
                        {
                            IntPtr ptr = Marshal.AllocHGlobal(sizeof(int));
                            try
                            {
                                if (PexInterop.C_GetVariableInfo(_Handle, objectIndex, varIndex,
                                    out _, out _, out _, out _, ptr) > 0)
                                    return Marshal.ReadInt32(ptr);
                            }
                            finally { Marshal.FreeHGlobal(ptr); }
                            return 0;
                        }

                    case 4:
                        {
                            IntPtr ptr = Marshal.AllocHGlobal(sizeof(float));
                            try
                            {
                                if (PexInterop.C_GetVariableInfo(_Handle, objectIndex, varIndex,
                                    out _, out _, out _, out _, ptr) > 0)
                                {
                                    byte[] b = new byte[4];
                                    Marshal.Copy(ptr, b, 0, 4);
                                    return BitConverter.ToSingle(b, 0);
                                }
                            }
                            finally { Marshal.FreeHGlobal(ptr); }
                            return 0.0f;
                        }

                    case 5:
                        {
                            IntPtr ptr = Marshal.AllocHGlobal(sizeof(byte));
                            try
                            {
                                if (PexInterop.C_GetVariableInfo(_Handle, objectIndex, varIndex,
                                    out _, out _, out _, out _, ptr) > 0)
                                    return Marshal.ReadByte(ptr) != 0;
                            }
                            finally { Marshal.FreeHGlobal(ptr); }
                            return false;
                        }

                    default: return null;
                }
            }
            catch { return null; }
        }

        private void LoadObjectProperties(PexObject obj, ushort objectIndex)
        {
            ushort count = PexInterop.C_GetPropertyCount(_Handle, objectIndex);
            for (ushort j = 0; j < count; j++)
            {
                if (PexInterop.C_GetPropertyInfo(_Handle, objectIndex, j,
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
            ushort count = PexInterop.C_GetStateCount(_Handle, objectIndex);
            for (ushort j = 0; j < count; j++)
            {
                if (PexInterop.C_GetStateInfo(_Handle, objectIndex, j,
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
                if (PexInterop.C_GetStateFunctionInfo(_Handle,
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
            ushort count = PexInterop.C_GetFunctionParamCount(_Handle, objectIndex, stateIndex, funcIndex);
            for (ushort i = 0; i < count; i++)
            {
                if (PexInterop.C_GetFunctionParamInfo(_Handle,
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
            ushort count = PexInterop.C_GetFunctionLocalCount(_Handle, objectIndex, stateIndex, funcIndex);
            for (ushort i = 0; i < count; i++)
            {
                if (PexInterop.C_GetFunctionLocalInfo(_Handle,
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
                if (PexInterop.C_GetInstructionInfo(_Handle,
                    objectIndex, stateIndex, funcIndex, i,
                    out byte opcode, out ushort argCount) > 0)
                {
                    var instr = new PexInstruction { Opcode = opcode };

                    for (ushort argIdx = 0; argIdx < argCount; argIdx++)
                    {
                        IntPtr argPtr = Marshal.AllocHGlobal(8);
                        try
                        {
                            if (PexInterop.C_GetInstructionArgument(_Handle,
                                objectIndex, stateIndex, funcIndex, i, argIdx,
                                out byte argType, argPtr) > 0)
                            {
                                var arg = new PexInstructionArgument { Type = argType };
                                switch (argType)
                                {
                                    case 0: arg.Value = null; break;
                                    case 1: case 2: arg.Value = (ushort)Marshal.ReadInt16(argPtr); break;
                                    case 3: arg.Value = Marshal.ReadInt32(argPtr); break;
                                    case 4:
                                        byte[] fb = new byte[4];
                                        Marshal.Copy(argPtr, fb, 0, 4);
                                        arg.Value = BitConverter.ToSingle(fb, 0);
                                        break;
                                    case 5: arg.Value = Marshal.ReadByte(argPtr) != 0; break;
                                    default: arg.Value = null; break;
                                }
                                instr.Arguments.Add(arg);
                            }
                        }
                        finally { Marshal.FreeHGlobal(argPtr); }
                    }

                    func.Instructions.Add(instr);
                }
            }
        }

        public string GetString(ushort index)
            => index < StringTable.Count ? StringTable[index].Value : "";

        private string GetStringUtf8(ushort index)
        {
            try
            {
                int len = PexInterop.C_GetStringUtf8(_Handle, index, null, 0);
                if (len <= 0) return "";
                byte[] buf = new byte[len + 1];
                PexInterop.C_GetStringUtf8(_Handle, index, buf, buf.Length);
                int nullIdx = Array.IndexOf(buf, (byte)0);
                return Encoding.UTF8.GetString(buf, 0, nullIdx >= 0 ? nullIdx : len);
            }
            catch { return ""; }
        }

        private static string PtrToWideStr(IntPtr ptr)
            => ptr != IntPtr.Zero ? (Marshal.PtrToStringUni(ptr) ?? "") : "";

        private static ushort[] ReadUshortArray(IntPtr ptr, int count)
        {
            if (ptr == IntPtr.Zero || count <= 0) return Array.Empty<ushort>();
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
        public class PexVariable { public ushort NameIndex, TypeNameIndex; public uint UserFlags; public byte DataType; public object DataValue = ""; public string GetName(PexReader r) => r?.GetString(NameIndex) ?? ""; public string GetTypeName(PexReader r) => r?.GetString(TypeNameIndex) ?? ""; }
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