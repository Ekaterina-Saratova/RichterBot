using Microsoft.Extensions.Configuration;
using NLog;

namespace RichterBot
{
    internal class Program
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        static async Task Main()
        {
            Console.WriteLine("Hello, World!");
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();
            var token  = configuration["AppSettings:Token"];
            if (string.IsNullOrEmpty(token))
            {
                _logger.Error("Беда с токеном.");
                return;
            }

            await BotService.Start(token);

            // Чтобы докер жил.
            await Task.Run(() => Thread.Sleep(Timeout.Infinite));
        }
    }
}
