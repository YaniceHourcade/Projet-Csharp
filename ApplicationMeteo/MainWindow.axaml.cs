using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System.Net.Http;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using System.Collections.Generic;

namespace ApplicationMeteo;

public partial class MainWindow : Window
{
    private WeatherAPI _weatherAPI; // Instance de l'API météo

    // Constructeur de la fenêtre principale
    public MainWindow()
    {
        InitializeComponent();
        var apiKey = Config.LoadApiKey("config.json");
        _weatherAPI = new WeatherAPI(apiKey);
        var settings = _weatherAPI.LoadSettings("options.json");
        DefaultCitySave.Text = "Ville par défaut : " + settings.DefaultCity;
        DefaultLangue.Text = "Langue enregistrée : " + settings.Language;
        CityForecast.Text = "Prévisions pour la ville de " + settings.DefaultCity;
        ShowWeatherForecast(settings.DefaultCity);

        // Événements de clic sur les boutons
        SearchButton.Click += async (sender, e) => await OnSearchButtonClick();
        SaveButton.Click += async (sender, e) => await OnSaveButtonClick();
        ButtonFR.Click += async (sender, e) => await OnFrenchButtonClick();
        ButtonEN.Click += async (sender, e) => await OnEnglishButtonClick();
        ButtonES.Click += async (sender, e) => await OnSpanishButtonClick();
    }
    // Classe pour gérer le fichier de configuration contenant la clé API
    public class Config
    {
        public string? ApiKey { get; set; }
        // Méthode pour charger la clé API depuis un fichier JSON
        public static string LoadApiKey(string filePath)
        {
            try
            {
                var json = File.ReadAllText(filePath);
                var config = JsonSerializer.Deserialize<Config>(json);
                if (string.IsNullOrWhiteSpace(config?.ApiKey))
                    throw new Exception("La clé API est manquante ou vide.");
                return config.ApiKey;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur : {ex.Message}");
                Environment.Exit(1);
                return null;
            }
        }
    }
    // Méthode pour gérer le clic sur le bouton de recherche de météo
    private async Task OnSearchButtonClick()
    {
        try
        {
            var city = CityTextBox.Text?.Trim();
            if (string.IsNullOrEmpty(city))
            {
                ResetWeatherUI();
                return;
            }

            var weatherResult = await _weatherAPI.GetWeather(city);
            if (weatherResult != null)
            {
                UpdateWeatherUI(weatherResult);
            }
            else
            {
                ResetWeatherUI();
            }
        }
        catch (HttpRequestException)
        {
            WeatherInfoTextBlockVille.Text = "Erreur de connexion à l'API météo. Vérifiez votre connexion.";
            ResetWeatherUI();
        }
        catch (Exception ex)
        {
            WeatherInfoTextBlockVille.Text = $"Erreur : {ex.Message}";
            ResetWeatherUI();
        }
    }
    // Méthode pour réinitialiser l'interface météo (textes et images)
    private void ResetWeatherUI()
    {
        WeatherInfoTextBlockVille.Text = "Ville introuvable.";
        WeatherInfoTextBlockTemperature.Text = string.Empty;
        WeatherInfoTextInfo.Text = string.Empty;
        WeatherInfoDescription.Text = string.Empty;
        WeatherImage.Source = null;
    }
    // Méthode pour mettre à jour l'interface avec les données météo
    private void UpdateWeatherUI(WeatherAPI.WeatherResult weatherResult)
    {
        WeatherInfoTextBlockVille.Text = "Ville : " + weatherResult.WeatherVille;
        WeatherInfoTextBlockTemperature.Text = weatherResult.WeatherTemperature;
        WeatherInfoTextInfo.Text = weatherResult.WeatherInfo;
        WeatherInfoDescription.Text = weatherResult.WeatherDescription;

        if (weatherResult.ImageData.Length > 0)
            WeatherImage.Source = new Bitmap(new MemoryStream(weatherResult.ImageData));
        else
            WeatherImage.Source = null;
    }
    // Méthode pour afficher les prévisions météo sur 5 jours
    private async Task ShowWeatherForecast(string city)
    {
        try
        {
            var forecastResults = new List<WeatherAPI.ForecastWeatherResult>();
            for (int i = 1; i <= 5; i++)
            {
                var result = await _weatherAPI.GetForecastWeather(city, i);
                if (result == null)
                {
                    ClearForecastUI();
                    return;
                }
                forecastResults.Add(result);
            }

            UpdateForecastUI(forecastResults);
        }
        catch (HttpRequestException)
        {
            ClearForecastUI();
        }
        catch (Exception)
        {
            ClearForecastUI();
        }
    }
    // Méthode pour mettre à jour l'interface avec les prévisions météo
    private void UpdateForecastUI(List<WeatherAPI.ForecastWeatherResult> forecastResults)
    {
        var dateControls = new[] { ForecastDate1, ForecastDate2, ForecastDate3, ForecastDate4, ForecastDate5 };
        var tempControls = new[] { ForecastTemperature1, ForecastTemperature2, ForecastTemperature3, ForecastTemperature4, ForecastTemperature5 };
        var infoControls = new[] { ForecastTextInfo1, ForecastTextInfo2, ForecastTextInfo3, ForecastTextInfo4, ForecastTextInfo5 };
        var descControls = new[] { ForecastDescription1, ForecastDescription2, ForecastDescription3, ForecastDescription4, ForecastDescription5 };
        var imageControls = new[] { ForecastImage1, ForecastImage2, ForecastImage3, ForecastImage4, ForecastImage5 };

        for (int i = 0; i < forecastResults.Count; i++)
        {
            var forecast = forecastResults[i];
            dateControls[i].Text = forecast.WeatherDate;
            tempControls[i].Text = forecast.WeatherTemperature;
            infoControls[i].Text = forecast.WeatherInfo;
            descControls[i].Text = forecast.WeatherDescription;

            imageControls[i].Source = forecast.ImageData.Length > 0 
                ? new Bitmap(new MemoryStream(forecast.ImageData)) 
                : null;
        }
    }
    // Méthode pour effacer les prévisions affichées
    private void ClearForecastUI()
    {
        var textControls = new[] { ForecastDate1, ForecastTemperature1, ForecastTextInfo1, ForecastDescription1,
                                ForecastDate2, ForecastTemperature2, ForecastTextInfo2, ForecastDescription2,
                                ForecastDate3, ForecastTemperature3, ForecastTextInfo3, ForecastDescription3,
                                ForecastDate4, ForecastTemperature4, ForecastTextInfo4, ForecastDescription4,
                                ForecastDate5, ForecastTemperature5, ForecastTextInfo5, ForecastDescription5 };

        var imageControls = new[] { ForecastImage1, ForecastImage2, ForecastImage3, ForecastImage4, ForecastImage5 };

        foreach (var control in textControls)
        {
            control.Text = string.Empty;
        }

        foreach (var imageControl in imageControls)
        {
            imageControl.Source = null;
        }
    }
    // Méthode pour gérer le clic sur le bouton de sauvegarde de la ville par défaut
    private async Task OnSaveButtonClick()
    {
        try
        {
            var defaultCity = DefaultCity.Text?.Trim();
            if (string.IsNullOrEmpty(defaultCity))
            {
                DefaultCitySave.Text = "Veuillez entrer une ville valide.";
                return;
            }

            var defaultCityResult = await _weatherAPI.GetWeather(defaultCity);
            if (defaultCityResult != null)
            {
                DefaultCitySave.Text = "Ville par défaut : " + defaultCityResult.WeatherVille;
                CityForecast.Text = "Ville : " + defaultCityResult.WeatherVille;
                await ShowWeatherForecast(defaultCity);
                _weatherAPI.UpdateDefaultCity("options.json", defaultCity);
            }
            else
            {
                DefaultCitySave.Text = "Ville introuvable, vérifier le nom.";
            }
        }
        catch (HttpRequestException)
        {
            DefaultCitySave.Text = "Impossible de se connecter à l'API. Vérifiez votre connexion.";
        }
        catch (Exception ex)
        {
            DefaultCitySave.Text = $"Erreur : {ex.Message}";
        }
    }
    // Méthode pour gérer les changements de langue
    private async Task OnLanguageButtonClick(string languageCode, string languageName)
    {
        DefaultLangue.Text = "Langue enregistrée : " + languageName;
        await Task.Run(() => _weatherAPI.UpdateLangue("options.json", languageCode));
    }
    // Méthodes pour chaque bouton de langue
    private Task OnFrenchButtonClick() => OnLanguageButtonClick("fr", "Français");
    private Task OnEnglishButtonClick() => OnLanguageButtonClick("en", "Anglais");
    private Task OnSpanishButtonClick() => OnLanguageButtonClick("es", "Espagnol");
}
