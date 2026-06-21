namespace DNS_Sinkhole.Model
{
    public class DnsLogEntry
    {
        public string Timestamp { get; set; } = string.Empty;
        public string Domain { get; set; } = string.Empty;
        public string ClientIp { get; set; } = string.Empty;
        public bool IsBlocked { get; set; }
    }
}
