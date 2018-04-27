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

        [Command("wordstats", RunMode = RunMode.Async)]
        [Alias("ws")]
        [Summary("Lists the most frequently used words in this channel or the selected channel, or lists the most frequently used words by the user, if specified.")]
        public async Task WordStats(int messageCount = 100, IMessageChannel inputChannel = null, IGuildUser user = null)
        {
            // Return if larger than 500
            if (Context.User.Id != 210150851606609921)
                if (messageCount > 500)
                { await ReplyAsync("The message count has to be less than 500"); return; }

            // Trigger typing
            await Context.Channel.TriggerTypingAsync();

            // Define the list of words
            Dictionary<string, int> words = new Dictionary<string, int>(); // Dictionary<word, count>

            // Define the list of messages
            List<IMessage> messages = new List<IMessage>();

            // Specify channel
            IMessageChannel channel;
            channel = (inputChannel == null) ?
                    Context.Channel :
                    inputChannel;

            // Grab messages
            try
            {
                messages = (user == null) ?
                    (await channel.GetMessagesAsync(messageCount).Flatten().ToList()) :
                    (await channel.GetMessagesAsync(messageCount).Flatten().Where(x => x.Author.Id == user.Id).ToList());
            }
            catch { await ReplyAsync("Sorry! I don't have access to the specified channel."); return; }

            // Count words
            foreach (var message in messages)
            {
                // Split each message into words
                foreach (var word in message.Content.Split(' '))
                {
                    // Skip words that start with symbols/numbers such as :word: .word etc..
                    try
                    {
                        if (!char.IsLetter(word[0]))
                            continue;
                    }
                    catch { continue; }

                    // Skip words that are shorter than 3
                    if (word.Length < 3)
                        continue;

                    // Skip most common words up until "over"
                    if (new List<string>() { "the", "and", "that", "have", "for", "not", "with", "you", "this", "but", "his", "from", "they", "say", "her", "she", "will", "one", "all", "would", "there", "their", "what", "out", "about", "who", "get", "which", "when", "make", "can", "like", "time", "just", "him", "know", "take", "people", "into", "year", "your", "good", "some", "could", "them", "see", "other", "than", "then", "now", "look", "only", "come", "its", "over", /*mine*/ "was", "how", "it's", "i'm", "too", "are" }.Contains(word.ToLower())) continue;

                    // Increment by 1 if it already exists, or add it to the list if it doesn't
                    if (words.Keys.Contains(word.ToLower()))
                        words[word.ToLower()]++;
                    else
                        words.Add(word.ToLower(), 1);
                }
            }

            // Order by count
            var temp = words.ToList();
            temp.Sort((x, y) => y.Value.CompareTo(x.Value));

            // Make the embed
            var e = new EmbedBuilder()
            {
                Color = Program.embedColor,
                Title = "Word Stats",
                Description = string.Format("Here are the most frequently used words{0}in {1}:",
                user == null ? " " : " by " + user.Mention + " ",
                "#" + channel.Name
                )
            };

            // Add the fields to the embed
            for (int i = 0; i < 9; i++)
            {
                if (temp.Count - 1 < i)
                    continue;
                e.AddField(temp[i].Key, temp[i].Value, true);
            }

            await ReplyAsync(string.Empty, embed: e.Build());
        }

        [Command("say", RunMode = RunMode.Async)]
        [Summary("Sends a message in this channel.")]
        public async Task Say([Remainder] string content)
        {
            await ReplyAsync(content);
        }
    }
}
