using System.Collections.Generic;
using Mono.Cecil;

namespace dotnetCampus.PublicAPI.Apis
{
    internal class DelegateReader : TypeReader
    {
        public override bool Match(TypeDefinition type) => type.BaseType?.FullName is "System.MulticastDelegate";

        public override IEnumerable<string> ReadCore(TypeDefinition type)
        {
            yield break;
        }
    }
}
