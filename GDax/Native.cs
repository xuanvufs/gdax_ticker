using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace GDax
{
    public static class Native
    {
        public const int STD_OUTPUT_HANDLE = -11;
        public const int MY_CODE_PAGE = 437;
        public const uint GENERIC_WRITE = 0x40000000;
        public const uint FILE_SHARE_WRITE = 0x2;
        public const uint OPEN_EXISTING = 0x3;
        public static int WM_NCLBUTTONDOWN => 0xA1;
        public static int HT_CAPTION => 0x2;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct DATA_BLOB
        {
            public int cbData;
            public IntPtr pbData;
        }

        [Flags]
        private enum CryptProtectFlags
        {
            // for remote-access situations where ui is not an option
            // if UI was specified on protect or unprotect operation, the call
            // will fail and GetLastError() will indicate ERROR_PASSWORD_RESTRICTION
            CRYPTPROTECT_UI_FORBIDDEN = 0x1,

            // per machine protected data -- any user on machine where CryptProtectData
            // took place may CryptUnprotectData
            CRYPTPROTECT_LOCAL_MACHINE = 0x4,

            // force credential synchronize during CryptProtectData()
            // Synchronize is only operation that occurs during this operation
            CRYPTPROTECT_CRED_SYNC = 0x8,

            // Generate an Audit on protect and unprotect operations
            CRYPTPROTECT_AUDIT = 0x10,

            // Protect data with a non-recoverable key
            CRYPTPROTECT_NO_RECOVERY = 0x20,

            // Verify the protection of a protected blob
            CRYPTPROTECT_VERIFY_PROTECTION = 0x40
        }

        [DllImport("kernel32.dll", EntryPoint = "GetStdHandle", SetLastError = true, CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll", EntryPoint = "AllocConsole", SetLastError = true, CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern int AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr CreateFile(string lpFileName, uint dwDesiredAccess, uint dwShareMode, uint lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, uint hTemplateFile);

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        [DllImport("Crypt32.dll", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CryptProtectData(ref DATA_BLOB pDataIn, String szDataDescr, ref DATA_BLOB pOptionalEntropy, IntPtr pvReserved, IntPtr pPromptStruct, CryptProtectFlags dwFlags, ref DATA_BLOB pDataOut);

        [DllImport("Crypt32.dll", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CryptUnprotectData(ref DATA_BLOB pDataIn, String szDataDescr, ref DATA_BLOB pOptionalEntropy, IntPtr pvReserved, IntPtr pPromptStruct, CryptProtectFlags dwFlags, ref DATA_BLOB pDataOut);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern void ZeroMemory(IntPtr handle, int length);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr LocalFree(IntPtr handle);

        //public static byte[] GetEncryptedData(SecureString value)
        //{
        //    var output = new DATA_BLOB();
        //    var input = new DATA_BLOB();
        //    var salt = new DATA_BLOB();

        //    RuntimeHelpers.PrepareConstrainedRegions();
        //    try
        //    {
        //        input.pbData = Marshal.SecureStringToBSTR(value);
        //        input.cbData = Marshal.ReadInt32(input.pbData, -4);

        //        salt.cbData = saltBytes.Length;
        //        salt.pbData = Marshal.AllocHGlobal(saltBytes.Length);
        //        Marshal.Copy(saltBytes, 0, salt.pbData, saltBytes.Length);

        //        if (!CryptProtectData(ref input, string.Empty, ref salt, IntPtr.Zero, IntPtr.Zero, CryptProtectFlags.CRYPTPROTECT_LOCAL_MACHINE | CryptProtectFlags.CRYPTPROTECT_UI_FORBIDDEN, ref output))
        //            throw new CryptographicException(Marshal.GetLastWin32Error());

        //        var encryptedData = new byte[output.cbData];
        //        Marshal.Copy(output.pbData, encryptedData, 0, encryptedData.Length);

        //        return encryptedData;
        //    }
        //    finally
        //    {
        //        if (output.pbData != IntPtr.Zero)
        //        {
        //            ZeroMemory(output.pbData, output.cbData);
        //            LocalFree(output.pbData);
        //        }

        //        if (input.pbData != IntPtr.Zero)
        //            Marshal.ZeroFreeBSTR(input.pbData);

        //        if (salt.pbData != IntPtr.Zero)
        //            Marshal.FreeHGlobal(salt.pbData);
        //    }
        //}

        //public static SecureString GetSecureString(byte[] encryptedData)
        //{
        //    var handle = new GCHandle();
        //    var output = new DATA_BLOB();
        //    var input = new DATA_BLOB();
        //    var salt = new DATA_BLOB();

        //    RuntimeHelpers.PrepareConstrainedRegions();
        //    try
        //    {
        //        handle = GCHandle.Alloc(encryptedData, GCHandleType.Pinned);
        //        input.cbData = encryptedData.Length;
        //        input.pbData = handle.AddrOfPinnedObject();

        //        salt.cbData = saltBytes.Length;
        //        salt.pbData = Marshal.AllocHGlobal(saltBytes.Length);
        //        Marshal.Copy(saltBytes, 0, salt.pbData, saltBytes.Length);

        //        if (!CryptUnprotectData(ref input, string.Empty, ref salt, IntPtr.Zero, IntPtr.Zero, CryptProtectFlags.CRYPTPROTECT_LOCAL_MACHINE | CryptProtectFlags.CRYPTPROTECT_UI_FORBIDDEN, ref output))
        //            throw new CryptographicException(Marshal.GetLastWin32Error());

        //        var data = new SecureString();
        //        for (var i = 0; i < output.cbData; i += 2)
        //        {
        //            var chr = (char)Marshal.ReadInt16(output.pbData, i);
        //            data.AppendChar(chr);
        //        }

        //        return data;
        //    }
        //    finally
        //    {
        //        if (output.pbData != IntPtr.Zero)
        //        {
        //            ZeroMemory(output.pbData, output.cbData);
        //            LocalFree(output.pbData);
        //        }

        //        if (handle.IsAllocated)
        //            handle.Free();

        //        if (salt.pbData != IntPtr.Zero)
        //            Marshal.FreeHGlobal(salt.pbData);
        //    }
        //}
    }
}
