﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Mono.Cecil;

namespace dotnetCampus.PublicAPI.Apis
{
    internal class EnumReader : TypeReader
    {
        public override bool Match(TypeDefinition type) => type.IsEnum;

        public override IEnumerable<string> ReadCore(TypeDefinition type)
        {
            var typeName = type.ToFormattedName();

            var isFlag = type.CustomAttributes.Any(x => x.AttributeType.FullName is "System.FlagsAttribute");
            var values = type.Fields.Where(x => x.IsPublic && x.IsStatic).Select(x =>
                Convert.ToInt64(x.Constant, CultureInfo.InvariantCulture)).ToArray();
            foreach (var field in type.Fields.Where(x => x.IsPublic && x.IsStatic))
            {
                var builder = new StringBuilder();

                var flags = GetFlags(Convert.ToInt64(field.Constant, CultureInfo.InvariantCulture), values).ToList();
                if (flags.Count == 1 || !isFlag)
                {
                    builder.Append($"{typeName}.{field.Name} = {Convert.ToInt64(field.Constant, CultureInfo.InvariantCulture)}");
                }
                else
                {
                    builder.Append($"{typeName}.{field.Name} = {string.Join(" | ", flags.Select(x => type.FormatValue(x)))}");
                }
                builder.Append($" -> {field.FieldType.ToFormattedName()}");

                yield return builder.ToString();
            }
        }

        private static IEnumerable<long> GetFlags(long value, long[] values)
        {
            long multipleBits = Convert.ToInt64(value);
            var multipleResults = new List<long>();
            for (int i = values.Length - 1; i >= 0; i--)
            {
                long mask = Convert.ToInt64(values[i]);
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

            long bits = Convert.ToInt64(value);
            var results = new List<long>();
            if (multipleResults.Count <= 1)
            {
                for (int i = values.Length - 1; i >= 0; i--)
                {
                    long mask = Convert.ToInt64(values[i]);
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
                return Enumerable.Empty<long>();
            if (Convert.ToInt64(value) != 0L)
                return results.Reverse<long>();
            if (bits == Convert.ToInt64(value) && values.Length > 0 && Convert.ToInt64(values[0]) == 0L)
                return values.Take(1);
            return Enumerable.Empty<long>();
        }
    }
}
