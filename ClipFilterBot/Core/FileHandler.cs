using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace TrafficRouter
{
	class FileHandler
	{
		private string _configsDir = string.Empty;
		private const string _filesFolderName = "Configs";

		public FileHandler()
		{
			string currentDir = Directory.GetCurrentDirectory();
			_configsDir = currentDir + "\\" + _filesFolderName + "\\";

			if (!Directory.Exists(_configsDir))
			{
				Directory.CreateDirectory(_configsDir);
			}
		}

		// We're always erasing previous files when opening them now, since we have no need to read from them more than once (when starting the bot).
		public async Task WriteToTextFileAsync(string fileName, string fileExtension, string[] fileContent, string extraDir = null)
		{
			string dir = extraDir == null ? _configsDir : _configsDir + extraDir;
			if (dir[dir.Length - 1] != '\\')
			{
				dir += "\\";
			}

			if (!Directory.Exists(dir))
			{
				Directory.CreateDirectory(dir);
			}

			string filePath = dir + fileName + fileExtension;
			if (!filePath.Contains("."))
			{
				filePath = dir + fileName + "." + fileExtension;
			}

			// Create overwrites existing files, but that's fine for our use-case.
			FileStream fileStream = File.Create(filePath);
			StreamWriter streamWriter = new StreamWriter(fileStream);
			List<Task> tasks = new List<Task>();

			// Order in the file matters not.
			foreach (string line in fileContent)
			{
				tasks.Add(streamWriter.WriteLineAsync(line));
			}

			await Task.WhenAll(tasks);

			streamWriter.Close();
		}

		public string[] GetAllFileNamesInDir(string searchPattern, string extraDir = null)
		{
			string[] names = null;

			string dir = extraDir == null ? _configsDir : _configsDir + extraDir;
			if (dir[dir.Length - 1] != '\\')
			{
				dir += "\\";
			}

			if (Directory.Exists(dir))
			{
				DirectoryInfo dirInfo = new DirectoryInfo(dir);
				FileInfo[] files = dirInfo.GetFiles(searchPattern, SearchOption.TopDirectoryOnly);

				if (files.Length > 0)
				{
					names = new string[files.Length];

					for (uint i = 0; i < files.Length; ++i)
					{
						names[i] = files[i].Name;
					}
				}
			}

			return names;
		}

		public string[] ReadAllLinesTextFile(string fileName, string fileExtension, string extraDir = null)
		{
			string[] fileContents = null;

			string dir = extraDir == null ? _configsDir : _configsDir + extraDir;
			if (dir[dir.Length - 1] != '\\')
			{
				dir += "\\";
			}

			string filePath = dir + fileName + fileExtension;
			if (!filePath.Contains("."))
			{
				filePath = dir + fileName + "." + fileExtension;
			}

			if (File.Exists(filePath))
			{
				fileContents = File.ReadAllLines(filePath);
			}

			return fileContents;
		}
	}
}