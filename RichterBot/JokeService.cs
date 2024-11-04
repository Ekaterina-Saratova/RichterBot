using NLog;
using System.Text.Json;

namespace RichterBot
{
    public static class JokeService
    {
        private static readonly HttpClient _httpClient = new();
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        public static async Task<string?> GetJoke()
        {
            const string jokeApiUrl = "https://geek-jokes.sameerkumar.website/api?format=json";
            var joke = "Что-то пошло не так и это не шутка.";

            try
            {
                var response = await _httpClient.GetAsync(jokeApiUrl);
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(responseBody);
                joke = doc.RootElement.GetProperty("joke").GetString();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Ошибка при обработке запроса к {jokeApiUrl}");
            }
            return joke;
        }
    }
}
