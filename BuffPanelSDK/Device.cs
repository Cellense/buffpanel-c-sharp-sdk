using System;
using System.Management;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BuffPanel
{
	internal class Device
	{

		public static Dictionary<string, object> GetProperties()
		{
			return new Dictionary<string, object>() {
				{"sdk", Constants.SDK},
				{"sdk_version", Constants.VERSION},
				{"device_model", Environment.MachineName.ToString()},
				{"device_type", "PC"},
				{"os_version", GetFullOsName()},
				{"os_name", "Windows"}				
			};
		}

		private static string GetFullOsName()
		{
			OperatingSystem os = Environment.OSVersion;

			Version vs = os.Version;
			StringBuilder osFriendlyName = new StringBuilder();

			osFriendlyName.Append("Windows");

			if (os.Platform == PlatformID.Win32Windows)
			{
				switch (vs.Minor)
				{
					case 0:
						osFriendlyName.Append(" 95");
						break;
					case 10:
						if (vs.Revision.ToString() == "2222A")
							osFriendlyName.Append(" 98SE");
						else
							osFriendlyName.Append(" 98");
						break;
					case 90:
						osFriendlyName.Append(" Me");
						break;
					default:
						break;
				}
			}
			else if (os.Platform == PlatformID.Win32NT)
			{
				switch (vs.Major)
				{
					case 3:
						osFriendlyName.Append(" NT 3.51");
						break;
					case 4:
						osFriendlyName.Append(" NT 4.0");
						break;
					case 5:
						if (vs.Minor == 0)
							osFriendlyName.Append(" 2000");
						else
							osFriendlyName.Append(" XP");
						break;
					case 6:
						if (vs.Minor == 0)
							osFriendlyName.Append(" Vista");
						else if (vs.Minor == 1)
							osFriendlyName.Append(" 7");
						else if (vs.Minor == 2)
							osFriendlyName.Append(" 8");
						else if (vs.Minor == 3)
							osFriendlyName.Append(" 8.1");
						break;
					case 10:
						osFriendlyName.Append(" 10");
						break;
					default:
						break;
				}
			}
			if (Environment.Is64BitOperatingSystem)
			{
				return osFriendlyName.Append(" 64bit").ToString();
			}
			else
			{
				return osFriendlyName.Append(" 32bit").ToString();
			}


		}
	}
}
