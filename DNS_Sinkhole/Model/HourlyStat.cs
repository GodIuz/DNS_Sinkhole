namespace DNS_Sinkhole.Model
{
    public class HourlyStat
    {
        public string Id { get; set; } = string.Empty;
        public DateTime Hour { get; set; }
        public int TotalQueries { get; set; }
        public int BlockedQueries { get; set; }
    }
}
