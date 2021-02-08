using System;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System.Linq;
using HyperBot.Models;
using ImageMagick;
using System.Collections.Generic;
using System.IO;

namespace HyperBot.Modules
{
    public class FlagModule : BaseCommandModule
    {
        [Command("flag"), Aliases("vflag")]
        public async Task VFlag(CommandContext ctx, [RemainingText] string colors)
        {
            if (colors.Length == 0) throw new UserError("You must pass a space seperated list of hex colors to this command");
            var colorsList = new List<String>(colors.Split(" "));
            var builtInFlags = new[] { new FlagPreset {
                    // Trans
                    RoughColors = new[] {"blue", "pink", "white", "pink", "blue"},
                    PreciseColors = new[] { "#55CDFC", "#F7A8B8", "#FFFFFF", "#F7A8B8", "#55CDFC" }
                },
                new FlagPreset {
                    // Bisexual
                    RoughColors = new[] {"pink", "pink", "purple", "blue", "blue"},
                    PreciseColors = new[] { "#D60270", "#D60270", "#9B4F96", "#0038A8", "#0038A8" }
                },
                new FlagPreset {
                    // Bisexual
                    RoughColors = new[] {"pink", "purple", "blue"},
                    PreciseColors = new[] { "#D60270", "#D60270", "#9B4F96", "#0038A8", "#0038A8" }
                },
                new FlagPreset {
                    // Non-binary
                    RoughColors = new[] {"yellow", "white", "purple", "black"},
                    PreciseColors = new[] { "#FFF430", "#FFFFFF", "#9C59D1", "#000000" }
                },
                new FlagPreset {
                    // Pansexual
                    RoughColors = new[] {"pink", "yellow", "blue"},
                    PreciseColors = new[] { "#FF1B8D", "#FFDA00", "#1BB3FF" }
                },
                new FlagPreset {
                    // Asexual
                    RoughColors = new[] {"black", "grey", "white", "purple"},
                    PreciseColors = new[] { "#000000", "#A4A4A4", "#FFFFFF", "#810081" }
                },
                new FlagPreset {
                    // Asexual
                    RoughColors = new[] {"black", "gray", "white", "purple"},
                    PreciseColors = new[] { "#000000", "#A4A4A4", "#FFFFFF", "#810081" }
                }
            }.ToList();
            var matchingBuiltInFlag = builtInFlags.Where(f => String.Join(" ", f.RoughColors).ToLower() == colors).FirstOrDefault();
            if (matchingBuiltInFlag != null) colorsList = matchingBuiltInFlag.PreciseColors.ToList();
            var scaleFactor = 200;
            var targetWidth = 5 * scaleFactor;
            var targetHeight = 3 * scaleFactor;
            using (var images = new MagickImageCollection())
            {
                foreach (var color in colorsList)
                    images.Add(new MagickImage(new MagickColor(color), targetWidth, targetHeight / colorsList.Count));
                var output = images.AppendVertically();
                output.Format = MagickFormat.Png;
                await ctx.Channel.SendMessageAsync(new DiscordMessageBuilder().WithFile("file.png", new MemoryStream(output.ToByteArray())));
            }
        }
        [Command("hflag")]
        public async Task HFlag(CommandContext ctx, [RemainingText] string colors)
        {
            if (colors.Length == 0) throw new UserError("You must pass a space seperated list of hex colors to this command");
            var colorsList = new List<String>(colors.Split(" "));
            var scaleFactor = 200;
            var targetWidth = 5 * scaleFactor;
            var targetHeight = 3 * scaleFactor;
            using (var images = new MagickImageCollection())
            {
                foreach (var color in colorsList)
                    images.Add(new MagickImage(new MagickColor(color), targetWidth / colorsList.Count, targetHeight));
                var output = images.AppendHorizontally();
                output.Format = MagickFormat.Png;
                await ctx.Channel.SendMessageAsync(new DiscordMessageBuilder().WithFile("file.png", new MemoryStream(output.ToByteArray())));
            }
        }

    }
}