using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;

namespace dotnetCampus.PublicAPI.Apis
{
    internal class ClassReader : TypeReader
    {
        public override bool Match(TypeDefinition type) => true;

        public override IEnumerable<string> ReadCore(TypeDefinition type)
        {
            string typeName = FormatTypeName(type);

            foreach (var method in type.Methods.Where(x => x.IsConstructor
                && (x.IsPublic || x.Attributes.HasFlag(MethodAttributes.Family) || x.Attributes.HasFlag(MethodAttributes.FamORAssem))))
            {
                var builder = new StringBuilder();

                if (method.Attributes.HasFlag(MethodAttributes.Static))
                {
                    builder.Append($"static ");
                }
                var index = type.Name.IndexOf('`');
                var methodName = index >= 0 ? type.Name.Substring(0, index) : type.Name;
                builder.Append($"{typeName}.{methodName}");
                var parameterList = FormatParameterList(method);
                builder.Append($"({parameterList})");
                builder.Append($" -> {FormatTypeName(method.ReturnType)}");

                yield return builder.ToString();
            }

            foreach (var field in type.Fields.Where(x =>
                x.IsPublic
                || x.Attributes.HasFlag(FieldAttributes.Family)
                || x.Attributes.HasFlag(FieldAttributes.FamORAssem)))
            {
                var builder = new StringBuilder();

                if (field.HasConstant)
                {
                    builder.Append($"const {typeName}.{field.Name}");
                    if (field.HasConstant)
                    {
                        builder.Append($" = {FormatValue(field.FieldType, field.Constant)}");
                    }
                }
                else
                {
                    if (field.IsStatic)
                    {
                        builder.Append($"static ");
                    }
                    if (field.IsInitOnly)
                    {
                        builder.Append($"readonly ");
                    }
                    builder.Append($"{typeName}.{field.Name}");
                }
                builder.Append($" -> {FormatTypeName(field.FieldType)}");

                yield return builder.ToString();
            }

            foreach (var @event in type.Events.Where(x =>
                x.AddMethod != null
                && (x.AddMethod.IsPublic || x.AddMethod.Attributes.HasFlag(MethodAttributes.Family) || x.AddMethod.Attributes.HasFlag(MethodAttributes.FamORAssem))))
            {
                var builder = new StringBuilder();

                builder.Append($"{typeName}.{@event.Name}");
                builder.Append($" -> ");
                builder.Append(FormatTypeName(@event.EventType));

                yield return builder.ToString();
            }

            foreach (var property in type.Properties)
            {
                if (property.Name is "Item")
                {
                    if (property.GetMethod != null
                        && (property.GetMethod.IsPublic || property.GetMethod.Attributes.HasFlag(MethodAttributes.Family) || property.GetMethod.Attributes.HasFlag(MethodAttributes.FamORAssem)))
                    {
                        var builder = new StringBuilder();

                        builder.Append($"{FormatModifiers(property.GetMethod)}{typeName}.this[{FormatParameterList(property.GetMethod)}]");
                        builder.Append($".get -> ");
                        builder.Append(FormatTypeName(property.PropertyType));

                        yield return builder.ToString();
                    }
                    if (property.SetMethod != null
                        && (property.SetMethod.IsPublic || property.SetMethod.Attributes.HasFlag(MethodAttributes.Family) || property.SetMethod.Attributes.HasFlag(MethodAttributes.FamORAssem)))
                    {
                        var builder = new StringBuilder();

                        builder.Append($"{FormatModifiers(property.SetMethod)}{typeName}.this[{FormatParameterList(property.GetMethod)}]");
                        builder.Append($".set -> void");
                        builder.AppendLine();

                        yield return builder.ToString();
                    }
                }
                else
                {
                    if (property.GetMethod != null
                        && (property.GetMethod.IsPublic || property.GetMethod.Attributes.HasFlag(MethodAttributes.Family) || property.GetMethod.Attributes.HasFlag(MethodAttributes.FamORAssem)))
                    {
                        var builder = new StringBuilder();

                        builder.Append($"{FormatModifiers(property.GetMethod)}{typeName}.{property.Name}");
                        builder.Append($".get -> ");
                        builder.Append(FormatTypeName(property.PropertyType));

                        yield return builder.ToString();
                    }
                    if (property.SetMethod != null
                        && (property.SetMethod.IsPublic || property.SetMethod.Attributes.HasFlag(MethodAttributes.Family) || property.SetMethod.Attributes.HasFlag(MethodAttributes.FamORAssem)))
                    {
                        var builder = new StringBuilder();

                        builder.Append($"{FormatModifiers(property.SetMethod)}{typeName}.{property.Name}");
                        builder.Append($".set -> void");

                        yield return builder.ToString();
                    }
                }
            }

            foreach (var method in type.Methods.Where(x =>
                !x.IsConstructor
                && (x.IsPublic || x.Attributes.HasFlag(MethodAttributes.Family) || x.Attributes.HasFlag(MethodAttributes.FamORAssem))
                && !x.IsGetter
                && !x.IsSetter
                && !x.IsAddOn
                && !x.IsRemoveOn))
            {
                var builder = new StringBuilder();

                string methodName = FormatMethodName(method);
                builder.Append($"{FormatModifiers(method)}{typeName}.{methodName}");
                var parameterList = FormatParameterList(method);
                builder.Append($"({parameterList})");
                builder.Append($" -> {FormatTypeName(method.ReturnType)}");

                yield return builder.ToString();
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
                format += $"{FormatTypeName(p.ParameterType)} {p.Name}";
                if (p.IsOptional)
                {
                    format += $" = {FormatValue(p.ParameterType, p.Constant)}";
                }
                return format;
            }
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
                return $"implicit operator {FormatTypeName(method.ReturnType)}";
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
                return FormatMemberNameIncludingGenerics(method.Name, method.GenericParameters);
            }
        }

        private static string FormatMemberNameIncludingGenerics(string fullName, IList<GenericParameter> generics)
            => FormatMemberNameIncludingGenerics(fullName, generics.Cast<TypeReference>().ToList());

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
    }
}
