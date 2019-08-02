using dotnetCampus.Cli;
using dotnetCampus.PublicAPI.Tasks;

namespace dotnetCampus.PublicAPI
{
    class Program
    {
        static void Main(string[] args)
        {
            //System.Diagnostics.Debugger.Launch();
            CommandLine.Parse(args)
                .AddHandler<GenerateTask>(o => o.Run())
                .AddHandler<PrepareApiFileTask>(o => o.Run())
                .AddHandler<ShipApiTask>(o => o.Run())
                .Run();
        }
    }
}
