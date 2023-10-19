using System;
using System.Collections.Generic;
using System.Reflection;

namespace BunnyLibs
{
	public class InterfaceHelper
	{
		public static List<Type> TypesImplementingInterface(Type parent)
		{
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies(); // This should get from all mods
			List<Type> implementingTypes = new List<Type>();

			foreach (Assembly assembly in assemblies)
			{
				Type[] types = assembly.GetTypes();

				foreach (Type type in types)
				{
					if (parent.IsAssignableFrom(type))
					{
						implementingTypes.Add(type);
					}
				}
			}

			return implementingTypes;
		}
	}
}