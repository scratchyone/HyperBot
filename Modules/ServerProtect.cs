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
using System.Text.RegularExpressions;
using System.Net;
using System.Collections.Generic;
using System.Net.Http;
using HtmlAgilityPack;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Security.Cryptography;
using System.Text;
using Humanizer;

namespace HyperBot.Modules
{
    [Group("serverprotect")]
    public class ServerProtectModule : Cog
    {
        public DataContext _context { private get; set; }

        [Command("enable")]
        [RequireUserPermissions(Permissions.ManageMessages)]
        public async Task Enable(CommandContext ctx)
        {
            if (_context.ServerProtectGuilds.Where(sp => sp.Guild == ctx.Guild.Id).Any()) throw new UserError("ServerProtect already enabled for this server");
            var item = new ServerProtectGuild
            {
                Guild = ctx.Guild.Id
            };
            await _context.AddAsync(item);
            await _context.SaveChangesAsync();
            await ctx.RespondAsync(Embeds.Success.WithDescription($"Enabled ServerProtect. HyperBot will scan all new messages in this server and delete any that contain dangerous content."));
        }
        [Command("disable")]
        [RequireUserPermissions(Permissions.ManageMessages)]
        public async Task Disable(CommandContext ctx)
        {
            var item = _context.ServerProtectGuilds.Where(i => i.Guild == ctx.Guild.Id).SingleOrDefault();
            if (item == null) throw new UserError("ServerProtect is already disabled in this server");
            _context.RemoveRange(item);
            await _context.SaveChangesAsync();
            await ctx.RespondAsync(Embeds.Success.WithDescription($"Disabled ServerProtect!"));
        }
        [Command("stats")]
        public async Task Stats(CommandContext ctx)
        {
            var totalIpGrabbers = _context.IPGrabberUrls.Count();
            var totalUnsafeFiles = _context.UnsafeFiles.Count();
            await ctx.RespondAsync(Embeds.Info.AddField("IP Grabber URLs", $"HyperBot has {"IP grabber URLs".ToQuantity(totalIpGrabbers)} in its security dataset", true)
                .AddField("Unsafe Files", $"HyperBot has {"unsafe file hashes".ToQuantity(totalUnsafeFiles)} in its security dataset", true));
        }

        [Command("savehashfromurl")]
        [RequireOwner]
        public async Task SaveHashFromUrl(CommandContext ctx, string url, [RemainingText] string description)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                HttpClient client = new HttpClient();
                HttpResponseMessage response = await client.GetAsync(url);
                var hash = GetHash(sha256Hash, await response.Content.ReadAsByteArrayAsync());
                var alreadyInDb = _context.UnsafeFiles.Where(sp => sp.Hash == hash).SingleOrDefault();
                if (alreadyInDb != null)
                {
                    _context.Remove(alreadyInDb);
                    await _context.AddAsync(new ServerProtectUnsafeFile
                    {
                        Description = description,
                        Hash = hash
                    });
                    await _context.SaveChangesAsync();
                    await ctx.RespondAsync(Embeds.Success.WithDescription($"Updated hash ({hash}) in the database!"));

                }
                else
                {
                    await (_context.AddAsync(new ServerProtectUnsafeFile
                    {
                        Description = description,
                        Hash = hash
                    }));
                    await _context.SaveChangesAsync();
                    await ctx.RespondAsync(Embeds.Success.WithDescription($"Saved hash ({hash}) to the database!"));
                }
            }
        }
        [Command("saveipgrabbers")]
        [RequireOwner]
        public async Task SaveIPGrabbers(CommandContext ctx, [RemainingText] string domainsString)
        {
            var domains = domainsString.Split(" ");
            var skippedDomains = new List<String>();
            foreach (var domain in domains)
            {
                if (_context.IPGrabberUrls.Where(url => url.Domain == domain).Any()) skippedDomains.Add(domain);
                else await _context.AddAsync(new IPGrabberUrl { Domain = domain });
            }
            await _context.SaveChangesAsync();
            if (domains.Length - skippedDomains.Count > 0)
                await ctx.RespondAsync(Embeds.Success.WithDescription($"Saved {"domains".ToQuantity(domains.Length - skippedDomains.Count)} to the database!"));
            if (skippedDomains.Count > 0) await ctx.RespondAsync(
                Embeds.Warning.WithDescription($"Skipped {skippedDomains.Humanize()}, already in database."));

        }

        private static string GetHash(HashAlgorithm hashAlgorithm, byte[] input)
        {

            // Convert the input string to a byte array and compute the hash.
            byte[] data = hashAlgorithm.ComputeHash(input);

            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            var sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data
            // and format each one as a hexadecimal string.
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            // Return the hexadecimal string.
            return sBuilder.ToString();
        }

        private async static IAsyncEnumerable<(Uri, HttpContent)> TraceLink(String link)
        {
            HttpClientHandler httpClientHandler = new HttpClientHandler();
            httpClientHandler.AllowAutoRedirect = false;
            HttpClient client = new HttpClient(httpClientHandler);
            var url = link;
            var redirectCounter = 0;
            while (true)
            {
                if (redirectCounter > 15) yield break;
                HttpResponseMessage response = await client.GetAsync(url);
                var doc = new HtmlDocument();
                doc.LoadHtml(await response.Content.ReadAsStringAsync());
                var linksInPage = doc.DocumentNode.SelectNodes("//a");
                if (linksInPage != null)
                {
                    foreach (var aTag in linksInPage)
                    {
                        if (aTag.Attributes.Contains("href"))
                        {
                            Uri href = null;
                            try
                            {
                                href = new Uri(aTag.Attributes["href"].Value);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e);
                            }
                            if (href != null) yield return (href, null);
                        }
                    }
                }
                yield return (new Uri(url), response.Content);
                if (response.StatusCode == HttpStatusCode.Moved ||
                    response.StatusCode == HttpStatusCode.MovedPermanently ||
                    response.StatusCode == HttpStatusCode.Redirect ||
                    response.StatusCode == HttpStatusCode.RedirectKeepVerb ||
                    response.StatusCode == HttpStatusCode.PermanentRedirect ||
                    response.StatusCode == HttpStatusCode.TemporaryRedirect)
                {
                    url = response.Headers.Location.ToString();
                    redirectCounter++;
                }
                else
                {
                    break;
                }
            }
        }
        private static async Task HandlePossiblyUnsafeFile(DiscordMessage message, HttpContent content, DataContext _context)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                var hash = GetHash(sha256Hash, await content.ReadAsByteArrayAsync());
                var found = _context.UnsafeFiles.Where(sp => sp.Hash == hash).SingleOrDefault();
                if (found != null)
                {
                    try
                    {
                        await message.DeleteAsync();
                    }
                    catch (Exception e) { }
                    await message.Channel.SendMessageAsync(new DiscordMessageBuilder()
                        .WithEmbed(Embeds.Warning.WithTitle("Warning! Unsafe File Detected!")
                        .WithDescription($"The previous (now deleted) message by {message.Author.Mention} contains a link that points to an unsafe file.")
                        .AddField("Description", found.Description)
                        .WithFooter("Protected by ServerProtect")));
                }
            }
            return;
        }

        new public static void OnStart(DiscordClient client, IConfiguration configuration)
        {
            var _context = new DataContext();
            client.MessageCreated += (client, args) =>
            {
                _ = Task.Run(async () =>
                    {
                        var enabledInGuild = _context.ServerProtectGuilds.Where(sp => sp.Guild == args.Guild.Id).Any();
                        if (args.Author.Id == client.CurrentUser.Id) return;
                        if (enabledInGuild)
                        {
                            try
                            {
                                // Begin ServerProtect scans
                                Regex urlParser = new Regex(@"https?:\/\/(www\.)?[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9()@:%_\+.~#?&//=]*)",
                                RegexOptions.Compiled | RegexOptions.IgnoreCase);
                                var urls = urlParser.Matches(args.Message.Content).Select(m => m.Value).ToList();
                                foreach (var embed in args.Message.Embeds)
                                {
                                    if (embed.Image != null)
                                    {
                                        urls.Add(embed.Image.Url.ToString());
                                    }
                                    if (embed.Thumbnail != null)
                                    {
                                        urls.Add(embed.Thumbnail.Url.ToString());
                                    }
                                }
                                foreach (var url in urls)
                                {
                                    var alreadyTriggeredIPGrabberWarning = false;
                                    await foreach ((var trace, var content) in TraceLink(url))
                                    {
                                        var found = _context.IPGrabberUrls.FirstOrDefault(ig => trace.Host == ig.Domain);
                                        if (found != null && !alreadyTriggeredIPGrabberWarning)
                                        {
                                            await args.Channel.SendMessageAsync(new DiscordMessageBuilder()
                                                .WithEmbed(Embeds.Warning.WithTitle("Warning! IP Grabber Link Detected!")
                                                    .WithDescription($"This message contains a link that points to {found.Domain}, a known IP grabber domain.")
                                                    .WithFooter("Protected by ServerProtect"))
                                                .WithReply(args.Message.Id));
                                            alreadyTriggeredIPGrabberWarning = true;
                                        }
                                        if (content != null)
                                        {
                                            await HandlePossiblyUnsafeFile(args.Message, content, _context);
                                        }
                                    }
                                }
                                foreach (var attachment in args.Message.Attachments)
                                {
                                    HttpClient client = new HttpClient();
                                    HttpResponseMessage response = await client.GetAsync(attachment.Url);
                                    await HandlePossiblyUnsafeFile(args.Message, response.Content, _context);
                                }
                            }
                            catch (Exception e)
                            {
                                client.Logger.LogError(e.ToString());
                            }
                        }
                    });
                return Task.CompletedTask;
            };
        }
    }
}