using YoutubeExplode;
using YoutubeExplode.Common;

internal class Program
{

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

                        System.IO.File.AppendAllText($"../transcripts/{video.Id}.txt", caption.ToString());
                    }
                }

            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine(ex.Message);
                continue;
            }

        }
    }

}




// 4. Save transcript to individual text file.