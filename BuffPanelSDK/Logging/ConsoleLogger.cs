using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BuffPanel.Logging
{

	/**
	 * Logs all messages to the console.
	 */
	public class ConsoleLogger : Logger
	{

		public void Log(Level level, string message)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("BuffPanel [");
			sb.Append(Enum.GetName(typeof(Level), level));
			sb.Append("]: ");
			sb.Append(message);
			Console.WriteLine(sb.ToString());
		}

		public bool IsLevelEnabled(Level level)
		{
			return true;
		}
	}

}
