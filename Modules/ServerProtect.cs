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
        private async static IAsyncEnumerable<Uri> TraceLink(String link)
        {
            HttpClientHandler httpClientHandler = new HttpClientHandler();
            httpClientHandler.AllowAutoRedirect = false;
            HttpClient client = new HttpClient(httpClientHandler);
            yield return new Uri(link);
            var url = link;
            var redirectCounter = 0;
            while (true)
            {
                if (redirectCounter > 15) yield break;
                HttpResponseMessage response = await client.GetAsync(url);
                Console.WriteLine(response.StatusCode);
                var doc = new HtmlDocument();
                doc.LoadHtml(await response.Content.ReadAsStringAsync());
                //Console.WriteLine(await response.Content.ReadAsStringAsync());
                var linksInPage = doc.DocumentNode.SelectNodes("//a");
                Console.WriteLine(linksInPage);
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
                            if (href != null) yield return href;
                        }
                    }
                }
                if (response.StatusCode == HttpStatusCode.Moved ||
                    response.StatusCode == HttpStatusCode.MovedPermanently ||
                    response.StatusCode == HttpStatusCode.Redirect ||
                    response.StatusCode == HttpStatusCode.RedirectKeepVerb ||
                    response.StatusCode == HttpStatusCode.PermanentRedirect ||
                    response.StatusCode == HttpStatusCode.TemporaryRedirect)
                {
                    yield return response.Headers.Location;
                    url = response.Headers.Location.ToString();
                    redirectCounter++;
                }
                else
                {
                    break;
                }
            }
        }

        new public static void OnStart(DiscordClient client, IConfiguration configuration)
        {
            var serverProtectData = JsonSerializer.Deserialize<ServerProtectData>(File.ReadAllText("serverprotect_data.json"));
            client.Logger.LogInformation($"Loaded {serverProtectData.IPGrabberURLs.Length} IP grabber urls and {serverProtectData.UnsafeFiles.Length} unsafe files");
            var _context = new DataContext();
            client.MessageCreated += (client, args) =>
            {
                _ = Task.Run(async () =>
                    {
                        var enabledInGuild = _context.ServerProtectGuilds.Where(sp => sp.Guild == args.Guild.Id).Any();
                        if (args.Author.Id == client.CurrentUser.Id) return;
                        if (enabledInGuild)
                        {
                            // Begin ServerProtect scans
                            Regex urlParser = new Regex(@"https?:\/\/(www\.)?[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9()@:%_\+.~#?&//=]*)",
                                RegexOptions.Compiled | RegexOptions.IgnoreCase);
                            var urls = urlParser.Matches(args.Message.Content).Select(m => m.Value);
                            foreach (var url in urls)
                            {
                                await foreach (var trace in TraceLink(url))
                                {
                                    var found = serverProtectData.IPGrabberURLs.FirstOrDefault(ig => trace.Host == ig);
                                    if (found != null)
                                    {
                                        await args.Channel.SendMessageAsync(new DiscordMessageBuilder()
                                            .WithEmbed(Embeds.Warning.WithTitle("Warning! IP Grabber Link Detected!")
                                                .WithDescription($"This message contains a link that points to {found}, a known IP grabber domain."))
                                            .WithReply(args.Message.Id));
                                        break;
                                    }
                                }
                            }
                        }
                    });
                return Task.CompletedTask;
            };
        }
    }
}