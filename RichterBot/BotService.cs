using NLog;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace RichterBot
{
    public class BotService
    {
        private const string _bibleButton = "bible";
        private const string _jokeButton = "joke";
        private static readonly InlineKeyboardMarkup _inlineKeyboard = new(new List<InlineKeyboardButton[]>
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("Цитата из Библии", _bibleButton),
                InlineKeyboardButton.WithCallbackData("Анекдот", _jokeButton),
                InlineKeyboardButton.WithUrl("Скачать книгу", "https://777russia.ru/book/vfm-admin/vfm-downloader.php?q=dXBsb2Fkcy8lRDAlOUYlRDAlQTAlRDAlOUUlRDAlOTMlRDAlQTAlRDAlOTAlRDAlOUMlRDAlOUMlRDAlOTglRDAlQTAlRDAlOUUlRDAlOTIlRDAlOTAlRDAlOUQlRDAlOTglRDAlOTUvQyUyMy9DTFIlMjB2aWElMjBDJTIzJTIwJTI4Lk5FVCUyMDQuNSUyOSUyQyUyMDR0aCUyMEVkaXRpb24lMjAyMDEyJTIwJTI4JUQwJUJGJUQwJUI1JUQxJTgwJUQwJUI1JUQwJUIyJUQwJUJFJUQwJUI0JTJDJTIwJUQwJUJEJUQwJUIwJTIwJUQxJTgwJUQxJTgzJUQxJTgxJUQxJTgxJUQwJUJBJUQwJUJFJUQwJUJDJTI5LnBkZg==&h=a78169a73ff1cf92d049d7c2f9541c17"),
            }
        });
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public static async Task Start(string token)
        {
            var botClient = new TelegramBotClient(token);
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates =
                [
                    UpdateType.Message,
                    UpdateType.CallbackQuery,
                ],
                ThrowPendingUpdates = false,
            };

            botClient.StartReceiving(UpdateHandler, ErrorHandler, receiverOptions);

            var me = await botClient.GetMeAsync();

            _logger.Info($"{me.FirstName} запущен!");
        }

        private static Task ErrorHandler(ITelegramBotClient botClient, Exception error, CancellationToken cancellationToken)
        {
            var errorMessage = error switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => error.ToString()
            };

            _logger.Error(errorMessage);

            return Task.CompletedTask;
        }

        private static async Task UpdateHandler(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            try
            {
                switch (update.Type)
                {
                    case UpdateType.Message:
                    {
                        await MessageHandler(botClient, update);
                        return;
                    }

                    case UpdateType.CallbackQuery:
                    {
                        await CallbackQueryHandler(botClient, update);
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Ошибка при обработке Update");
            }
        }

        private static async Task MessageHandler(ITelegramBotClient botClient, Update update)
        {
            var message = update.Message;
            if (message?.Text is not { } text) return;

            var chat = message.Chat;

            if (!text.StartsWith('/'))
                return;

            _logger.Trace($"Пришло сообщение от ${update.Message?.From} : {text}");

            if (text.Contains("/help", StringComparison.InvariantCultureIgnoreCase))
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "Чо help, иди книгу читай.", replyToMessageId: message.MessageId);
                return;
            }

            if (text.Contains("/start", StringComparison.InvariantCultureIgnoreCase))
            {
                await botClient.SendTextMessageAsync(
                    chat.Id,
                    "Выбирай",
                    replyMarkup: _inlineKeyboard);
            }
        }

        private static async Task CallbackQueryHandler(ITelegramBotClient botClient, Update update)
        {
            var callbackQuery = update.CallbackQuery;
            if (callbackQuery == null) 
                return;

            var user = callbackQuery.From;
            _logger.Trace($"{user.FirstName} ({user.Id}) нажал на кнопку: {callbackQuery.Data}");
            var chat = callbackQuery.Message?.Chat;
            if (chat == null)
                return;

            switch (callbackQuery.Data)
            {
                case _jokeButton:
                {
                    await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
                    var text = await JokeService.GetJoke() ?? "Колобок повесился.";
                    await botClient.SendTextMessageAsync(chat.Id, text);
                    return;
                }

                case _bibleButton:
                {
                    var quote = BookService.GetRandomQuote();
                    await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
                    await botClient.SendTextMessageAsync(chat.Id, quote);
                    return;
                }
            }
        }
    }
}
