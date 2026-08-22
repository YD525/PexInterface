using System;
using System.Runtime.InteropServices;

namespace PexInterface
{
    /// <summary>
    /// Owns a native PEX reader instance created through the validated native interop boundary.
    /// </summary>
    /// <remarks>
    /// The handle is released by the same native module that created it. Instances are not thread-safe; callers
    /// must serialize access to the owning <see cref="PexReader"/>.
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
        /// <exception cref="OutOfMemoryException">
        /// Thrown when the native module cannot allocate an instance.
        /// </exception>
        /// <exception cref="NotSupportedException">Thrown when the loaded native ABI is incompatible.</exception>
        internal static PexInstanceSafeHandle Create()
        {
            PexNativeContract.EnsureCompatible();
            IntPtr nativeHandle = PexNativeMethods.C_CreateInstance();
            if (nativeHandle == IntPtr.Zero)
                throw PexNativeContract.CreateException("create a PEX reader instance");

            var safeHandle = new PexInstanceSafeHandle();
            safeHandle.SetHandle(nativeHandle);
            return safeHandle;
        }

        /// <inheritdoc />
        protected override bool ReleaseHandle()
        {
            try
            {
                PexNativeMethods.C_DestroyInstance(handle);
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
    /// Owns a native line-number array returned by the debug-information ABI call.
    /// </summary>
    /// <remarks>
    /// The pointer contains <c>lineCount</c> unsigned 16-bit elements and must be released by the same
    /// PEX native module through its matching buffer-release function.
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
                PexNativeMethods.C_FreeBuffer(handle);
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
