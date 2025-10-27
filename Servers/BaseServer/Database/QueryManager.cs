
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace BaseServer.Database
{
    public static class QueryManager
    {
        private static Dictionary<string, string> _queries = new Dictionary<string, string>();

        public static void LoadQueries(string directoryPath)
        {
            if (!Directory.Exists(directoryPath)) return;

            foreach (var file in Directory.GetFiles(directoryPath, "*.xml", SearchOption.AllDirectories))
            {
                XDocument doc = XDocument.Load(file);
                string namespaceName = doc.Root.Attribute("namespace")?.Value;

                foreach (var element in doc.Root.Elements())
                {
                    string queryId = $"{namespaceName}.{element.Attribute("id")?.Value}";
                    string query = element.Value.Trim();
                    _queries[queryId] = query;
                }
            }
        }

        public static string GetQuery(string queryId)
        {
            if (_queries.ContainsKey(queryId))
            {
                return _queries[queryId];
            }
            throw new KeyNotFoundException($"Query with id '{queryId}' not found.");
        }
    }
}
