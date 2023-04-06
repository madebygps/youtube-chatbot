using YoutubeExplode;
using YoutubeExplode.Common;

internal class Program
{
    // ✅ 1. Get every video ID from my channel
    // ✅ 2. Save every video ID to an array.
    private static async Task Main(string[] args)
    {
        var videoIds = new List<string>();
        var youtube = new YoutubeClient();
        var channelUrl = "https://www.youtube.com/channel/UCbjgKwnWnGG7sKCPTRgrFcw";

        var videos = await youtube.Channels.GetUploadsAsync(channelUrl);

        foreach (var video in videos)
        {
            var videoInfo = await youtube.Videos.GetAsync(video.Id);
            var trackManifest = await youtube.Videos.ClosedCaptions.GetManifestAsync($"https://www.youtube.com/watch?v={video.Id}");
            System.IO.File.WriteAllText($"transcript_{video.Id}.txt", video.Title.ToString());

            try
            {
                var trackInfo = trackManifest.GetByLanguage("en");

                if (trackInfo != null)
                {
                    var track = await youtube.Videos.ClosedCaptions.GetAsync(trackInfo);

                    foreach (var caption in track.Captions) // Get all text from captions
                    {
                        // Append caption to transcript text file                        

                        System.IO.File.AppendAllText($"transcript_{video.Id}.txt", caption.ToString());
                    }
                    // Get all text from captions



                }

            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine(ex.Message);
                continue;
            }


        }
    }

    // 3. iterate over the array of IDS and download transcript for each video.
    public async Task<string> GetTranscriptAsync(string videoId)
    {
        var apiUrl = $"https://www.youtube.com/api/timedtext?lang=en&v={videoId}";
        using var httpClient = new HttpClient();
        var response = await httpClient.GetAsync(apiUrl);

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            return content;
        }

        return null;
    }
}




// 4. Save transcript to individual text file.