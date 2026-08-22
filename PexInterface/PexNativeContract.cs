using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace PexInterface
{
    /// <summary>
    /// Validates the native ABI and centralizes native error and string marshaling rules.
    /// </summary>
    internal static class PexNativeContract
    {
        internal const uint SupportedAbiVersion = 1;
        private const int MaximumErrorLength = 4096;
        private const int MaximumProductVersionLength = 64;
        private const int MaximumPexStringLength = ushort.MaxValue;
        private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);
        private static readonly Lazy<uint> LoadedAbiVersion = new Lazy<uint>(LoadAbiVersion, true);

        /// <summary>
        /// Gets the validated ABI version of the loaded native module.
        /// </summary>
        /// <returns>The exact supported ABI version.</returns>
        /// <exception cref="NotSupportedException">
        /// Thrown when the native module does not expose or implement the supported ABI.
        /// </exception>
        internal static uint AbiVersion => LoadedAbiVersion.Value;

        /// <summary>
        /// Ensures that the loaded native module implements the supported ABI.
        /// </summary>
        /// <exception cref="NotSupportedException">
        /// Thrown when the native module does not expose or implement the supported ABI.
        /// </exception>
        internal static void EnsureCompatible()
        {
            ValidateAbiVersion(AbiVersion);
        }

        /// <summary>
        /// Validates a reported ABI version independently from module loading.
        /// </summary>
        /// <param name="abiVersion">The version reported by the native module.</param>
        /// <exception cref="NotSupportedException">
        /// Thrown when <paramref name="abiVersion"/> is unsupported.
        /// </exception>
        internal static void ValidateAbiVersion(uint abiVersion)
        {
            if (abiVersion != SupportedAbiVersion)
            {
                throw new NotSupportedException(
                    string.Format(
                        "Unsupported PEX native ABI version {0}. This assembly requires ABI version {1}.",
                        abiVersion,
                        SupportedAbiVersion));
            }
        }

        /// <summary>
        /// Returns the managed product version from the validated native module.
        /// </summary>
        /// <returns>The UTF-8 product version, or <c>Unknown</c> when no value is available.</returns>
        internal static string ReadProductVersion()
        {
            EnsureCompatible();
            int length = PexNativeMethods.C_GetVersionLength();
            IntPtr pointer = PexNativeMethods.C_GetVersion();
            if (length <= 0 || length > MaximumProductVersionLength || pointer == IntPtr.Zero)
                return "Unknown";

            byte[] bytes = new byte[length];
            Marshal.Copy(pointer, bytes, 0, bytes.Length);
            return StrictUtf8.GetString(bytes);
        }

        /// <summary>
        /// Copies a borrowed UTF-16 pointer immediately into a managed string.
        /// </summary>
        /// <param name="pointer">The nullable borrowed native pointer.</param>
        /// <returns>The copied string, or an empty string for a null pointer.</returns>
        internal static string ReadBorrowedUnicodeString(IntPtr pointer)
        {
            return pointer == IntPtr.Zero ? string.Empty : Marshal.PtrToStringUni(pointer) ?? string.Empty;
        }

        /// <summary>
        /// Reads a string-table entry with bounded two-call UTF-8 marshaling.
        /// </summary>
        /// <param name="handle">The non-null borrowed reader handle.</param>
        /// <param name="index">The string-table index.</param>
        /// <returns>The decoded UTF-8 value.</returns>
        /// <exception cref="Exception">Thrown when the native request fails or returns invalid UTF-8.</exception>
        internal static string ReadStringUtf8(IntPtr handle, ushort index)
        {
            EnsureCompatible();
            int length = PexNativeMethods.C_GetStringUtf8(handle, index, null, 0);
            if (length < 0)
                throw CreateException("read a PEX string");
            if (length == 0)
                return string.Empty;
            if (length > MaximumPexStringLength)
                throw new InvalidDataException("The native PEX string length exceeds the supported format limit.");

            byte[] buffer = new byte[length + 1];
            int copiedLength = PexNativeMethods.C_GetStringUtf8(handle, index, buffer, buffer.Length);
            if (copiedLength != length)
                throw CreateException("copy a PEX string");
            if (buffer[length] != 0)
                throw new InvalidDataException("The native PEX string is not null-terminated.");

            return StrictUtf8.GetString(buffer, 0, length);
        }

        /// <summary>
        /// Pins a null-terminated UTF-8 string for the duration of a native call.
        /// </summary>
        /// <param name="value">The managed value to encode.</param>
        /// <param name="action">The native call that consumes the temporary pointer.</param>
        /// <returns>The native result returned by <paramref name="action"/>.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="value"/> or <paramref name="action"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="value"/> contains an embedded null character.
        /// </exception>
        internal static int WithUtf8String(string value, Func<IntPtr, int> action)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            if (value.IndexOf('\0') >= 0)
                throw new ArgumentException("PEX strings cannot contain embedded null characters.", nameof(value));

            byte[] payload = StrictUtf8.GetBytes(value + "\0");
            GCHandle pinnedPayload = GCHandle.Alloc(payload, GCHandleType.Pinned);
            try
            {
                return action(pinnedPayload.AddrOfPinnedObject());
            }
            finally
            {
                pinnedPayload.Free();
            }
        }

        /// <summary>
        /// Allocates a four-byte value buffer for a native metadata call and copies its result.
        /// </summary>
        /// <param name="action">The native call that optionally writes the value.</param>
        /// <param name="value">Receives the copied four-byte union value.</param>
        /// <returns>The native result returned by <paramref name="action"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="action"/> is null.</exception>
        internal static int WithValueBuffer(Func<IntPtr, int> action, out PexNativeValue value)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            IntPtr buffer = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(PexNativeValue)));
            try
            {
                Marshal.WriteInt32(buffer, 0);
                int result = action(buffer);
                value = (PexNativeValue)Marshal.PtrToStructure(buffer, typeof(PexNativeValue));
                return result;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        /// <summary>
        /// Creates a managed exception from the thread-local native status and error message.
        /// </summary>
        /// <param name="operation">A user-safe description of the failed operation.</param>
        /// <returns>An exception that represents the native failure category.</returns>
        internal static Exception CreateException(string operation)
        {
            if (string.IsNullOrWhiteSpace(operation))
                operation = "complete the PEX operation";

            PexNativeStatus status = PexNativeMethods.C_GetLastStatus();
            string detail = ReadLastError();
            string message = string.IsNullOrWhiteSpace(detail)
                ? "The native PEX module could not " + operation + "."
                : "The native PEX module could not " + operation + ": " + detail;

            switch (status)
            {
                case PexNativeStatus.InvalidArgument:
                    return new ArgumentException(message);
                case PexNativeStatus.OutOfRange:
                    return new ArgumentOutOfRangeException(null, message);
                case PexNativeStatus.IoError:
                    return new IOException(message);
                case PexNativeStatus.ParseError:
                    return new InvalidDataException(message);
                case PexNativeStatus.OutOfMemory:
                    return new OutOfMemoryException(message);
                case PexNativeStatus.BufferTooSmall:
                case PexNativeStatus.InternalError:
                case PexNativeStatus.Ok:
                default:
                    return new InvalidOperationException(message);
            }
        }

        private static uint LoadAbiVersion()
        {
            try
            {
                uint abiVersion = PexNativeMethods.C_GetAbiVersion();
                ValidateAbiVersion(abiVersion);
                return abiVersion;
            }
            catch (EntryPointNotFoundException exception)
            {
                throw new NotSupportedException(
                    "The loaded PEX native module does not expose the required ABI version query.",
                    exception);
            }
        }

        private static string ReadLastError()
        {
            int length = PexNativeMethods.C_GetLastErrorUtf8(null, 0);
            if (length <= 0 || length > MaximumErrorLength)
                return string.Empty;

            byte[] buffer = new byte[length + 1];
            int copiedLength = PexNativeMethods.C_GetLastErrorUtf8(buffer, buffer.Length);
            if (copiedLength != length || buffer[length] != 0)
                return string.Empty;

            try
            {
                return StrictUtf8.GetString(buffer, 0, length);
            }
            catch (DecoderFallbackException)
            {
                return string.Empty;
            }
        }
    }
}
