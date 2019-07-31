using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using dotnetCampus.PublicAPI.Core;
using Mono.Cecil;

namespace dotnetCampus.PublicAPI.Tasks
{
    /// <summary>
    /// 生成 API 到文件。
    /// </summary>
    internal class GenerateTask : IPackageTask
    {
        public void Execute(string[] args)
        {
            GenerateApisToFiles(args[2], args[4]);
        }

        private void GenerateApisToFiles(string assemblyFile, string apiFile)
        {
            var builder = new StringBuilder();

            var module = ModuleDefinition.ReadModule(assemblyFile);
            foreach (var type in module.Types.Concat(module.Types.SelectMany(x => x.NestedTypes)).Where(x =>
                  (x.IsPublic || x.IsNestedPublic || x.Attributes.HasFlag(TypeAttributes.NestedFamily))
                  && !x.IsRuntimeSpecialName))
            {
                if (type.IsEnum)
                {
                    AppendEnum(builder, type);
                }
                else if (type.BaseType?.FullName is "System.MulticastDelegate")
                {
                    AppendDelegate(builder, type);
                }
                else
                {
                    AppendClass(builder, type);
                }
            }

            File.WriteAllText(apiFile, builder.ToString());
        }

        private static void AppendClass(StringBuilder builder, TypeDefinition type)
        {
            string typeName = FormatTypeName(type);

            builder.AppendLine(typeName);

            foreach (var method in type.Methods.Where(x => x.IsConstructor
                && (x.IsPublic || x.Attributes.HasFlag(MethodAttributes.Family) || x.Attributes.HasFlag(MethodAttributes.FamORAssem))))
            {
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
                builder.AppendLine();
            }

            if (type.FullName.Contains("ContinuousPartOperation"))
            {

            }

            foreach (var field in type.Fields.Where(x =>
                x.IsPublic
                || x.Attributes.HasFlag(FieldAttributes.Family)
                || x.Attributes.HasFlag(FieldAttributes.FamORAssem)))
            {
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
                builder.AppendLine();
            }

            foreach (var @event in type.Events.Where(x =>
                x.AddMethod != null
                && (x.AddMethod.IsPublic || x.AddMethod.Attributes.HasFlag(MethodAttributes.Family) || x.AddMethod.Attributes.HasFlag(MethodAttributes.FamORAssem))))
            {
                builder.Append($"{typeName}.{@event.Name}");
                builder.Append($" -> ");
                builder.AppendLine(FormatTypeName(@event.EventType));
            }

            foreach (var property in type.Properties)
            {
                if (property.Name is "Item")
                {
                    if (property.GetMethod != null
                        && (property.GetMethod.IsPublic || property.GetMethod.Attributes.HasFlag(MethodAttributes.Family) || property.GetMethod.Attributes.HasFlag(MethodAttributes.FamORAssem)))
                    {
                        builder.Append($"{FormatModifiers(property.GetMethod)}{typeName}.this[{FormatParameterList(property.GetMethod)}]");
                        builder.Append($".get -> ");
                        builder.AppendLine(FormatTypeName(property.PropertyType));
                    }
                    if (property.SetMethod != null
                        && (property.SetMethod.IsPublic || property.SetMethod.Attributes.HasFlag(MethodAttributes.Family) || property.SetMethod.Attributes.HasFlag(MethodAttributes.FamORAssem)))
                    {
                        builder.Append($"{FormatModifiers(property.SetMethod)}{typeName}.this[{FormatParameterList(property.GetMethod)}]");
                        builder.Append($".set -> void");
                        builder.AppendLine();
                    }
                }
                else
                {
                    if (property.GetMethod != null
                        && (property.GetMethod.IsPublic || property.GetMethod.Attributes.HasFlag(MethodAttributes.Family) || property.GetMethod.Attributes.HasFlag(MethodAttributes.FamORAssem)))
                    {
                        builder.Append($"{FormatModifiers(property.GetMethod)}{typeName}.{property.Name}");
                        builder.Append($".get -> ");
                        builder.AppendLine(FormatTypeName(property.PropertyType));
                    }
                    if (property.SetMethod != null
                        && (property.SetMethod.IsPublic || property.SetMethod.Attributes.HasFlag(MethodAttributes.Family) || property.SetMethod.Attributes.HasFlag(MethodAttributes.FamORAssem)))
                    {
                        builder.Append($"{FormatModifiers(property.SetMethod)}{typeName}.{property.Name}");
                        builder.Append($".set -> void");
                        builder.AppendLine();
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
                if (method.FullName.Contains("FillInto"))
                {

                }

                string methodName = FormatMethodName(method);
                builder.Append($"{FormatModifiers(method)}{typeName}.{methodName}");
                var parameterList = FormatParameterList(method);
                builder.Append($"({parameterList})");
                builder.Append($" -> {FormatTypeName(method.ReturnType)}");
                builder.AppendLine();
            }
        }

        private static void AppendEnum(StringBuilder builder, TypeDefinition type)
        {
            string typeName = FormatTypeName(type);

            builder.AppendLine(typeName);

            var isFlag = type.CustomAttributes.Any(x => x.AttributeType.FullName is "System.FlagsAttribute");
            var values = type.Fields.Where(x => x.IsPublic && x.IsStatic).Select(x => (int)x.Constant).ToArray();
            foreach (var field in type.Fields.Where(x => x.IsPublic && x.IsStatic))
            {
                if (field.Name is "UpdatedContent")
                {

                }
                var flags = GetFlags((int)field.Constant, values).ToList();
                if (flags.Count == 1 || !isFlag)
                {
                    builder.Append($"{typeName}.{field.Name} = {field.Constant}");
                }
                else
                {
                    builder.Append($"{typeName}.{field.Name} = {string.Join(" | ", flags.Select(x => FormatValue(type, x)))}");
                }
                builder.Append($" -> {FormatTypeName(field.FieldType)}");
                builder.AppendLine();
            }
        }

        private static void AppendDelegate(StringBuilder builder, TypeDefinition type)
        {
            string typeName = FormatTypeName(type);
            builder.AppendLine(typeName);
        }

        private static string FormatTypeName(TypeReference type)
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
            if (method.FullName.Contains("CopyRight"))
            {

            }

            if (method.Attributes.HasFlag(MethodAttributes.Static))
            {
                return "static ";
            }
            else if (method.IsVirtual && method.IsReuseSlot && method.IsHideBySig)
            {
                return "override ";
            }
            else if(!method.DeclaringType.IsInterface && method.IsAbstract)
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
                if (method.FullName.Contains("op_"))
                {

                }
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

        private static string FormatValue(TypeReference type, object value)
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

        private static IEnumerable<int> GetFlags(int value, int[] values)
        {
            ulong multipleBits = Convert.ToUInt64(value);
            var multipleResults = new List<int>();
            for (int i = values.Length - 1; i >= 0; i--)
            {
                ulong mask = Convert.ToUInt64(values[i]);
                if (value == values[i])
                    continue;
                if (i == 0 && mask == 0L)
                    break;
                if ((multipleBits & mask) == mask)
                {
                    multipleResults.Add(values[i]);
                    multipleBits -= mask;
                }
            }

            ulong bits = Convert.ToUInt64(value);
            var results = new List<int>();
            if (multipleResults.Count <= 1)
            {
                for (int i = values.Length - 1; i >= 0; i--)
                {
                    ulong mask = Convert.ToUInt64(values[i]);
                    if (i == 0 && mask == 0L)
                        break;
                    if ((bits & mask) == mask)
                    {
                        results.Add(values[i]);
                        bits -= mask;
                    }
                }
            }
            else
            {
                bits = multipleBits;
                results = multipleResults;
            }

            if (bits != 0L)
                return Enumerable.Empty<int>();
            if (Convert.ToUInt64(value) != 0L)
                return results.Reverse<int>();
            if (bits == Convert.ToUInt64(value) && values.Length > 0 && Convert.ToUInt64(values[0]) == 0L)
                return values.Take(1);
            return Enumerable.Empty<int>();
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
