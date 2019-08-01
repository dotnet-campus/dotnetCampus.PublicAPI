using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Mono.Cecil;

namespace dotnetCampus.PublicAPI.Apis
{
    public class ApiReader
    {
        private readonly FileInfo _assemblyFile;

        private readonly TypeReader[] _typeReaders =
        {
            new EnumReader(),
            new DelegateReader(),
            new ClassReader(),
        };

        public ApiReader(FileInfo assemblyFile)
        {
            _assemblyFile = assemblyFile ?? throw new ArgumentNullException(nameof(assemblyFile));
        }

        public IEnumerable<string> Read()
        {
            var builder = new StringBuilder();

            var module = ModuleDefinition.ReadModule(_assemblyFile.FullName);
            foreach (var type in module.Types.Concat(module.Types.SelectMany(x => x.NestedTypes)).Where(x =>
                  (x.IsPublic || x.IsNestedPublic || x.Attributes.HasFlag(TypeAttributes.NestedFamily))
                  && !x.IsRuntimeSpecialName))
            {
                foreach (var reader in _typeReaders)
                {
                    if (reader.Match(type))
                    {
                        foreach (var api in reader.Read(type))
                        {
                            yield return api;
                        }
                        break;
                    }
                }
            }
        }
    }
}
