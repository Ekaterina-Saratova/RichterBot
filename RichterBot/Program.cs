using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using NLog;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace RichterBot
{
    internal class Program
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private static string jokeApiUrl = "https://geek-jokes.sameerkumar.website/api?format=json";
        private static readonly HttpClient _httpClient = new();
        private static ITelegramBotClient _botClient;

        private static ReceiverOptions _receiverOptions;

        private static readonly HashSet<string> _pidorSet = new(StringComparer.OrdinalIgnoreCase)
            { "пидор", "пидорас", "пидарас", "пидар", "педик", "пидрила", "педрила", "гей" };

        static async Task Main()
        {
            Console.WriteLine("Hello, World!");
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();
            var token  = configuration["AppSettings:Token"];
            _botClient = new TelegramBotClient(token);
            _receiverOptions = new ReceiverOptions
            {
                AllowedUpdates =
                [
                    UpdateType.Message,
                    UpdateType.CallbackQuery,
                ],
                ThrowPendingUpdates = false,
            };

            using var cts = new CancellationTokenSource();

            //_botClient.StartReceiving(UpdateHandler, ErrorHandler, _receiverOptions, cts.Token);
            _ = Task.Run(() => _botClient.StartReceiving(UpdateHandler, ErrorHandler, _receiverOptions, cts.Token));

            var me = await _botClient.GetMeAsync();
            logger.Info($"{me.FirstName} запущен!");

            //Console.WriteLine("Press Enter to exit...");
            //Console.ReadLine();
            //cts.Cancel();
            await Task.Run(() => Thread.Sleep(Timeout.Infinite));
        }

        private static async Task UpdateHandler(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            try
            {
                switch (update.Type)
                {
                    case UpdateType.Message:
                    {
                        var message = update.Message;
                        var text = message!.Text;
                        var chat = message.Chat;
                        logger.Trace($"Пришло сообщение от ${update.Message?.From} : {text}");

                        if (text != null && text.Contains("Рихтер", StringComparison.InvariantCultureIgnoreCase) && _pidorSet.Any(keyword => text.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0))
                        {
                            await botClient.SendTextMessageAsync(
                                chat.Id,
                                "Сам ты пидор.", // отправляем то, что написал пользователь
                                replyToMessageId: message
                                    .MessageId // по желанию можем поставить этот параметр, отвечающий за "ответ" на сообщение
                            );
                            return;
                        }
                        if (text != null && text.Contains("/help", StringComparison.InvariantCultureIgnoreCase))
                        {
                            await botClient.SendTextMessageAsync(
                                message.Chat.Id,
                                "Чо help, иди книгу читай.", // отправляем то, что написал пользователь
                                replyToMessageId: message
                                    .MessageId // по желанию можем поставить этот параметр, отвечающий за "ответ" на сообщение
                            );
                            return;
                        }
                        if (text != null && text.Contains("/start", StringComparison.InvariantCultureIgnoreCase))
                        {
                            var inlineKeyboard = new InlineKeyboardMarkup(
                                new List<InlineKeyboardButton[]>()
                                {
                                    new InlineKeyboardButton[]
                                    {
                                        InlineKeyboardButton.WithCallbackData("Цитата из Библии", "bible"),
                                        InlineKeyboardButton.WithCallbackData("Анекдот", "joke"),
                                        InlineKeyboardButton.WithUrl("Скачать книгу", "https://777russia.ru/book/vfm-admin/vfm-downloader.php?q=dXBsb2Fkcy8lRDAlOUYlRDAlQTAlRDAlOUUlRDAlOTMlRDAlQTAlRDAlOTAlRDAlOUMlRDAlOUMlRDAlOTglRDAlQTAlRDAlOUUlRDAlOTIlRDAlOTAlRDAlOUQlRDAlOTglRDAlOTUvQyUyMy9DTFIlMjB2aWElMjBDJTIzJTIwJTI4Lk5FVCUyMDQuNSUyOSUyQyUyMDR0aCUyMEVkaXRpb24lMjAyMDEyJTIwJTI4JUQwJUJGJUQwJUI1JUQxJTgwJUQwJUI1JUQwJUIyJUQwJUJFJUQwJUI0JTJDJTIwJUQwJUJEJUQwJUIwJTIwJUQxJTgwJUQxJTgzJUQxJTgxJUQxJTgxJUQwJUJBJUQwJUJFJUQwJUJDJTI5LnBkZg==&h=a78169a73ff1cf92d049d7c2f9541c17"),
                                    },
                                });

                            await botClient.SendTextMessageAsync(
                                chat.Id,
                                "Выбирай",
                                replyMarkup: inlineKeyboard);
                                return;
                        }

                        return;
                    }

                    case UpdateType.CallbackQuery:
                        {
                            var callbackQuery = update.CallbackQuery;
                            var user = callbackQuery.From;
                            logger.Trace($"{user.FirstName} ({user.Id}) нажал на кнопку: {callbackQuery.Data}");
                            var chat = callbackQuery.Message.Chat;

                            switch (callbackQuery.Data)
                            {
                                case "joke":
                                    {
                                        await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);

                                        await botClient.SendTextMessageAsync(
                                            chat.Id,
                                            await GetJoke());
                                        return;
                                    }

                                case "bible":
                                {
                                    var quote = Book.GetRandomQuote();
                                        await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "Внемлите истине");

                                        await botClient.SendTextMessageAsync(
                                            chat.Id,
                                            quote);
                                        return;
                                    }
                            }

                            return;
                        }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Ошибка при оброаботке Update");
            }
        }

        private static Task ErrorHandler(ITelegramBotClient botClient, Exception error, CancellationToken cancellationToken)
        {
            var errorMessage = error switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => error.ToString()
            };

            Console.WriteLine(errorMessage);
            return Task.CompletedTask;
        }

        private static async Task<string> GetJoke()
        {
            string joke = "Что-то пошло не так и это не шутка.";
            try
            {
                // Send a GET request to the API
                var response = await _httpClient.GetAsync(jokeApiUrl);
                response.EnsureSuccessStatusCode(); // Throw if not a success code.

                // Read the response content as a string
                var responseBody = await response.Content.ReadAsStringAsync();

                // Parse the JSON response
                var json = JObject.Parse(responseBody);
                joke = json["joke"].ToString();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Ошибка при обработке запроса");
            }
            return joke;
        }
    }
}
