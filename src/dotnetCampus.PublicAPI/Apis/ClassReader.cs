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
            string typeName = type.ToFormattedName();

            foreach (var method in type.Methods.Where(x => x.IsConstructor
                && (x.IsPublic || x.Attributes.HasFlag(MethodAttributes.Family) || x.Attributes.HasFlag(MethodAttributes.FamORAssem))))
            {
                var index = type.Name.IndexOf('`');
                var methodName = index >= 0 ? type.Name.Substring(0, index) : type.Name;
                var (modifiers, _, parameters) = method.ToFormattedParts();
                var api = $"{modifiers}{typeName}.{methodName}({parameters}) -> {method.ReturnType.ToFormattedName()}";
                yield return api;
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
                        builder.Append($" = {field.FieldType.FormatValue(field.Constant)}");
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
                builder.Append($" -> {field.FieldType.ToFormattedName()}");

                yield return builder.ToString();
            }

            foreach (var @event in type.Events.Where(x =>
                x.AddMethod != null
                && (x.AddMethod.IsPublic || x.AddMethod.Attributes.HasFlag(MethodAttributes.Family) || x.AddMethod.Attributes.HasFlag(MethodAttributes.FamORAssem))))
            {
                var builder = new StringBuilder();

                builder.Append($"{typeName}.{@event.Name}");
                builder.Append($" -> ");
                builder.Append(@event.EventType.ToFormattedName());

                yield return builder.ToString();
            }

            foreach (var property in type.Properties)
            {
                if (property.Name is "Item")
                {
                    if (property.GetMethod != null
                        && (property.GetMethod.IsPublic || property.GetMethod.Attributes.HasFlag(MethodAttributes.Family) || property.GetMethod.Attributes.HasFlag(MethodAttributes.FamORAssem)))
                    {
                        var (modifiers, _, parameters) = property.GetMethod.ToFormattedParts();
                        var api = $"{modifiers}{typeName}.this[{parameters}].get -> {property.PropertyType.ToFormattedName()}";
                        yield return api;
                    }
                    if (property.SetMethod != null
                        && (property.SetMethod.IsPublic || property.SetMethod.Attributes.HasFlag(MethodAttributes.Family) || property.SetMethod.Attributes.HasFlag(MethodAttributes.FamORAssem)))
                    {
                        var (_, _, parameters) = property.GetMethod.ToFormattedParts();
                        var (modifiers, _, _) = property.SetMethod.ToFormattedParts();
                        var api = $"{modifiers}{typeName}.this[{parameters}].set -> void";
                        yield return api;
                    }
                }
                else
                {
                    if (property.GetMethod != null
                        && (property.GetMethod.IsPublic || property.GetMethod.Attributes.HasFlag(MethodAttributes.Family) || property.GetMethod.Attributes.HasFlag(MethodAttributes.FamORAssem)))
                    {
                        var (modifiers, _, parameters) = property.GetMethod.ToFormattedParts();
                        var api = $"{modifiers}{typeName}.{property.Name}.get -> {property.PropertyType.ToFormattedName()}";
                        yield return api;
                    }
                    if (property.SetMethod != null
                        && (property.SetMethod.IsPublic || property.SetMethod.Attributes.HasFlag(MethodAttributes.Family) || property.SetMethod.Attributes.HasFlag(MethodAttributes.FamORAssem)))
                    {
                        var (modifiers, _, parameters) = property.SetMethod.ToFormattedParts();
                        var api = $"{modifiers}{typeName}.{property.Name}.set -> void";
                        yield return api;
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
                var (modifiers, methodName, parameters) = method.ToFormattedParts();
                var api = $"{modifiers}{typeName}.{methodName}({parameters}) -> {method.ReturnType.ToFormattedName()}";
                yield return api;
            }
        }
    }
}
