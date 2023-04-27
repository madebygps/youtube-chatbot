using Azure;
using Azure.AI.TextAnalytics;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Newtonsoft.Json;
using YoutubeExplode;
using YoutubeExplode.Common;

internal class Program
{
    private static string transcriptsDirectory = "../transcripts";
    private static TextAnalyticsClient textClient;
    private static List<string> videoKeyPhrases;
    private static async Task Main(string[] args)
    {
        DotNetEnv.Env.Load();
        string indexName = "videoindex3";
        string searchEndpoint = System.Environment.GetEnvironmentVariable("searchEndpoint");
        string searchApiKey = System.Environment.GetEnvironmentVariable("searchApiKey");
        var videoIds = new List<string>();
        var youtubeClient = new YoutubeClient();
        var channelUrl = "https://www.youtube.com/channel/UCbjgKwnWnGG7sKCPTRgrFcw";
        var videos = await youtubeClient.Channels.GetUploadsAsync(channelUrl);
        var searchClient = new SearchClient(new Uri(searchEndpoint), indexName, new AzureKeyCredential(searchApiKey));
        // if apiKey or endpoint are not set, exit the program

        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("textApiKey")))
        {
            Console.WriteLine("Please set the azure text service apikey variable");
            return; 
        }
        else if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("textEndpoint")))
        {
            Console.WriteLine("Please set the text service endpoint variable");
            return;
        }
        else
        {
            var textApiKey = Environment.GetEnvironmentVariable("textApiKey");
            var textEndpoint = new Uri(Environment.GetEnvironmentVariable("textEndpoint"));
            var credentials = new AzureKeyCredential(textApiKey);
            textClient = new TextAnalyticsClient(textEndpoint, credentials);
        }

        foreach (var video in videos)
        {
            var videoInfo = await youtubeClient.Videos.GetAsync(video.Id);
            var trackManifest = await youtubeClient.Videos.ClosedCaptions.GetManifestAsync($"https://www.youtube.com/watch?v={video.Id}");
            string videoTitle = videoInfo.Title.ToString();
            string videoTranscript = "";
            string outputPath = $"videos/{video.Id}.json";
            try
            {
                var trackInfo = trackManifest.GetByLanguage("en");
                if (trackInfo != null)
                {
                    var track = await youtubeClient.Videos.ClosedCaptions.GetAsync(trackInfo);
                    foreach (var caption in track.Captions) // Get all text from captions
                    {
                        videoTranscript += caption.ToString();
                    }
                    await SaveVideoDataAsync(video.Id.ToString(), video.Title.ToString(), videoTranscript, outputPath);
                    var document = new Dictionary<string, object>
                    {
                        {"id", video.Id.ToString()},
                        {"title", video.Title.ToString()},
                        {"transcript", videoTranscript},
                        {"key_phrases", videoKeyPhrases},
                    };
                    var batch = IndexDocumentsBatch.Create(IndexDocumentsAction.Upload(document));
                    searchClient.IndexDocuments(batch);
                }
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine(ex.Message);
                continue;
            }
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
            tasks.Add(textClient.ExtractKeyPhrasesAsync(chunk));
        }
        await Task.WhenAll(tasks);
        var keyPhrases = tasks.SelectMany(task => task.Result.Value).ToList();
        videoKeyPhrases = keyPhrases;
        return keyPhrases;
    }

    public static async Task SaveVideoDataAsync(string videoId, string videoTitle, string videoTranscript, string outputPath)
    {
        var keyPhrases = await ExtractKeyPhrasesFromLargeTextAsync(videoTranscript);
        var videoData = new Dictionary<string, object>
    {
        {"id", videoId},
        {"title", videoTitle},
        {"transcript", videoTranscript},
        {"key_phrases", keyPhrases},
    };

        string json = JsonConvert.SerializeObject(videoData, Newtonsoft.Json.Formatting.Indented);
        await File.WriteAllTextAsync(outputPath, json);
    }
}