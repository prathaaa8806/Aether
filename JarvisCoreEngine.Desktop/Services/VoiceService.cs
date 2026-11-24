using System;
using System.Globalization;
using System.IO;
using System.Speech.Recognition;
using System.Speech.Synthesis;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using JarvisCoreEngine.Interfaces;

namespace JarvisCoreEngine.Desktop.Services
{
    public class VoiceService
    {
        // ===== FIELDS =====
        private readonly IAppLogger _logger;
        private readonly TransliterationService _transliterator;
        private readonly EdgeTtsService _edgeTts;
        private readonly SpeechSynthesizer _speaker;
        private readonly MediaPlayer _mediaPlayer;
        private readonly DispatcherTimer _volumeTimer;
        private readonly string _tempAudioPath;
        private SpeechRecognitionEngine? _recognizer;
        private bool _isHindiVoice = false;
        private string _edgeVoice = EdgeTtsService.VOICE_JARVIS_BRITISH;
        private int _speechRate = 0;
        private int _volume = 100;
        private bool _isPlayingMedia = false;

        // ===== EVENTS =====
        public event Action? OnListeningStarted;
        public event Action? OnListeningStopped;
        public event Action<string>? OnSpeechRecognized;
        public event Action<string>? OnRecognitionError;

        public event Action? OnSpeakingStarted;
        public event Action? OnSpeakingStopped;
        public event Action<int>? OnVolumeLevel;

        // ===== STATE =====
        public bool IsListening { get; private set; }
        public bool IsSpeaking { get; private set; }
        public bool ContinuousMode { get; private set; }

        // ===== CONSTRUCTOR =====
        public VoiceService(IAppLogger logger, TransliterationService transliterator, EdgeTtsService edgeTts)
        {
            _logger = logger;
            _transliterator = transliterator;
            _edgeTts = edgeTts;
            _tempAudioPath = Path.Combine(Path.GetTempPath(), "jarvis_voice_output.mp3");

            _speaker = new SpeechSynthesizer();
            _speaker.SetOutputToDefaultAudioDevice();
            _speaker.Rate = 0;
            _speaker.Volume = 100;

            _mediaPlayer = new MediaPlayer();
            _mediaPlayer.MediaEnded += (s, e) => HandlePlaybackFinished();
            _mediaPlayer.MediaFailed += (s, e) =>
            {
                _logger.Warn($"Media playback failed: {e.ErrorException.Message}");
                HandlePlaybackFinished();
            };

            _volumeTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            var random = new Random();
            _volumeTimer.Tick += (s, e) =>
            {
                if (IsSpeaking)
                {
                    OnVolumeLevel?.Invoke(random.Next(40, 100));
                }
            };

            TrySelectPreferredVoice();

            _speaker.SpeakStarted += (s, e) => HandlePlaybackStarted();
            _speaker.SpeakCompleted += (s, e) => HandlePlaybackFinished();
            _speaker.SpeakProgress += (s, e) =>
            {
                OnVolumeLevel?.Invoke(random.Next(40, 100));
            };

            _logger.Log("🎙 Voice service initialized (Neural Edge-TTS + SAPI Fallback ready).");
        }

        private void HandlePlaybackStarted()
        {
            IsSpeaking = true;
            OnSpeakingStarted?.Invoke();

            if (ContinuousMode && IsListening)
            {
                _logger.Log("⏸ Pausing mic — AETHER speaking.");
                DisposeRecognizer();
                IsListening = false;
            }
        }

        private void HandlePlaybackFinished()
        {
            _isPlayingMedia = false;
            _volumeTimer.Stop();
            IsSpeaking = false;
            OnSpeakingStopped?.Invoke();

            if (ContinuousMode && !IsListening)
            {
                _logger.Log("▶ Resuming mic — AETHER finished.");
                StartRecognizerInternal();
            }
        }

        // ===== SELECT DEFAULT VOICE =====
        private void TrySelectPreferredVoice()
        {
            try
            {
                _logger.Log("Available voices on system:");
                foreach (var v in _speaker.GetInstalledVoices())
                {
                    var i = v.VoiceInfo;
                    _logger.Log($"  • {i.Name} | Gender: {i.Gender} | Lang: {i.Culture.Name}");
                }

                foreach (var voice in _speaker.GetInstalledVoices())
                {
                    var info = voice.VoiceInfo;
                    if (info.Gender == VoiceGender.Male && info.Culture.Name.StartsWith("en"))
                    {
                        _speaker.SelectVoice(info.Name);
                        _logger.Log($"🎵 Voice selected (Male English): {info.Name}");
                        return;
                    }
                }

                foreach (var voice in _speaker.GetInstalledVoices())
                {
                    var info = voice.VoiceInfo;
                    if (info.Gender == VoiceGender.Male)
                    {
                        _speaker.SelectVoice(info.Name);
                        _logger.Log($"🎵 Voice selected (Male): {info.Name}");
                        return;
                    }
                }

                _logger.Warn("⚠ No male voice installed.");
            }
            catch (Exception ex)
            {
                _logger.Error($"Voice selection failed: {ex.Message}");
            }
        }

        // ===== SET VOICE GENDER =====
        public void SetVoiceGender(string gender)
        {
            try
            {
                string val = (gender ?? "").ToLower();
                VoiceGender targetGender;
                string targetCulture = "en";

                if (val.Contains("hindi") && val.Contains("female"))
                {
                    targetGender = VoiceGender.Female;
                    targetCulture = "hi";
                    _isHindiVoice = true;
                    _edgeVoice = "hi-IN-SwaraNeural";
                }
                else if (val.Contains("hindi"))
                {
                    targetGender = VoiceGender.Male;
                    targetCulture = "hi";
                    _isHindiVoice = true;
                    _edgeVoice = EdgeTtsService.VOICE_JARVIS_HINDI;
                }
                else if (val.Contains("american"))
                {
                    targetGender = VoiceGender.Male;
                    targetCulture = "en";
                    _isHindiVoice = false;
                    _edgeVoice = EdgeTtsService.VOICE_JARVIS_AMERICAN;
                }
                else if (val.Contains("female"))
                {
                    targetGender = VoiceGender.Female;
                    targetCulture = "en";
                    _isHindiVoice = false;
                    _edgeVoice = EdgeTtsService.VOICE_JARVIS_FEMALE;
                }
                else
                {
                    targetGender = VoiceGender.Male;
                    targetCulture = "en";
                    _isHindiVoice = false;
                    _edgeVoice = EdgeTtsService.VOICE_JARVIS_BRITISH;
                }

                _logger.Log($"🎵 Voice selected: {gender} (Neural Edge: {_edgeVoice})");

                foreach (var voice in _speaker.GetInstalledVoices())
                {
                    var info = voice.VoiceInfo;
                    if (info.Gender == targetGender && info.Culture.Name.StartsWith(targetCulture))
                    {
                        _speaker.SelectVoice(info.Name);
                        _logger.Log($"🎵 SAPI fallback voice selected: {info.Name}");
                        return;
                    }
                }

                foreach (var voice in _speaker.GetInstalledVoices())
                {
                    var info = voice.VoiceInfo;
                    if (info.Gender == targetGender)
                    {
                        _speaker.SelectVoice(info.Name);
                        _logger.Log($"🎵 SAPI fallback voice: {info.Name}");
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Voice change failed: {ex.Message}");
            }
        }

        // ===== SET RATE =====
        public void SetSpeechRate(int rate)
        {
            try
            {
                _speechRate = Math.Max(-10, Math.Min(10, rate));
                _speaker.Rate = _speechRate;
                _logger.Log($"🎵 Speech rate set to: {_speechRate}");
            }
            catch (Exception ex)
            {
                _logger.Error($"Set rate failed: {ex.Message}");
            }
        }

        // ===== SET VOLUME =====
        public void SetVolume(int volume)
        {
            try
            {
                _volume = Math.Max(0, Math.Min(100, volume));
                _speaker.Volume = _volume;
                if (_mediaPlayer != null)
                {
                    Application.Current?.Dispatcher?.Invoke(() => _mediaPlayer.Volume = _volume / 100.0);
                }
                _logger.Log($"🎵 Volume set to: {_volume}");
            }
            catch (Exception ex)
            {
                _logger.Error($"Set volume failed: {ex.Message}");
            }
        }

        // ===== SPEAK (Neural Edge-TTS with SAPI 5 fallback) =====
        public async void Speak(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            try
            {
                StopSpeaking();

                string finalText = text;

                if (_isHindiVoice && _transliterator != null && ContainsRomanLetters(text))
                {
                    _logger.Log("🔄 Transliterating to Devanagari for Hindi voice...");
                    finalText = await _transliterator.RomanToDevanagariAsync(text);
                    _logger.Log($"🔄 Spoken text: {finalText}");
                }

                // 1. Try Neural Edge-TTS (Natural, studio quality)
                int ratePercent = _speechRate * 5; // -10..+10 to -50%..+50%
                var audioBytes = await _edgeTts.SynthesizeToBytesAsync(finalText, _edgeVoice, ratePercent, 0);

                if (audioBytes != null && audioBytes.Length > 0)
                {
                    try
                    {
                        await File.WriteAllBytesAsync(_tempAudioPath, audioBytes);

                        Application.Current?.Dispatcher?.Invoke(() =>
                        {
                            HandlePlaybackStarted();
                            _isPlayingMedia = true;
                            _volumeTimer.Start();

                            _mediaPlayer.Open(new Uri(_tempAudioPath));
                            _mediaPlayer.Volume = _volume / 100.0;
                            _mediaPlayer.Play();
                        });
                        return;
                    }
                    catch (Exception ex)
                    {
                        _logger.Warn($"Local MP3 playback error: {ex.Message}. Falling back to SAPI.");
                    }
                }

                // 2. Offline / SAPI 5 fallback
                _logger.Log("Using local SAPI voice.");
                _speaker.SpeakAsyncCancelAll();
                _speaker.SpeakAsync(finalText);
            }
            catch (Exception ex)
            {
                _logger.Error($"TTS failed: {ex.Message}");
            }
        }

        private bool ContainsRomanLetters(string text)
        {
            foreach (var c in text)
            {
                if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z'))
                    return true;
            }
            return false;
        }

        public void StopSpeaking()
        {
            try
            {
                if (_isPlayingMedia)
                {
                    Application.Current?.Dispatcher?.Invoke(() =>
                    {
                        _mediaPlayer.Stop();
                        _mediaPlayer.Close();
                    });
                }
                _speaker.SpeakAsyncCancelAll();
                HandlePlaybackFinished();
            }
            catch (Exception ex)
            {
                _logger.Error($"Stop speaking failed: {ex.Message}");
            }
        }

        // ===== START LISTENING =====
        public void StartListening()
        {
            if (ContinuousMode) return;

            ContinuousMode = true;
            StartRecognizerInternal();
            OnListeningStarted?.Invoke();
            _logger.Log("🎤 Continuous mode ON.");
        }

        // ===== STOP LISTENING =====
        public void StopListening()
        {
            ContinuousMode = false;

            DisposeRecognizer();
            IsListening = false;

            OnListeningStopped?.Invoke();
            _logger.Log("🛑 Continuous mode OFF.");
        }

        // ===== INTERNAL START =====
        private void StartRecognizerInternal()
        {
            try
            {
                DisposeRecognizer();

                CultureInfo culture;
                try
                {
                    culture = new CultureInfo("en-US");
                    _recognizer = new SpeechRecognitionEngine(culture);
                }
                catch
                {
                    _logger.Warn("en-US not installed. Using default.");
                    _recognizer = new SpeechRecognitionEngine();
                }

                _recognizer.LoadGrammar(new DictationGrammar());
                _recognizer.SetInputToDefaultAudioDevice();

                _recognizer.InitialSilenceTimeout = TimeSpan.Zero;
                _recognizer.BabbleTimeout = TimeSpan.Zero;
                _recognizer.EndSilenceTimeout = TimeSpan.FromSeconds(1);
                _recognizer.EndSilenceTimeoutAmbiguous = TimeSpan.FromSeconds(1.5);

                _recognizer.SpeechRecognized += OnSpeechRecognizedHandler;
                _recognizer.SpeechRecognitionRejected += OnSpeechRejectedHandler;
                _recognizer.AudioLevelUpdated += OnAudioLevelHandler;
                _recognizer.RecognizeCompleted += OnRecognizeCompletedHandler;

                _recognizer.RecognizeAsync(RecognizeMode.Multiple);
                IsListening = true;

                _logger.Log("✅ Recognizer active.");
            }
            catch (Exception ex)
            {
                _logger.Error($"Recognizer start failed: {ex.Message}");
                OnRecognitionError?.Invoke($"Mic error: {ex.Message}");
                IsListening = false;
            }
        }

        private void DisposeRecognizer()
        {
            try
            {
                if (_recognizer != null)
                {
                    _recognizer.SpeechRecognized -= OnSpeechRecognizedHandler;
                    _recognizer.SpeechRecognitionRejected -= OnSpeechRejectedHandler;
                    _recognizer.AudioLevelUpdated -= OnAudioLevelHandler;
                    _recognizer.RecognizeCompleted -= OnRecognizeCompletedHandler;

                    try { _recognizer.RecognizeAsyncStop(); } catch { }
                    _recognizer.Dispose();
                    _recognizer = null;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Dispose recognizer failed: {ex.Message}");
            }
        }

        // ===== HANDLERS =====
        private void OnSpeechRecognizedHandler(object? sender, SpeechRecognizedEventArgs e)
        {
            string text = e.Result.Text;
            float confidence = e.Result.Confidence;

            _logger.Log($"🎤 Heard: '{text}' (confidence: {confidence:P0})");

            if (IsSpeaking)
            {
                _logger.Log("Ignored — AETHER is speaking.");
                return;
            }

            if (!string.IsNullOrWhiteSpace(text) && confidence > 0.3f)
            {
                OnSpeechRecognized?.Invoke(text);
            }
        }

        private void OnSpeechRejectedHandler(object? sender, SpeechRecognitionRejectedEventArgs e)
        {
            _logger.Warn("Speech rejected (low confidence).");
        }

        private void OnAudioLevelHandler(object? sender, AudioLevelUpdatedEventArgs e)
        {
            OnVolumeLevel?.Invoke(e.AudioLevel);
        }

        private void OnRecognizeCompletedHandler(object? sender, RecognizeCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                _logger.Error($"Recognition error: {e.Error.Message}");
            }

            if (ContinuousMode && !IsSpeaking && !e.Cancelled)
            {
                _logger.Log("🔁 Auto-restarting recognizer (continuous mode).");
                System.Threading.Tasks.Task.Run(() =>
                {
                    System.Threading.Thread.Sleep(300);
                    if (ContinuousMode && !IsSpeaking)
                    {
                        StartRecognizerInternal();
                    }
                });
            }
        }
    }
}