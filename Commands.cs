using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System.Linq;

namespace MovieNightBot
{
    class Commands : BaseCommandModule
    {
        [Command("set")]
        public async Task SetMovie(CommandContext ctx)
        {
            if (Program.verifiedUsers.Contains(ctx.Member.Id))
            {
                var vote = new Vote();
                await vote.SetUp(ctx.Channel, ctx.Member);
                Program.lastVote = vote;
            }
            else
            { await ctx.Channel.SendMessageAsync("Only verified users can use this bot."); }
        }

        [Command("load")]
        public async Task LoadLastVote(CommandContext ctx)
        {
            if (Program.lastVote != null)
            { await Program.lastVote.SetUp(ctx.Channel, ctx.Member); }
            else { await ctx.Channel.SendMessageAsync("No last vote recorded. use mv.set to set a new vote."); }
        }
    }
}
