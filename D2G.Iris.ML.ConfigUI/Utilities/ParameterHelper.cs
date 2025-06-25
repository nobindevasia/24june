using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace D2G.Iris.ML.Utils
{
    public static class ParameterHelper
    {
        private static readonly HashSet<string> ExcludedParameterNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "LabelColumnName",
            "FeatureColumnName",
            "ExampleWeightColumnName",
            "RowGroupColumnName",
            "GroupIdColumnName",
            "ScoreColumnName",
            "PredictedLabelColumnName",
            "ProbabilityColumnName"
        };

        public static bool IsConfigurableParameter(string name, Type type)
        {
            if (ExcludedParameterNames.Contains(name))
                return false;

            var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

            return underlyingType.IsPrimitive ||
                   underlyingType == typeof(string) ||
                   underlyingType == typeof(decimal) ||
                   underlyingType.IsEnum ||
                   underlyingType == typeof(TimeSpan);
        }

        public static string GetFriendlyTypeName(Type type)
        {
            var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

            string baseName = underlyingType.Name switch
            {
                "Int32" => "int",
                "Double" => "double",
                "Single" => "float",
                "Boolean" => "bool",
                "String" => "string",
                _ => underlyingType.IsEnum ? "enum" : underlyingType.Name
            };

            return baseName; 
        }

        public static List<PropertyInfo> GetConfigurableProperties(Type optionsType)
        {
            if (optionsType == null)
                return new List<PropertyInfo>();

            return optionsType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite && p.CanRead)
                .Where(p => IsConfigurableParameter(p.Name, p.PropertyType))
                .OrderBy(p => p.Name)
                .ToList();
        }

        public static List<FieldInfo> GetConfigurableFields(Type optionsType)
        {
            if (optionsType == null)
                return new List<FieldInfo>();

            return optionsType.GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Where(f => !f.IsInitOnly && !f.IsLiteral)
                .Where(f => IsConfigurableParameter(f.Name, f.FieldType))
                .OrderBy(f => f.Name)
                .ToList();
        }

        public static object ConvertParameterValue(string value, Type targetType)
        {
            var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            if (underlyingType == typeof(int))
                return int.Parse(value);
            else if (underlyingType == typeof(double))
                return double.Parse(value);
            else if (underlyingType == typeof(float))
                return float.Parse(value.Replace("f", ""));
            else if (underlyingType == typeof(bool))
                return bool.Parse(value);
            else if (underlyingType.IsEnum)
                return Enum.Parse(underlyingType, value, true);
            else
                return value;
        }

        public static string GetDefaultValue(Type type)
        {
            var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

            if (underlyingType == typeof(int))
                return "0";
            else if (underlyingType == typeof(double))
                return "0.0";
            else if (underlyingType == typeof(float))
                return "0.0f";
            else if (underlyingType == typeof(bool))
                return "true";
            else if (underlyingType.IsEnum)
            {
                var enumValues = Enum.GetNames(underlyingType);
                return enumValues.Length > 0 ? enumValues[0] : "";
            }
            else
                return "";
        }

        public static string GetValueHint(Type type)
        {
            var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

            if (underlyingType.IsEnum)
            {
                var enumValues = Enum.GetNames(underlyingType);
                return $"Valid values: {string.Join(", ", enumValues)}";
            }
            else if (underlyingType == typeof(bool))
            {
                return "Valid values: true, false";
            }
            else if (underlyingType == typeof(int))
            {
                return "Enter an integer value";
            }
            else if (underlyingType == typeof(double) || underlyingType == typeof(float))
            {
                return "Enter a decimal value";
            }
            else
            {
                return "Enter a value";
            }
        }

        public static string CreateParameterDisplayText(string name, Type type)
        {
            return $"{name} ({GetFriendlyTypeName(type)})";
        }

        public static List<string> GetParameterDisplayList(Type optionsType)
        {
            var result = new List<string>();

            var properties = GetConfigurableProperties(optionsType);
            var fields = GetConfigurableFields(optionsType);

            result.AddRange(properties.Select(p => CreateParameterDisplayText(p.Name, p.PropertyType)));
            result.AddRange(fields.Select(f => CreateParameterDisplayText(f.Name, f.FieldType)));

            return result.OrderBy(x => x).ToList();
        }

        public static string CreateParameterTooltip(Type optionsType)
        {
            var displayList = GetParameterDisplayList(optionsType);

            if (!displayList.Any())
                return "No configurable parameters available for this algorithm.";

            return "Available Parameters:\n" + string.Join("\n", displayList);
        }
    }

    public class FieldAsProperty : PropertyInfo
    {
        private readonly FieldInfo _field;

        public FieldAsProperty(FieldInfo field)
        {
            _field = field;
        }

        public override string Name => _field.Name;
        public override Type PropertyType => _field.FieldType;
        public override bool CanWrite => !_field.IsInitOnly && !_field.IsLiteral;
        public override bool CanRead => true;

        public override object GetValue(object obj, BindingFlags invokeAttr, Binder binder, object[] index, System.Globalization.CultureInfo culture)
        {
            return _field.GetValue(obj);
        }

        public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, object[] index, System.Globalization.CultureInfo culture)
        {
            _field.SetValue(obj, value);
        }

        public override PropertyAttributes Attributes => PropertyAttributes.None;
        public override Type DeclaringType => _field.DeclaringType;
        public override Type ReflectedType => _field.ReflectedType;

        public override MethodInfo GetGetMethod(bool nonPublic) => null;
        public override MethodInfo GetSetMethod(bool nonPublic) => null;
        public override MethodInfo[] GetAccessors(bool nonPublic) => new MethodInfo[0];

        public override ParameterInfo[] GetIndexParameters() => new ParameterInfo[0];
        public override object[] GetCustomAttributes(Type attributeType, bool inherit) => _field.GetCustomAttributes(attributeType, inherit);
        public override object[] GetCustomAttributes(bool inherit) => _field.GetCustomAttributes(inherit);
        public override bool IsDefined(Type attributeType, bool inherit) => _field.IsDefined(attributeType, inherit);
    }

    public class ParameterItem
    {
        public PropertyInfo Property { get; set; }
        public string DisplayText { get; set; }
    }
}