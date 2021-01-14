using System;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System.Linq;
using HyperBot.Models;
using HyperBot.Data;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using NYoutubeDL;
using System.IO;
using Google.Cloud.Storage.V1;


namespace HyperBot.Modules
{
    [Group("download"), Aliases("dl")]
    public class DownloadModule : BaseCommandModule
    {
        public DataContext context { private get; set; }
        public IConfiguration Configuration { private get; set; }
        [Command("audio")]
        public async Task Audio(CommandContext ctx, [RemainingText] string url)
        {
            await DownloadFlow(ctx, url, "mp3", "audio/mpeg");
        }
        [Command("video")]
        public async Task Video(CommandContext ctx, [RemainingText] string url)
        {
            await DownloadFlow(ctx, url, "mp4", "video/mp4");
        }
        public async Task DownloadFlow(CommandContext ctx, string url, string extension, string contentType)
        {
            if (url == null) throw new UserError("URL must be provided to this command");
            if (url.Length > 100) throw new UserError("URL must be less than or equal to 100 characters");
            await ctx.RespondAsync(Embeds.Info.WithDescription($"Starting download!"));
            var youtubeDl = new YoutubeDL();
            var filePath = $"./Downloads/{Guid.NewGuid()}.{extension}";
            youtubeDl.Options.FilesystemOptions.Output = filePath;
            if (Configuration["Cookies"] != null)
                youtubeDl.Options.FilesystemOptions.Cookies = Configuration["Cookies"];
            if (Configuration["UserAgent"] != null)
                youtubeDl.Options.WorkaroundsOptions.UserAgent = Configuration["UserAgent"];
            if (extension == "mp3")
            {
                youtubeDl.Options.PostProcessingOptions.ExtractAudio = true;
                youtubeDl.Options.PostProcessingOptions.AudioFormat = NYoutubeDL.Helpers.Enums.AudioFormat.mp3;
            }
            else
            {
                youtubeDl.Options.VideoFormatOptions.Format = NYoutubeDL.Helpers.Enums.VideoFormat.mp4;
            }
            youtubeDl.VideoUrl = url;
            youtubeDl.StandardErrorEvent += async (e, message) =>
            {
                if (message.StartsWith("WARNING:")) return;
                await ctx.RespondAsync(HyperBot.Embeds.Error.WithDescription($"Download failed: ```{message}```"));
                youtubeDl.CancelDownload();
            };
            await youtubeDl.DownloadAsync();
            var maxFileSize = 7 * 1024 * 1024; // 7 MB
            using (var stream = File.OpenRead(filePath))
            {
                if (stream.Length > maxFileSize)
                {
                    var storage = StorageClient.Create();
                    var objectName = $"{youtubeDl.Info.Title}-{Guid.NewGuid()}.{extension}".Replace(" ", "_");
                    var gFile = storage.UploadObject("hyperbotdownloads", objectName, contentType, stream);

                    await ctx.Channel.SendMessageAsync(new DiscordMessageBuilder()
                        .WithEmbed(HyperBot.Embeds.Success
                        .WithTitle("Downloaded!")
                        .WithDescription("This link will expire in 30 days")));
                    await ctx.Channel.SendMessageAsync($"https://storage.googleapis.com/hyperbotdownloads/{System.Uri.EscapeUriString(objectName)}");
                }
                else
                {
                    await ctx.Channel.SendMessageAsync(new DiscordMessageBuilder().WithFile($"{youtubeDl.Info.Title}.{extension}", stream));
                }
            }
            File.Delete(filePath);
        }
    }
}