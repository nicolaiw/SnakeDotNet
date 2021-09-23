using System;
using System.Windows;

namespace SnakeDotNet
{
    /// <summary>
    /// Interaction logic for MenuWindow.xaml
    /// </summary>
    public partial class MenuWindow : Window
    {
        private Action<MenuWindow> _onResume;
        public MenuWindow(Action<MenuWindow> onExit, Action<MenuWindow> onResume)
        {
            InitializeComponent();

            _onResume = onResume;

            BtnExit.Click += (s, a) => onExit(this);
            BtnResume.Click += (s, a) => onResume(this);

            BtnExit.Focus();

            KeyDown += MenuWindow_KeyDown;
        }

        private void MenuWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if(e.Key is System.Windows.Input.Key.Escape)
            {
                _onResume(this);
            }
        }
    }
}
