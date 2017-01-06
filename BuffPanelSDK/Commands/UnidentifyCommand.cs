using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BuffPanel.Commands
{
	internal class UnidentifyCommand : Command
	{

		public void Execute(State state)
		{
			state.RemoveIds();
		}

	}
}
