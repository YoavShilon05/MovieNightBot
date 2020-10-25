using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using System.Linq;

namespace MovieNightBot
{
    static class IO
    {

        public static async Task<DiscordEmoji> GetReactionFeedback(DiscordChannel channel, string message, DiscordMember member = null)
        {
            DiscordMessage msg = await channel.SendMessageAsync(message);
            var reaction = await Program.bot.interactivity.WaitForReactionAsync((MessageReactionAddEventArgs r) => r.Message.Id == msg.Id && !r.User.IsBot && (r.User.Id == member.Id || member == null));
            return reaction.Result.Emoji;
        }

        public static async Task<string> GetStringFeedback(DiscordChannel channel, string message, DiscordMember member=null)
        {
            DiscordMessage msg = await channel.SendMessageAsync(message);

            var result = await Program.bot.interactivity.WaitForMessageAsync((DiscordMessage m) => !m.Author.IsBot && (m.Author.Id == member.Id || member == null));

            return result.Result.Content;
        }

        public static async Task<int> GetIntFeedback(DiscordChannel channel, string message, DiscordMember member=null)
        {
            DiscordMessage msg = await channel.SendMessageAsync(message);

            int result = 0;

            await Program.bot.interactivity.WaitForMessageAsync((DiscordMessage m) => int.TryParse(m.Content, out result) && !m.Author.IsBot && (m.Author.Id == member.Id || member == null));

            return result;
        }

        public static async Task<bool> GetBoolFeedback(DiscordChannel channel, string message, DiscordMember member=null)
        {
            DiscordMessage msg = await channel.SendMessageAsync(message);
            await msg.CreateReactionAsync(DiscordEmoji.FromName(Program.bot.client, ":white_check_mark:"));
            await msg.CreateReactionAsync(DiscordEmoji.FromName(Program.bot.client, ":x:"));

            var result = await Program.bot.interactivity.WaitForReactionAsync((MessageReactionAddEventArgs e) => (new string[] { ":x:", ":white_check_mark:" }.Contains(e.Emoji.GetDiscordName()))
                                                                                                && e.Message.Id == msg.Id && !e.User.IsBot && (e.User.Id == member.Id || member == null));
            Console.WriteLine(result.Result.Emoji.GetDiscordName());
            return result.Result.Emoji.GetDiscordName() == ":white_check_mark:";
        }

        public static async Task<int> GetIntFeedbackUntil(DiscordChannel channel, string message, Func<int, bool> check, string errorMessage, DiscordMember member=null)
        {
            int result;
            while (true)
            {
                result = await GetIntFeedback(channel, message, member);
                bool success = check(result);
                if (success) break;
                else await channel.SendMessageAsync(errorMessage);
            }
            return result;
        }
        
        public static async Task<Movie> GetMovieFeedback(DiscordChannel channel, string message, Movie[] movies, DiscordMember member=null)
        {
            var msg = await channel.SendMessageAsync(message);
            Dictionary<DiscordEmoji, Movie> movieEmojis = new Dictionary<DiscordEmoji, Movie> { };
            foreach (Movie movie in movies) { await msg.CreateReactionAsync(movie.emoji); movieEmojis.Add(movie.emoji, movie); }
            var movieReaction = await Program.bot.interactivity.WaitForReactionAsync((MessageReactionAddEventArgs r) => movieEmojis.Keys.Contains(r.Emoji)
                                                                            && r.Message.Id == msg.Id && r.User.Id == member.Id);
            return movieEmojis[movieReaction.Result.Emoji];
        }
    
        public static async Task<string> GetPoleFeedback(DiscordChannel channel, string message, Dictionary<DiscordEmoji, string> actions, DiscordMember member=null, DiscordEmbedBuilder embed=null)
        {
            if (embed == null) embed = new DiscordEmbedBuilder();
            
            foreach(DiscordEmoji e in actions.Keys) embed.AddField(e.Name, actions[e], true); 

            var msg = await channel.SendMessageAsync(message, false, embed);
            foreach (DiscordEmoji emoji in actions.Keys) { await msg.CreateReactionAsync(emoji); }

            var action = await Program.bot.interactivity.WaitForReactionAsync((MessageReactionAddEventArgs r) => actions.Keys.Contains(r.Emoji)
                                                                        && r.Message.Id == msg.Id && r.User.Id == member.Id);
            return actions[action.Result.Emoji];            
        }
    }
}
