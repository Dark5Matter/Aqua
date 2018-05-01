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

            await Context.Message.DeleteAsync();
            await Context.Channel.SendMessageAsync(Context.User.Mention + " quoted the following message:", embed: new EmbedBuilder()
            {
                Color = Program.embedColor,
                Author = new EmbedAuthorBuilder()
                {
                    IconUrl = m.Author.GetAvatarUrl(),
                    Name = $"{m.Author.Username}#{m.Author.Discriminator} @ #{m.Channel.Name}"
                },
                Description = m.Content,
                Timestamp = m.Timestamp
            }.Build());
        }

        [Command("google")]
        [Alias("g")]
        [Summary("Returns the first result from google.")]
        public async Task Google([Remainder] string query)
        {
            var results = await GoogleAPI.GoogleAsync(query);
            if (results.Items == null || results.SearchInformation.TotalResults == "0")
            { await ReplyAsync($"Sorry, no results were found for \"{query}\"."); return; }

            await ReplyAsync(string.Empty, embed: new EmbedBuilder()
            {
                Color = Program.embedColor,
                Title = $"Google search - {query}",
                Url = "https://www.google.com/search?q=" + System.Net.WebUtility.UrlEncode(query),
                Description = $"**{results.Items[0].Title}**\n{results.Items[0].Link}\n\n{results.Items[0].Snippet}",
                Footer = new EmbedFooterBuilder()
                {
                    Text = $"Search time: {results.SearchInformation.SearchTime.ToString()}s"
                }
            }.Build());
        }

        [Command("say", RunMode = RunMode.Async)]
        [Summary("Sends a message in this channel.")]
        public async Task Say([Remainder] string content)
        {
            await ReplyAsync(content);
        }
    }
}
