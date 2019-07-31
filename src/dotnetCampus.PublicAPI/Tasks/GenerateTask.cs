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
            GenerateApisToFiles(args[4], args[6]);
        }

        private void GenerateApisToFiles(string assemblyFile, string apiFile)
        {
        }
    }
}
