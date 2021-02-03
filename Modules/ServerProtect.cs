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

namespace HyperBot.Modules
{
    [Group("serverprotect")]
    public class ServerProtectModule : Cog
    {
        public DataContext _context { private get; set; }
        private static String[] ipGrabberURLs = new[] { "grabify.link", "bmwforum.co", "leancoding.co", "spottyfly.com", "stopify.co", "yoütu.be", "discörd.com", "minecräft.com", "freegiftcards.co", "disçordapp.com", "xda-developers.us", "quickmessage.us", "fortnight.space", "fortnitechat.site", "youshouldclick.us", "joinmy.site", "crabrave.pw", "xn--yotu-1ra.be", "xn--disordapp-s3a.com", "xn--minecrft-5za.com", "xn--discrd-zxa.com", "iplogger.org", "2no.co", "iplogger.com", "iplogger.ru", "yip.su", "curiouscat.club", "catsnthings.com", "www.ps3cfw.com", "blasze.tk", "api.grabify.link", "iplis.org", "02ip.ru", "iplogger.co", "iplogger.info", "ipgraber.ru", "lovebird.guru", "trulove.guru", "dateing.club", "otherhalf.life", "shrekis.life", "datasig.io", "datauth.io", "headshot.monster", "gaming-at-my.best", "programing.monster", "screenshare.host", "gamingfun.me", "ipgrabber.ru", "iplist.ru", "ezstat.ru", "yourmy.monster", "imageshare.best", "mypic.icu", "screenshot.best", "grabify.world", "grabify.icu", "progaming.monster", "catsnthing.com", "catsnthings.fun" };

        [Command("enable")]
        [RequirePermissions(Permissions.ManageMessages)]
        public async Task Enable(CommandContext ctx)
        {
            if (_context.ServerProtectGuilds.Where(sp => sp.Guild == ctx.Guild.Id).Any()) throw new UserError("ServerProtect already enabled for this server");
            var item = new ServerProtectGuild
            {
                Guild = ctx.Guild.Id
            };
            await _context.AddAsync(item);
            await _context.SaveChangesAsync();
            await ctx.RespondAsync(Embeds.Success.WithDescription($"Enabled ServerProtect. HyperBot will start monitoring all messages in this server for dangerous content."));
        }
        [Command("disable")]
        public async Task Disable(CommandContext ctx)
        {
            var item = _context.ServerProtectGuilds.Where(i => i.Guild == ctx.Guild.Id).SingleOrDefault();
            if (item == null) throw new UserError("ServerProtect is already disabled in this server");
            _context.RemoveRange(item);
            await _context.SaveChangesAsync();
            await ctx.RespondAsync(Embeds.Success.WithDescription($"Disabled ServerProtect!"));
        }
        private async static Task<List<Uri>> TraceLink(String link)
        {
            HttpClientHandler httpClientHandler = new HttpClientHandler();
            httpClientHandler.AllowAutoRedirect = false;
            HttpClient client = new HttpClient(httpClientHandler);
            var allUrls = new List<Uri>();
            allUrls.Add(new Uri(link));
            var url = link;
            var redirectCounter = 0;
            while (true)
            {
                try
                {
                    if (redirectCounter > 15) return allUrls;
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
                                try
                                {
                                    Console.WriteLine(aTag.Attributes["href"].Value);
                                    allUrls.Add(new Uri(aTag.Attributes["href"].Value));
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine(e);
                                }
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
                        allUrls.Add(response.Headers.Location);
                        url = response.Headers.Location.ToString();
                        redirectCounter++;
                    }
                    else
                    {
                        break;
                    }
                }
                catch (Exception e)
                {
                }
            }
            return allUrls;
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
                            // Begin ServerProtect scans
                            Regex urlParser = new Regex(@"https?:\/\/(www\.)?[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9()@:%_\+.~#?&//=]*)",
                                RegexOptions.Compiled | RegexOptions.IgnoreCase);
                            var urls = urlParser.Matches(args.Message.Content).Select(m => m.Value);
                            foreach (var url in urls)
                            {
                                foreach (var trace in await TraceLink(url))
                                {
                                    var found = ipGrabberURLs.FirstOrDefault(ig => trace.Host == ig);
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