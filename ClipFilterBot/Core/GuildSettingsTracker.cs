using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrafficRouter
{
	class GuildSettingsTracker
	{
		public enum ActionResult
		{
			Succesful,
			GuildNotFound,
			ChannelNotFound,
			TrackedMessageNotFound,
			FilterAlreadyExists
		}

		internal class SettingsToTrack
		{
			internal class ChannelMessageCombo
			{
				internal ChannelMessageCombo(ulong channelId, string messageToTrack)
				{
					_channelId = channelId;
					_messageToTrack = messageToTrack;
				}

				internal ulong _channelId;
				internal string _messageToTrack;
			}

			internal ulong _guildId;
			internal List<ChannelMessageCombo> _activeFilters = new List<ChannelMessageCombo>(); //channelId - messageStringToTrack
		}

		private TrafficRouterBot _routerBot = null;
		private FileHandler _fileHandler = null;

		// SettingsDir is relative to the Config folder defined inside fileHandler.
		private static string _settingsDir = "UserSettings\\";
		private List<SettingsToTrack> _activeGuilds = new List<SettingsToTrack>();

		public GuildSettingsTracker(TrafficRouterBot routerBot, FileHandler fileHandler)
		{
			_routerBot = routerBot;
			_fileHandler = fileHandler;

			ReadSettingsFromFile();
		}

		public ActionResult AddFilterToGuild(ulong guildId, ulong channelId, string trackedMessage)
		{
			ActionResult result = ActionResult.Succesful;
			SettingsToTrack settings = null;

			if (!_routerBot.IsBotInGuild(guildId))
			{
				result = ActionResult.GuildNotFound;
			}
			else if (!_routerBot.IsChannelInGuild(guildId, channelId))
			{
				result = ActionResult.ChannelNotFound;
			}
			else
			{
				settings = GetSettingsForGuild(guildId);

				if (settings == null)
				{
					settings = new SettingsToTrack();
					settings._guildId = guildId;
					settings._activeFilters = new List<SettingsToTrack.ChannelMessageCombo>();
					_activeGuilds.Add(settings);
				}

				// Does filter already exist? Dont want double stored.
				foreach(var filter in settings._activeFilters)
				{
					if (filter._messageToTrack.Equals(trackedMessage))
					{
						result = ActionResult.FilterAlreadyExists;
					}
				}
			}

			if (result == ActionResult.Succesful)
			{
				settings._activeFilters.Add(new SettingsToTrack.ChannelMessageCombo(channelId, trackedMessage));
			}

			return result;
		}

		private SettingsToTrack GetSettingsForGuild(ulong guildId)
		{
			SettingsToTrack foundSettings = null;

			foreach (SettingsToTrack settings in _activeGuilds)
			{
				if (settings._guildId == guildId)
				{
					foundSettings = settings;
					break;
				}
			}

			return foundSettings;
		}

		public ActionResult RemoveFilterFromGuild(ulong guildId, string trackedMessage)
		{
			ActionResult result = ActionResult.Succesful;

			if (!_routerBot.IsBotInGuild(guildId))
			{
				result = ActionResult.GuildNotFound;
			}
			else
			{
				SettingsToTrack settings = GetSettingsForGuild(guildId);
				if (settings != null)
				{
					bool doesGuildHaveFilter = false;

					foreach (var filter in settings._activeFilters)
					{
						if (filter._messageToTrack.Equals(trackedMessage))
						{
							doesGuildHaveFilter = true;
							settings._activeFilters.Remove(filter);
							break;
						}
					}

					if (!doesGuildHaveFilter)
					{
						result = ActionResult.TrackedMessageNotFound;
					}
				}
				else
				{
					result = ActionResult.TrackedMessageNotFound;
				}
			}

			return result;
		}

		public ActionResult ClearFiltersForGuild(ulong guildId)
		{
			ActionResult result = ActionResult.Succesful;
			SettingsToTrack settings = null;

			if (!_routerBot.IsBotInGuild(guildId))
			{
				result = ActionResult.GuildNotFound;
			}
			else
			{
				settings = GetSettingsForGuild(guildId);
				if (settings == null ||
					settings._activeFilters.Count <= 0)
				{
					result = ActionResult.TrackedMessageNotFound;
				}
			}

			if (result == ActionResult.Succesful)
			{
				settings._activeFilters.Clear();
			}

			return result;
		}

		public List<SettingsToTrack.ChannelMessageCombo> GetActiveFiltersForGuild(ulong guildId)
		{
			List<SettingsToTrack.ChannelMessageCombo> guildFilters = null;

			SettingsToTrack settings = GetSettingsForGuild(guildId);
			if (_routerBot.IsBotInGuild(guildId) && settings != null)
			{
				guildFilters = settings._activeFilters;
			}

			return guildFilters;
		}

		public async Task FlushSettingsAsync()
		{
			List<string> fileContent = new List<string>();
			List<Task> tasks = new List<Task>();

			// Doesnt matter in which order we create file and write to it.
			foreach (SettingsToTrack guild in _activeGuilds)
			{
				string fileName = guild._guildId.ToString();
				fileContent.Clear();

				foreach (var filter in guild._activeFilters)
				{
					fileContent.Add(filter._channelId.ToString() + "," + filter._messageToTrack);

					Console.WriteLine(filter._channelId.ToString() + "," + filter._messageToTrack);
				}

				tasks.Add(_fileHandler.WriteToTextFileAsync(fileName, ".txt", fileContent.ToArray(), _settingsDir));
			}

			await Task.WhenAll(tasks);
		}

		private void ReadSettingsFromFile()
		{
			_activeGuilds.Clear();
			string[] fileNames = _fileHandler.GetAllFileNamesInDir("*.txt", _settingsDir);

			if (fileNames != null)
			{
				foreach (string fileName in fileNames)
				{
					// Remove file extension from filename
					string fileNameWithoutExtension = fileName.Split('.')[0];

					ulong guildId;
					if (ulong.TryParse(fileNameWithoutExtension, out guildId))
					{
						SettingsToTrack guild = new SettingsToTrack();
						guild._guildId = guildId;
						guild._activeFilters = new List<SettingsToTrack.ChannelMessageCombo>();

						string[] lines = _fileHandler.ReadAllLinesTextFile(fileNameWithoutExtension, ".txt", _settingsDir);

						foreach (string line in lines)
						{
							// Save strucutre is as follows "channelid","msg".
							// See GuildSettingsTracker.FlushSettingsAsync for the code
							string[] guildData = line.Split(',');
							ulong channelId;
							if (ulong.TryParse(guildData[0], out channelId))
							{
								guild._activeFilters.Add(new SettingsToTrack.ChannelMessageCombo(channelId, guildData[1]));
							}
						}

						_activeGuilds.Add(guild);
					}
				}
			}
		}

		private SettingsToTrack GetGuildSettingsFromId(ulong guildId)
		{
			SettingsToTrack guildSettings = null;

			foreach (SettingsToTrack guild in _activeGuilds)
			{
				if (guild._guildId == guildId)
				{
					guildSettings = guild;
					break;
				}
			}

			return guildSettings;
		}

		public void RemoveAllFiltersForGuildChannel(ulong guildId, ulong channelId)
		{
			SettingsToTrack guildSettings = GetGuildSettingsFromId(guildId);

			for (int i = guildSettings._activeFilters.Count - 1; i >= 0; i--)
			{
				if (guildSettings._activeFilters[i]._channelId == channelId)
				{
					guildSettings._activeFilters.RemoveAt(i);
				}
			}
		}

		public void UpdateGuildChannelId(ulong guildId, ulong oldChannelId, ulong newChannelId)
		{
			SettingsToTrack guildSettings = GetGuildSettingsFromId(guildId);

			foreach (var filter in guildSettings._activeFilters)
			{
				if (filter._channelId == oldChannelId)
				{
					filter._channelId = newChannelId;
				}
			}
		}
	}
}