using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Discord;
using Discord.WebSocket;

namespace TrafficRouter
{
	class CommandHandler
	{
		TrafficRouterBot _routerBot = null;

		private readonly string[] _commandPrefixes =  
		{
			"!TrafficRouter",
			"!TrafficR",
			"!TR",
			"!TRouter"
		};

		// Note!!!!
		// Make sure CommandId and _commandNames matches in order and size!!-
		private enum CommandId
		{
			Help,
			AddFilter,
			ViewActiveFilters,
			RemoveFilter,
			ClearFilters
		}

		private readonly string[] _commandNames =
		{
			"Help",
			"AddFilter",
			"ViewActiveFilters",
			"RemoveFilter",
			"ClearFilters"
		};

		private readonly string[] _extraCommandDescription = 
		{
			" \n " +
			"   desc: *Prints this helpful message.*",

			" channel-name msg-to-reroute          ex: \"!tr AddFilter general www.youtube.com\"\n" +
			"   desc: *Adds a filter which reroute messages to a channel that contain a specified message.*",

			" \n " +
			"   desc: *Prints a message of all active filters in this server.*",

			" \n " +
			"   desc: *Remove a single specified filter.*",

			" \n " +
			"   desc: *Removes ALL active filters on the server.*"
		};

		public CommandHandler(TrafficRouterBot routerBot)
		{
			_routerBot = routerBot;
		}

		public bool DoesMsgContainCommandPrefix(string message)
		{
			message = message.ToLower();
			bool containsPrefix = false;

			foreach (string prefix in _commandPrefixes)
			{
				if (message.Length < prefix.Length)
				{
					continue;
				}

				if (message.Substring(0, prefix.Length).Equals(prefix.ToLower()))
				{
					containsPrefix = true;
					break;
				}
			}

			return containsPrefix;
		}

		public async Task SelectCommandFromMessage(SocketUserMessage message)
		{
			int commandToExecute = -1;

			string rawMessage = message.ToString();

			for (uint i = 0; i < _commandNames.Length; ++i)
			{
				if (rawMessage.ToString().ToLower().Contains(_commandNames[i].ToLower()))
				{
					commandToExecute = (int)i;
				}
			}

			switch (commandToExecute)
			{
				case (int)CommandId.Help:
					await CommandHelp(message.Channel);
					break;
				case (int)CommandId.AddFilter:
					await CommandAddFilter(message.Channel);
					break;
				case (int)CommandId.ViewActiveFilters:
					await CommandViewActiveFilters(message.Channel);
					break;
				case (int)CommandId.RemoveFilter:
					await CommandRemoveFilter(message.Channel);
					break;
				case (int)CommandId.ClearFilters:
					await CommandClearFilters(message.Channel);
					break;
				default:
					await message.Channel.SendMessageAsync("Unrecognized command, write \"!TrafficRouter help\" for a command guide.");
					break;
			}
		}

		private async Task CommandHelp(IMessageChannel channel)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("These are all the command prefixes: \n\n");

			foreach (string prefix in _commandPrefixes)
			{
				stringBuilder.Append("   - **" + prefix + "**\n");
			}

			stringBuilder.Append("\n");

			stringBuilder.Append("And here are all the commands: \n\n");

			for (uint i = 0; i < _commandNames.Length; ++i)
			{
				stringBuilder.Append("   - **" + _commandNames[i] + "**");

				if (!string.IsNullOrEmpty(_extraCommandDescription[i]))
				{
					stringBuilder.Append(_extraCommandDescription[i]);
				}

				stringBuilder.Append("\n\n");
			}

			await channel.SendMessageAsync(stringBuilder.ToString());
		}

		private async Task CommandAddFilter(IMessageChannel temp)
		{
			await temp.SendMessageAsync("Performing AddFilter command ...");
		}

		private async Task CommandViewActiveFilters(IMessageChannel temp)
		{
			await temp.SendMessageAsync("Performing ViewActiveFilters command ...");
		}

		private async Task CommandRemoveFilter(IMessageChannel temp)
		{
			await temp.SendMessageAsync("Performing RemoveFilter command ...");
		}

		private async Task CommandClearFilters(IMessageChannel temp)
		{
			await temp.SendMessageAsync("Performing ClearFilters command ...");
		}
	}
}
