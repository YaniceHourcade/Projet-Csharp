using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Text.Json;

namespace ApplicationMeteo
{
    public class WeatherAPI
    {
        private readonly string apiKey;
        // Constructeur pour initialiser la clé API
        public WeatherAPI(string apiKey) => this.apiKey = apiKey;
        // Classe pour stocker les paramètres de l'application
        public class AppSettings
        {
            public string DefaultCity { get; set; } = "Paris";
            public string Language { get; set; } = "fr";
        }
        // Méthode pour charger les paramètres depuis un fichier JSON
        public AppSettings LoadSettings(string filePath)
        {
            if (!File.Exists(filePath))
            {
                var defaultSettings = new AppSettings();
                File.WriteAllText(filePath, JsonSerializer.Serialize(defaultSettings, new JsonSerializerOptions { WriteIndented = true }));
                return defaultSettings;
            }
            return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(filePath)) ?? new AppSettings();
        }
        // Méthode pour mettre à jour la ville par défaut dans les paramètres
        public void UpdateDefaultCity(string filePath, string newDefaultCity)
        {
            var settings = LoadSettings(filePath);
            settings.DefaultCity = newDefaultCity;
            File.WriteAllText(filePath, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
        }
        // Méthode pour mettre à jour la langue dans les paramètres
        public void UpdateLangue(string filePath, string newLangue)
        {
            var settings = LoadSettings(filePath);
            settings.Language = newLangue;
            File.WriteAllText(filePath, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
        }
        // Classe pour stocker les informations météo et l'image
        public class WeatherResult
        {
            public string? WeatherVille { get; set; }
            public string? WeatherTemperature { get; set; }
            public string? WeatherInfo { get; set; }
            public string? WeatherDescription { get; set; }
            public byte[] ImageData { get; set; } = Array.Empty<byte>();
        }
        // Classe pour stocker les prévisions météo pour un jour donné
        public class ForecastWeatherResult
        {
            public string? WeatherVille { get; set; }
            public string? WeatherDate { get; set; }
            public string? WeatherTemperature { get; set; }
            public string? WeatherInfo { get; set; }
            public string? WeatherDescription { get; set; }
            public byte[] ImageData { get; set; } = Array.Empty<byte>();
        }
        // Méthode pour obtenir les informations météo actuelles d'une ville
        public async Task<WeatherResult?> GetWeather(string city)
        {
            using var httpClient = new HttpClient();
            var settings = LoadSettings("options.json");
            var url = $"https://api.openweathermap.org/data/2.5/weather?q={city}&appid={apiKey}&units=metric&lang={settings.Language}";

            try
            {
                var response = await httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode) return response.StatusCode == System.Net.HttpStatusCode.NotFound ? null : throw new Exception($"Erreur de l'API : {response.StatusCode}");

                var json = await response.Content.ReadAsStringAsync();
                var data = JObject.Parse(json);
                var iconCode = data["weather"]?[0]?["icon"]?.ToString();
                if (string.IsNullOrEmpty(iconCode)) throw new Exception("Icône météo non trouvée.");
                
                // Récupérer les informations météo nécessaires
                var cityName = data["name"]?.ToString();
                var latitude = data["coord"]?["lat"]?.ToString();
                var longitude = data["coord"]?["lon"]?.ToString();
                var temp = data["main"]?["temp"]?.ToString();
                var description = data["weather"]?[0]?["description"]?.ToString();
                var humidity = data["main"]?["humidity"]?.ToString();
                var imageUrl = $"http://openweathermap.org/img/wn/{iconCode}@2x.png";
                var imageResponse = await httpClient.GetAsync(imageUrl);

                if (string.IsNullOrEmpty(cityName) || string.IsNullOrEmpty(latitude) || string.IsNullOrEmpty(longitude) || string.IsNullOrEmpty(temp) || string.IsNullOrEmpty(description) || string.IsNullOrEmpty(humidity)) return null;

                byte[] imageData = Array.Empty<byte>();
                if (imageResponse.IsSuccessStatusCode)
                    imageData = await imageResponse.Content.ReadAsByteArrayAsync();

                return new WeatherResult
                {
                    WeatherVille = cityName,
                    WeatherInfo = $"Latitude : {latitude}\nLongitude : {longitude}\nHumidité : {humidity}%",
                    WeatherTemperature = $"{temp}°C\n",
                    WeatherDescription = $"Description : {description}\n",
                    ImageData = imageData
                };
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Erreur lors de la requête : {ex.Message}");
                return null;
            }
        }
        // Méthode pour obtenir les prévisions météo pour un jour donné
        public async Task<ForecastWeatherResult?> GetForecastWeather(string city, int day)
        {
            using var httpClient = new HttpClient();
            var settings = LoadSettings("options.json");
            var url = $"https://api.openweathermap.org/data/2.5/forecast?q={city}&appid={apiKey}&units=metric&lang={settings.Language}";

            try
            {
                var response = await httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode) return null;

                var json = await response.Content.ReadAsStringAsync();
                var data = JObject.Parse(json);
                var forecastList = data["list"]?.ToObject<JArray>();

                if (forecastList == null || forecastList.Count <= day) return null;

                var tomorrow = DateTime.UtcNow.AddDays(day).Date;
                JToken? targetForecast = null;
                foreach (var forecast in forecastList)
                {
                    var dateText = forecast["dt_txt"]?.ToString();
                    if (!string.IsNullOrEmpty(dateText) && DateTime.TryParse(dateText, out var datee) && datee.Date == tomorrow && datee.Hour == 12)
                    {
                        targetForecast = forecast;
                        break;
                    }
                }

                if (targetForecast == null) return null;

                var iconCode = targetForecast["weather"]?[0]?["icon"]?.ToString();
                if (string.IsNullOrEmpty(iconCode)) return null;
                
                // Récupérer les informations nécessaires pour la prévision
                var cityName = data["city"]?["name"]?.ToString();
                var date = targetForecast["dt_txt"]?.ToString();
                var latitude = data["city"]?["coord"]?["lat"]?.ToString();
                var longitude = data["city"]?["coord"]?["lon"]?.ToString();
                var temp = targetForecast["main"]?["temp"]?.ToString();
                var description = targetForecast["weather"]?[0]?["description"]?.ToString();
                var humidity = targetForecast["main"]?["humidity"]?.ToString();
                var imageUrl = $"http://openweathermap.org/img/wn/{iconCode}.png";

                byte[] imageData = Array.Empty<byte>();
                var imageResponse = await httpClient.GetAsync(imageUrl);
                if (imageResponse.IsSuccessStatusCode)
                    imageData = await imageResponse.Content.ReadAsByteArrayAsync();

                if (string.IsNullOrEmpty(cityName) || string.IsNullOrEmpty(date) || string.IsNullOrEmpty(temp) || string.IsNullOrEmpty(description)) return null;

                return new ForecastWeatherResult
                {
                    WeatherVille = cityName,
                    WeatherDate = date,
                    WeatherInfo = $"Latitude : {latitude}\nLongitude : {longitude}\nHumidité : {humidity}%",
                    WeatherTemperature = $"{temp}°C",
                    WeatherDescription = $"Description : {description}",
                    ImageData = imageData
                };
            }
            catch
            {
                return null;
            }
        }
    }
}