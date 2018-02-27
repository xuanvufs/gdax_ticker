using Microsoft.Win32.SafeHandles;
using System;
using System.IO;
using System.Text;

namespace GDax
{
    public static class Helper
    {
        public static void CreateDebugConsole()
        {
            if (Native.AllocConsole() == 0) return;

            // Console.OpenStandardOutput eventually calls into GetStdHandle. As per MSDN documentation of GetStdHandle:
            // http://msdn.microsoft.com/en-us/library/windows/desktop/ms683231(v=vs.85).aspx will return the redirected
            // handle and not the allocated console: "The standard handles of a process may be redirected by a call to
            // SetStdHandle, in which case  GetStdHandle returns the redirected handle. If the standard handles have been
            // redirected, you can specify the CONIN$ value in a call to the CreateFile function to get a handle to a
            // console's input buffer. Similarly, you can specify the CONOUT$ value to get a handle to a console's active
            // screen buffer."

            // Get the handle to CONOUT$.    
            var stdHandle = Native.CreateFile("CONOUT$", Native.GENERIC_WRITE, Native.FILE_SHARE_WRITE, 0, Native.OPEN_EXISTING, 0, 0);
            var safeFileHandle = new SafeFileHandle(stdHandle, true);
            var standardOutput = new StreamWriter(new FileStream(safeFileHandle, FileAccess.Write), Encoding.GetEncoding(Native.MY_CODE_PAGE))
            {
                AutoFlush = true
            };

            Console.SetOut(standardOutput);
        }
    }
}
