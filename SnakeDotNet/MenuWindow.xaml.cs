using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SnakeDotNet
{
    /// <summary>
    /// Interaction logic for MenuWindow.xaml
    /// </summary>
    public partial class MenuWindow : Window
    {
        public MenuWindow(Action<MenuWindow> onExit, Action<MenuWindow> onResume)
        {
            InitializeComponent();

            this.BtnExit.Click += (s, a) => onExit(this);
            this.BtnResume.Click += (s, a) => onResume(this);

            this.BtnExit.Focus();
        }
    }
}
