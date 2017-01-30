using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows.Data;

namespace DriveErrorTest
{
	public class EnumHelper
	{
		public static IEnumerable<string> GetEnumDescriptions(Type enumType)
		{
			foreach (var item in Enum.GetNames(enumType))
			{
				FieldInfo fi = enumType.GetField(item);

				DescriptionAttribute[] attributes =
					(DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);

				if (attributes != null && attributes.Length > 0)
					yield return attributes[0].Description;
				else
					yield return item;
			}
		}
	}

	//public static class EnumToIEnumerable
	//{
	//	public static IEnumerable<KeyValuePair<Enum, string>> GetIEnumerable(Type type)
	//	{
	//		return Enum.GetValues(type).Cast<Enum>().Select((e) => new KeyValuePair<Enum, string>(e,
	//			EnumDescriptionConverter.GetEnumDescription((Enum)e)));
	//	}
	//}
}
