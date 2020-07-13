using System;
using System.Threading.Tasks;

using Discord;
using Discord.WebSocket;

namespace TrafficRouter
{
	class ChannelHelper
	{
		// Cannot guarantee that ulong.MaxValue isn't a valid ID for a channel, hence why were returning a bool if we succeed or not, and channelId as out param.
		public static bool GetChannelIdFromName(SocketGuild guild, string channelName, out ulong channelId)
		{
			channelId = ulong.MaxValue;

			foreach (SocketGuildChannel channel in guild.Channels)
			{
				if (channel != null && channel.Name.Equals(channelName))
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
			else
			{
				Console.WriteLine("Text Channel with ID: \"" + channelId + "\"is null in guild: \"" + guild.Name + "\".");
			}
		}
	}
}