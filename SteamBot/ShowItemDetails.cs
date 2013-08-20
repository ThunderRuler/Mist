using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MetroFramework.Forms;

namespace MistClient
{
    public partial class ShowItemDetails : MetroForm
    {
        public ShowItemDetails()
        {
            InitializeComponent();
            Util.LoadTheme(metroStyleManager1);
        }
    }
}
