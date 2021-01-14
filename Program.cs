using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using DSharpPlus;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using System.Linq;
using HyperBot.Data;
using HyperBot.Models;
using HyperBot.Modules;
using Microsoft.EntityFrameworkCore;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using System.Timers;

namespace HyperBot
{
    class Program
    {
        static private IConfiguration Configuration;
        static private DataContext _context;

        static Task<int> PrefixResolver(DiscordMessage message, DiscordUser client)
        {
            var mentionPrefixLength = CommandsNextUtilities.GetMentionPrefixLength(message, client);
            if (mentionPrefixLength != -1) return Task.FromResult(mentionPrefixLength);
            var guildId = message.Channel.GuildId;
            if (guildId == 0) guildId = message.ChannelId;
            var prefixes = _context.Prefixes.Where(prefix => prefix.Guild == guildId)
                .Select(prefix => prefix.PrefixText).OrderByDescending(prefix => prefix.Length).ToList();
            prefixes.Add(Configuration["Prefix"]);
            foreach (var prefix in prefixes)
            {
                var prefixLength = CommandsNextUtilities.GetStringPrefixLength(message, prefix);
                if (prefixLength != -1) return Task.FromResult(prefixLength);
            }
            return Task.FromResult(-1);
        }

        static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            Configuration = builder.Build();
            _context = new DataContext();
            MainAsync().GetAwaiter().GetResult();
        }

        static async void UpdateStatus(DiscordClient client)
        {
            await client.UpdateStatusAsync(new DiscordActivity
            {
                ActivityType = ActivityType.Playing,
                Name = $"h!help | in {client.Guilds.Count} servers"
            });
            client.Logger.LogInformation("Updated status");
        }


        static async Task MainAsync()
        {

            var discord = new DiscordClient(new DiscordConfiguration()
            {
                Token = Configuration["Token"],
                TokenType = TokenType.Bot,
                MinimumLogLevel = LogLevel.Information,
                Intents = DiscordIntents.AllUnprivileged | DiscordIntents.GuildMembers
            });
            var services = new ServiceCollection()
                .AddSingleton<DataContext>()
                .AddSingleton<IConfiguration>(Configuration)
                .BuildServiceProvider();

            var commands = discord.UseCommandsNext(new CommandsNextConfiguration()
            {
                Services = services,
                PrefixResolver = (m) => PrefixResolver(m, discord.CurrentUser)
            });
            commands.RegisterCommands<MyFirstModule>();
            commands.RegisterCommands<PinboardModule>();
            commands.RegisterCommands<PrefixModule>();
            commands.RegisterCommands<PagerModule>();
            commands.RegisterCommands<DownloadModule>();
            PagerModule.OnStart(discord, Configuration);
            commands.CommandErrored += (a, b) => HandleErrors(a, b, discord);
            var aTimer = new System.Timers.Timer(new TimeSpan(0, 30, 0).TotalMilliseconds);
            aTimer.Elapsed += (_, _) => UpdateStatus(discord);
            aTimer.Start();
            discord.Ready += (client, ready) =>
           {
               UpdateStatus(client);
               return Task.CompletedTask;
           };
            discord.UseInteractivity(new InteractivityConfiguration());
            await discord.ConnectAsync();

            await Task.Delay(-1);

        }
        static async Task HandleErrors(CommandsNextExtension ex, CommandErrorEventArgs er, DiscordClient client)
        {
            client.Logger.LogError(er.Exception.ToString());
            if (er.Exception is HyperBot.UserError)
            {
                await er.Context.RespondAsync(embed: Embeds.Error
                    .WithDescription(er.Exception.Message));
            }
            else if (er.Exception is ChecksFailedException)
            {
                foreach (var check in (er.Exception as ChecksFailedException).FailedChecks)
                {
                    if (check is RequireUserPermissionsAttribute)
                    {
                        await er.Context.RespondAsync(embed: Embeds.Error
                            .WithDescription($"You need {(check as RequireUserPermissionsAttribute).Permissions.ToString()} to run that command."));
                    }
                    else if (check is RequireGuildAttribute)
                    {
                        await er.Context.RespondAsync(embed: Embeds.Error
                            .WithDescription($"You can only run that command in a server."));
                    }
                }
            }
            else if (er.Exception is System.ArgumentException)
            {
                await er.Context.RespondAsync(embed: Embeds.Error.WithTitle("Syntax Error").WithDescription($"Run `{er.Context.Prefix}help {er.Command.QualifiedName}` for more information."));
            }
            else if (er.Exception is CommandNotFoundException) { }
            else if (er.Exception is DbUpdateException)
            {
                await er.Context.RespondAsync(embed: Embeds.Error.WithTitle("Database Error").WithDescription($"Something is seriously wrong. This isn't your fault.\n```{er.Exception.Message.Truncate(2000)}```"));
            }
            else
            {
                await er.Context.RespondAsync(embed: Embeds.Error.WithTitle("Unhandled Error")
                    .WithDescription($"Something has gone wrong.\n```{er.Exception.Message.Truncate(2000)}```"));
            }
        }

    }
    public class MyFirstModule : BaseCommandModule
    {
        public DataContext _context { private get; set; }

        [Command("ping")]
        public async Task Ping(CommandContext ctx)
        {
            await ctx.RespondAsync($"Pong! Ping is {ctx.Client.Ping} ms");

        }
        [Command("invite")]
        public async Task Invite(CommandContext ctx)
        {
            await ctx.RespondAsync(ctx.Client.CreateInvite());

        }
    }

}
