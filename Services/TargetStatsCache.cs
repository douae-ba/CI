using System.Collections.Concurrent;

namespace CI.Services
{
    public static class TargetStatsCache
    {
        private class Entry
        {
            public List<TargetRow> Rows = new();
            public DateTime FileLastWriteTime;
        }

        private static readonly ConcurrentDictionary<string, Entry> _cache = new();

        // Retourne les lignes en cache SI le fichier n'a pas changé depuis la dernière lecture, sinon null
        public static List<TargetRow>? TryGet(string filePath)
        {
            if (!_cache.TryGetValue(filePath, out var entry))
                return null;

            if (!File.Exists(filePath))
                return null;

            var currentWriteTime = File.GetLastWriteTimeUtc(filePath);
            if (currentWriteTime != entry.FileLastWriteTime)
                return null; // le fichier a changé depuis -> on force une relecture

            return entry.Rows;
        }

        public static void Set(string filePath, List<TargetRow> rows)
        {
            if (!File.Exists(filePath)) return;
            _cache[filePath] = new Entry
            {
                Rows = rows,
                FileLastWriteTime = File.GetLastWriteTimeUtc(filePath)
            };
        }

        public static void Invalidate(string filePath)
        {
            _cache.TryRemove(filePath, out _);
        }

        public static void InvalidateAll()
        {
            _cache.Clear();
        }
    }
}