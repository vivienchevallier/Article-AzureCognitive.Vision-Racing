using Microsoft.Cognitive.CustomVision.Training;
using Microsoft.Cognitive.CustomVision.Training.Models;
using Microsoft.Rest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace VisionRacing.TrainingRacingImages
{
    class Program
    {
        static void Main(string[] args)
        {
            var trainingKey = "Your Custom Vision training key.";

            Start(trainingKey).Wait();
        }

        private static async Task Start(string trainingKey)
        {
            var projectName = " ";
            var trainingApi = GetTrainingApi(trainingKey);

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
                        await WorkOnProject(trainingApi, projectName);
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

        private static async Task WorkOnProject(TrainingApi trainingApi, string name)
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
                Console.WriteLine("  1: Create Karting images");
                Console.WriteLine("  2: Create F1 images");
                Console.WriteLine("  3: Create MotoGP images");
                Console.WriteLine("  4: Create Rally images");
                Console.WriteLine("  5: Train project");
                Console.WriteLine("  6: Delete project");
                Console.WriteLine();
                Console.WriteLine($"Press any other key to exit project {name}");
                option = Console.ReadLine();

                switch (option)
                {
                    case "1":
                        await CreateTagImages(trainingApi, project.Id, ImageType.Karting);
                        break;
                    case "2":
                        await CreateTagImages(trainingApi, project.Id, ImageType.F1);
                        break;
                    case "3":
                        await CreateTagImages(trainingApi, project.Id, ImageType.MotoGP);
                        break;
                    case "4":
                        await CreateTagImages(trainingApi, project.Id, ImageType.Rally);
                        break;
                    case "5":
                        await TrainProject(trainingApi, project.Id);
                        break;
                    case "6":
                        await DeleteProject(trainingApi, project.Id);
                        option = string.Empty;
                        break;
                    default:
                        option = string.Empty;
                        break;
                } 
            }
        }

        private static async Task CreateTagImages(TrainingApi trainingApi, Guid projectId, ImageType imageType)
        {
            Console.Clear();

            var imageTag = await GetOrCreateTag(trainingApi, projectId, imageType.ToString());
            var racingTag = await GetOrCreateTag(trainingApi, projectId, "Racing");
            var imageTypeCount = GetImageCountPerImageType(imageType);

            if (imageTag.ImageCount != imageTypeCount)
            {
                Console.WriteLine($"{imageType} images creation in progress...");

                var images = new List<ImageUrlCreateEntry>();

                for (int i = 1; i <= imageTypeCount; i++)
                {
                    images.Add(new ImageUrlCreateEntry($"https://github.com/vivienchevallier/Article-AzureCognitive.Vision-Racing/raw/master/Images/{imageType}/{imageType}%20({i}).jpg"));
                }

                var tags = new List<Guid>() { imageTag.Id, racingTag.Id };
                var response = await trainingApi.CreateImagesFromUrlsAsync(projectId, new ImageUrlCreateBatch(images, tags));

                Console.Clear();
                Console.WriteLine($"{imageType} images successfully created.");
            }
            else
            {
                Console.WriteLine($"{imageType} images already created.");
            }

            Console.WriteLine();
            Console.WriteLine("Press any key to continue");
            Console.ReadLine();
        }

        private static async Task DeleteProject(TrainingApi trainingApi, Guid projectId)
        {
            Console.Clear();

            await trainingApi.DeleteProjectAsync(projectId);

            Console.WriteLine("Project deleted... Press any key to continue");
            Console.ReadLine();
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
                default:
                    return 0;
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

        private static async Task<Tag> GetOrCreateTag(TrainingApi trainingApi, Guid projectId, string name)
        {
            var tagList = await trainingApi.GetTagsAsync(projectId);

            var tag = tagList.Tags.Where(t => t.Name.ToUpper() == name.ToUpper()).SingleOrDefault();

            if (tag == null)
            {
                tag = await trainingApi.CreateTagAsync(projectId, name);
            }

            return tag;
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

        private static async Task TrainProject(TrainingApi trainingApi, Guid projectId)
        {
            var iteration = await trainingApi.TrainProjectAsync(projectId);

            while (iteration.Status == "Training")
            {
                Console.Clear();
                Console.WriteLine("Training in progress...");

                Thread.Sleep(1000);

                iteration = await trainingApi.GetIterationAsync(projectId, iteration.Id);
            }

            iteration.IsDefault = true;
            trainingApi.UpdateIteration(projectId, iteration.Id, iteration);

            Console.WriteLine();
            Console.WriteLine("Project successfully trained... Press any key to continue");
            Console.ReadLine();
        }

        private enum ImageType
        {
            F1,
            Karting,
            MotoGP,
            Rally
        }
    }
}
