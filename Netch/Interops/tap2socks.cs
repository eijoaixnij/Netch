using System.Runtime.InteropServices;
using System.Text;
using Serilog;

namespace Netch.Interops
{
    public static class tap2socks
    {
        public enum NameList
        {
            TYPE_BYPBIND,
            TYPE_BYPLIST,
            TYPE_DNSADDR,
            TYPE_ADAPMTU,
            TYPE_TCPREST,
            TYPE_TCPTYPE,
            TYPE_TCPHOST,
            TYPE_TCPUSER,
            TYPE_TCPPASS,
            TYPE_TCPMETH,
            TYPE_TCPPROT,
            TYPE_TCPPRPA,
            TYPE_TCPOBFS,
            TYPE_TCPOBPA,
            TYPE_UDPREST,
            TYPE_UDPTYPE,
            TYPE_UDPHOST,
            TYPE_UDPUSER,
            TYPE_UDPPASS,
            TYPE_UDPMETH,
            TYPE_UDPPROT,
            TYPE_UDPPRPA,
            TYPE_UDPOBFS,
            TYPE_UDPOBPA
        }

        public static bool Dial(NameList name, string value)
        {
            Log.Debug($"[tap2socks] Dial {name}: {value}");
            return tap_dial((int)name, Encoding.UTF8.GetBytes(value));
        }

        [DllImport("tap2socks.bin", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool tap_dial(int name, byte[] value);

        [DllImport("tap2socks.bin", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool tap_init();

        [DllImport("tap2socks.bin", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool tap_free();

        [DllImport("tap2socks.bin", CallingConvention = CallingConvention.Cdecl)]
        public static extern string tap_name();

        [DllImport("tap2socks.bin", CallingConvention = CallingConvention.Cdecl)]
        public static extern ulong tap_getUP();

        [DllImport("tap2socks.bin", CallingConvention = CallingConvention.Cdecl)]
        public static extern ulong tap_getDL();
    }
}