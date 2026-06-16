

namespace DNS_Sinkhole
{
     public class BlockListStore
     {
        private HashSet<string> _blockedDomains = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        private readonly object _lock = new();

        public bool IsBlocked(string domain)
        {
            lock (_lock)
            {
                return _blockedDomains.Contains(domain);
            }
        }

        public void UpdateList(HashSet<string> newList)
        {
            lock (_lock)
            {
                _blockedDomains = newList;
            }
        }

        public int Count => _blockedDomains.Count;
     }
}