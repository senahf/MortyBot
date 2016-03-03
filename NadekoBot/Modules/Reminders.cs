using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.Modules;

namespace NadekoBot.Modules
{
    class Reminders : DiscordModule
    {
        public Reminders()
        {
           
        }
        private async void PollReminders(CommandEventArgs e)
        {

        }
        public override void Install(ModuleManager manager)
        {
            
            manager.CreateCommands("!remind", cgb =>
            {
                cgb.AddCheck(Classes.Permissions.PermissionChecker.Instance);

                commands.ForEach(com => com.Init(cgb));

            });
        }
    }
}
