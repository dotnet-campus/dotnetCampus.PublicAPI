using System.IO;
using dotnetCampus.Cli;

namespace dotnetCampus.PublicAPI.Tasks
{
    [Verb("prepare")]
    internal class PrepareApiFileTask
    {
        [Option("ApiUnshippedFile")]
        public string ApiUnshippedFile { get; set; }

        [Option("ApiShippedFile")]
        public string ApiShippedFile { get; set; }

        public void Run()
        {
            var directory = Path.GetDirectoryName(ApiUnshippedFile);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (!File.Exists(ApiUnshippedFile))
            {
                File.WriteAllText(ApiUnshippedFile, "");
            }
            if (!File.Exists(ApiShippedFile))
            {
                File.WriteAllText(ApiShippedFile, "");
            }
        }
    }
}
