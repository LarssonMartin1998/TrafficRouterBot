using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Discord;
using Discord.WebSocket;

namespace TrafficRouter
{
	class MessageHandler
	{
		private TrafficRouterBot _routerBot = null;
		private CommandHandler _commandHandler = null;
		private GuildSettingsTracker _settingsTracker = null;

		public MessageHandler(TrafficRouterBot routerBot, CommandHandler commandHandler, GuildSettingsTracker settingsTracker)
		{
			_routerBot = routerBot;
			_routerBot.Client.MessageReceived += OnMessageReceived;

			_commandHandler = commandHandler;
			_settingsTracker = settingsTracker;
		}

		private async Task OnMessageReceived(SocketMessage message)
		{
			SocketUserMessage userMessage = (SocketUserMessage)message;

			// ensures we don't process system/other bot messages
			if (userMessage == null || message.Source != MessageSource.User)
			{
				return;
			}

			string messageString = userMessage.ToString();

			if (_commandHandler.DoesMsgContainCommandPrefix(messageString))
			{
				await _commandHandler.SelectCommandFromMessage(userMessage);
			}
			else
			{
				SocketGuildChannel guildChannel = (SocketGuildChannel)userMessage.Channel;
				if (guildChannel != null)
				{
					List<GuildSettingsTracker.SettingsToTrack.ChannelMessageCombo> activeFilters = _settingsTracker.GetActiveFiltersForGuild(guildChannel.Guild.Id);

					if (activeFilters != null && activeFilters.Count > 0)
					{
						ulong leadingChannel = 0;
						string leadingFilter = string.Empty;
						bool foundFilter = false;

						foreach (var filter in activeFilters)
						{
							if (messageString.Contains(filter._messageToTrack))
							{
								if (filter._messageToTrack.Length >= leadingFilter.Length)
								{
									leadingChannel = filter._channelId;
									leadingFilter = filter._messageToTrack;
									foundFilter = true;
								}
							}	
						}

						if (foundFilter && leadingChannel != userMessage.Channel.Id)
						{
							string fullMessage = "**" + userMessage.Author.Username + ": **" + messageString;
							// Don't care about the tasks, don't depend on them in any way.Also, we don't depend on any data from the methods, just care about the possible exceptions.
							Task sendMsg = ChannelHelper.SendMessageToGuildChannel(guildChannel.Guild, leadingChannel, fullMessage).ContinueWith(t => Console.WriteLine(t.Exception), TaskContinuationOptions.OnlyOnFaulted);
							Task dltMsg = userMessage.DeleteAsync().ContinueWith(t => Console.WriteLine(t.Exception), TaskContinuationOptions.OnlyOnFaulted);

							Console.WriteLine("Attempting to reroute message: \"" + fullMessage + "\"");
						}
					}
				}
			}
		}
	}
}