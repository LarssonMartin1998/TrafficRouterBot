using System;
using System.Threading.Tasks;

using Discord;
using Discord.WebSocket;

namespace TrafficRouter
{
	class TrafficRouterBot
	{
		private DiscordSocketClient _client = null;
		private const string _token = "NzMwMDMxOTQ1Njk3OTg0NTQy.XwZVCw.6jZDJnTMFQ0_YfVi7C1VBEmzSw8";

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
			new MessageHandler(this);

			await _client.LoginAsync(TokenType.Bot, _token);
			await _client.StartAsync();

			// Block this task until the program is closed.
			await Task.Delay(-1);
		}

		private Task Log(LogMessage msg)
		{
			Console.WriteLine(msg.ToString());
			return Task.CompletedTask;
		}

		private Task Ready()
		{
			Console.WriteLine("Ready event fired.");
			return Task.CompletedTask;
		}
	}
}