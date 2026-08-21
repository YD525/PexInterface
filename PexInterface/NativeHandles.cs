using System;
using System.Runtime.InteropServices;

namespace PexInterface
{
    /// <summary>
    /// Owns a native PEX reader instance created by <see cref="PexInterop.C_CreateInstance"/>.
    /// </summary>
    /// <remarks>
    /// The handle is released with <see cref="PexInterop.C_DestroyInstance"/>. Instances are not thread-safe;
    /// callers must serialize access to the owning <see cref="PexReader"/>.
    /// </remarks>
    internal sealed class PexInstanceSafeHandle : SafeHandle
    {
        private PexInstanceSafeHandle()
            : base(IntPtr.Zero, true)
        {
        }

        /// <inheritdoc />
        public override bool IsInvalid => handle == IntPtr.Zero;

        /// <summary>
        /// Creates an owned native PEX reader instance.
        /// </summary>
        /// <returns>A safe handle that owns the native instance.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the native library cannot allocate a reader instance.
        /// </exception>
        internal static PexInstanceSafeHandle Create()
        {
            IntPtr nativeHandle = PexInterop.C_CreateInstance();
            if (nativeHandle == IntPtr.Zero)
                throw new InvalidOperationException("Failed to create a native PEX reader instance.");

            var safeHandle = new PexInstanceSafeHandle();
            safeHandle.SetHandle(nativeHandle);
            return safeHandle;
        }

        /// <inheritdoc />
        protected override bool ReleaseHandle()
        {
            try
            {
                PexInterop.C_DestroyInstance(handle);
                return true;
            }
            catch (DllNotFoundException)
            {
                return false;
            }
            catch (EntryPointNotFoundException)
            {
                return false;
            }
            catch (BadImageFormatException)
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Owns a native line-number array returned by <see cref="PexInterop.C_GetDebugFunctionInfo"/>.
    /// </summary>
    /// <remarks>
    /// The pointer contains <c>lineCount</c> unsigned 16-bit elements and must be released by the same
    /// PEX native module through <see cref="PexInterop.C_FreeBuffer"/>.
    /// </remarks>
    internal sealed class PexLineNumberBufferSafeHandle : SafeHandle
    {
        /// <summary>
        /// Takes ownership of a native line-number array.
        /// </summary>
        /// <param name="buffer">The nullable pointer returned by the native debug-info call.</param>
        internal PexLineNumberBufferSafeHandle(IntPtr buffer)
            : base(IntPtr.Zero, true)
        {
            SetHandle(buffer);
        }

        /// <inheritdoc />
        public override bool IsInvalid => handle == IntPtr.Zero;

        /// <inheritdoc />
        protected override bool ReleaseHandle()
        {
            try
            {
                PexInterop.C_FreeBuffer(handle);
                return true;
            }
            catch (DllNotFoundException)
            {
                return false;
            }
            catch (EntryPointNotFoundException)
            {
                return false;
            }
            catch (BadImageFormatException)
            {
                return false;
            }
        }
    }
}
