using System.Net;
using System.Runtime.InteropServices;

namespace ParserIpExeMonitor;

internal static class TcpTableProvider
{
    private const int AfInet = 2;
    private const int ErrorInsufficientBuffer = 122;

    public static IReadOnlyList<TcpConnectionInfo> GetAllTcpConnections()
    {
        var bufferSize = 0;
        var firstCall = GetExtendedTcpTable(IntPtr.Zero, ref bufferSize, true, AfInet, TcpTableClass.TcpTableOwnerPidAll, 0);
        if (firstCall != ErrorInsufficientBuffer)
        {
            return [];
        }

        var buffer = Marshal.AllocHGlobal(bufferSize);
        try
        {
            var result = GetExtendedTcpTable(buffer, ref bufferSize, true, AfInet, TcpTableClass.TcpTableOwnerPidAll, 0);
            if (result != 0)
            {
                return [];
            }

            var connectionCount = Marshal.ReadInt32(buffer);
            var rowPtr = IntPtr.Add(buffer, sizeof(int));
            var rowSize = Marshal.SizeOf<MibTcpRowOwnerPid>();
            var list = new List<TcpConnectionInfo>(connectionCount);

            for (var i = 0; i < connectionCount; i++)
            {
                var row = Marshal.PtrToStructure<MibTcpRowOwnerPid>(rowPtr);
                list.Add(new TcpConnectionInfo
                {
                    ProcessId = (int)row.OwningPid,
                    LocalAddress = ParseIp(row.LocalAddr),
                    LocalPort = ParsePort(row.LocalPort),
                    RemoteAddress = ParseIp(row.RemoteAddr),
                    RemotePort = ParsePort(row.RemotePort),
                    State = row.State.ToString()
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

    private static string ParseIp(uint ip)
    {
        var bytes = BitConverter.GetBytes(ip);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }

        return new IPAddress(bytes).ToString();
    }

    private static int ParsePort(uint port)
    {
        var value = (short)(port & 0xFFFF);
        return (ushort)IPAddress.NetworkToHostOrder(value);
    }

    [DllImport("iphlpapi.dll", SetLastError = true)]
    private static extern int GetExtendedTcpTable(
        IntPtr pTcpTable,
        ref int pdwSize,
        bool bOrder,
        int ulAf,
        TcpTableClass tableClass,
        uint reserved);

    private enum TcpTableClass
    {
        TcpTableBasicListener,
        TcpTableBasicConnections,
        TcpTableBasicAll,
        TcpTableOwnerPidListener,
        TcpTableOwnerPidConnections,
        TcpTableOwnerPidAll
    }

    private enum MibTcpState : uint
    {
        Closed = 1,
        Listen = 2,
        SynSent = 3,
        SynRcvd = 4,
        Established = 5,
        FinWait1 = 6,
        FinWait2 = 7,
        CloseWait = 8,
        Closing = 9,
        LastAck = 10,
        TimeWait = 11,
        DeleteTcb = 12
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MibTcpRowOwnerPid
    {
        public MibTcpState State;
        public uint LocalAddr;
        public uint LocalPort;
        public uint RemoteAddr;
        public uint RemotePort;
        public uint OwningPid;
    }
}
