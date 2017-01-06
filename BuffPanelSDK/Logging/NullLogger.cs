using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BuffPanel.Logging
{

	/**
	 * Does not log messages at all.
	 */
	public class NullLogger : Logger
	{
		public void Log(Level level, string message)
		{
			// no op
		}

		public bool IsLevelEnabled(Level level)
		{
			return false;
		}
	}

}
