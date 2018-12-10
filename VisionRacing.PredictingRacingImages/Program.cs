using Microsoft.Cognitive.CustomVision.Prediction;
using Microsoft.Cognitive.CustomVision.Training;
using Microsoft.Cognitive.CustomVision.Training.Models;
using Microsoft.Rest;
using System;
using System.Linq;
using System.Threading.Tasks;
using ImageUrl = Microsoft.Cognitive.CustomVision.Prediction.Models.ImageUrl;

namespace VisionRacing.PredictingRacingImages
{
    class Program
    {
        static void Main(string[] args)
        {
            var trainingKey = "Your Custom Vision training key.";
            var predictionKey = "Your Custom Vision prediction key.";

            Start(trainingKey, predictionKey).Wait();
        }

        private static async Task Start(string trainingKey, string predictionKey)
        {
            var projectName = " ";
            var trainingApi = GetTrainingApi(trainingKey);
            var predictionEndpoint = GetPredictionEndpoint(predictionKey);

            while (!string.IsNullOrEmpty(projectName))
            {
                try
                {
                    Console.Clear();
                    await ListProjects(trainingApi);

                    Console.WriteLine("Please enter a project name or press enter to exit:");
                    projectName = Console.ReadLine();

                    if (!string.IsNullOrEmpty(projectName))
                    {
                        await WorkOnProject(trainingApi, predictionEndpoint, projectName);
                    }
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"An error occurred: {Environment.NewLine}{ex.Message}");

                    if (ex is HttpOperationException)
                    {
                        Console.WriteLine(((HttpOperationException)ex).Response.Content);
                    }

                    Console.ResetColor();
                    Console.WriteLine();
                    Console.WriteLine();
                    Console.WriteLine("Press any key to continue");
                    Console.ReadLine();
                }
            }
        }

        private static async Task WorkOnProject(TrainingApi trainingApi, PredictionEndpoint predictionEndpoint, string name)
        {
            var option = " ";

            while (!string.IsNullOrEmpty(option))
            {
                Console.Clear();

                var project = await GetOrCreateProject(trainingApi, name);
                Console.WriteLine($"  --- Project {project.Name} ---");
                Console.WriteLine();

                await ListProjectTags(trainingApi, project.Id);

                Console.WriteLine("Type an option number:");
                Console.WriteLine("  1: Predict Karting images");
                Console.WriteLine("  2: Predict F1 images");
                Console.WriteLine("  3: Predict MotoGP images");
                Console.WriteLine("  4: Predict Rally images");
                Console.WriteLine("  5: Predict Test images");
                Console.WriteLine();
                Console.WriteLine($"Press any other key to exit project {name}");
                option = Console.ReadLine();

                switch (option)
                {
                    case "1":
                        await StartPrediction(predictionEndpoint, project.Id, ImageType.Karting);
                        break;
                    case "2":
                        await StartPrediction(predictionEndpoint, project.Id, ImageType.F1);
                        break;
                    case "3":
                        await StartPrediction(predictionEndpoint, project.Id, ImageType.MotoGP);
                        break;
                    case "4":
                        await StartPrediction(predictionEndpoint, project.Id, ImageType.Rally);
                        break;
                    case "5":
                        await StartPrediction(predictionEndpoint, project.Id, ImageType.Test);
                        break;
                    default:
                        option = string.Empty;
                        break;
                }
            }
        }

        private static int GetImageCountPerImageType(ImageType imageType)
        {
            switch (imageType)
            {
                case ImageType.F1:
                    return 7;
                case ImageType.Karting:
                    return 35;
                case ImageType.MotoGP:
                    return 7;
                case ImageType.Rally:
                    return 6;
                case ImageType.Test:
                    return 10;
                default:
                    return 0;
            }
        }

        private static string GetImageDescriptionPerImageTypeAndNumber(ImageType imageType, int imageNumber)
        {
            switch (imageType)
            {
                case ImageType.Test:
                    switch (imageNumber)
                    {
                        case 1:
                        case 2:
                            return "Solo kart racer on track";
                        case 3:
                        case 7:
                        case 10:
                            return "Multiple kart racers on track";
                        case 4:
                        case 9:
                            return "Solo kart racer on pre-grid";
                        case 5:
                            return "Kart racers on a podium";
                        case 6:
                            return "A tent in a karting paddock";
                        case 8:
                            return "A racing helmet";
                        default:
                            return string.Empty;
                    }
                case ImageType.F1:
                case ImageType.Karting:
                case ImageType.MotoGP:
                case ImageType.Rally:
                default:
                    return string.Empty;
            }
        }

        private static async Task<Project> GetOrCreateProject(TrainingApi trainingApi, string name)
        {
            var projects = await trainingApi.GetProjectsAsync();

            var project = projects.Where(p => p.Name.ToUpper() == name.ToUpper()).SingleOrDefault();

            if (project == null)
            {
                project = await trainingApi.CreateProjectAsync(name);
            }

            return project;
        }

        private static PredictionEndpoint GetPredictionEndpoint(string predictionKey)
        {
            return new PredictionEndpoint
            {
                ApiKey = predictionKey
            };
        }

        private static TrainingApi GetTrainingApi(string trainingKey)
        {
            return new TrainingApi
            {
                ApiKey = trainingKey
            };
        }

        private static async Task ListProjects(TrainingApi trainingApi)
        {
            var projects = await trainingApi.GetProjectsAsync();

            if (projects.Any())
            {
                Console.WriteLine($"Existing projects: {Environment.NewLine}{string.Join(Environment.NewLine, projects.Select(p => p.Name))}{Environment.NewLine}");
            }
        }

        private static async Task ListProjectTags(TrainingApi trainingApi, Guid projectId)
        {
            var tagList = await trainingApi.GetTagsAsync(projectId);

            if (tagList.Tags.Any())
            {
                Console.WriteLine($"Tags: {Environment.NewLine}{string.Join(Environment.NewLine, tagList.Tags.Select(t => $"  {t.Name} (Image count: {t.ImageCount})"))}{Environment.NewLine}");
            }
            else
            {
                Console.WriteLine($"No tags yet...{Environment.NewLine}");
            }
        }

        private static async Task StartPrediction(PredictionEndpoint predictionEndpoint, Guid projectId, ImageType imageType)
        {
            var imageTypeCount = GetImageCountPerImageType(imageType);

            for (int i = 1; i <= imageTypeCount; i++)
            {
                Console.Clear();
                Console.WriteLine($"Image {imageType} {i}/{imageTypeCount} prediction in progress...");

                var imageDescription = GetImageDescriptionPerImageTypeAndNumber(imageType, i);

                if (!string.IsNullOrEmpty(imageDescription))
                {
                    Console.WriteLine();
                    Console.WriteLine($"Description: {imageDescription}");
                }

                var imagePredictionResult = await predictionEndpoint.PredictImageUrlWithNoStoreAsync(projectId, new ImageUrl($"https://github.com/vivienchevallier/Article-AzureCognitive.Vision-Racing/raw/master/Images/{imageType}/{imageType}%20({i}).jpg"));

                Console.Clear();

                if (imagePredictionResult.Predictions.Any())
                {
                    Console.WriteLine($"Image {imageType} {i}/{imageTypeCount}: {imageDescription}{Environment.NewLine}{string.Join(Environment.NewLine, imagePredictionResult.Predictions.Select(p => $"  {p.Tag}: {p.Probability:P1}"))}{Environment.NewLine}");
                }
                else
                {
                    Console.WriteLine($"Image {imageType} {i}/{imageTypeCount}: no predictions yet...{Environment.NewLine}");
                }

                if (i < imageTypeCount)
                {
                    Console.WriteLine("Press enter to predict next image or any other key to stop predictions");

                    if (Console.ReadKey().Key != ConsoleKey.Enter)
                    {
                        break;
                    }
                }
                else
                {
                    Console.WriteLine("All images predicted... Press any key to continue");
                    Console.ReadLine();
                }
            }
        }

        private enum ImageType
        {
            F1,
            Karting,
            MotoGP,
            Rally,
            Test
        }
    }
}
