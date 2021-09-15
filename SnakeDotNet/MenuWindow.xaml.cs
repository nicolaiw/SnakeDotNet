using System;
using System.Windows;

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
