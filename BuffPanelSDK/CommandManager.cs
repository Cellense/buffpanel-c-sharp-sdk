using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using BuffPanel.Commands;

namespace BuffPanel
{
	internal class CommandManager
	{

		private BlockingCollection<Command> queue;
		private State state;

		public CommandManager(BlockingCollection<Command> queue, State state)
		{
			this.queue = queue;
			this.state = state;
		}

		public void Consume()
		{
			this.state.Initialize();
			while (!queue.IsCompleted)
			{
				Command command = null;
				try
				{
					command = queue.Take();
				}
				catch (InvalidOperationException) { }
				if (command != null) command.Execute(state);
			}
			if (state.sendAllQueued)
			{
				state.SendAllNow();
			}
			else
			{
				state.StopSending();
			}
		}

	}
}
