using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using dotnetCampus.PublicAPI.Core;

namespace dotnetCampus.PublicAPI.Tasks
{
    internal class PrepareApiFileTask : IPackageTask
    {
        public void Execute(string[] args)
        {
            PrepareApiFiles(args[2], args[4]);
        }

        private void PrepareApiFiles(string apiFile, string shippedApiFile)
        {
            var directory = Path.GetDirectoryName(apiFile);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (!File.Exists(apiFile))
            {
                File.WriteAllText(apiFile, "");
            }
            if (!File.Exists(shippedApiFile))
            {
                File.WriteAllText(shippedApiFile, "");
            }
        }
    }
}
