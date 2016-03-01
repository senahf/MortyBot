using Discord;
using System;
using System.IO;
using Newtonsoft.Json;
using Discord.Commands;
using NadekoBot.Modules;
using Discord.Modules;
using Discord.Audio;
using NadekoBot.Extensions;
using System.Timers;
using System.Linq;
using System.Threading;

namespace NadekoBot {
    class NadekoBot {
        public static DiscordClient client;
        public static string botMention;
        public static string GoogleAPIKey = null;
        public static ulong OwnerID;
        public static Channel OwnerPrivateChannel = null;
        public static string password;
        public static string TrelloAppKey;
        public static bool ForwardMessages = false;
        public static Credentials creds;

        static void Main() {
            //load credentials from credentials.json
            bool loadTrello = false;
            try {
                creds = JsonConvert.DeserializeObject<Credentials>(File.ReadAllText("credentials.json"));
                botMention = creds.BotMention;
                if (string.IsNullOrWhiteSpace(creds.GoogleAPIKey)) {
                    Console.WriteLine("No google api key found. You will not be able to use music and links won't be shortened.");
                }
                else {
                    Console.WriteLine("Google API key provided.");
                    GoogleAPIKey = creds.GoogleAPIKey;
                }
                if (string.IsNullOrWhiteSpace(creds.TrelloAppKey)) {
                    Console.WriteLine("No trello appkey found. You will not be able to use trello commands.");
                }
                else {
                    Console.WriteLine("Trello app key provided.");
                    TrelloAppKey = creds.TrelloAppKey;
                    loadTrello = true;
                }
                if (creds.ForwardMessages != true)
                    Console.WriteLine("Not forwarding messages.");
                else {
                    ForwardMessages = true;
                    Console.WriteLine("Forwarding messages.");
                }
                if (string.IsNullOrWhiteSpace(creds.SoundCloudClientID))
                    Console.WriteLine("No soundcloud Client ID found. Soundcloud streaming is disabled.");
                else
                    Console.WriteLine("SoundCloud streaming enabled.");

                OwnerID = creds.OwnerID;
                password = creds.Password;
            }
            catch (Exception ex) {
                Console.WriteLine($"Failed to load stuff from credentials.json, RTFM\n{ex.Message}");
                Console.ReadKey();
                return;
            }

            //create new discord client
            client = new DiscordClient(new DiscordConfigBuilder() {
                MessageCacheSize = 20,
                LogLevel = LogSeverity.Warning,
                LogHandler = (s, e) => {
                    try {
                        Console.WriteLine($"Severity: {e.Severity}\nMessage: {e.Message}\nExceptionMessage: {e.Exception?.Message ?? "-"}");//\nException: {(e.Exception?.ToString() ?? "-")}");
                    }
                    catch { }
                }
            });

            //create a command service
            var commandService = new CommandService(new CommandServiceConfigBuilder {
                AllowMentionPrefix = false,
                CustomPrefixHandler = m => 0,
                HelpMode = HelpMode.Disabled,
                ErrorHandler = async (s, e) => {
                    try {
                        if (e.ErrorType != CommandErrorType.BadPermissions)
                            return;
                        if (string.IsNullOrWhiteSpace(e.Exception.Message))
                            return;
                        await e.Channel.SendMessage(e.Exception.Message);
                    }
                    catch { }
                }
            });

            //reply to personal messages and forward if enabled.
            client.MessageReceived += Client_MessageReceived;
            client.MessageReceived += Notifications;

            //add command service
            var commands = client.AddService<CommandService>(commandService);

            //create module service
            var modules = client.AddService<ModuleService>(new ModuleService());

            //add audio service
            var audio = client.AddService<AudioService>(new AudioService(new AudioServiceConfigBuilder() {
                Channels = 2,
                EnableEncryption = false,
                EnableMultiserver = true,
                Bitrate = 128,
            }));

            //install modules
            modules.Add(new Administration(), "Administration", ModuleFilter.None);
            modules.Add(new Help(), "Help", ModuleFilter.None);
            modules.Add(new PermissionModule(), "Permissions", ModuleFilter.None);
            modules.Add(new Conversations(), "Conversations", ModuleFilter.None);
            modules.Add(new Gambling(), "Gambling", ModuleFilter.None);
            modules.Add(new Games(), "Games", ModuleFilter.None);
            modules.Add(new Music(), "Music", ModuleFilter.None);
            modules.Add(new Searches(), "Searches", ModuleFilter.None);
            if (loadTrello)
                modules.Add(new Trello(), "Trello", ModuleFilter.None);

            //run the bot
            client.ExecuteAndWait(async () => {
                try {
                    await client.Connect(creds.Username, creds.Password);
                }
                catch (Exception ex) {
                    Console.WriteLine($"Probably wrong EMAIL or PASSWORD.\n{ex.Message}");
                    Console.ReadKey();
                    Console.WriteLine(ex);
                    Console.ReadKey();
                    return;
                }
                Console.WriteLine("-----------------");
                Console.WriteLine(await NadekoStats.Instance.GetStats());
                Console.WriteLine("-----------------");

                try {
                    OwnerPrivateChannel = await client.CreatePrivateChannel(OwnerID);
                }
                catch {
                    Console.WriteLine("Failed creating private channel with the owner");
                }

                Classes.Permissions.PermissionsHandler.Initialize();
                
                client.ClientAPI.SendingRequest += (s, e) => {

                    try {
                        var request = e.Request as Discord.API.Client.Rest.SendMessageRequest;
                        if (request != null) {
                            //@everyοne
                            request.Content = request.Content?.Replace("@everyone", "@everryone") ?? "_error_";
                            if (string.IsNullOrWhiteSpace(request.Content))
                                e.Cancel = true;
                            //else
                            //    Console.WriteLine("Sending request");
                        }
                    }
                    catch {
                        Console.WriteLine("SENDING REQUEST ERRORED!!!!");
                    }
                };

                //client.ClientAPI.SentRequest += (s, e) => {
                //    try {
                //        var request = e.Request as Discord.API.Client.Rest.SendMessageRequest;
                //        if (request != null) {
                //            Console.WriteLine("Sent.");
                //        }
                //    }
                //    catch { Console.WriteLine("SENT REQUEST ERRORED!!!"); }
                //};
            });
            Console.WriteLine("Exiting...");
            Console.ReadKey();
        }

        private static async void Notifications(object sender, MessageEventArgs e)
        {
            if (e.Message.Text.StartsWith("!") || e.Message.Text.StartsWith(".") || e.Message.Text.StartsWith("~") || e.Channel == null || e.User.Id == client.CurrentUser.Id || e.User.Id == 153586072092147712) return;
            new Thread(async () =>
            {
                try
                {
                    Thread.Sleep(2000);
                    Thread.CurrentThread.IsBackground = true;
                    StreamReader file = new StreamReader("notifications.txt");
                    String line;
                    while ((line = file.ReadLine()) != null)
                    {
                        int index = line.IndexOf(";");
                        string keyword = (index > 0 ? line.Substring(0, index) : "");
                        string user = line.Substring(line.LastIndexOf(';') + 1);
                        keyword = keyword.Remove(0, 2);
                        keyword = keyword.ToLower();
                        if (e.Message.Text.ToLower().Contains(keyword))
                        {
                            if (Convert.ToUInt64(user) == e.User.Id) return;
                            Channel rawr = await client.CreatePrivateChannel(Convert.ToUInt64(user));
                            await rawr.SendMessage($"{e.User.Name} mentioned you in {e.Channel.Name} with the following message:\r\n```{e.Message.Text}```\r\n`{keyword}`");
                        }
                    }
                    file.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }).Start();
        }

        private static async void Client_MessageReceived(object sender, MessageEventArgs e) {
            try {
                if (e.Server != null || e.User.Id == client.CurrentUser.Id) return;
                if (PollCommand.ActivePolls.SelectMany(kvp => kvp.Key.Users.Select(u => u.Id)).Contains(e.User.Id)) return;
                // just ban this trash AutoModerator
                // and cancer christmass spirit
                // and crappy shotaslave
                if (e.User.Id == 105309315895693312 ||
                    e.User.Id == 119174277298782216 ||
                    e.User.Id == 143515953525817344)
                    return; // FU

                if (!NadekoBot.creds.DontJoinServers) {
                    try {
                        await (await client.GetInvite(e.Message.Text)).Accept();
                        await e.Channel.SendMessage("I got in!");
                        return;
                    }
                    catch {
                        if (e.User.Id == 109338686889476096) { //carbonitex invite
                            await e.Channel.SendMessage("Failed to join the server.");
                            return;
                        }
                    }
                }

                if (ForwardMessages && OwnerPrivateChannel != null)
                    await OwnerPrivateChannel.SendMessage(e.User + ": ```\n" + e.Message.Text + "\n```");
            }
            catch { }
        }
    }
}

//95520984584429568 meany