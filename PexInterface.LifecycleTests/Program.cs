using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace PexInterface.LifecycleTests
{
    internal static class Program
    {
        private const int IterationCount = 10000;

        private static int Main()
        {
            string fixturePath = Path.Combine(
                Path.GetTempPath(),
                "PexInterface-Lifecycle-" + Guid.NewGuid().ToString("N") + ".pex");

            try
            {
                File.WriteAllBytes(fixturePath, CreateMinimalPex());
                VerifyDebugBufferOwnership(fixturePath);
                VerifyReaderLifecycle();
                Console.WriteLine("Lifecycle tests passed: 20,000 ownership cycles.");
                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception.GetType().Name + ": " + exception.Message);
                return 1;
            }
            finally
            {
                try
                {
                    if (File.Exists(fixturePath))
                        File.Delete(fixturePath);
                }
                catch (IOException)
                {
                    // The operating system will eventually clean its temporary directory.
                }
                catch (UnauthorizedAccessException)
                {
                    // Cleanup failure does not invalidate the ownership assertions.
                }
            }
        }

        private static void VerifyDebugBufferOwnership(string fixturePath)
        {
            using (var reader = new PexReader())
            {
                reader.LoadPex(fixturePath);
                IntPtr nativeHandle = reader.GetHandle();

                for (int iteration = 0; iteration < IterationCount; iteration++)
                {
                    int result = PexInterop.C_GetDebugFunctionInfo(
                        nativeHandle,
                        0,
                        out ushort objectNameIndex,
                        out ushort stateNameIndex,
                        out ushort functionNameIndex,
                        out byte functionType,
                        out IntPtr lineNumbers,
                        out int lineCount);

                    Require(result == 1, "The native debug function could not be read.");
                    Require(objectNameIndex == 0 && stateNameIndex == 1 && functionNameIndex == 2,
                        "The native debug function indexes changed.");
                    Require(functionType == 0 && lineCount == 3, "The native debug function shape changed.");

                    using (var buffer = new PexLineNumberBufferSafeHandle(lineNumbers))
                    {
                        Require(!buffer.IsInvalid, "The native line-number buffer is null.");
                        IntPtr pointer = buffer.DangerousGetHandle();
                        Require(Marshal.ReadInt16(pointer, 0) == 10, "The first line number changed.");
                        Require(Marshal.ReadInt16(pointer, 2) == 20, "The second line number changed.");
                        Require(Marshal.ReadInt16(pointer, 4) == 30, "The third line number changed.");
                    }
                }
            }
        }

        private static void VerifyReaderLifecycle()
        {
            for (int iteration = 0; iteration < IterationCount; iteration++)
            {
                var reader = new PexReader();
                Require(reader.GetHandle() != IntPtr.Zero, "The native reader handle is null.");

                reader.Close();
                Require(reader.GetHandle() != IntPtr.Zero, "Close did not create a replacement reader handle.");

                reader.Dispose();
                reader.Dispose();
                RequireThrows<ObjectDisposedException>(() => reader.GetHandle());
                RequireThrows<ObjectDisposedException>(() => reader.Close());
            }
        }

        private static byte[] CreateMinimalPex()
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                WriteUInt32BigEndian(writer, 0xFA57C0DE);
                writer.Write((byte)3);
                writer.Write((byte)9);
                WriteUInt16BigEndian(writer, 1);
                WriteUInt64BigEndian(writer, 0);
                WriteSizedUtf8(writer, string.Empty);
                WriteSizedUtf8(writer, string.Empty);
                WriteSizedUtf8(writer, string.Empty);

                WriteUInt16BigEndian(writer, 3);
                WriteSizedUtf8(writer, "Object");
                WriteSizedUtf8(writer, "State");
                WriteSizedUtf8(writer, "Function");

                writer.Write((byte)1);
                WriteUInt64BigEndian(writer, 0);
                WriteUInt16BigEndian(writer, 1);
                WriteUInt16BigEndian(writer, 0);
                WriteUInt16BigEndian(writer, 1);
                WriteUInt16BigEndian(writer, 2);
                writer.Write((byte)0);
                WriteUInt16BigEndian(writer, 3);
                WriteUInt16BigEndian(writer, 10);
                WriteUInt16BigEndian(writer, 20);
                WriteUInt16BigEndian(writer, 30);

                WriteUInt16BigEndian(writer, 0);
                WriteUInt16BigEndian(writer, 0);
                writer.Flush();
                return stream.ToArray();
            }
        }

        private static void WriteSizedUtf8(BinaryWriter writer, string value)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            WriteUInt16BigEndian(writer, checked((ushort)bytes.Length));
            writer.Write(bytes);
        }

        private static void WriteUInt16BigEndian(BinaryWriter writer, ushort value)
        {
            writer.Write((byte)(value >> 8));
            writer.Write((byte)value);
        }

        private static void WriteUInt32BigEndian(BinaryWriter writer, uint value)
        {
            writer.Write((byte)(value >> 24));
            writer.Write((byte)(value >> 16));
            writer.Write((byte)(value >> 8));
            writer.Write((byte)value);
        }

        private static void WriteUInt64BigEndian(BinaryWriter writer, ulong value)
        {
            writer.Write((byte)(value >> 56));
            writer.Write((byte)(value >> 48));
            writer.Write((byte)(value >> 40));
            writer.Write((byte)(value >> 32));
            writer.Write((byte)(value >> 24));
            writer.Write((byte)(value >> 16));
            writer.Write((byte)(value >> 8));
            writer.Write((byte)value);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        private static void RequireThrows<TException>(Action action)
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

            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }
    }
}
