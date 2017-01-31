using System;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Windows.Data;
using System.Windows.Markup;

namespace DriveErrorTest
{
	public class EnumBindingSourceExtension : MarkupExtension
	{
		private Type _enumType;

		public Type EnumType
		{
			get { return _enumType; }
			set
			{
				if (value != _enumType)
				{
					if (null != value)
					{
						Type enumType = Nullable.GetUnderlyingType(value) ?? value;
						if (!enumType.IsEnum)
							throw new ArgumentException("Type must be for an Enum.");
					}

					_enumType = value;
				}
			}
		}

		public EnumBindingSourceExtension()
		{
		}

		public EnumBindingSourceExtension(Type enumType)
		{
			EnumType = enumType;
		}

		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			if (null == _enumType)
				throw new InvalidOperationException("The EnumType must be specified.");

			var actualEnumType = Nullable.GetUnderlyingType(_enumType) ?? _enumType;
			var enumValues = Enum.GetValues(actualEnumType);

			if (actualEnumType == _enumType)
				return enumValues;

			Array tempArray = Array.CreateInstance(actualEnumType, enumValues.Length + 1);
			enumValues.CopyTo(tempArray, 1);
			return tempArray;
		}
	}

	public class EnumDescriptionTypeConverter : EnumConverter
	{
		public EnumDescriptionTypeConverter(Type type)
			: base(type)
		{
		}
		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
		{
			if (destinationType == typeof(string))
			{
				if (value != null)
				{
					FieldInfo fi = value.GetType().GetField(value.ToString());
					if (fi != null)
					{
						var attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);
						return ((attributes.Length > 0) && (!string.IsNullOrEmpty(attributes[0].Description))) ? attributes[0].Description : value.ToString();
					}
				}

				return string.Empty;
			}

			return base.ConvertTo(context, culture, value, destinationType);
		}
	}

	public class TimeSpanConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (!(value is TimeSpan))
				return string.Empty;

			var t = (TimeSpan)value;

			return TimeSpanValueParser.GetString(t.Days, t.Hours, t.Minutes);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return TimeSpan.Parse(value.ToString());
		}
	}

	public static class TimeSpanValueParser
	{
		public static string GetString(int days, int hours, int minutes)
		{
			string result = "";

			if (days > 0)
				result += days + " д. ";

			if (hours > 0)
				result += hours + " ч. ";

			if (minutes > 0)
				result += minutes + " м."; 

			return result;
		}
	}
}