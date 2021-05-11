using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Text;

// Import namespaces
// Add dependencies for System.Windows.Extensions, Microsoft.Cognitiveservices.speech, Microsoft.Extensions.Configuration.Json

using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Translation;
using System.Media;


namespace speech_translation
{
    class Program
    {
        private static SpeechConfig speechConfig;
        private static SpeechTranslationConfig translationConfig;

        static async Task Main(string[] args)
        {
            try
            {
                // Get config settings from AppSettings
                IConfigurationBuilder builder = new ConfigurationBuilder().AddJsonFile("appsettings.json");
                IConfigurationRoot configuration = builder.Build();
                string cogSvcKey = configuration["CognitiveServiceKey"];
                string cogSvcRegion = configuration["CognitiveServiceRegion"];


                // Set a dictionary of supported voices
                var voices = new Dictionary<string, string>
                {
                    ["fr"] = "fr-FR-Julie",
                    ["es"] = "es-ES-Laura",
                    ["hi"] = "hi-IN-Kalpana"
                };

                // Configure translation
                translationConfig = SpeechTranslationConfig.FromSubscription(cogSvcKey, cogSvcRegion);
                translationConfig.SpeechRecognitionLanguage = "en-US";
                Console.WriteLine("Ready to translate from " + translationConfig.SpeechRecognitionLanguage);


                string targetLanguage = "";
                while (targetLanguage != "quit")
                {
                    Console.WriteLine("\nEnter a target language\n fr = French\n es = Spanish\n hi = Hindi\n Enter anything else to stop\n");
                    targetLanguage = Console.ReadLine().ToLower();
                    // Check if the user has requested a language that this app supports
                    if (voices.ContainsKey(targetLanguage))
                    {
                        // Because the synthesised speech event only supports 1:1 translation, we'll remove any languages already in the translationconfig
                        if (translationConfig.TargetLanguages.Count > 1)
                        {
                            foreach (string language in translationConfig.TargetLanguages)
                            {
                                translationConfig.RemoveTargetLanguage(language);
                            }
                        }
                        
                        // and add the requested one in
                        translationConfig.AddTargetLanguage(targetLanguage);
                        translationConfig.VoiceName = voices[targetLanguage];
                        await Translate(targetLanguage);
                    }
                    else
                    {
                        targetLanguage = "quit";
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }

        }
        static async Task Translate(string targetLanguage)
        {
            string translation = "";

            // Translate speech
            string audioFile = "station.wav";
            SoundPlayer wavPlayer = new SoundPlayer(audioFile);
            // Play the audio synchronously, otherwise the translation will interrupt it
            wavPlayer.PlaySync();
            using AudioConfig audioConfig = AudioConfig.FromWavFileInput(audioFile);
            using TranslationRecognizer translator = new TranslationRecognizer(translationConfig, audioConfig);
            
            // Add an inline event handler to handle the Synthesizing event and output the audio to the current output device
            translator.Synthesizing += (_, e) =>
            {
                var audio = e.Result.GetAudio();
                if (audio.Length > 0)
                {
                    // Output to a file using File.WriteAllBytes("YourAudioFile.wav", audio);
                    // Or place the data into a stream and play to the current output device
                    using (System.IO.MemoryStream ms = new System.IO.MemoryStream(audio))
                    {
                        // Construct the sound player
                        SoundPlayer player = new SoundPlayer(ms);
                        player.Play();
                    }
                }
            };
            Console.WriteLine("Getting speech from file...");
            TranslationRecognitionResult result = await translator.RecognizeOnceAsync();
            Console.WriteLine($"Translating '{result.Text}'");
            translation = result.Translations[targetLanguage];
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine(translation);


        }
    }
}