using System;
using System.ComponentModel;
using System.Globalization;

namespace GTSCommon.DataModels
{
    public class Vector3Converter : ExpandableObjectConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            try
            {
                var s = (string) value;
                if (s == null) return null;
                s = s.Replace("X:", "").Replace("Y:", "").Replace("Z:", "");
                var tokens = s.Split(' ');
                return new XVector3(float.Parse(tokens[0]), float.Parse(tokens[1]), float.Parse(tokens[2]));
            }
            catch
            {
                if (context.PropertyDescriptor != null) return context.PropertyDescriptor.GetValue(context.Instance);
            }
            return null;
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value,
            Type destinationType)
        {
            var p = (XVector3) value;

            return "X:" + p.X + " Y:" + p.Y + " Z:" + p.Z;
        }
    }
}