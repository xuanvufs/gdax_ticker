using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;

namespace GDax.Helpers
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct DATA_BLOB
    {
        public int cbData;
        public IntPtr pbData;
    }

    [Flags]
    internal enum CryptProtectFlags
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

    [Flags]
    public enum WindowStylesEx : uint
    {
        /// <summary>Specifies a window that accepts drag-drop files.</summary>
        WS_EX_ACCEPTFILES = 0x00000010,

        /// <summary>Forces a top-level window onto the taskbar when the window is visible.</summary>
        WS_EX_APPWINDOW = 0x00040000,

        /// <summary>Specifies a window that has a border with a sunken edge.</summary>
        WS_EX_CLIENTEDGE = 0x00000200,

        /// <summary>
        /// Specifies a window that paints all descendants in bottom-to-top painting order using double-buffering.
        /// This cannot be used if the window has a class style of either CS_OWNDC or CS_CLASSDC. This style is not supported in Windows 2000.
        /// </summary>
        /// <remarks>
        /// With WS_EX_COMPOSITED set, all descendants of a window get bottom-to-top painting order using double-buffering.
        /// Bottom-to-top painting order allows a descendent window to have translucency (alpha) and transparency (color-key) effects,
        /// but only if the descendent window also has the WS_EX_TRANSPARENT bit set.
        /// Double-buffering allows the window and its descendents to be painted without flicker.
        /// </remarks>
        WS_EX_COMPOSITED = 0x02000000,

        /// <summary>
        /// Specifies a window that includes a question mark in the title bar. When the user clicks the question mark,
        /// the cursor changes to a question mark with a pointer. If the user then clicks a child window, the child receives a WM_HELP message.
        /// The child window should pass the message to the parent window procedure, which should call the WinHelp function using the HELP_WM_HELP command.
        /// The Help application displays a pop-up window that typically contains help for the child window.
        /// WS_EX_CONTEXTHELP cannot be used with the WS_MAXIMIZEBOX or WS_MINIMIZEBOX styles.
        /// </summary>
        WS_EX_CONTEXTHELP = 0x00000400,

        /// <summary>
        /// Specifies a window which contains child windows that should take part in dialog box navigation.
        /// If this style is specified, the dialog manager recurses into children of this window when performing navigation operations
        /// such as handling the TAB key, an arrow key, or a keyboard mnemonic.
        /// </summary>
        WS_EX_CONTROLPARENT = 0x00010000,

        /// <summary>Specifies a window that has a double border.</summary>
        WS_EX_DLGMODALFRAME = 0x00000001,

        /// <summary>
        /// Specifies a window that is a layered window.
        /// This cannot be used for child windows or if the window has a class style of either CS_OWNDC or CS_CLASSDC.
        /// </summary>
        WS_EX_LAYERED = 0x00080000,

        /// <summary>
        /// Specifies a window with the horizontal origin on the right edge. Increasing horizontal values advance to the left.
        /// The shell language must support reading-order alignment for this to take effect.
        /// </summary>
        WS_EX_LAYOUTRTL = 0x00400000,

        /// <summary>Specifies a window that has generic left-aligned properties. This is the default.</summary>
        WS_EX_LEFT = 0x00000000,

        /// <summary>
        /// Specifies a window with the vertical scroll bar (if present) to the left of the client area.
        /// The shell language must support reading-order alignment for this to take effect.
        /// </summary>
        WS_EX_LEFTSCROLLBAR = 0x00004000,

        /// <summary>
        /// Specifies a window that displays text using left-to-right reading-order properties. This is the default.
        /// </summary>
        WS_EX_LTRREADING = 0x00000000,

        /// <summary>
        /// Specifies a multiple-document interface (MDI) child window.
        /// </summary>
        WS_EX_MDICHILD = 0x00000040,

        /// <summary>
        /// Specifies a top-level window created with this style does not become the foreground window when the user clicks it.
        /// The system does not bring this window to the foreground when the user minimizes or closes the foreground window.
        /// The window does not appear on the taskbar by default. To force the window to appear on the taskbar, use the WS_EX_APPWINDOW style.
        /// To activate the window, use the SetActiveWindow or SetForegroundWindow function.
        /// </summary>
        WS_EX_NOACTIVATE = 0x08000000,

        /// <summary>
        /// Specifies a window which does not pass its window layout to its child windows.
        /// </summary>
        WS_EX_NOINHERITLAYOUT = 0x00100000,

        /// <summary>
        /// Specifies that a child window created with this style does not send the WM_PARENTNOTIFY message to its parent window when it is created or destroyed.
        /// </summary>
        WS_EX_NOPARENTNOTIFY = 0x00000004,

        /// <summary>Specifies an overlapped window.</summary>
        WS_EX_OVERLAPPEDWINDOW = WS_EX_WINDOWEDGE | WS_EX_CLIENTEDGE,

        /// <summary>Specifies a palette window, which is a modeless dialog box that presents an array of commands.</summary>
        WS_EX_PALETTEWINDOW = WS_EX_WINDOWEDGE | WS_EX_TOOLWINDOW | WS_EX_TOPMOST,

        /// <summary>
        /// Specifies a window that has generic "right-aligned" properties. This depends on the window class.
        /// The shell language must support reading-order alignment for this to take effect.
        /// Using the WS_EX_RIGHT style has the same effect as using the SS_RIGHT (static), ES_RIGHT (edit), and BS_RIGHT/BS_RIGHTBUTTON (button) control styles.
        /// </summary>
        WS_EX_RIGHT = 0x00001000,

        /// <summary>Specifies a window with the vertical scroll bar (if present) to the right of the client area. This is the default.</summary>
        WS_EX_RIGHTSCROLLBAR = 0x00000000,

        /// <summary>
        /// Specifies a window that displays text using right-to-left reading-order properties.
        /// The shell language must support reading-order alignment for this to take effect.
        /// </summary>
        WS_EX_RTLREADING = 0x00002000,

        /// <summary>Specifies a window with a three-dimensional border style intended to be used for items that do not accept user input.</summary>
        WS_EX_STATICEDGE = 0x00020000,

        /// <summary>
        /// Specifies a window that is intended to be used as a floating toolbar.
        /// A tool window has a title bar that is shorter than a normal title bar, and the window title is drawn using a smaller font.
        /// A tool window does not appear in the taskbar or in the dialog that appears when the user presses ALT+TAB.
        /// If a tool window has a system menu, its icon is not displayed on the title bar.
        /// However, you can display the system menu by right-clicking or by typing ALT+SPACE.
        /// </summary>
        WS_EX_TOOLWINDOW = 0x00000080,

        /// <summary>
        /// Specifies a window that should be placed above all non-topmost windows and should stay above them, even when the window is deactivated.
        /// To add or remove this style, use the SetWindowPos function.
        /// </summary>
        WS_EX_TOPMOST = 0x00000008,

        /// <summary>
        /// Specifies a window that should not be painted until siblings beneath the window (that were created by the same thread) have been painted.
        /// The window appears transparent because the bits of underlying sibling windows have already been painted.
        /// To achieve transparency without these restrictions, use the SetWindowRgn function.
        /// </summary>
        WS_EX_TRANSPARENT = 0x00000020,

        /// <summary>Specifies a window that has a border with a raised edge.</summary>
        WS_EX_WINDOWEDGE = 0x00000100
    }

    public enum WindowLongFlags
    {
        GWL_EXSTYLE = -20,
        GWLP_HINSTANCE = -6,
        GWLP_HWNDPARENT = -8,
        GWL_ID = -12,
        GWL_STYLE = -16,
        GWL_USERDATA = -21,
        GWL_WNDPROC = -4,
        DWLP_USER = 0x8,
        DWLP_MSGRESULT = 0x0,
        DWLP_DLGPROC = 0x4
    }

    public static class Native
    {
        public const int STD_OUTPUT_HANDLE = -11;
        public const int MY_CODE_PAGE = 437;
        public const uint GENERIC_WRITE = 0x40000000;
        public const uint FILE_SHARE_WRITE = 0x2;
        public const uint OPEN_EXISTING = 0x3;
        private const int SALT_LENGTH = 8;

        public static int WM_NCLBUTTONDOWN => 0xA1;
        public static int HT_CAPTION => 0x2;

        [DllImport("kernel32.dll", EntryPoint = "GetStdHandle", SetLastError = true, CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll", EntryPoint = "AllocConsole", SetLastError = true, CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern int AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr CreateFile(string lpFileName, uint dwDesiredAccess, uint dwShareMode, uint lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, uint hTemplateFile);

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
        private static extern IntPtr IntSetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
        private static extern Int32 IntSetWindowLong(IntPtr hWnd, int nIndex, Int32 dwNewLong);

        private static int IntPtrToInt32(IntPtr intPtr)
        {
            return unchecked((int)intPtr.ToInt64());
        }

        [DllImport("kernel32.dll", EntryPoint = "SetLastError")]
        public static extern void SetLastError(int dwErrorCode);

        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        [DllImport("Crypt32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CryptProtectData(ref DATA_BLOB pDataIn, String szDataDescr, ref DATA_BLOB pOptionalEntropy, IntPtr pvReserved, IntPtr pPromptStruct, CryptProtectFlags dwFlags, ref DATA_BLOB pDataOut);

        [DllImport("Crypt32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CryptUnprotectData(ref DATA_BLOB pDataIn, String szDataDescr, ref DATA_BLOB pOptionalEntropy, IntPtr pvReserved, IntPtr pPromptStruct, CryptProtectFlags dwFlags, ref DATA_BLOB pDataOut);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern void ZeroMemory(IntPtr handle, int length);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr LocalFree(IntPtr handle);

        public static IntPtr SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            int error = 0;
            IntPtr result = IntPtr.Zero;
            // Win32 SetWindowLong doesn't clear error on success
            SetLastError(0);

            if (IntPtr.Size == 4)
            {
                // use SetWindowLong
                Int32 tempResult = IntSetWindowLong(hWnd, nIndex, IntPtrToInt32(dwNewLong));
                error = Marshal.GetLastWin32Error();
                result = new IntPtr(tempResult);
            }
            else
            {
                // use SetWindowLongPtr
                result = IntSetWindowLongPtr(hWnd, nIndex, dwNewLong);
                error = Marshal.GetLastWin32Error();
            }

            if ((result == IntPtr.Zero) && (error != 0))
            {
                throw new System.ComponentModel.Win32Exception(error);
            }

            return result;
        }

        public static byte[] GetEncryptedData(SecureString value)
        {
            var output = new DATA_BLOB();
            var input = new DATA_BLOB();
            var salt = new DATA_BLOB();

            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                var saltBytes = new byte[SALT_LENGTH];
                using (var rng = new RNGCryptoServiceProvider())
                {
                    rng.GetBytes(saltBytes);
                }

                input.pbData = Marshal.SecureStringToBSTR(value);
                input.cbData = Marshal.ReadInt32(input.pbData, -4);

                salt.cbData = SALT_LENGTH;
                salt.pbData = Marshal.AllocHGlobal(SALT_LENGTH);
                Marshal.Copy(saltBytes, 0, salt.pbData, SALT_LENGTH);

                if (!CryptProtectData(ref input, string.Empty, ref salt, IntPtr.Zero, IntPtr.Zero, CryptProtectFlags.CRYPTPROTECT_LOCAL_MACHINE | CryptProtectFlags.CRYPTPROTECT_UI_FORBIDDEN, ref output))
                    throw new CryptographicException(Marshal.GetLastWin32Error());

                var encryptedData = new byte[SALT_LENGTH + output.cbData];
                Array.Copy(saltBytes, encryptedData, SALT_LENGTH);
                Marshal.Copy(output.pbData, encryptedData, SALT_LENGTH, encryptedData.Length);

                return encryptedData;
            }
            finally
            {
                if (output.pbData != IntPtr.Zero)
                {
                    ZeroMemory(output.pbData, output.cbData);
                    LocalFree(output.pbData);
                }

                if (input.pbData != IntPtr.Zero)
                    Marshal.ZeroFreeBSTR(input.pbData);

                if (salt.pbData != IntPtr.Zero)
                    Marshal.FreeHGlobal(salt.pbData);
            }
        }

        public static SecureString GetSecureString(byte[] encryptedData)
        {
            var handle = new GCHandle();
            var output = new DATA_BLOB();
            var input = new DATA_BLOB();
            var salt = new DATA_BLOB();

            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                var saltBytes = new byte[SALT_LENGTH];
                Array.Copy(encryptedData, saltBytes, SALT_LENGTH);

                encryptedData = encryptedData.Skip(SALT_LENGTH).ToArray();
                handle = GCHandle.Alloc(encryptedData, GCHandleType.Pinned);
                input.cbData = encryptedData.Length;
                input.pbData = handle.AddrOfPinnedObject();

                salt.cbData = SALT_LENGTH;
                salt.pbData = Marshal.AllocHGlobal(SALT_LENGTH);
                Marshal.Copy(saltBytes, 0, salt.pbData, SALT_LENGTH);

                if (!CryptUnprotectData(ref input, string.Empty, ref salt, IntPtr.Zero, IntPtr.Zero, CryptProtectFlags.CRYPTPROTECT_LOCAL_MACHINE | CryptProtectFlags.CRYPTPROTECT_UI_FORBIDDEN, ref output))
                    throw new CryptographicException(Marshal.GetLastWin32Error());

                var data = new SecureString();
                for (var i = 0; i < output.cbData; i += 2)
                {
                    var chr = (char)Marshal.ReadInt16(output.pbData, i);
                    data.AppendChar(chr);
                }

                return data;
            }
            finally
            {
                if (output.pbData != IntPtr.Zero)
                {
                    ZeroMemory(output.pbData, output.cbData);
                    LocalFree(output.pbData);
                }

                if (handle.IsAllocated)
                    handle.Free();

                if (salt.pbData != IntPtr.Zero)
                    Marshal.FreeHGlobal(salt.pbData);
            }
        }
    }
}