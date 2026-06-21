using DNS_Sinkhole.Model;
using LiteDB;
using System.Collections.Concurrent;

namespace DNS_Sinkhole
{
    public class StatsStore
    {
        private int _totalQueries = 0;
        private int _blockedQueries = 0;
        private readonly ConcurrentQueue<string> _recentBlocks = new();
        private const int MaxLogs = 10;
        private readonly LiteDatabase _db;
        private readonly ILiteCollection<HourlyStat> _hourlyStats;
        public int TotalQueries => _totalQueries;
        public int BlockedQueries => _blockedQueries;
        public IEnumerable<string> RecentBlocks => _recentBlocks;
        private readonly ILiteCollection<ClientStat> _clientStats;
        private readonly ConcurrentQueue<DnsLogEntry> _liveLogs = new();
        private const int MaxLiveLogs = 500;
        public StatsStore()
        {
            _db = new LiteDatabase(@"Filename=Analytics.db;Connection=Shared;");
            _hourlyStats = _db.GetCollection<HourlyStat>("hourly_stats");
            _clientStats = _db.GetCollection<ClientStat>("client_stats");
            _hourlyStats.EnsureIndex(x => x.Hour);
        }

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

        private void UpdateDatabase(int totalInc, int blockInc)
        {
            var currentHour = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, 0, 0);
            var id = currentHour.ToString("yyyy-MM-dd-HH");
            var stat = _hourlyStats.FindById(id);
            if (stat == null)
            {
                stat = new HourlyStat { Id = id, Hour = currentHour, TotalQueries = totalInc, BlockedQueries = blockInc };
                _hourlyStats.Insert(stat);
            }
            else
            {
                stat.TotalQueries += totalInc;
                stat.BlockedQueries += blockInc;
                _hourlyStats.Update(stat);
            }
        }

        public IEnumerable<HourlyStat> GetHistory(int hoursBack = 24)
        {
            var cutoff = DateTime.Now.AddHours(-hoursBack);
            return _hourlyStats.Find(x => x.Hour >= cutoff).OrderBy(x => x.Hour).ToList();
        }

        public void AddBlockedClient(string ipAddress)
        {
            var client = _clientStats.FindById(ipAddress);
            if (client == null)
            {
                _clientStats.Insert(new ClientStat { Id = ipAddress, BlockedCount = 1 });
            }
            else
            {
                client.BlockedCount++;
                _clientStats.Update(client);
            }
        }

        public IEnumerable<object> GetTopClients(int limit = 5)
        {
            return _clientStats.FindAll()
                               .OrderByDescending(x => x.BlockedCount)
                               .Take(limit)
                               .Select(x => new { ip = x.Id, count = x.BlockedCount })
                               .ToList();
        }

        public void AddLiveLog(string domain, string clientIp, bool isBlocked)
        {
            _liveLogs.Enqueue(new DnsLogEntry
            {
                Timestamp = DateTime.Now.ToString("HH:mm:ss"),
                Domain = domain,
                ClientIp = clientIp,
                IsBlocked = isBlocked
            });
            while (_liveLogs.Count > MaxLiveLogs)
            {
                _liveLogs.TryDequeue(out _);
            }
        }

        public IEnumerable<DnsLogEntry> GetLiveLogs()
        {
            return _liveLogs.Reverse().ToList();
        }

        public void Dispose()
        {
            _db?.Dispose();
        }
    }
}