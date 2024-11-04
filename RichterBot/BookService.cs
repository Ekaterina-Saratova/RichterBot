using System.Text.Json;
using File = System.IO.File;

namespace RichterBot
{
    public class BookService
    {
        private const string FileName = "Quotes.json";
        private static List<Quote> _quotes;

        public static string GetRandomQuote()
        {
            var random = new Random();
            var index = random.Next(_quotes.Count);
            var randomItem = _quotes[index];

            return $"И молвил Рихтер как боженька: {randomItem.Text} \nГлава {randomItem.Chapter}, Страница {randomItem.Page}";
        }

        static BookService()
        {
            var jsonString = File.ReadAllText(FileName);
            _quotes = JsonSerializer.Deserialize<List<Quote>>(jsonString)!;
        }
    }

    public struct Quote(int chapter, int page, string text)
    {
        public string Text { get; set; } = text;
        public int Chapter { get; set; } = chapter;
        public int Page { get; set; } = page;
    }
}
