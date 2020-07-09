using System;
using System.Threading.Tasks;

using Discord;
using Discord.WebSocket;

namespace TrafficRouter
{
	class MessageHandler
	{
		private readonly string[] _acceptableClipLinks =
		{
			"https://medal.tv/clips/"
		};

		private TrafficRouterBot _routerBot = null;
		private CommandHandler _commandHandler = null;

		public MessageHandler(TrafficRouterBot routerBot)
		{
			_routerBot = routerBot;
			_routerBot.Client.MessageReceived += OnMessageReceived;

			_commandHandler = new CommandHandler(routerBot);
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
				SocketGuildChannel guildChannel = userMessage.Channel as SocketGuildChannel;
				if (guildChannel != null && !guildChannel.Name.Equals("clips") && DoesMsgContainClipLink(messageString))
				{
					string fullMessage = userMessage.Author.Username + ": " + messageString;
					await ChannelHelper.SendMessageToGuildChannel(guildChannel.Guild, "clips", fullMessage);
					await userMessage.DeleteAsync();

					Console.WriteLine("Attempting to reroute message: \"" + fullMessage + "\"");
				}
			}
		}

		private bool DoesMsgContainClipLink(string message)
		{
			bool containsClipLink = false;

			foreach (string clipString in _acceptableClipLinks)
			{
				if (message.Contains(clipString))
				{
					containsClipLink = true;
					break;
				}
			}

			return containsClipLink;
		}
	}
}