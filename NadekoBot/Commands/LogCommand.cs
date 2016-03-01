﻿using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Discord.Commands;
using Discord;
using System.Collections.Generic;

namespace NadekoBot.Commands {
    class LogCommand : DiscordCommand {

        public LogCommand() : base() {
            NadekoBot.client.MessageReceived += MsgRecivd;
            NadekoBot.client.MessageDeleted += MsgDltd;
            NadekoBot.client.MessageUpdated += MsgUpdtd;
            NadekoBot.client.UserUpdated += UsrUpdtd;
        }

        ConcurrentDictionary<Server, Channel> logs = new ConcurrentDictionary<Server, Channel>();
        ConcurrentDictionary<Server, Channel> loggingPresences = new ConcurrentDictionary<Server, Channel>();
        //
        ConcurrentDictionary<Channel, Channel> voiceChannelLog = new ConcurrentDictionary<Channel, Channel>();

        public override Func<CommandEventArgs, Task> DoFunc() => async e => {
            if (e.User.Id != NadekoBot.OwnerID ||
                          !e.User.ServerPermissions.ManageServer)
                return;
            Channel ch;
            if (!logs.TryRemove(e.Server, out ch)) {
                logs.TryAdd(e.Server, e.Channel);
                await e.Channel.SendMessage($"**I WILL BEGIN LOGGING SERVER ACTIVITY IN THIS CHANNEL**");
                return;
            }

            await e.Channel.SendMessage($"**NO LONGER LOGGING IN {ch.Mention} CHANNEL**");
        };

        private async void MsgRecivd(object sender, MessageEventArgs e) {
            try {
                if (e.Server == null || e.Channel.IsPrivate || e.User.Id == NadekoBot.client.CurrentUser.Id)
                    return;
                Channel ch;
                if (!logs.TryGetValue(e.Server, out ch) || e.Channel == ch)
                    return;
                await ch.SendMessage($"`Type:` **Message received** `Time:` **{DateTime.Now}** `Channel:` **{e.Channel.Name}**\n`{e.User}:` {e.Message.Text}");
            }
            catch { }
        }
        private async void MsgDltd(object sender, MessageEventArgs e) {
            try {
                if (e.Server == null || e.Channel.IsPrivate || e.User.Id == NadekoBot.client.CurrentUser.Id)
                    return;
                Channel ch;
                if (!logs.TryGetValue(e.Server, out ch) || e.Channel == ch)
                    return;
                await ch.SendMessage($"`Type:` **Message deleted** `Time:` **{DateTime.Now}** `Channel:` **{e.Channel.Name}**\n`{e.User}:` {e.Message.Text}");
            }
            catch { }
        }
        private async void MsgUpdtd(object sender, MessageUpdatedEventArgs e) {
            try {
                if (e.Server == null || e.Channel.IsPrivate || e.User.Id == NadekoBot.client.CurrentUser.Id)
                    return;
                Channel ch;
                if (!logs.TryGetValue(e.Server, out ch) || e.Channel == ch)
                    return;
                await ch.SendMessage($"`Type:` **Message updated** `Time:` **{DateTime.Now}** `Channel:` **{e.Channel.Name}**\n**BEFORE**: `{e.User}:` {e.Before.Text}\n---------------\n**AFTER**: `{e.User}:` {e.After.Text}");
            }
            catch { }
        }
        private async void UsrUpdtd(object sender, UserUpdatedEventArgs e) {
            try {
                Channel ch;
                if (loggingPresences.TryGetValue(e.Server, out ch))
                    if (e.Before.Status != e.After.Status) {
                        var msg = await ch.SendMessage($"**{e.Before.Name}** is now **{e.After.Status}**.");
                        await Task.Delay(4000);
                        await msg.Delete();
                    }
            }
            catch { }

            try {
                if (e.Before.VoiceChannel != null && voiceChannelLog.ContainsKey(e.Before.VoiceChannel)) {
                    if (e.After.VoiceChannel != e.Before.VoiceChannel)
                        await voiceChannelLog[e.Before.VoiceChannel].SendMessage($"🎼`{e.Before.Name} has left the` {e.Before.VoiceChannel.Mention} `voice channel.`");
                }
                if (e.After.VoiceChannel != null && voiceChannelLog.ContainsKey(e.After.VoiceChannel)) {
                    if (e.After.VoiceChannel != e.Before.VoiceChannel)
                        await voiceChannelLog[e.After.VoiceChannel].SendMessage($"🎼`{e.After.Name} has joined the`{e.After.VoiceChannel.Mention} `voice channel.`");
                }
            }
            catch { }

            try {
                Channel ch;
                if (!logs.TryGetValue(e.Server, out ch))
                    return;
                string str = $"`Type:` **User updated** `Time:` **{DateTime.Now}** `User:` **{e.Before.Name}**\n";
                if (e.Before.Name != e.After.Name)
                    str += $"`New name:` **{e.After.Name}**";
                else if (e.Before.AvatarUrl != e.After.AvatarUrl)
                    str += $"`New Avatar:` {e.After.AvatarUrl}";
                else if (e.Before.Status != e.After.Status)
                    str += $"Status `{e.Before.Status}` -> `{e.After.Status}`";
                else
                    return;
                await ch.SendMessage(str);
            }
            catch { }
        }
        public override void Init(CommandGroupBuilder cgb) {
            cgb.CreateCommand(".logserver")
                  .Description("Toggles logging in this channel. Logs every message sent/deleted/edited on the server. BOT OWNER ONLY. SERVER OWNER ONLY.")
                  .Do(DoFunc());

            cgb.CreateCommand(".userpresence")
                  .Description("Starts logging to this channel when someone from the server goes online/offline/idle. BOT OWNER ONLY. SERVER OWNER ONLY.")
                  .Do(async e => {
                      if (e.User.Id != NadekoBot.OwnerID ||
                          !e.User.ServerPermissions.ManageServer)
                          return;
                      Channel ch;
                      if (!loggingPresences.TryRemove(e.Server, out ch)) {
                          loggingPresences.TryAdd(e.Server, e.Channel);
                          await e.Channel.SendMessage($"**User presence notifications enabled.**");
                          return;
                      }

                      await e.Channel.SendMessage($"**User presence notifications disabled.**");
                  });

            cgb.CreateCommand(".voicepresence")
                  .Description("Toggles logging to this channel whenever someone joins or leaves a voice channel you are in right now.")
                  .Do(async e => {
                      if (e.User.Id != NadekoBot.OwnerID ||
                          !e.User.ServerPermissions.ManageServer)
                          return;                    

                      if (e.User.VoiceChannel == null) {
                          await e.Channel.SendMessage("💢 You are not in a voice channel right now. If you are, please rejoin it.");
                          return;
                      }
                      Channel throwaway;
                      if (!voiceChannelLog.TryRemove(e.User.VoiceChannel, out throwaway)) {
                          voiceChannelLog.TryAdd(e.User.VoiceChannel, e.Channel);
                          await e.Channel.SendMessage($"`Logging user updates for` {e.User.VoiceChannel.Mention} `voice channel.`");
                      }
                      else
                          await e.Channel.SendMessage($"`Stopped logging user updates for` {e.User.VoiceChannel.Mention} `voice channel.`");
                  });
        }
    }
}
