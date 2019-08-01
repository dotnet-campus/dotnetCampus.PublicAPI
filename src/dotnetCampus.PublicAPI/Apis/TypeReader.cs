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
            string typeName = FormatTypeName(type);
            yield return typeName;

            foreach (var api in ReadCore(type))
            {
                yield return api;
            }
        }

        public abstract IEnumerable<string> ReadCore(TypeDefinition type);


        protected static string FormatTypeName(TypeReference type)
        {
            if (type.FullName.StartsWith("System.Nullable`1", StringComparison.InvariantCulture)
                && type is GenericInstanceType gt)
            {
                return $"{GetName(gt.GenericArguments[0].FullName)}?";
            }

            var typeName = GetTypeName(type.FullName);
            var generics = type is GenericInstanceType git
                ? git.GenericArguments.Cast<TypeReference>().ToList()
                : type.GenericParameters.Cast<TypeReference>().ToList();

            return FormatMemberNameIncludingGenerics(typeName, generics);

            string GetTypeName(string fullName)
            {
                fullName = fullName.Replace('/', '.');
                if (fullName.EndsWith("[]", StringComparison.InvariantCulture))
                {
                    return $"{GetName(fullName.Substring(0, fullName.Length - 2))}[]";
                }
                else if (fullName.EndsWith("[0...,0...]", StringComparison.InvariantCulture))
                {
                    return $"{GetName(fullName.Substring(0, fullName.Length - 11))}[,]";
                }

                return GetName(fullName);
            }

            string GetName(string name) => KeywordMapping.TryGetValue(name, out var n) ? n : name;
        }

        private static string FormatMemberNameIncludingGenerics(string fullName, IList<TypeReference> generics)
        {
            string name;
            if (generics.Count > 0)
            {
                var index = fullName.IndexOf('`');
                if (index >= 0)
                {
                    fullName = fullName.Substring(0, index);
                }
                name = $"{fullName}<{string.Join(", ", generics.Select(x => FormatTypeName(x)))}>";
            }
            else
            {
                name = fullName;
            }
            return name;
        }

        protected static string FormatValue(TypeReference type, object value)
        {
            if (type is TypeDefinition t && t.IsEnum)
            {
                var fieldName = t.Fields.FirstOrDefault(x => x.Constant?.Equals(value) is true)?.Name;
                if (fieldName != null)
                {
                    string typeName = FormatTypeName(type);
                    return $"{typeName}.{fieldName}";
                }
            }
            return FormatValue(value);
        }

        private static string FormatValue(object value)
        {
            if (value is null)
            {
                return "null";
            }
            if (value is true)
            {
                return "true";
            }
            if (value is false)
            {
                return "false";
            }
            if (value is string s)
            {
                return $@"""{s}""";
            }
            return value.ToString();
        }


        private static readonly Dictionary<string, string> KeywordMapping = new Dictionary<string, string>
        {
            { "System.Object", "object" },
            { "System.Void", "void" },
            { "System.Boolean", "bool" },
            { "System.Byte", "byte" },
            { "System.UInt16", "ushort" },
            { "System.Int16", "short" },
            { "System.UInt32", "uint" },
            { "System.Int32", "int" },
            { "System.UInt64", "ulong" },
            { "System.Int64", "long" },
            { "System.Single", "float" },
            { "System.Double", "double" },
            { "System.Decimal", "decimal" },
            { "System.Char", "char" },
            { "System.String", "string" },
        };
    }
}
