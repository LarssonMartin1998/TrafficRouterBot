using System;
using System.Threading.Tasks;

using Discord;
using Discord.WebSocket;

namespace ClipFilterBot
{
	public class Program
	{
		private string[] _acceptableClipLinks =
		{
			"https://medal.tv/clips/"
		};
		
		private DiscordSocketClient _client = null;

		public static void Main(string[] args)
		{
			new Program().MainAsync().GetAwaiter().GetResult();
		}

		public static bool GetChannelIdFromName(SocketGuild guild, string channelName, out ulong channelId)
		{
			channelId = ulong.MaxValue;

			foreach (SocketGuildChannel channel in guild.Channels)
			{
				if (channel.Name.Equals(channelName))
				{
					channelId = channel.Id;
					return true;
				}
			}

			Console.WriteLine("Warning: Could NOT find channel of name: \"" + channelName + "\" in guild.");
			return false;
		}

		public async static Task SendMessageToGuildChannel(SocketGuild guild, ulong channelId, string message)
		{
			SocketTextChannel textChannel = guild.GetTextChannel(channelId);

			if (textChannel != null)
			{
				await textChannel.SendMessageAsync(message);
			}
		}

		public async static Task SendMessageToGuildChannel(SocketGuild guild, string channelName, string message)
		{
			ulong channelId;
			GetChannelIdFromName(guild, channelName, out channelId);
			await SendMessageToGuildChannel(guild, channelId, message);
		}

		public async Task MainAsync()
		{
			_client = new DiscordSocketClient();

			_client.Log += Log;
			_client.Ready += Ready;
			_client.MessageReceived += OnMessageReceived;

			var token = "NzMwMDMxOTQ1Njk3OTg0NTQy.XwR9JA.iiJ9fa-UIK1vnPPaCqsCVi1ChLA";

			await _client.LoginAsync(TokenType.Bot, token);
			await _client.StartAsync();

			// Block this task until the program is closed.
			await Task.Delay(-1);
		}

		private async Task OnMessageReceived(SocketMessage message)
		{
			SocketUser user = message.Author;

			if (user.Id != _client.CurrentUser.Id)
			{
				string messageString = message.ToString();

				SocketGuildChannel guildChannel = message.Channel as SocketGuildChannel;
				if (guildChannel != null && !guildChannel.Name.Equals("clips") && DoesMsgContainClipLink(messageString))
				{
					await SendMessageToGuildChannel(guildChannel.Guild, "clips", user.Username + ": " + messageString);
					await message.DeleteAsync();
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

		private async Task Ready()
		{

		}

		private Task Log(LogMessage msg)
		{
			Console.WriteLine(msg.ToString());
			return Task.CompletedTask;
		}
	}
}