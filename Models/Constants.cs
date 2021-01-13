using DSharpPlus.Entities;

namespace HyperBot
{
    class Colors
    {
        public static DiscordColor Info = new DiscordColor("#1da0ff");
        public static DiscordColor Success = new DiscordColor("#1dbb4f");
        public static DiscordColor Error = new DiscordColor("#e74d4d");
        public static DiscordColor Warning = new DiscordColor("#d8ae2b");
    }
    class Embeds
    {
        public static DiscordEmbedBuilder Success { get => new DiscordEmbedBuilder().WithTitle("Success!").WithColor(Colors.Success); }
        public static DiscordEmbedBuilder Error { get => new DiscordEmbedBuilder().WithTitle("Error").WithColor(Colors.Error); }
        public static DiscordEmbedBuilder Warning { get => new DiscordEmbedBuilder().WithTitle("Warning!").WithColor(Colors.Warning); }
        public static DiscordEmbedBuilder Info { get => new DiscordEmbedBuilder().WithTitle("Info!").WithColor(Colors.Info); }
    }
}