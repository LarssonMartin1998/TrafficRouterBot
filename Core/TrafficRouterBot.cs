using System;
using System.Threading.Tasks;

using Discord;
using Discord.WebSocket;
using Discord.Commands;

namespace TrafficRouter
{
	class TrafficRouterBot
	{
		private const string _gameStatus = "!tr help";

		private DiscordSocketClient _client = null;

		private FileHandler _fileHandler = null;
		private MessageHandler _messageHandler = null;
		private CommandHandler _commandHandler = null;
		private GuildSettingsTracker _guildSettingsTracker = null;

		public DiscordSocketClient Client
		{
			get
			{
				return _client;
			}

			private set
			{
				_client = value;
			}
		}

		public async Task InitAsync()
		{
			_client = new DiscordSocketClient();

			_client.Log += Log;
			_client.Ready += Ready;
			_client.ChannelDestroyed += ChannelDestroyed;
			_client.ChannelUpdated += ChannelUpdated;

			_fileHandler = new FileHandler();
			_guildSettingsTracker = new GuildSettingsTracker(this, _fileHandler);
			_commandHandler = new CommandHandler(this, _guildSettingsTracker);
			_messageHandler = new MessageHandler(this, _commandHandler, _guildSettingsTracker);

			// You will need to add a text file called "token.txt", in a dir: "exe_root\Configs\" and paste your bot token in the file.
			string[] tokenFileContent = _fileHandler.ReadAllLinesTextFile("token", ".txt");
			string token = string.Empty;

			if (tokenFileContent != null)
			{
				foreach (string tokenFileLine in tokenFileContent)
				{
					token += tokenFileLine;
				}

				await _client.LoginAsync(TokenType.Bot, token);
				await _client.StartAsync();
			}
			else
			{
				Console.WriteLine("ERROR: You need to create a \"token.txt\" file under .exe-root\\Configs\\ dir.");
			}

			// Block this task until the program is closed.
			await Task.Delay(-1);
		}

		public bool IsBotInGuild(ulong guildId)
		{
			return GetGuildFromId(guildId) != null;
		}

		public SocketGuild GetGuildFromId(ulong guildId)
		{
			SocketGuild foundGuild = null;

			foreach (SocketGuild guild in _client.Guilds)
			{
				if (guild.Id == guildId)
				{
					foundGuild = guild;
					break;
				}
			}

			return foundGuild;
		}

		public SocketGuildChannel GetChannelFromId(ulong guildId, ulong channelId)
		{
			SocketGuildChannel foundChannel = null;

			SocketGuild guild = GetGuildFromId(guildId);
			if (guild != null)
			{
				foreach (SocketGuildChannel channel in guild.Channels)
				{
					if (channel.Id == channelId)
					{
						foundChannel = channel;
						break;
					}
				}
			}

			return foundChannel;
		}

		public bool IsChannelInGuild(ulong guildId, ulong channelId)
		{
			return GetChannelFromId(guildId, channelId) != null;
		}

		private Task ChannelUpdated(SocketChannel originalChannel, SocketChannel newChannel)
		{
			SocketGuildChannel guildChannel = (SocketGuildChannel)originalChannel;
			if (guildChannel != null)
			{
				_guildSettingsTracker.UpdateGuildChannelId(guildChannel.Guild.Id, originalChannel.Id, newChannel.Id);
			}

			return Task.CompletedTask;
		}

		private async Task ChannelDestroyed(SocketChannel channel)
		{
			SocketGuildChannel guildChannel = (SocketGuildChannel)channel;
			if (guildChannel != null)
			{
				_guildSettingsTracker.RemoveAllFiltersForGuildChannel(guildChannel.Guild.Id, channel.Id);
			}

			await _guildSettingsTracker.FlushSettingsAsync();
		}

		private Task Log(LogMessage msg)
		{
			Console.WriteLine(msg.ToString());
			return Task.CompletedTask;
		}

		private async Task Ready()
		{
			await _client.SetGameAsync(_gameStatus, null, ActivityType.Playing);
		}
	}
}