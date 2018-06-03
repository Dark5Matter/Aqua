using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using System.Diagnostics;
using Discord.WebSocket;

namespace Aqua
{
    public class MessagesModule : ModuleBase
    {
        [Command("quote")]
        [Alias("q")]
        [Summary("Quotes a message from this channel, or a different channel if one is specified.")]
        public async Task Qu(ulong id, IMessageChannel ch = null)
        {
            #region Grab messages
            IMessage m;
            if (ch == null)
                m = await Context.Channel.GetMessageAsync(id);
            else
                m = await ch.GetMessageAsync(id);
            #endregion

            if (m == null)
            { await ReplyAsync("Sorry, I couldn't find the specified message."); return; }

            await Context.Message.DeleteAsync();

            var eb = BuildQuote(m);
            if (eb == null)
            { await ReplyAsync("Sorry, this message cannot be quoted."); return; }

            await Context.Channel.SendMessageAsync(Context.User.Mention + " quoted the following message:",
                embed: eb.Build());
        }
        #region BuildQuote
        private EmbedBuilder BuildQuote(IMessage message)
        {
            var eb = new EmbedBuilder()
                .WithAuthor($"{message.Author} @ #{message.Channel.Name}", message.Author.GetAvatarUrl())
                .WithDescription(message.Content)
                .WithColor(Program.embedColor)
                .WithTimestamp(message.Timestamp);

            var attachment = message.Attachments.FirstOrDefault();
            var embed = message.Embeds.FirstOrDefault();

            if (attachment != null && attachment.Width.HasValue && attachment.Width.Value != 0)
                eb.WithImageUrl(attachment.Url);

            else if (embed != null)
            {
                if (embed.Image.HasValue)
                    eb.WithImageUrl(embed.Image.Value.Url);

                else if (!string.IsNullOrWhiteSpace(embed.Url)/* && Globals.ImageFormats.Any(embed.Url.EndsWith)*/)
                    eb.WithImageUrl(embed.Url);

                else if (embed.Thumbnail.HasValue)
                    eb.WithImageUrl(embed.Thumbnail.Value.Url);
            }

            if (string.IsNullOrWhiteSpace(message.Content) && string.IsNullOrWhiteSpace(eb.Description) && string.IsNullOrWhiteSpace(eb.ImageUrl))
                return null;

            return eb;
        }
        #endregion

        [Command("google")]
        [Alias("g")]
        [Summary("Returns the first 5 results from google.")]
        public async Task Google([Remainder] string query)
        {
            var results = await GoogleAPI.GoogleAsync(query);
            if (results.Items == null || results.SearchInformation.TotalResults == "0")
            { await ReplyAsync($"Sorry, no results were found for \"{query}\"."); return; }

            var eb = BuildGoogle(results, query);
            if (eb == null)
                await ReplyAsync("Sorry, I couldn't post the results.");
            else
                await ReplyAsync(string.Empty, embed: eb.Build());
        }
        #region BuildGoogle
        private EmbedBuilder BuildGoogle(GResults results, string query)
        {
            var eb = new EmbedBuilder()
                .WithColor(Program.embedColor)
                .WithTitle($"Google search - \"{query}\"")
                .WithUrl("https://www.google.com/search?q=" + System.Net.WebUtility.UrlEncode(query))
                .WithDescription("")
                .WithFooter(new EmbedFooterBuilder()
                    .WithText($"Search time: {results.SearchInformation.SearchTime.ToString()}s"));

            foreach (var item in results.Items.Take(5))
                eb.Description +=
                    $"[{item.Title}]({item.Link})\n{item.Snippet}\n\n";

            return eb;
        }
        #endregion

        [Command("googleimages")]
        [Alias("gi")]
        [Summary("Returns a random image result from google. (Picks a random image from the top 10 results)")]
        public async Task GoogleImages([Remainder] string query)
        {
            var results = await GoogleAPI.GoogleAsync(query, "&searchType=image");
            if (results.Items == null || results.SearchInformation.TotalResults == "0")
            { await ReplyAsync($"Sorry, no results were found for \"{query}\"."); return; }

            var eb = BuildGoogleImages(results, query);
            if (eb == null)
                await ReplyAsync("Sorry, I couldn't post the results.");
            else
                await ReplyAsync(string.Empty, embed: eb.Build());
        }
        #region BuildGoogleImages
        private EmbedBuilder BuildGoogleImages(GResults results, string query)
        {
            var list = results.Items.Take(10).ToList();
            var eb = new EmbedBuilder()
                .WithColor(Program.embedColor)
                .WithTitle($"Google image search - \"{query}\"")
                .WithUrl("https://www.google.com/search?q=" + System.Net.WebUtility.UrlEncode(query))
                .WithImageUrl(list[new Random().Next(0, list.Count)].Link)
                .WithFooter(new EmbedFooterBuilder()
                    .WithText($"Search time: {results.SearchInformation.SearchTime.ToString()}s"));

            return eb;
        }
        #endregion

        [Command("say", RunMode = RunMode.Async)]
        [Summary("Sends a message in this channel.")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task Say([Remainder] string content)
        {
            await ReplyAsync(content);
        }

        [RequireUserPermission(GuildPermission.Administrator)]
        [Command("prune")]
        [Summary("Prunes the last x messages in this channel.")]
        public async Task Prune(int amount)
        {
            #region Return if not bot owner
            if (Context.User.Id != 210150851606609921)
            {
                await Context.Channel.SendMessageAsync("", embed: new EmbedBuilder()
                {
                    Description = "Insufficient permissions",
                    Color = new Color(255, 0, 0)
                }.Build());
                return;
            }
            #endregion

            await (Context.Channel as ITextChannel).DeleteMessagesAsync(await Context.Channel.GetMessagesAsync(amount + 1).Flatten().ToList());
        }

        [RequireUserPermission(GuildPermission.Administrator)]
        [Command("count")]
        [Summary("Counts the messages between the last message sent in this channel and the specified message.")]
        public async Task Count(ulong messageid, int limit = 5000)
        {
            int count = 0;
            var messages = (await Context.Channel.GetMessagesAsync(limit).FlattenAsync()).ToList();

            foreach (var msg in messages)
            {
                count++;
                if (msg.Id == messageid)
                    break;
            }

            await Context.Channel.SendMessageAsync(string.Empty, embed:
                new EmbedBuilder()
                {
                    Color = Program.embedColor,
                    Title = "Count Result",
                    Description = $"There are {count} messages in the way. :^)"
                }.Build());
        }

        [Command("grabpfp")]
        [Summary("Grabs the profile picture link for the provided user.")]
        public async Task GrabPfp(ulong userid, int size = 512)
        {
            using (Discord.Rest.DiscordRestClient restC = new Discord.Rest.DiscordRestClient())
            {
                await restC.LoginAsync(TokenType.Bot, Properties.Settings.Default._key);

                var user = await restC.GetUserAsync(userid);

                await ReplyAsync(user.GetAvatarUrl().Replace("?size=128", "?size=" + size.ToString()));
            }
        }
    }
}
