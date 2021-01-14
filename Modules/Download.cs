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
            await DownloadFlow(ctx, url, "mp3");
        }
        [Command("video")]
        public async Task Video(CommandContext ctx, [RemainingText] string url)
        {
            await DownloadFlow(ctx, url, "mp4");
        }
        public async Task DownloadFlow(CommandContext ctx, string url, string extension)
        {
            if (url == null) throw new UserError("URL must be provided to this command");
            if (url.Length > 100) throw new UserError("URL must be less than or equal to 100 characters");
            await ctx.RespondAsync(Embeds.Info.WithDescription($"Starting download!"));
            await ctx.TriggerTypingAsync();
            var youtubeDl = new YoutubeDL();
            var filePath = $"./Downloads/{Guid.NewGuid()}.{extension}";
            youtubeDl.Options.FilesystemOptions.Output = filePath;
            youtubeDl.VideoUrl = url;
            youtubeDl.StandardErrorEvent += async (e, message) =>
            {
                if (message.StartsWith("WARNING:")) return;
                await ctx.RespondAsync(HyperBot.Embeds.Error.WithDescription($"Download failed: ```{message}```"));
                youtubeDl.CancelDownload();
            };
            try
            {
                await youtubeDl.DownloadAsync(url);
                var stream = File.OpenRead(filePath);
                await ctx.Channel.SendMessageAsync(new DiscordMessageBuilder().WithFile(youtubeDl.Info.Title + $".{extension}", stream));
                stream.Close();
            }
            catch { }
            File.Delete(filePath);
        }

    }
}