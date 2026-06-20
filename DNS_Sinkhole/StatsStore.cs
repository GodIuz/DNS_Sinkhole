using System.Collections.Concurrent;

namespace DNS_Sinkhole
{
    public class StatsStore
    {
        private int _totalQueries = 0;
        private int _blockedQueries = 0;
        private readonly ConcurrentQueue<string> _recentBlocks = new ConcurrentQueue<string>();
        private const int MaxLogs = 10;
        public int TotalQueries => _totalQueries;
        public int BlockedQueries => _blockedQueries;
        public IEnumerable<string> RecentBlocks => _recentBlocks;

        public void AddQuery()
        {
            _totalQueries++;
        }

        public void AddBlocked(string domain)
        {
            _blockedQueries++;
            _recentBlocks.Enqueue($"[{System.DateTime.Now:HH:mm:ss}] BLOCKED: {domain}");

            while (_recentBlocks.Count > MaxLogs)
            {
                _recentBlocks.TryDequeue(out _);
            }
        }
    }
}