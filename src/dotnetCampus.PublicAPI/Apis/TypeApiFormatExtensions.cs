using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace dotnetCampus.PublicAPI.Apis
{
    internal static class TypeApiFormatExtensions
    {
        public static string ToFormattedName(this TypeReference type)
        {
            // 对 T? 类型的需要特殊考虑。
            if (type.FullName.StartsWith("System.Nullable`1", StringComparison.InvariantCulture)
                && type is GenericInstanceType ngt)
            {
                return $"{FormatBuildInTypeName(ngt.GenericArguments[0].FullName)}?";
            }

            // 对 (T, T) 类型的需要特殊考虑。
            if (type.FullName.StartsWith("System.ValueTuple`", StringComparison.InvariantCulture)
                && type is GenericInstanceType vgt)
            {
                return $"({string.Join(", ", vgt.GenericArguments.Select(x => x.ToFormattedName()))})";
            }

            var typeName = FormatTypeName(type.FullName);
            var generics = type is GenericInstanceType git
                ? git.GenericArguments.Cast<TypeReference>().ToList()
                : type.GenericParameters.Cast<TypeReference>().ToList();

            return FormatGenericsTypeName(typeName, generics);

            string FormatTypeName(string fullName)
            {
                fullName = fullName.Replace('/', '.');
                if (fullName.EndsWith("[]", StringComparison.InvariantCulture))
                {
                    return $"{FormatBuildInTypeName(fullName.Substring(0, fullName.Length - 2))}[]";
                }
                else if (fullName.EndsWith("[0...,0...]", StringComparison.InvariantCulture))
                {
                    return $"{FormatBuildInTypeName(fullName.Substring(0, fullName.Length - 11))}[,]";
                }

                return FormatBuildInTypeName(fullName);
            }

            string FormatGenericsTypeName(string fullName, IList<TypeReference> genericTypes)
            {
                string name;
                if (genericTypes.Count > 0)
                {
                    var index = fullName.IndexOf('`');
                    if (index >= 0)
                    {
                        fullName = fullName.Substring(0, index);
                    }
                    name = $"{fullName}<{string.Join(", ", genericTypes.Select(ToFormattedName))}>";
                }
                else
                {
                    name = fullName;
                }
                return name;
            }

            string FormatBuildInTypeName(string name) => BuildInTypes.TryGetValue(name, out var n) ? n : name;
        }

        public static string FormatValue(this TypeReference type, object value)
        {
            if (type is TypeDefinition t && t.IsEnum)
            {
                var fieldName = t.Fields.FirstOrDefault(x => x.Constant?.Equals(value) is true)?.Name;
                if (fieldName != null)
                {
                    string typeName = type.ToFormattedName();
                    return $"{typeName}.{fieldName}";
                }
            }
            return FormatValue(value);

            string FormatValue(object v)
            {
                if (v is null)
                {
                    return "null";
                }
                if (v is true)
                {
                    return "true";
                }
                if (v is false)
                {
                    return "false";
                }
                if (v is string s)
                {
                    return $@"""{s}""";
                }
                return v.ToString();
            }
        }

        private static readonly Dictionary<string, string> BuildInTypes = new Dictionary<string, string>
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
