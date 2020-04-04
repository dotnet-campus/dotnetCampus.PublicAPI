using System.IO;
using System.Linq;
using dotnetCampus.Cli;

namespace dotnetCampus.PublicAPI.Tasks
{
    [Verb("ship")]
    internal class ShipApiTask
    {
        [Option("ApiUnshippedFile")]
        public string ApiUnshippedFile { get; set; }

        [Option("ApiShippedFile")]
        public string ApiShippedFile { get; set; }

        public void Run()
        {
            var lines1 = File.ReadAllLines(ApiUnshippedFile);
            var lines2 = File.ReadAllLines(ApiShippedFile);
            var lines = lines1.Union(lines2).OrderBy(x => x);
            File.WriteAllLines(ApiShippedFile, lines);
            File.WriteAllText(ApiUnshippedFile, "");
        }
    }
}
