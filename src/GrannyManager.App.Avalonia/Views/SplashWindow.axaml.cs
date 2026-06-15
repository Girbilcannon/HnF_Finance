using Avalonia.Controls;

namespace GrannyManager.App.Avalonia.Views
{
    public partial class SplashWindow : Window
    {
        private readonly TextBlock? _statusTextBlock;

        public SplashWindow()
        {
            InitializeComponent();
            _statusTextBlock = this.FindControl<TextBlock>("StatusTextBlock");
        }

        public void SetStatus(string status)
        {
            if (_statusTextBlock is not null)
                _statusTextBlock.Text = status;
        }
    }
}
