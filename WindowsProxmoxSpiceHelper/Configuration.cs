namespace ProxmoxSpiceHelper;

public class Configuration
{
    public required string Host { get; init; }
    public string? Node { get; set; }
    public uint Port { get; init; } = 8006;
    public required string TokenId { get; init; }
    public required string TokenSecret { get; init; }
    public required uint VmId { get; init; }
}