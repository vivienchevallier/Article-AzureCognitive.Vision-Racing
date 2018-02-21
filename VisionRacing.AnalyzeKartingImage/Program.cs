using Microsoft.ProjectOxford.Vision;
using Microsoft.ProjectOxford.Vision.Contract;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace VisionRacing.AnalyzeKartingImage
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var apiKey = "Your Cognitive Services Vision API Key.";
                var apiUrl = "Cognitive Services Vision API URL.";

                var kartingImageUrl = "https://github.com/vivienchevallier/Article-AzureCognitive.Vision-Racing/raw/master/Images/Karting/Karting%20(9).jpg";

                AnalyzeImage(apiKey, apiUrl, kartingImageUrl).Wait();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Console.WriteLine();
            }

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine();
            Console.WriteLine("Press any key to exit...");
            Console.ReadLine();
        }

        private static async Task AnalyzeImage(string apiKey, string apiUrl, string imageUrl)
        {
            var vsc = new VisionServiceClient(apiKey, apiUrl);

            var visualFeatures = new VisualFeature[]
            {
                VisualFeature.Description, VisualFeature.Tags
            };

            var analysisResult = await vsc.AnalyzeImageAsync(imageUrl, visualFeatures);

            ShowAnalysisResult(analysisResult);
        }

        private static void ShowAnalysisResult(AnalysisResult result)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Log("Image analysis result");
            Console.WriteLine();

            if (result.Description != null)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Log("1. Image description");

                Console.ForegroundColor = ConsoleColor.Gray;

                if (result.Description.Captions.Any())
                {
                    foreach (var caption in result.Description.Captions)
                    {
                        Log($"  Caption: {caption.Text} (Confidence {caption.Confidence.ToString("P0")})");
                    }
                }
                else
                {
                    Log("  No image caption");
                }

                Console.WriteLine();

                if (result.Description.Tags.Any())
                {
                    Log($"  Tags: {string.Join(", ", result.Description.Tags)}");

                    Console.WriteLine();
                }
            }

            if (result.Tags != null && result.Tags.Any())
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Log("2. Image tags");

                foreach (var tag in result.Tags)
                {
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Log($"  Name: {tag.Name} (Confidence {tag.Confidence.ToString("P0")}{(string.IsNullOrEmpty(tag.Hint) ? string.Empty : $" | Hint: {tag.Hint}")})");
                }

                Console.WriteLine();
            }
        }

        private static void Log(string message)
        {
            Console.WriteLine(message);
        }
    }
}