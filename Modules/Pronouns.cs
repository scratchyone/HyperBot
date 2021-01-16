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
using System.Diagnostics;
using NYoutubeDL;


namespace HyperBot.Modules
{
    public class PronounModule : BaseCommandModule
    {
        public DataContext context { private get; set; }

        private string ProvidePronounExample(PronounSet pronounSet)
        {
            var subjectPronoun = pronounSet.Set.Split("/")[0];
            var objectPronoun = pronounSet.Set.Split("/")[1];
            var possessiveDeterminer = pronounSet.Set.Split("/")[2];
            var possessivePronoun = pronounSet.Set.Split("/")[3];
            var reflexivePronoun = pronounSet.Set.Split("/")[4];
            return String.Join(Environment.NewLine, new[] {
                $"This morning, {subjectPronoun} went to the park.",
                $"I went with {objectPronoun}.",
                $"And {subjectPronoun} brought {possessiveDeterminer} frisbee.",
                $"At least I think it was {possessivePronoun}.",
                $"By the end of the day, {subjectPronoun} started throwing the frisbee to {reflexivePronoun}."
            });
        }

        [Command("pronoun"), Aliases("pronouns")]
        public async Task Pronoun(CommandContext ctx, [RemainingText] string pronounSet)
        {
            if (pronounSet == null) throw new UserError("Must supply text as pronoun set");
            var dbPronoun = context.Pronouns.Where(p => p.Set.StartsWith(pronounSet)).FirstOrDefault();
            if (dbPronoun == null)
                if (pronounSet.Split("/").Count() == 5)
                    dbPronoun = new PronounSet
                    {
                        Set = pronounSet
                    };
                else
                    throw new UserError("Pronoun set not found in database. You must supply 5 / seperated pronouns of the form <subject_pronoun>/<object_pronoun>/<possessive_determiner>/<possessive_pronoun>/<reflexive_pronoun>");
            await ctx.RespondAsync(Embeds.Info
                .WithTitle($"Pronoun Example for {String.Join("/", dbPronoun.Set.Split("/").Take(2))}")
                .WithDescription(ProvidePronounExample(dbPronoun)));
        }
        [Command("addpronoun"), Aliases("addpronouns")]
        [Description("Add a pronoun set to the database. Example: h!addpronoun <subject_pronoun>/<object_pronoun>/<possessive_determiner>/<possessive_pronoun>/<reflexive_pronoun>")]
        [RequireOwnerAttribute]
        public async Task AddPronoun(CommandContext ctx, [RemainingText] string pronounSet)
        {
            if (pronounSet == null || pronounSet.Split("/").Count() != 5) throw new UserError("Must supply 5 / seperated pronouns");
            var existingPronoun = context.Pronouns.FirstOrDefault(p => p.Set == pronounSet);
            if (existingPronoun != null) throw new UserError("Pronoun set already exists");

            await context.AddAsync(new PronounSet
            {
                Set = pronounSet
            });
            await context.SaveChangesAsync();
            await ctx.RespondAsync(HyperBot.Embeds.Success.WithDescription("Added pronouns to the database"));
        }
        [Command("removepronoun"), Aliases("removepronouns")]
        [Description("Remove a pronoun set from the database")]
        [RequireOwnerAttribute]
        public async Task RemovePronouns(CommandContext ctx, [RemainingText] string pronounSet)
        {
            if (pronounSet == null) throw new UserError("Must supply text as pronoun set");
            var pronouns = context.Pronouns.Where(p => p.Set.StartsWith(pronounSet));
            var amount = pronouns.Count();
            context.RemoveRange(pronouns);
            await context.SaveChangesAsync();
            await ctx.RespondAsync(HyperBot.Embeds.Success.WithDescription($"Removed {amount} pronouns from the database"));
        }
    }
}