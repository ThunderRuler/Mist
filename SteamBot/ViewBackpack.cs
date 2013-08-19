using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using MetroFramework.Forms;

namespace MistClient
{
    public partial class ViewBackpack : MetroForm
    {
        SteamBot.Bot bot;

        public ViewBackpack(SteamBot.Bot bot)
        {
            InitializeComponent();
            this.bot = bot;
            Util.LoadTheme(metroStyleManager1);
        }

        private void button_ok_Click(object sender, EventArgs e)
        {
            if (text_profile.Text.Length < 17 || text_profile.Text == "" || Regex.IsMatch(text_profile.Text, "^[A-Za-z]$"))
            {
                MessageBox.Show("The SteamID64 is invalid. It must be 17 characters and cannot be blank or contain letters.",
                                "Error",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error,
                                MessageBoxDefaultButton.Button1);
            }
            else
            {
                ulong id = Convert.ToUInt64(text_profile.Text);
                this.Close();
                var showBP = new ShowBackpackGrid(bot, id);
                showBP.Show();
                showBP.Activate();
            }
        }

        private void button_cancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}