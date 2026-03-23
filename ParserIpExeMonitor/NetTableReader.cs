namespace ParserIpExeMonitor;

/// <summary>Сводка всех записей из TCP/UDP таблиц Windows (IPv4 + IPv6) с PID.</summary>
internal static class NetTableReader
{
    public static IReadOnlyList<NetConnectionInfo> GetAll()
    {
        var list = new List<NetConnectionInfo>(256);
        list.AddRange(TcpTableProvider.GetAllTcpConnections());
        list.AddRange(UdpTableProvider.GetAllUdpEndpoints());
        return list;
    }
}
