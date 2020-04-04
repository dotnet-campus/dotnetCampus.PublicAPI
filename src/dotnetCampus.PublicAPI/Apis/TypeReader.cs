using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace dotnetCampus.PublicAPI.Apis
{
    internal abstract class TypeReader
    {
        public abstract bool Match(TypeDefinition type);

        public IEnumerable<string> Read(TypeDefinition type)
        {
            string typeName = type.ToFormattedName();
            yield return typeName;

            foreach (var api in ReadCore(type))
            {
                yield return api;
            }
        }

        public abstract IEnumerable<string> ReadCore(TypeDefinition type);
    }
}
