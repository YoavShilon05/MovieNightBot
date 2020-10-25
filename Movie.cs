using System;
using System.Collections.Generic;
using System.Text;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System.Threading.Tasks;
using IMDbApiLib;
using IMDbApiLib.Models;
using System.Linq;


namespace MovieNightBot
{

    struct FilmData
    {
        public string genre;
        public string director;
        public string lead;
        public int year;
        public string extraInfo;
        public string awards;
        
        
        public FilmData(string genre, string director, string lead, int year, string extraInfo = "", string awards = "")
        {
            this.genre = genre;
            this.director = director;
            this.lead = lead;
            this.year = year;
            this.extraInfo = extraInfo;
            this.awards = awards;
        }

        
    }

    class Movie
    {
        public string name;
        public DiscordEmoji emoji;
        public FilmData info;

        public Movie(string name, DiscordEmoji emoji)
        {
            this.name = name;
            this.emoji = emoji;
        }

        public void SetFilmInfo(string genre, string director, string lead, int year, string extraInfo = "", string awards = "")
        {
            info = new FilmData(genre, director, lead, year, extraInfo, awards);
        }
    }

    struct Attendence
    {
        public DiscordUser[] arriving;
        public DiscordUser[] notArriving;
        public DiscordUser[] unsure;
        
        public Attendence(DiscordUser[] arriving, DiscordUser[] notArriving, DiscordUser[] unsure)
        {
            this.arriving = arriving;
            this.notArriving = notArriving;
            this.unsure = unsure;
        }
    }

    class Vote
    {
        List<Movie> movies;
        DateTime date;
        DateTime voteConclusionDate;
        DateTime attendenceDate;

        string movieNightRole;
        DiscordChannel sendToChannel;

        DiscordMessage attendenceMessage;
        Attendence attendence;

        DiscordMessage voteMessage;

        Movie winner;

        public async Task SetUp(DiscordChannel channel, DiscordMember member)
        {
            movieNightRole = "<@&" + await IO.GetStringFeedback(channel, "Start out by by entering the id of the @Movie night role.", member) + ">";
            string channelMention = await IO.GetStringFeedback(channel, "And tag channel you want to send the messages to.", member);
            sendToChannel = await Program.bot.client.GetChannelAsync(ulong.Parse(channelMention.Substring(2, channelMention.Length - 3)));
            date = await GetDate(channel, member, "When will the movie be?");
            voteConclusionDate = await GetRelativeDate(channel, member, date, "How many hours before the movie do you want to conclude the Vote?", true);
            attendenceDate = await GetRelativeDate(channel, member, voteConclusionDate, "How many hours before the vote conclusion do you want to send the 'whos coming' pole?", true);

            await GetMovieList(channel, member);

            await StartAwaitingProcess(sendToChannel, channel , member);
        }

        public async Task StartAwaitingProcess(DiscordChannel channel, DiscordChannel dmchannel, DiscordMember member, int extraTime = 10)
        {
            voteMessage = await SendMessage(channel);

            async Task WaitUntil(double ms)
            {
                if (ms - DateTime.Now.TimeOfDay.TotalMilliseconds > 0)
                {
                    await Task.Delay((int)(ms - DateTime.Now.TimeOfDay.TotalMilliseconds));
                }
                else
                { 
                    await Task.Delay(extraTime * 1000);
                }
            }
            
            double reminderMs = attendenceDate.TimeOfDay.TotalMilliseconds;
            double voteConclusionMs = voteConclusionDate.TimeOfDay.TotalMilliseconds;
            double dateMs = date.TimeOfDay.TotalMilliseconds;

            await WaitUntil(reminderMs);
            await SendAttendencePole(channel);
            await WaitUntil(voteConclusionMs);
            attendence = await GetAttendenceResults();
            winner = await AskVoteConclusion(dmchannel, member);
            await ConcludeVote(channel);
            await WaitUntil(dateMs);
            await StartMovie(channel);
        }

        Func<int, bool> GetMaxNumberCheck(int maxNum)
        {
            bool Result(int objInput)
            {
                int input = (int)objInput;
                return input <= maxNum && input > 0;
            }
            return Result;
        }

        private async Task GetMovieList(DiscordChannel channel, DiscordMember member)
        {
            if (movies == null) movies = new List<Movie>();

            async Task<Movie> NewMovie()
            {
                var movieData = await IMDB.SearchMovie(await IO.GetStringFeedback(channel, "Movie search?", member));
                Movie movie = new Movie(
                    movieData.Title, await IO.GetReactionFeedback(channel, "React on this message the movie emoji.", member)
                );
                
                var cast = await IMDB.GetMovieDirAndLead(movieData);
                movie.SetFilmInfo(await IO.GetStringFeedback(channel, "Movie Genre?", member), cast[0], cast[1], await IMDB.GetMovieYear(movieData));

                return movie;
            }

            async Task EditMovie(Movie movie)
            {
                Dictionary<DiscordEmoji, string> actions = new Dictionary<DiscordEmoji, string> {
                { DiscordEmoji.FromName(Program.bot.client, ":banana:"), "emoji"}, //Edit Emoji
                { DiscordEmoji.FromName(Program.bot.client, ":movie_camera:"), "director"}, //Edit Director
                { DiscordEmoji.FromName(Program.bot.client, ":sunglasses:"), "lead"}, //Edit Lead
                { DiscordEmoji.FromName(Program.bot.client, ":cowboy:"), "genre"}, //Edit Genre
                { DiscordEmoji.FromName(Program.bot.client, ":grey_question:"), "extra info"}, //Edit Extra Info
                { DiscordEmoji.FromName(Program.bot.client, ":star:"), "awards" }, //Edit Awards
                { DiscordEmoji.FromName(Program.bot.client, ":clock130:"), "year" }, //Edit Year
                { DiscordEmoji.FromName(Program.bot.client, ":arrow_left:"), "back" } //Go backs
                };

                while (true)
                {
                    string action = await IO.GetPoleFeedback(channel, "Select the movie field you want to edit", actions, member);
                
                    switch (action)
                    {
                        case "emoji":
                            var newEmojiMsg = await channel.SendMessageAsync("React on this message the new emoji");
                            var newEmoji = await Program.bot.interactivity.WaitForReactionAsync((MessageReactionAddEventArgs r) =>
                                                                                    r.Message.Id == newEmojiMsg.Id && r.User.Id == member.Id);
                            movie.emoji = newEmoji.Result.Emoji;
                            break;

                        case "director":
                            movie.info.director = await IO.GetStringFeedback(channel, "Who is the director of the movie?", member);
                            break;

                        case "lead":
                            movie.info.lead = await IO.GetStringFeedback(channel, "Who is the lead actor of the movie?", member);
                            break;

                        case "genre":
                            movie.info.genre = await IO.GetStringFeedback(channel, "What is the genre of the movie?", member);
                            break;

                        case "extra_info":
                            movie.info.extraInfo = await IO.GetStringFeedback(channel, "Any extra info on the movie?", member);
                            break;

                        case "awards":
                            movie.info.awards = await IO.GetStringFeedback(channel, "Special awards the movie got?", member);
                            break;

                        case "year":
                            movie.info.year = await IO.GetIntFeedback(channel, "At what year did the movie come out?", member);
                            break;

                        case "back":
                            return;
                    }
                }
            };
            

            Dictionary<DiscordEmoji, string> actions = new Dictionary<DiscordEmoji, string> {
                { DiscordEmoji.FromName(Program.bot.client, ":movie_camera:"), "add movie" }, //Add Movie
                { DiscordEmoji.FromName(Program.bot.client, ":x:"), "remove movie"}, //Remove Movie
                { DiscordEmoji.FromName(Program.bot.client, ":wrench:"), "edit movie"}, //Edit Movie
                { DiscordEmoji.FromName(Program.bot.client, ":white_check_mark:"), "done"}, //Finish
            };

            while (true)
            {
                string action = await IO.GetPoleFeedback(channel, "current movie list : ", actions, member, GetMessageEmbed(movies.ToArray()));

                switch (action)
                {
                    case "add movie":
                        movies.Add(await NewMovie());
                        break;

                    case "remove movie":
                        movies.Remove(await IO.GetMovieFeedback(channel, "Select the movie you want to remove", movies.ToArray(), member));
                        break;

                    case "edit movie":
                        await EditMovie(await IO.GetMovieFeedback(channel, "Select the movie you want to edit", movies.ToArray(), member));
                        break;

                    case "done":
                        return;

                }
            }            
        }

        private async Task<DateTime> GetDate(DiscordChannel channel, DiscordMember member, string message)
        {
            await channel.SendMessageAsync(message);
            int month = await IO.GetIntFeedbackUntil(channel, "month?", GetMaxNumberCheck(12), "answer was not valid", member);
            int day = await IO.GetIntFeedbackUntil(channel, "day? (input as a number)", GetMaxNumberCheck(31), "answer was not valid", member);
            int hour = await IO.GetIntFeedbackUntil(channel, "hour? (1 - 1am to 24 - 12 am)", GetMaxNumberCheck(24), "answer was not valid", member);

            if (hour == 24) hour = 0;

            DateTime result = new DateTime(
                DateTime.Now.Year, month, day, hour, 0, 0
            );

            return result;
        }

        private async Task<DateTime> GetRelativeDate(DiscordChannel channel, DiscordMember member, DateTime other, string message, bool negative)
        {
            int hours = await IO.GetIntFeedbackUntil(channel, message, GetMaxNumberCheck(24), "number was invalid", member);

            if (negative) hours *= -1;

            DateTime result = new DateTimeOffset(
                other, new TimeSpan(hours, 0, 0)
            ).Date;

            return result;
        }

        private DiscordEmbedBuilder GetMessageEmbed(Movie[] movies)
        {
            var embed = new DiscordEmbedBuilder
            {
                Title = $"\nThe options for {date.DayOfWeek}({date.Day}.{date.Month}) at {date.Hour} are",
                Description = $"Vote Concludes at {date.Hour}:00 {date.Day}.{date.Month}",
                Footer = new DiscordEmbedBuilder.EmbedFooter { Text = "(Please only vote up to 3 times.)" }
            };

            foreach (Movie movie in movies)
            {
                string embedMessageContent = $"{movie.info.genre}               Ft.  (Dir. {movie.info.director}, Lead {movie.info.lead})";
                if (movie.info.extraInfo != "") embedMessageContent += $" ({movie.info.extraInfo})";
                if (movie.info.awards != "") embedMessageContent += $" **{movie.info.awards}**";

                embed.AddField($"React with    {movie.emoji.Name}    for      **\"{movie.name}\"**       ({movie.info.year})",
                    embedMessageContent, false);
                
            }

            return embed;
        }

        public async Task<DiscordMessage> SendMessage(DiscordChannel channel)
        {
            var msg = await channel.SendMessageAsync(movieNightRole, false, GetMessageEmbed(movies.ToArray()));
            foreach (Movie movie in movies) await msg.CreateReactionAsync(movie.emoji);
            return msg;
        }

        public async Task SendAttendencePole(DiscordChannel channel)
        {
            attendenceMessage = await channel.SendMessageAsync($"**{movieNightRole}**\nAre you coming to today's Movie Night? (if the film you voted for had been chosen)\n" +
                $"(if you choose yes, your vote will be counted towards today's vote)\n" +
                $":grey_question: means \"not sure\"");

            await attendenceMessage.CreateReactionAsync(DiscordEmoji.FromName(Program.bot.client, ":white_check_mark:"));
            await attendenceMessage.CreateReactionAsync(DiscordEmoji.FromName(Program.bot.client, ":x:"));
            await attendenceMessage.CreateReactionAsync(DiscordEmoji.FromName(Program.bot.client, ":grey_question:"));
        }

        public async Task<Attendence> GetAttendenceResults()
        {
            List<DiscordUser> arriving = new List<DiscordUser>();
            List<DiscordUser> notArriving = new List<DiscordUser>();
            List<DiscordUser> unsure = new List<DiscordUser>();
            
            foreach (DiscordUser u in await attendenceMessage.GetReactionsAsync(DiscordEmoji.FromName(Program.bot.client, ":white_check_mark:"))) arriving.Add(u);
            foreach (DiscordUser u in await attendenceMessage.GetReactionsAsync(DiscordEmoji.FromName(Program.bot.client, ":x:"))) notArriving.Add(u);
            foreach (DiscordUser u in await attendenceMessage.GetReactionsAsync(DiscordEmoji.FromName(Program.bot.client, ":grey_question:"))) unsure.Add(u);


            Attendence result = new Attendence(
                arriving.ToArray(), notArriving.ToArray(), unsure.ToArray()
            );
            return result;
        }

        public async Task<Movie> AskVoteConclusion(DiscordChannel channel, DiscordMember member)
        {
            DiscordEmbedBuilder embed = new DiscordEmbedBuilder();

            List<DiscordUser> coming = new List<DiscordUser> { };
            List<DiscordUser> notComing = new List<DiscordUser> { };
            List<DiscordUser> unsure = new List<DiscordUser> { };
            List<string> comingStr = new List<string> { };
            List<string> notComingStr = new List<string> { };
            List<string> unsureStr = new List<string> { };

            foreach (DiscordUser u in attendence.arriving) if (u.Id != 745805052400238603) { coming.Add(u); comingStr.Add(u.Username); }
            foreach (DiscordUser u in attendence.notArriving) if (u.Id != 745805052400238603) { notComing.Add(u); notComingStr.Add(u.Username); }
            foreach (DiscordUser u in attendence.unsure) if (u.Id != 745805052400238603) { unsure.Add(u); unsureStr.Add(u.Username); }


            foreach (Movie movie in movies) 
            {
                var usersVoted = await voteMessage.GetReactionsAsync(movie.emoji);
                int votes = 0;
                int unsureVotes = 0;
                foreach (DiscordUser u in usersVoted)
                { if (coming.Contains(u)) votes++; else if (unsure.Contains(u)) unsureVotes++; }
                embed.AddField(movie.name, $"got {votes.ToString()} votes from people that are coming, {unsureVotes} from people that are unsure.");
            }

            embed.AddField("Arriving : ", string.Join(", ", comingStr));
            embed.AddField("Not Arriving : ", string.Join(", ", notComingStr));
            embed.AddField("Unsure : ", string.Join(", ", unsureStr));


            List<DiscordEmoji> movieEmojis = new List<DiscordEmoji>();
            var msg = await channel.SendMessageAsync($"{member.Mention}, select the movie you want to win.", false, embed);
            foreach (Movie movie in movies) { await msg.CreateReactionAsync(movie.emoji); movieEmojis.Add(movie.emoji); }

            var winnerReaction = await Program.bot.interactivity.WaitForReactionAsync((MessageReactionAddEventArgs r) => (movieEmojis.Contains(r.Emoji) && r.User.Id == member.Id && r.Message.Id == msg.Id));

            await channel.SendMessageAsync("Enjoy the movie!");

            foreach (Movie movie in movies)
            { if (movie.emoji == winnerReaction.Result.Emoji) return movie; }
            throw new Exception("Movie was chosen but no matching reaction was found in movies list.");

        }

        public async Task ConcludeVote(DiscordChannel channel)
        {
            await channel.SendMessageAsync($"{movieNightRole}\n\"{winner.name}\"  {winner.info.genre}  Ft.   (Dir.{winner.info.director}, " +
                $"Lead {winner.info.lead}) won tonight!\nTune in at {date.TimeOfDay.Hours.ToString()}:00 to watch with us on Movie Night.");
        }

        public async Task StartMovie(DiscordChannel channel)
        {
            await channel.SendMessageAsync($"Yo {movieNightRole}, Movie is starting!\nCome over.");
        }
    }

    static class IMDB
    {
        static ApiLib imdb;
        public static void SetUp() { imdb = new ApiLib("k_zp5m38ke"); }
        public static async Task<SearchResult> SearchMovie(string title)
        {
            var movieSearch = await imdb.SearchMovieAsync(title);
            var movie = movieSearch.Results[0];
            return movie;
        }

        public static async Task<string[]> GetMovieDirAndLead(SearchResult movie)
        {
            var cast = await imdb.FullCastDataAsync(movie.Id);
            string dir = cast.Directors.Items[0].Name;
            string lead = cast.Actors[0].Name;
            return new string[2] { dir, lead };
        }

        /*public static async Task<string> GetMovieGenre(SearchResult movie)
        {
            var exsites = await imdb.ExternalSitesAsync(movie.Id);
            Console.WriteLine(exsites);
            return "test genre";
        }*/

        public static async Task<int> GetMovieYear(SearchResult movie)
        {
            var exsites = await imdb.ExternalSitesAsync(movie.Id);
            return int.Parse(exsites.Year);
            
        }
    }
}
