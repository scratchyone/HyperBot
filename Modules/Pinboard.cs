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
    [Group("pinboard")]
    public class PinboardModule : BaseCommandModule
    {
        public DataContext _context { private get; set; }

        [Command("add")]
        public async Task Add(CommandContext ctx, [RemainingText] string text)
        {
            if (text == null) throw new UserError("Text must be provided to this command");
            if (text.Length > 1024) throw new UserError("Text must be less than or equal to 1024 characters");
            var item = new PinboardItem
            {
                Author = ctx.Message.Author.Id,
                Text = text,
                Timestamp = DateTime.UtcNow
            };
            await _context.AddAsync(item);
            await _context.SaveChangesAsync();
            await ctx.RespondAsync(Embeds.Success.WithDescription($"Success! Added item to your pinboard with ID `{item.Id}`"));
        }
        [Command("remove"), Aliases("delete")]
        public async Task Remove(CommandContext ctx, long id)
        {
            var item = _context.PinboardItems.SingleOrDefault(i => i.Author == ctx.Message.Author.Id && i.Id == id);
            if (item == null) throw new UserError("Pinboard item not found");
            _context.Remove(item);
            await _context.SaveChangesAsync();
            await ctx.RespondAsync(Embeds.Success.WithDescription($"Success! Removed item from your pinboard"));
        }
        [Command("list")]
        public async Task List(CommandContext ctx)
        {
            var items = _context.PinboardItems.Where(i => i.Author == ctx.Message.Author.Id);
            var first = true;
            if (items.Count() == 0)
            {
                var embed = new DiscordEmbedBuilder()
                    .WithTitle($"{ctx.Message.Author.Username}'s Pinboard")
                    .WithDescription($"*No items on pinboard*");
                await ctx.RespondAsync(embed);
            }

            foreach (var chunk in items.ToList().ChunkBy(25))
            {
                var embed = new DiscordEmbedBuilder();
                if (first) embed.WithTitle($"{ctx.Message.Author.Username}'s Pinboard");
                foreach (var field in chunk) embed.AddField($"ID {field.Id}", field.Text.Truncate(1024));
                await ctx.RespondAsync(embed);
                first = false;
            }
        }
        [Command("clear"), Aliases("erase")]
        public async Task Clear(CommandContext ctx)
        {
            await ctx.RespondAsync("Are you sure? This will erase your entire pinboard.");
            if (!await ctx.Confirm()) return;
            var items = _context.PinboardItems.Where(i => i.Author == ctx.Message.Author.Id);
            _context.RemoveRange(items);
            await _context.SaveChangesAsync();
            await ctx.RespondAsync(Embeds.Success.WithDescription($"Success! Removed all items from your pinboard"));
        }
        [Command("search")]
        public async Task Search(CommandContext ctx, [RemainingText] string query)
        {
            if (query == null) throw new UserError("A query must be provided to this command");
            await ctx.Channel.TriggerTypingAsync();
            var dbItems = _context.PinboardItems.Where(i => i.Author == ctx.Message.Author.Id);
            var index = new FullTextIndexBuilder<int>().Build();
            foreach (var item in dbItems)
            {
                var text = item.Text;
                if (item.Text.Contains("<@")) text += " ping discord";
                if (item.Text.Contains("<#")) text += " channel discord";
                await index.AddAsync(item.Id, text);
            }
            var results = index.Search(query);
            var first = true;
            var chunks = results.ToList().ChunkBy(25);
            if (chunks.Count == 0)
            {
                var embed = new DiscordEmbedBuilder()
                    .WithTitle($"Search Results for {ctx.Message.Author.Username}'s Pinboard")
                    .WithDescription($"*No search results for \"{query}\"*");
                await ctx.RespondAsync(embed);
            }
            foreach (var chunk in chunks)
            {
                var embed = new DiscordEmbedBuilder();
                if (first) embed.WithTitle($"Search Results for {ctx.Message.Author.Username}'s Pinboard").WithDescription($"*Search results for \"{query}\"*");
                foreach (var field in chunk) embed.AddField($"ID {field.Key}", dbItems.Single(i => i.Id == field.Key).Text.Truncate(1024));
                await ctx.RespondAsync(embed);
                first = false;
            }
        }
    }
}