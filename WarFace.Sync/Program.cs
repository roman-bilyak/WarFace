using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using Microsoft.Extensions.Configuration;
using TL;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appconfig.json", true, true)
    .AddUserSecrets<AppConfig>()
    .AddEnvironmentVariables(prefix: "WARFACE_")
    .AddCommandLine(args)
    .Build();

AppConfig? appConfig = configuration.Get<AppConfig>();

using var telegram = new WTelegram.Client(x => appConfig?.TelegramAPI?.GetValue(x) ?? TelegramConfigProvider(x));
User user = await telegram.LoginUserIfNeeded();
Console.WriteLine($"We are logged-in as {user.username ?? user.first_name + " " + user.last_name} (id {user.id})");

IFaceClient faceClient = new FaceClient(new ApiKeyServiceClientCredentials(appConfig?.FaceAPI?.SubscriptionKey)) { Endpoint = appConfig?.FaceAPI?.Endpoint };
const string largeFaceListId = "warface";
const string largeFaceListName = "warface";

await faceClient.LargeFaceList.DeleteAsync(largeFaceListId);
await faceClient.LargeFaceList.CreateAsync(largeFaceListId, largeFaceListName, recognitionModel: RecognitionModel.Recognition04);

foreach (string? channelName in appConfig?.TelegramAPI?.Channels)
{
    var channel = await telegram.Contacts_ResolveUsername(channelName);

    for (int offset_id = 0; ;)
    {
        var messages = await telegram.Messages_GetHistory(channel.UserOrChat.ToInputPeer(), offset_id);
        if (messages.Messages.Length == 0)
        {
            break;
        }

        foreach (var msgBase in messages.Messages)
        {
            if (msgBase is Message msg)
            {
                string messageUrl = $"https://t.me/c/{channel.UserOrChat.ID}/{msg.ID}";

                if (msg.media is MessageMediaPhoto { photo: Photo photo })
                {
                    using MemoryStream memoryStream = new MemoryStream();
                    await telegram.DownloadFileAsync(photo, memoryStream);

                    var detectedFaces = await faceClient.Face.DetectWithStreamAsync(new MemoryStream(memoryStream.ToArray()), recognitionModel: RecognitionModel.Recognition04, detectionModel: DetectionModel.Detection03);

                    foreach (var detectedFace in detectedFaces)
                    {
                        IList<int> targetFace = new List<int>() { detectedFace.FaceRectangle.Left, detectedFace.FaceRectangle.Top, detectedFace.FaceRectangle.Width, detectedFace.FaceRectangle.Height };
                        try
                        {
                            await faceClient.LargeFaceList.AddFaceFromStreamAsync(largeFaceListId, new MemoryStream(memoryStream.ToArray()), userData: messageUrl, targetFace: targetFace, detectionModel: DetectionModel.Detection03);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Exception!!!");
                        }
                    }
                }
                else if (msg.media is MessageMediaDocument { document: Document document })
                {
                    //int slash = document.mime_type.IndexOf('/'); // quick & dirty conversion from MIME type to file extension
                    //string filename = (slash > 0 ? $"{document.id}.{document.mime_type[(slash + 1)..]}" : $"{document.id}.bin");

                    //using var fileStream = File.Create(filename);
                    //await telegram.DownloadFileAsync(document, fileStream);
                    //fileStream.Dispose();

                    //var capture = new VideoCapture(filename);
                    //var image = new Mat();
                    //while (capture.IsOpened())
                    //{
                    //    capture.Read(image);
                    //    if (image.Empty())
                    //    {
                    //        break;
                    //    }

                    //    var detectedFaces = await faceClient.Face.IdentifyAsync(image.ToMemoryStream());
                    //    foreach (var detectedFace in detectedFaces)
                    //    {
                    //        try
                    //        {
                    //            IList<int> targetFace = new List<int>() { detectedFace.FaceRectangle.Left, detectedFace.FaceRectangle.Top, detectedFace.FaceRectangle.Width, detectedFace.FaceRectangle.Height };
                    //            await faceClient.LargeFaceList.AddFaceFromStreamAsync(largeFaceListId, image.ToMemoryStream(), userData: messageUrl, detectionModel: DetectionModel.Detection03, targetFace: targetFace);
                    //        }
                    //        catch (Exception ex)
                    //        {
                    //            Console.WriteLine("Exception!!!");
                    //        }
                    //    }
                    //}

                    //File.Delete(filename);
                }
            }
        }
        offset_id = messages.Messages[^1].ID;
    }
}

await faceClient.LargeFaceList.TrainAsync(largeFaceListId);

Console.WriteLine("Done!");
Console.ReadLine();

static string? TelegramConfigProvider(string what)
{
    switch (what)
    {
        case "api_hash":
        case "api_id":
        case "phone_number":
        case "verification_code":
        case "first_name":
        case "last_name":
        case "password":
            {
                Console.Write($"Telegram {what.Replace("_", " ")}: ");
                return Console.ReadLine();
            }
        default:
            return null;
    }
}