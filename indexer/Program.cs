using Azure;
using Azure.AI.TextAnalytics;
using Newtonsoft.Json;
using YoutubeExplode;
using YoutubeExplode.Common;

internal class Program
{
    private static string transcriptsDirectory = "../transcripts";
    private static TextAnalyticsClient client;

    private static List<string> keyPhrases;

    private static async Task Main(string[] args)
    {

        var videoIds = new List<string>();
        var youtube = new YoutubeClient();
        var channelUrl = "https://www.youtube.com/channel/UCbjgKwnWnGG7sKCPTRgrFcw";

        var videos = await youtube.Channels.GetUploadsAsync(channelUrl);

        // if apiKey or endpoint are not set, exit the program

        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("apikey")))
        {
            Console.WriteLine("Please set the openai_api_key environment variable");
            return;
        }
        else if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("endpoint")))
        {
            Console.WriteLine("Please set the endpoint environment variable");
            return;
        }
        else
        {

            var apiKey = Environment.GetEnvironmentVariable("apikey");
            var endpoint = new Uri(Environment.GetEnvironmentVariable("endpoint"));
            var credentials = new AzureKeyCredential(apiKey);
            client = new TextAnalyticsClient(endpoint, credentials);

        }


        foreach (var video in videos)
        {
            var videoInfo = await youtube.Videos.GetAsync(video.Id);
            var trackManifest = await youtube.Videos.ClosedCaptions.GetManifestAsync($"https://www.youtube.com/watch?v={video.Id}");
            string videoTitle = videoInfo.Title.ToString();
            string videoTranscript = "";
            string outputPath = $"{video.Id}.json";

            try
            {
                var trackInfo = trackManifest.GetByLanguage("en");

                if (trackInfo != null)
                {
                    var track = await youtube.Videos.ClosedCaptions.GetAsync(trackInfo);

                    foreach (var caption in track.Captions) // Get all text from captions
                    {
                        videoTranscript += caption.ToString();
                        keyPhrases = await ExtractKeyPhrasesFromLargeTextAsync(caption.ToString());
                    }
                }
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine(ex.Message);
                continue;
            }
            await SaveVideoDataAsync(video.Id.ToString(), video.Title.ToString(), videoTranscript, outputPath);
        }

    }

    static async Task<List<string>> ExtractKeyPhrasesFromLargeTextAsync(string largeText, int chunkSize = 5000)
    {
        int textLength = largeText.Length;
        var tasks = new List<Task<Response<KeyPhraseCollection>>>();
        for (int i = 0; i < textLength; i += chunkSize)
        {
            int currentChunkSize = Math.Min(chunkSize, textLength - i);
            string chunk = largeText.Substring(i, currentChunkSize);
            tasks.Add(client.ExtractKeyPhrasesAsync(chunk));
        }

        await Task.WhenAll(tasks);
        var keyPhrases = tasks.SelectMany(task => task.Result.Value).ToList();
        return keyPhrases;
    }


    public static async Task SaveVideoDataAsync(string videoId, string videoTitle, string videoTranscript, string outputPath)
    {
        var keyPhrases = await ExtractKeyPhrasesFromLargeTextAsync(videoTranscript);

        var videoData = new Dictionary<string, object>
    {
        {"video_id", videoId},
        {"title", videoTitle},
        {"transcript", videoTranscript},
        {"key_phrases", keyPhrases},
    };

        string json = JsonConvert.SerializeObject(videoData, Newtonsoft.Json.Formatting.Indented);
        await File.WriteAllTextAsync(outputPath, json);
    }

}