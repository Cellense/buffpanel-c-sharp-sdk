using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BuffPanel.Logging
{

	/**
	 * Instances of Logger must be safe to use from multiple threads concurently.
	 */
	public interface Logger
	{

		/**
		 * Logs a message to the log
		 */
		void Log(Level level, string message);

		/**
		 * Returns whether the logger would log the specified level.
		 * If this method returns false, the SDK does not even construct the
		 * log messages.
		 */
		bool IsLevelEnabled(Level level);
	}
}


