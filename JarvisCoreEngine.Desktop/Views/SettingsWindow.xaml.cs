using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using JarvisCoreEngine.Interfaces;
using JarvisCoreEngine.Models;
using JarvisCoreEngine.Services;

namespace JarvisCoreEngine.Desktop.Views
{
    public partial class SettingsWindow : Window
    {
        private readonly AiSettings _aiSettings;
        private readonly AiService _aiService;
        private readonly IAppLogger _logger;

        private readonly Desktop.Services.VoiceService _voiceService;

        public SettingsWindow(AiSettings aiSettings, AiService aiService,
                              Desktop.Services.VoiceService voiceService, IAppLogger logger)
        {
            InitializeComponent();

            _aiSettings = aiSettings;
            _aiService = aiService;
            _voiceService = voiceService;
            _logger = logger;

            LoadCurrentSettings();
        }

        // ===== LOAD SETTINGS INTO UI =====
        private void LoadCurrentSettings()
        {
            try
            {
                // AI Settings
                ApiKeyBox.Text = _aiSettings.ApiKey;

                foreach (ComboBoxItem item in ModelCombo.Items)
                {
                    if (item.Content.ToString() == _aiSettings.Model)
                    {
                        ModelCombo.SelectedItem = item;
                        break;
                    }
                }

                PromptBox.Text = _aiSettings.SystemPrompt;

                // Voice (default Male)
                VoiceCombo.SelectedIndex = 0;  // Male

                // Sliders (default values, you can load saved later)
                RateSlider.Value = 0;
                VolumeSlider.Value = 100;
                RateValue.Text = "0";
                VolumeValue.Text = "100";

                _logger.Log("⚙ Settings loaded into UI.");
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to load settings: {ex.Message}");
            }
        }

        // ===== TITLE BAR DRAG =====
        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        // ===== CLOSE =====
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // ===== SLIDERS =====
        private void RateSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (RateValue != null)
                RateValue.Text = ((int)e.NewValue).ToString();
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (VolumeValue != null)
                VolumeValue.Text = ((int)e.NewValue).ToString();
        }

        // ===== MEMORY ACTIONS =====
        private void ResetMemory_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Reset current conversation? AETHER will start fresh.",
                "Reset Memory",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _aiService.ResetConversation();
                MessageBox.Show("Conversation reset.", "Done", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ForgetMemory_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "⚠ WARNING: This will permanently delete ALL AETHER memory.\n\nAre you sure?",
                "Forget Everything",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                _aiService.ForgetEverything();
                MessageBox.Show("All memory wiped.", "Done", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // ===== SAVE =====
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // ===== GATHER VALUES =====
                string newApiKey = ApiKeyBox.Text.Trim();
                string newModel = (ModelCombo.SelectedItem as ComboBoxItem)?.Content.ToString() ?? _aiSettings.Model;
                string newPrompt = PromptBox.Text.Trim();
                string voiceGender = (VoiceCombo.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Male";
                int speechRate = (int)RateSlider.Value;
                int volume = (int)VolumeSlider.Value;

                // ===== VALIDATE =====
                if (string.IsNullOrWhiteSpace(newApiKey))
                {
                    MessageBox.Show("API Key cannot be empty.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(newPrompt))
                {
                    MessageBox.Show("Personality cannot be empty.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // ===== APPLY LIVE (immediate effect) =====

                // AI Settings (live update)
                _aiSettings.ApiKey = newApiKey;
                _aiSettings.Model = newModel;
                _aiSettings.SystemPrompt = newPrompt;

                // Voice Settings (live update)
                _voiceService.SetVoiceGender(voiceGender);
                _voiceService.SetSpeechRate(speechRate);
                _voiceService.SetVolume(volume);

                // ===== PERSIST TO JSON =====
                SaveToJson(newApiKey, newModel, newPrompt);

                _logger.Log("⚙ Settings saved + applied LIVE.");

                // ===== TEST VOICE =====
                _voiceService.Speak("Settings applied successfully, Commander.");

                MessageBox.Show(
                    "✅ Settings saved and applied successfully!\n\nAETHER will use new settings immediately.",
                    "Saved",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                this.Close();
            }
            catch (Exception ex)
            {
                _logger.Error($"Save failed: {ex.Message}");
                MessageBox.Show($"Save failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ===== SAVE TO JSON FILE =====
        private void SaveToJson(string apiKey, string model, string prompt)
        {
            string jsonPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");

            if (!File.Exists(jsonPath))
            {
                throw new FileNotFoundException("appsettings.json not found.");
            }

            string jsonText = File.ReadAllText(jsonPath);
            var node = JsonNode.Parse(jsonText) ?? throw new Exception("Invalid JSON.");

            // Update AiSettings section
            if (node["AiSettings"] != null)
            {
                node["AiSettings"]!["ApiKey"] = apiKey;
                node["AiSettings"]!["Model"] = model;
                node["AiSettings"]!["SystemPrompt"] = prompt;
            }

            string updated = node.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(jsonPath, updated);
        }
    }
}