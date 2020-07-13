using System.Threading.Tasks;

namespace TrafficRouter
{
	public class Program
	{
		private static void Main(string[] args)
		{
			MainAsync().GetAwaiter().GetResult();
		}

		private static async Task MainAsync()
		{
			TrafficRouterBot bot = new TrafficRouterBot();
			await bot.InitAsync();
		}
	}
}