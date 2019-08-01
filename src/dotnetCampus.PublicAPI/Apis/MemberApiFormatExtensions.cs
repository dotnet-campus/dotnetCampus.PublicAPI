using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace dotnetCampus.PublicAPI.Apis
{
    internal static class MemberApiFormatExtensions
    {
        public static (string modifiers, string methodName, string parameters) ToFormattedParts(this MethodDefinition method)
        {
            return (FormatModifiers(method), FormatMethodName(method), FormatParameterList(method));
        }

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
                    format += $" = {p.ParameterType.FormatValue(p.Constant)}";
                }
                return format;
            }
        }
    }
}
