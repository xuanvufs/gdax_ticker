using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

namespace GDax.Helpers
{
    public static class Comparator
    {
        public static bool AreEqual(SecureString s1, SecureString s2)
        {
            if (s1 == s2 && s1 == null) return true;
            if (s1 == null || s2 == null) return false;
            if (s1.Length != s2.Length) return false;

            var p1 = IntPtr.Zero;
            var p2 = IntPtr.Zero;

            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                p1 = Marshal.SecureStringToBSTR(s1);
                p2 = Marshal.SecureStringToBSTR(s2);
                var length = Marshal.ReadInt32(p1, -4);
                var result = 0;
                for (var i = 0; i < length; ++i)
                    result |= Marshal.ReadByte(p1, i) ^ Marshal.ReadByte(p2, i);
                return result == 0;
            }
            finally
            {
                if (p1 != IntPtr.Zero)
                    Marshal.ZeroFreeBSTR(p1);
                if (p2 != IntPtr.Zero)
                    Marshal.ZeroFreeBSTR(p2);
            }
        }

        public static bool AreEqual<T>(T t1, T t2)
        {
            return EqualityComparer<T>.Default.Equals(t1, t2);
        }
    }
}