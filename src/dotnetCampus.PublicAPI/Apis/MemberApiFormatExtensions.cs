using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;

namespace dotnetCampus.PublicAPI.Apis
{
    internal static class MemberApiFormatExtensions
    {
        public static string ToFormattedName(this TypeReference type)
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
        }

        public static (string modifiers, string methodName, string parameters) ToFormattedParts(this MethodDefinition method)
        {
            return (FormatModifiers(method), FormatMethodName(method), FormatParameterList(method));
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
                name = $"{fullName}<{string.Join(", ", generics.Select(ToFormattedName))}>";
            }
            else
            {
                name = fullName;
            }
            return name;
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

        private static string FormatModifiers(MethodDefinition method)
        {
            if (method.Attributes.HasFlag(MethodAttributes.Static))
            {
                return "static ";
            }
            else if (method.IsVirtual && method.IsReuseSlot && method.IsHideBySig)
            {
                return "override ";
            }
            else if (!method.DeclaringType.IsInterface && method.IsAbstract)
            {
                return "abstract ";
            }
            else if (!method.DeclaringType.IsInterface && !method.IsFinal && method.IsVirtual && method.IsNewSlot)
            {
                return "virtual ";
            }
            return "";
        }

        private static string FormatMethodName(MethodDefinition method)
        {
            if (method.Name is "op_Implicit")
            {
                return $"implicit operator {method.ReturnType.ToFormattedName()}";
            }
            else if (method.Name is "op_True")
            {
                return $"operator true";
            }
            else if (method.Name is "op_False")
            {
                return $"operator false";
            }
            else if (method.Name is "op_Equality")
            {
                return $"operator ==";
            }
            else if (method.Name is "op_Inequality")
            {
                return $"operator !=";
            }
            else
            {
                return FormatMethodName(method.Name, method.GenericParameters);
            }

            string FormatMethodName(string fullName, IList<GenericParameter> generics)
            {
                string name;
                if (generics.Count > 0)
                {
                    var index = fullName.IndexOf('`');
                    if (index >= 0)
                    {
                        fullName = fullName.Substring(0, index);
                    }
                    name = $"{fullName}<{string.Join(", ", generics.Select(x => x.ToFormattedName()))}>";
                }
                else
                {
                    name = fullName;
                }
                return name;
            }
        }

        private static string FormatParameterList(MethodDefinition method)
        {
            var @this = "";
            if (method.CustomAttributes.Any(x => x.AttributeType.FullName == "System.Runtime.CompilerServices.ExtensionAttribute"))
            {
                @this = "this ";
            }
            var parameters = $"{@this}{string.Join(", ", method.Parameters.Select(p => FormatParameter(p)))}";
            return parameters;

            string FormatParameter(ParameterDefinition p)
            {
                var format = "";
                if (p.CustomAttributes.Any(x => x.AttributeType.FullName == "System.ParamArrayAttribute"))
                {
                    format += "params ";
                }
                format += $"{p.ParameterType.ToFormattedName()} {p.Name}";
                if (p.IsOptional)
                {
                    format += $" = {FormatValue(p.ParameterType, p.Constant)}";
                }
                return format;
            }
        }
    }
}
