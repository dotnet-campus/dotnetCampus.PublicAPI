using System.IO;
using System.Linq;
using System.Text;
using dotnetCampus.PublicAPI.Apis;
using dotnetCampus.PublicAPI.Core;

namespace dotnetCampus.PublicAPI.Tasks
{
    /// <summary>
    /// 生成 API 到文件。
    /// </summary>
    internal class GenerateTask : IPackageTask
    {
        public void Execute(string[] args)
        {
            GenerateApisToFiles(args[2], args[4], args[6]);
        }

        private void GenerateApisToFiles(string assemblyFile, string apiFile, string shippedApiFile)
        {
            var shippedLines = File.ReadAllLines(shippedApiFile);
            var builder = new StringBuilder();
            var reader = new ApiReader(new FileInfo(assemblyFile));
            foreach (var api in reader.Read())
            {
                if (!shippedLines.Contains(api))
                {
                    builder.AppendLine(api);
                }
            }
            var unshippedApis = builder.ToString();
            File.WriteAllText(apiFile, unshippedApis);
        }
    }
}
