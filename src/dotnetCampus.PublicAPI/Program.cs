using dotnetCampus.PublicAPI.Tasks;

namespace dotnetCampus.PublicAPI
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length is 0)
            {
                return;
            }

            if (args[0] is "prepare")
            {
                // 确保 API 文件存在。
                new PrepareApiFileTask().Execute(args);
            }
            else if (args[0] is "generate")
            {
                // 组织目标项目的文件夹结构。
                new GenerateTask().Execute(args);
            }
        }
    }
}
