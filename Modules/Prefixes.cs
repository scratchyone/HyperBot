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

namespace HyperBot.Modules
{
    [Description("Manage and view bot prefixes")]
    [Group("prefixes"), Aliases("prefix")]
    public class PrefixModule : BaseCommandModule
    {
        public DataContext context { private get; set; }
        public IConfiguration Configuration { private get; set; }

        [Command("list")]
        [Description("List all bot prefixes")]
        public async Task List(CommandContext ctx)
        {
            var guildId = ctx.Message.Channel.GuildId;
            if (guildId == 0) guildId = ctx.Message.ChannelId;
            var prefixes = context.Prefixes.Where(p => p.Guild == guildId).Select(p => p.PrefixText).ToList();
            prefixes.Add(Configuration["Prefix"]);
            await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithTitle("Prefixes")
                .WithColor(HyperBot.Colors.Info)
                .WithDescription(string.Join("\n", prefixes.Select(p => $"`{p}`"))));
        }
        [Command("add")]
        [Description("Add a new bot prefix")]
        [RequireUserPermissions(Permissions.ManageMessages)]
        public async Task Add(CommandContext ctx, [Description("The prefix text")][RemainingText] string prefix)
        {
            var guildId = ctx.Message.Channel.GuildId;
            if (guildId == 0) guildId = ctx.Message.ChannelId;
            var existingPrefix = context.Prefixes.FirstOrDefault(p => p.PrefixText == prefix && p.Guild == guildId);
            if (prefix.Length > 10) throw new UserError("Prefix must be less than 10 characters");
            if (existingPrefix != null) throw new UserError("Prefix already exists");
            await context.Prefixes.AddAsync(new Prefix { Guild = guildId, PrefixText = prefix });
            await context.SaveChangesAsync();
            await ctx.RespondAsync(embed: Embeds.Success.WithDescription("Prefix added!"));
        }
        [Command("remove")]
        [Description("Remove an existing bot prefix")]
        [RequireUserPermissions(Permissions.ManageMessages)]
        public async Task Remove(CommandContext ctx, [Description("The prefix text")][RemainingText] string prefix)
        {
            var guildId = ctx.Message.Channel.GuildId;
            if (guildId == 0) guildId = ctx.Message.ChannelId;
            var existingPrefix = context.Prefixes.SingleOrDefault(p => p.PrefixText == prefix && p.Guild == guildId);
            if (existingPrefix == null)
                throw new UserError("Prefix not found");
            context.Prefixes.Remove(existingPrefix);
            await context.SaveChangesAsync();
            await ctx.RespondAsync(embed: Embeds.Success.WithDescription("Prefix removed!"));
        }
    }
}