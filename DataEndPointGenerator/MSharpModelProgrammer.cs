﻿using Olive;
using Olive.Entities;
using Olive.Entities.Replication;
using System;
using System.Linq;
using System.Reflection;
using System.Text;

namespace OliveGenerator
{
    internal class MSharpModelProgrammer
    {
        ExposedType ExposedType;

        Type Type => ExposedType.GetType();

        public MSharpModelProgrammer(ExposedType type) => ExposedType = type;

        internal string Generate()
        {
            var r = new StringBuilder();

            r.AppendLine("using MSharp;");
            r.AppendLine("namespace " + Type.Namespace);
            r.AppendLine("{");
            r.AppendLine();
            r.AppendLine("public class " + Type.Name + " : EntityType");
            r.AppendLine("{");
            r.AppendLine("public " + Type.Name + "()");
            r.AppendLine("{");
            r.AppendLine($"Schema(\"{Type.Namespace}\");");

            foreach (var item in ExposedType.Fields)
                r.AppendLine(AddProperty(item));

            r.AppendLine("}");
            r.AppendLine("}");
            r.AppendLine("}");

            return new CSharpFormatter(r.ToString()).Format();
        }

        string AddProperty(ExposedField item)
        {
            var extraArgs = "";
            var type = item.GetPropertyType();
            var name = item.GetName();
            if (type.IsArray) type = type.GetElementType();

            bool isNullable;
            if (isNullable = type.IsNullable())
                type = type.GetGenericArguments().Single();

            var method = type.Name.ToPascalCaseId();

            if (item.IsAssociation)
            {
                method = "Associate" + "<" + type.Name + ">";
                if (type.IsEnum) method = "String";
            }

            if (item is ExposedPropertyInfo p && p.IsInverseAssociation)
            {
                type = type.GetGenericArguments().Single();
                method = "InverseAssociate<" + type.Name + ">";
                extraArgs += ", inverseOf: \"" + p.Property.GetCustomAttribute<InverseOfAttribute>()?.Association + "\"";
            }

            switch (method)
            {
                case "Boolean": method = "Bool"; break;
                case "Int32": method = "Int"; break;
                case "Int64": method = "Decimal"; break;
                default: break;
            }

            var result = method + $"(\"{name}\"{extraArgs})";

            if (type.Assembly == Context.AssemblyObject && type.IsArray)
                result += ".MaxCardinality(null)";

            if (!isNullable && type.IsValueType)
            {
                result += ".Mandatory()";
            }

            return result + ";";
        }
    }
}