using System.Diagnostics;
using System.IO;
using System.Linq;
using dotnetCampus.PublicAPI.Core;

namespace dotnetCampus.PublicAPI.Tasks
{
    internal class ShipApiTask : IPackageTask
    {
        public void Execute(string[] args)
        {
            GenerateApisToFiles(args[2], args[4]);
        }

        private void GenerateApisToFiles(string apiFile, string shippedApiFile)
        {
            var lines1 = File.ReadAllLines(apiFile);
            var lines2 = File.ReadAllLines(shippedApiFile);
            var lines = lines1.Union(lines2).OrderBy(x => x);
            File.WriteAllLines(shippedApiFile, lines);
            File.WriteAllText(apiFile, "");
        }
    }
}
