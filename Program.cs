using YoutubeExplode;
using YoutubeExplode.Common;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var youtube = new YoutubeClient();
        var channelUrl = "https://www.youtube.com/channel/UCbjgKwnWnGG7sKCPTRgrFcw";

        var videos = await youtube.Channels.GetUploadsAsync(channelUrl);

        foreach (var video in videos)
        {
            var videoInfo = await youtube.Videos.GetAsync(video.Id);
            Console.WriteLine($"{videoInfo.Title} - {videoInfo.Author} - {videoInfo.Id}");
        }
    }
}