using System.IO;
using System.Linq;
using System.Text;
using dotnetCampus.Cli;
using dotnetCampus.PublicAPI.Apis;

namespace dotnetCampus.PublicAPI.Tasks
{
    /// <summary>
    /// 生成 API 到文件。
    /// </summary>
    [Verb("generate")]
    internal class GenerateTask
    {
        [Option("AssemblyFile")]
        public string AssemblyFile { get; set; }

        [Option("ApiUnshippedFile")]
        public string ApiUnshippedFile { get; set; }

        [Option("ApiShippedFile")]
        public string ApiShippedFile { get; set; }

        public void Run()
        {
            var shippedLines = File.ReadAllLines(ApiShippedFile);
            var builder = new StringBuilder();
            var reader = new ApiReader(new FileInfo(AssemblyFile));
            foreach (var api in reader.Read())
            {
                if (!shippedLines.Contains(api))
                {
                    builder.AppendLine(api);
                }
            }
            var unshippedApis = builder.ToString();
            File.WriteAllText(ApiShippedFile, unshippedApis);
        }
    }
}
