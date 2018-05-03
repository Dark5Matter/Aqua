using Discord;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aqua
{
    public class Config
    {
        // Starboard
        public ulong StarboardID { get; set; }
        public Dictionary<ulong, int> Stars { get; set; }
        public Dictionary<ulong, ulong> StarRef { get; set; }

        // Welcome
        public ulong WelcomeID { get; set; }
        public string WelcomeMessage { get; set; }

        public static void Save(Dictionary<ulong, Config> cfg)
        {
            //File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + @"\Settings\cfg.txt", JsonConvert.SerializeObject(cfg));
            Properties.Settings.Default._config = JsonConvert.SerializeObject(cfg);
            Properties.Settings.Default.Save();
        }

        public static void CheckGuild(Dictionary<ulong, Config> cfg, ulong id)
        {
            if (!cfg.Keys.Contains(id))
                cfg.Add(id, new Config());
        }
    }

    public class BackupMessage
    {
        public ulong Id { get; set; }
        public ulong UserId { get; set; }
        public string Username { get; set; }
        public string Discriminator { get; set; }
        public string AvatarUrl { get; set; }
        public string Content { get; set; }
        public IEmbed Embed { get; set; }
        public DateTimeOffset Timestamp { get; set; }
    }
}
