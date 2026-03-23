using System.Net;
using System.Runtime.InteropServices;

namespace ParserIpExeMonitor;

internal static class UdpTableProvider
{
    private const int AfInet = 2;
    private const int AfInet6 = 23;
    private const int ErrorInsufficientBuffer = 122;

    public static IReadOnlyList<NetConnectionInfo> GetAllUdpEndpoints()
    {
        var list = new List<NetConnectionInfo>(64);
        list.AddRange(ReadUdpV4Table());
        list.AddRange(ReadUdpV6Table());
        return list;
    }

    private static List<NetConnectionInfo> ReadUdpV4Table()
    {
        var bufferSize = 0;
        if (GetExtendedUdpTable(IntPtr.Zero, ref bufferSize, true, AfInet, UdpTableClass.UdpTableOwnerPid, 0) !=
            ErrorInsufficientBuffer)
        {
            return [];
        }

        var buffer = Marshal.AllocHGlobal(bufferSize);
        try
        {
            if (GetExtendedUdpTable(buffer, ref bufferSize, true, AfInet, UdpTableClass.UdpTableOwnerPid, 0) != 0)
            {
                return [];
            }

            var count = Marshal.ReadInt32(buffer);
            var rowPtr = IntPtr.Add(buffer, sizeof(int));
            const int rowSize = 12;
            var list = new List<NetConnectionInfo>(count);

            for (var i = 0; i < count; i++)
            {
                var row = Marshal.PtrToStructure<MibUdpRowOwnerPid>(rowPtr);
                list.Add(new NetConnectionInfo
                {
                    Protocol = "UDP",
                    ProcessId = (int)row.OwningPid,
                    LocalAddress = ParseIpV4(row.LocalAddr),
                    LocalPort = ParsePort(row.LocalPort),
                    RemoteAddress = "*",
                    RemotePort = 0,
                    State = "UDP"
                });
                rowPtr = IntPtr.Add(rowPtr, rowSize);
            }

            return list;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static List<NetConnectionInfo> ReadUdpV6Table()
    {
        var bufferSize = 0;
        if (GetExtendedUdpTable(IntPtr.Zero, ref bufferSize, true, AfInet6, UdpTableClass.UdpTableOwnerPid, 0) !=
            ErrorInsufficientBuffer)
        {
            return [];
        }

        var buffer = Marshal.AllocHGlobal(bufferSize);
        try
        {
            if (GetExtendedUdpTable(buffer, ref bufferSize, true, AfInet6, UdpTableClass.UdpTableOwnerPid, 0) != 0)
            {
                return [];
            }

            var count = Marshal.ReadInt32(buffer);
            var rowPtr = IntPtr.Add(buffer, sizeof(int));
            const int rowSize = 28; // MIB_UDP6ROW_OWNER_PID
            var list = new List<NetConnectionInfo>(count);
            var localBytes = new byte[16];

            for (var i = 0; i < count; i++)
            {
                Marshal.Copy(rowPtr, localBytes, 0, 16);
                var localScope = (uint)Marshal.ReadInt32(rowPtr, 16);
                var localPort = ParsePort((uint)Marshal.ReadInt32(rowPtr, 20));
                var pid = Marshal.ReadInt32(rowPtr, 24);

                list.Add(new NetConnectionInfo
                {
                    Protocol = "UDP",
                    ProcessId = pid,
                    LocalAddress = FormatIPv6(localBytes, localScope),
                    LocalPort = localPort,
                    RemoteAddress = "*",
                    RemotePort = 0,
                    State = "UDP"
                });
                rowPtr = IntPtr.Add(rowPtr, rowSize);
            }

            return list;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static string ParseIpV4(uint ip)
    {
        var bytes = BitConverter.GetBytes(ip);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }

        return new IPAddress(bytes).ToString();
    }

    private static string FormatIPv6(byte[] addr, uint scopeId)
    {
        try
        {
            if (scopeId != 0)
            {
                return new IPAddress(addr, scopeId).ToString();
            }

            return new IPAddress(addr).ToString();
        }
        catch
        {
            return "?";
        }
    }

    private static int ParsePort(uint port)
    {
        var value = (short)(port & 0xFFFF);
        return (ushort)IPAddress.NetworkToHostOrder(value);
    }

    [DllImport("iphlpapi.dll", SetLastError = true)]
    private static extern int GetExtendedUdpTable(
        IntPtr pUdpTable,
        ref int pdwSize,
        bool bOrder,
        int ulAf,
        UdpTableClass tableClass,
        uint reserved);

    private enum UdpTableClass
    {
        UdpTableBasic,
        UdpTableOwnerPid
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MibUdpRowOwnerPid
    {
        public uint LocalAddr;
        public uint LocalPort;
        public uint OwningPid;
    }
}
