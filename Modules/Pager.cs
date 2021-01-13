using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using DSharpPlus;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using System.Linq;
using HyperBot.Data;
using HyperBot.Models;
using HyperBot;
using Lifti.Querying;
using Lifti;
using System.Collections.Generic;

namespace HyperBot.Modules
{
    [Group("pager"), Aliases(new[] { "highlight", "pagers", "highlights" })]
    public class PagerModule : Cog
    {
        public DataContext _context { private get; set; }

        [Command("add")]
        public async Task Add(CommandContext ctx, [RemainingText] string text)
        {
            if (text == null) throw new UserError("Text must be provided to this command");
            if (text.Length > 30) throw new UserError("Text must be less than or equal to 30 characters");
            var item = new PagerItem
            {
                Author = ctx.Message.Author.Id,
                Text = text.Replace("\n", ""),
            };
            // Check that user DMs are enabled
            try
            {
                var dmChannel = ctx.Channel;
                if (!ctx.Channel.IsPrivate) dmChannel = await ctx.Member.CreateDmChannelAsync();
                await dmChannel.SendMessageAsync(HyperBot.Embeds.Success
                    .WithTitle("Test Passed")
                    .WithDescription("You have your DMs enabled! This means I will be able to send you pager alerts."));
            }
            catch
            {
                throw new UserError("Your DMs must be enabled for me to be able to send you pager alerts.");
            }
            await _context.AddAsync(item);
            await _context.SaveChangesAsync();
            await ctx.RespondAsync(Embeds.Success.WithDescription($"Added pager item!"));
        }
        [Command("remove"), Aliases("delete")]
        public async Task Remove(CommandContext ctx, [RemainingText] string text)
        {
            if (text == null) throw new UserError("Text must be provided to this command");
            var item = _context.Pagers.Where(i => i.Author == ctx.Message.Author.Id && i.Text == text);
            if (item.Count() == 0) throw new UserError("Pager item not found");
            _context.RemoveRange(item);
            await _context.SaveChangesAsync();
            await ctx.RespondAsync(Embeds.Success.WithDescription($"Removed pager item!"));
        }
        [Command("list")]
        public async Task List(CommandContext ctx)
        {
            var items = _context.Pagers.Where(i => i.Author == ctx.Message.Author.Id);
            var first = true;
            if (items.Count() == 0)
            {
                var embed = new DiscordEmbedBuilder()
                    .WithTitle($"{ctx.Message.Author.Username}'s Pager Items")
                    .WithDescription($"*No pager items*");
                await ctx.RespondAsync(embed);
            }

            foreach (var chunk in items.ToList().ChunkBy(25))
            {
                var embed = new DiscordEmbedBuilder();
                if (first) embed.WithTitle($"{ctx.Message.Author.Username}'s Pager Items");
                embed.Description = "";
                foreach (var field in chunk) embed.Description += field.Text + "\n";
                await ctx.RespondAsync(embed);
                first = false;
            }
        }
        new public static void OnStart(DiscordClient client, IConfiguration configuration)
        {
            var _context = new DataContext();
            client.MessageCreated += (client, args) =>
            {
                _ = Task.Run(async () =>
                    {
                        var pagerItems = _context.Pagers
                            .Where(p => (p.Guild == null || p.Guild == args.Guild.Id) && p.Author != args.Author.Id);
                        foreach (var item in pagerItems)
                        {
                            if (args.Message.Content.Contains(item.Text))
                            {
                                // This message matches a valid pager
                                try
                                {
                                    var member = await args.Guild.GetMemberAsync(item.Author);
                                    var perms = member.PermissionsIn(args.Channel);
                                    if (!perms.HasPermission(Permissions.ReadMessageHistory) || !perms.HasPermission(Permissions.AccessChannels)) continue;
                                    var channel = await member.CreateDmChannelAsync();
                                    var embed = new DiscordEmbedBuilder();
                                    embed.WithTitle($"Pager Matched in {args.Guild.Name}");
                                    embed.WithUrl(args.Message.JumpLink);
                                    embed.WithAuthor((args.Author as DiscordMember).DisplayName, iconUrl: args.Author.AvatarUrl);
                                    embed.WithDescription(args.Message.Content
                                        .Replace("**", "")
                                        .Replace(item.Text, $"**{item.Text}**"));
                                    await channel.SendMessageAsync(embed);
                                }
                                catch { }
                            }
                        }
                    });
                return Task.CompletedTask;
            };
        }
    }
}