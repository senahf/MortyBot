﻿using Discord;
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
                ConnectionTimeout = 60000,
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
            //modules.Add(new Help(), "Help", ModuleFilter.None);
            modules.Add(new PermissionModule(), "Permissions", ModuleFilter.None);
            modules.Add(new Conversations(), "Conversations", ModuleFilter.None);
            modules.Add(new Gambling(), "Gambling", ModuleFilter.None);
            modules.Add(new Games(), "Games", ModuleFilter.None);
            modules.Add(new Music(), "Music", ModuleFilter.None);
            modules.Add(new Searches(), "Searches", ModuleFilter.None);
            if (loadTrello)
                modules.Add(new Trello(), "Trello", ModuleFilter.None);
            //modules.Add(new NSFW(), "NSFW", ModuleFilter.None);

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
                Console.WriteLine(NadekoStats.Instance.GetStats());
                Console.WriteLine("-----------------");

                try {
                    OwnerPrivateChannel = await client.CreatePrivateChannel(OwnerID);
                }
                catch {
                    Console.WriteLine("Failed creating private channel with the owner");
                }

                Classes.Permissions.PermissionsHandler.Initialize();

                client.ClientAPI.SendingRequest += (s, e) => {
                    var request = e.Request as Discord.API.Client.Rest.SendMessageRequest;
                    if (request != null) {
                        if (string.IsNullOrWhiteSpace(request.Content))
                            e.Cancel = true;
                        //else
                        //    Console.WriteLine("Sending request.");
                        request.Content = request.Content.Replace("@everyone", "@everyοne");
                    }
                };
            });
            Console.WriteLine("Exiting...");
            Console.ReadKey();
        }
        private static async void Notifications(object sender, MessageEventArgs e)
        {
            
        }
        private static async void Client_MessageReceived(object sender, MessageEventArgs e) {
            try {
                if (e.Server != null || e.User.Id == client.CurrentUser.Id) return;
                if (PollCommand.ActivePolls.SelectMany(kvp => kvp.Key.Users.Select(u => u.Id)).Contains(e.User.Id)) return;
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