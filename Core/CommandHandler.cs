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
		private TrafficRouterBot _routerBot = null;
		private GuildSettingsTracker _settingsTracker = null;

		private readonly string[] _commandPrefixes =  
		{
			"!TrafficRouter",
			"!TrafficR",
			"!TR",
			"!TRouter"
		};

		// Note!!!!
		// Make sure CommandId, _commandNames, _extraCommandDescription, and _numCommandParams matches in order and size!!-

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

			" channel-name msg-to-reroute          **ex:** \"!tr AddFilter general www.youtube.com\"\n" +
			"   desc: *Adds a filter which reroute messages to a channel that contain a specified message.*",

			" \n " +
			"   desc: *Prints a message of all active filters in this server.*",

			" filter-msg-to-remove                 **ex:** \"!tr RemoveFilter www.youtube.com\"\n " +
			"   desc: *Remove a single specified filter.*",

			" \n " +
			"   desc: *Removes ALL active filters on the server.*"
		};

		private readonly int[] _numCommandParams =
		{
			0,
			2,
			0,
			1,
			0
		};

		public CommandHandler(TrafficRouterBot routerBot, GuildSettingsTracker settingsTracker)
		{
			_routerBot = routerBot;
			_settingsTracker = settingsTracker;
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

		// Most if not all async calls are from sending messages, or doing a different task like flushing user settings, and we don't depend on any data from those async methods, hence, suppressing warning CS1998:
		// Async method lacks 'await' operators and will run synchronously
		// Most methods still run async but were catching their exceptions seperatley.
#pragma warning disable 1998
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

			// Remove the command prefix from the raw message. We have no use for it any longer.
			string[] rawMessageSplit = rawMessage.Split(' ');
			StringBuilder stringBuilder = new StringBuilder();

			for (uint i = 2; i < rawMessageSplit.Length; ++i)
			{
				stringBuilder.Append(rawMessageSplit[i]);

				if (!(i == rawMessageSplit.Length - 1))
				{
					stringBuilder.Append(" ");
				}
			}

			rawMessage = stringBuilder.ToString();

			switch (commandToExecute)
			{
				case (int)CommandId.Help:
					await CommandHelp(message.Channel);
					break;
				case (int)CommandId.AddFilter:
					await CommandAddFilter(message.Channel, rawMessage);
					break;
				case (int)CommandId.ViewActiveFilters:
					await CommandViewActiveFilters(message.Channel);
					break;
				case (int)CommandId.RemoveFilter:
					await CommandRemoveFilter(message.Channel, rawMessage);
					break;
				case (int)CommandId.ClearFilters:
					await CommandClearFilters(message.Channel);
					break;
				default:
					Task msgTask = message.Channel.SendMessageAsync("Unrecognized command, write \"!TrafficRouter help\" for a command guide.").ContinueWith(t => Console.WriteLine(t.Exception), TaskContinuationOptions.OnlyOnFaulted);
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

			Task msgTask = channel.SendMessageAsync(stringBuilder.ToString()).ContinueWith(t => Console.WriteLine(t.Exception), TaskContinuationOptions.OnlyOnFaulted);
		}

		private async Task CommandAddFilter(IMessageChannel channel, string rawMessage)
		{
			StringBuilder stringBuilder = new StringBuilder();

			string[] rawMessageWordsArray = rawMessage.Split(' ');

			if (rawMessageWordsArray.Length != _numCommandParams[(int)CommandId.AddFilter])
			{
				stringBuilder.Append(rawMessageWordsArray.Length < _numCommandParams[(int)CommandId.AddFilter] ? "Too **few** arguments in the command." : "Too **many** arguments in the command.");
				stringBuilder.Append("\nType \"!tr help\" for instructions of how to use all the commands.");
			}
			else
			{
				ulong channelId;
				SocketGuild guild = ((SocketGuildChannel)channel).Guild;
				if (ChannelHelper.GetChannelIdFromName(guild, rawMessageWordsArray[0], out channelId))
				{
					GuildSettingsTracker.ActionResult result = _settingsTracker.AddFilterToGuild(guild.Id, channelId, rawMessageWordsArray[1]);

					switch (result)
					{
						case GuildSettingsTracker.ActionResult.GuildNotFound:
							stringBuilder.Append("Nothing happend, internal error: Guild Not Found.");
							break;
						case GuildSettingsTracker.ActionResult.ChannelNotFound:
							stringBuilder.Append("I can't find the channel you're trying to reroute to, are you sure it's spelled correctly? \"" + rawMessageWordsArray[0] + "\". If it is spelled correctly, I might not have permissions for that channel :(");
							break;
						case GuildSettingsTracker.ActionResult.FilterAlreadyExists:
							stringBuilder.Append("It already exists a filter for the message that you're trying to add.");
							break;
						case GuildSettingsTracker.ActionResult.Succesful:
							{
								stringBuilder.Append("Filter added for *" + rawMessageWordsArray[1] + "* which will be rerouted to *#" + rawMessageWordsArray[0] + "*.");
								Task flushTask = _settingsTracker.FlushSettingsAsync().ContinueWith(t => Console.WriteLine(t.Exception), TaskContinuationOptions.OnlyOnFaulted); ;
							}
							break;
					}
				}
				else
				{
					stringBuilder.Append("I can't find the channel you're trying to reroute to, are you sure it's spelled correctly? \"" + rawMessageWordsArray[0] + "\". If it is spelled correctly, I might not have permissions for that channel :(");
				}
			}

			Task msgTask = channel.SendMessageAsync(stringBuilder.ToString()).ContinueWith(t => Console.WriteLine(t.Exception), TaskContinuationOptions.OnlyOnFaulted);
		}

		private async Task CommandViewActiveFilters(IMessageChannel channel)
		{
			StringBuilder stringBuilder = new StringBuilder();

			ulong guildId = ((SocketGuildChannel)channel).Guild.Id;
			List<GuildSettingsTracker.SettingsToTrack.ChannelMessageCombo> activeFilters = _settingsTracker.GetActiveFiltersForGuild(guildId);

			if (activeFilters == null || activeFilters.Count <= 0)
			{
				stringBuilder.Append("You currently have no filters active.");
			}
			else if (activeFilters.Count >= 1)
			{
				if (activeFilters.Count == 1)
				{
					stringBuilder.Append("You only have **one** filter active:\n");
				}
				else
				{
					stringBuilder.Append("These are your active filters:\n");
				}

				SocketGuild guild = _routerBot.Client.GetGuild(guildId);
				List<ulong> oldChannelIds = new List<ulong>();

				foreach (var filter in activeFilters)
				{
					// Text channel can be null if we loaded from previously saved settings, and the channel added has been deleted.
					SocketTextChannel textChannel = guild.GetTextChannel(filter._channelId);
					if (textChannel != null)
					{
						stringBuilder.Append("   - Reroute messages containing *" + filter._messageToTrack + "* to channel *" + textChannel.Name + "*.\n");
					}
					else
					{
						oldChannelIds.Add(filter._channelId);
						stringBuilder.Append("   - **Warning:** The message filter \"*" + filter._messageToTrack + "*\" targets an old channel, and was **removed**!\n");
					}
				}

				if (oldChannelIds.Count > 0)
				{
					foreach (ulong channelId in oldChannelIds)
					{
						_settingsTracker.RemoveAllFiltersForGuildChannel(guildId, channelId);
					}

					Task flushTask = _settingsTracker.FlushSettingsAsync().ContinueWith(t => Console.WriteLine(t.Exception), TaskContinuationOptions.OnlyOnFaulted);
				}
			}
			
			Task msgTask = channel.SendMessageAsync(stringBuilder.ToString()).ContinueWith(t => Console.WriteLine(t.Exception), TaskContinuationOptions.OnlyOnFaulted);
		}

		private async Task CommandRemoveFilter(IMessageChannel channel, string rawMessage)
		{
			StringBuilder stringBuilder = new StringBuilder();
			ulong guildId = ((SocketGuildChannel)channel).Guild.Id;

			string[] rawMessageWordsArray = rawMessage.Split(' ');

			if (rawMessageWordsArray.Length != _numCommandParams[(int)CommandId.RemoveFilter])
			{
				stringBuilder.Append(rawMessageWordsArray.Length < _numCommandParams[(int)CommandId.RemoveFilter] ? "Too **few** arguments in the command." : "Too **many** arguments in the command.");
				stringBuilder.Append("\nType \"!tr help\" for instructions of how to use all the commands.");
			}
			else if(string.IsNullOrEmpty(rawMessage))
			{
				stringBuilder.Append("Too **few** arguments in the command.");
				stringBuilder.Append("\nType \"!tr help\" for instructions of how to use all the commands.");
			}
			else
			{
				GuildSettingsTracker.ActionResult result = _settingsTracker.RemoveFilterFromGuild(guildId, rawMessageWordsArray[0]);

				switch (result)
				{
					case GuildSettingsTracker.ActionResult.GuildNotFound:
						stringBuilder.Append("Nothing happend, internal error: Guild Not Found.");
						break;
					case GuildSettingsTracker.ActionResult.TrackedMessageNotFound:
						stringBuilder.Append("I couldn't find any filter for the message *" + rawMessageWordsArray[0] + "* :( Make sure it's an exact match.");
						break;
					case GuildSettingsTracker.ActionResult.Succesful:
						{
							stringBuilder.Append("The filter for *" + rawMessageWordsArray[0] + "* has been removed!");
							Task flushTask = _settingsTracker.FlushSettingsAsync().ContinueWith(t => Console.WriteLine(t.Exception), TaskContinuationOptions.OnlyOnFaulted); ;
						}
						break;
				}
			}

			Task msgTask = channel.SendMessageAsync(stringBuilder.ToString()).ContinueWith(t => Console.WriteLine(t.Exception), TaskContinuationOptions.OnlyOnFaulted);
		}

		private async Task CommandClearFilters(IMessageChannel channel)
		{
			StringBuilder stringBuilder = new StringBuilder();

			ulong guildId = ((SocketGuildChannel)channel).Guild.Id;
			GuildSettingsTracker.ActionResult result = _settingsTracker.ClearFiltersForGuild(guildId);

			switch (result)
			{
				case GuildSettingsTracker.ActionResult.GuildNotFound:
					stringBuilder.Append("Nothing happend, internal error: Guild Not Found.");
					break;
				case GuildSettingsTracker.ActionResult.TrackedMessageNotFound:
					stringBuilder.Append("You don't have any filters active, but I cleared them anyway... just to be sure ;)");
					break;
				case GuildSettingsTracker.ActionResult.Succesful:
					{
						stringBuilder.Append("All your filters have been removed!");
						Task flushTask = _settingsTracker.FlushSettingsAsync().ContinueWith(t => Console.WriteLine(t.Exception), TaskContinuationOptions.OnlyOnFaulted);
					}
					break;
			}

			Task msgTask = channel.SendMessageAsync(stringBuilder.ToString()).ContinueWith(t => Console.WriteLine(t.Exception), TaskContinuationOptions.OnlyOnFaulted);
		}
#pragma warning restore 1998
	}
}