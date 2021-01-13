using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.CommandsNext;
using DSharpPlus.Interactivity.Extensions;
using System.Threading.Tasks;
using System;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;

namespace HyperBot
{
    public static class Extensions
    {
        public static string CreateInvite(this DiscordClient client)
        {
            return $"https://discord.com/api/oauth2/authorize?client_id={client.CurrentUser.Id}&permissions=2146958847&scope=bot";
        }
        public static async Task<bool> Confirm(this CommandContext ctx)
        {
            var emoji = DiscordEmoji.FromName(ctx.Client, ":white_check_mark:");
            var message = await ctx.RespondAsync(embed: Embeds.Warning.WithTitle("Are you sure?").WithDescription("React with :white_check_mark: to confirm."));
            await message.CreateReactionAsync(emoji);
            var result = await message.WaitForReactionAsync(ctx.Member, emoji, TimeSpan.FromSeconds(10));
            if (!result.TimedOut)
            {
                await message.ModifyAsync(embed: Embeds.Success.WithTitle("Confirmation Successful").Build());
                try
                {
                    await message.DeleteAllReactionsAsync();
                }
                catch
                { }
                return true;
            }
            else
            {
                await message.ModifyAsync(embed: Embeds.Error.WithTitle("Confirmation Failed").Build());
                try
                {
                    await message.DeleteAllReactionsAsync();
                }
                catch
                { }
                return false;
            }
        }
        public static string Truncate(this string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }
        public static List<List<T>> ChunkBy<T>(this List<T> source, int chunkSize)
        {
            return source
                .Select((x, i) => new { Index = i, Value = x })
                .GroupBy(x => x.Index / chunkSize)
                .Select(x => x.Select(v => v.Value).ToList())
                .ToList();
        }
        public static string CleanPings(this string value, DiscordGuild guild)
        {
            var newString = value;
            newString = newString.Replace("@everyone", "@​everyone").Replace("@here", "@​here");
            var rolePings = Regex.Matches(newString, "<@&([0-9]+)>");
            foreach (Match match in rolePings)
            {
                var role = guild.Roles.Select(r => r.Value).FirstOrDefault(r => r.Id.ToString() == match.Groups[1].Value);
                newString = newString.Replace(match.Groups[0].Value, role == null ? "PING" : "@​" + role.Name);
            }
            return newString;
        }
    }
}