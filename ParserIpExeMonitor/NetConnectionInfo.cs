namespace ParserIpExeMonitor;

/// <summary>Запись сокета: TCP или UDP, IPv4/IPv6 (адреса в строковом виде).</summary>
public sealed class NetConnectionInfo
{
    public required string Protocol { get; init; }
    public required int ProcessId { get; init; }
    public required string LocalAddress { get; init; }
    public required int LocalPort { get; init; }
    public required string RemoteAddress { get; init; }
    public required int RemotePort { get; init; }
    public required string State { get; init; }

    public string LocalEndpoint => FormatEndpoint(LocalAddress, LocalPort);
    public string RemoteEndpoint => FormatEndpoint(RemoteAddress, RemotePort);

    /// <summary>Для UDP и TCP LISTEN удалённая сторона в таблице ОС часто «пустая».</summary>
    public string RemoteDisplay =>
        Protocol == "UDP"
            ? "—"
            : State.Equals("Listen", StringComparison.OrdinalIgnoreCase) && IsUnspecifiedRemote
                ? "—"
                : RemoteEndpoint;

    private bool IsUnspecifiedRemote =>
        RemotePort == 0 &&
        (RemoteAddress is "0.0.0.0" or "::" ||
         RemoteAddress == "0:0:0:0:0:0:0:0");

    /// <summary>Ключ для дедупликации в дампе.</summary>
    public string DumpRowKey()
    {
        if (Protocol == "UDP")
        {
            return $"UDP|{LocalAddress}|{LocalPort}";
        }

        if (State.Equals("Listen", StringComparison.OrdinalIgnoreCase) || IsUnspecifiedRemote)
        {
            return $"TCP|LISTEN|{LocalAddress}|{LocalPort}";
        }

        return $"TCP|{RemoteAddress}|{RemotePort}";
    }

    /// <summary>Доп. строка в дампе для «нового» уникального сетевого признака.</summary>
    public string? UniqueExtraLine()
    {
        if (Protocol == "UDP")
        {
            return $"UNIQUE_UDP_BIND\t{LocalEndpoint}";
        }

        if (IsUnspecifiedRemote)
        {
            return null;
        }

        return $"UNIQUE_IP\t{RemoteAddress}";
    }

    private static string FormatEndpoint(string address, int port)
    {
        if (address.Contains(':', StringComparison.Ordinal) && !address.StartsWith('['))
        {
            return $"[{address}]:{port}";
        }

        return $"{address}:{port}";
    }
}
