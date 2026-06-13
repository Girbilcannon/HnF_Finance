using GrannyManager.App.Navigation;
using GrannyManager.App.Themes;

namespace GrannyManager.App.Pages;

public abstract class BasePage : UserControl, IAppPage
{
    protected readonly Panel ContentPanel;

    protected BasePage(AppPageKey pageKey, string title, string subtitle)
    {
        PageKey = pageKey;
        PageTitle = title;
        Dock = DockStyle.Fill;
        BackColor = AppColors.AppBackground;

        ContentPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppColors.PanelBackground,
            Padding = new Padding(28)
        };
        Controls.Add(ContentPanel);

        var titleLabel = new Label
        {
            Text = title,
            Dock = DockStyle.Top,
            Height = 42,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = AppColors.TextPrimary,
            Font = AppFonts.PageTitle
        };

        var subtitleLabel = new Label
        {
            Text = subtitle,
            Dock = DockStyle.Top,
            Height = 44,
            TextAlign = ContentAlignment.TopLeft,
            ForeColor = AppColors.TextMuted,
            Font = AppFonts.Body
        };

        ContentPanel.Controls.Add(subtitleLabel);
        ContentPanel.Controls.Add(titleLabel);
    }

    public AppPageKey PageKey { get; }
    public string PageTitle { get; }

    public virtual void OnNavigatedTo()
    {
    }

    public virtual bool CanNavigateAway() => true;

    protected Label AddSectionLabel(string text)
    {
        var label = new Label
        {
            Text = text,
            Dock = DockStyle.Top,
            Height = 34,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = AppColors.TextPrimary,
            Font = AppFonts.BodyBold,
            Padding = new Padding(0, 8, 0, 0)
        };
        ContentPanel.Controls.Add(label);
        label.BringToFront();
        return label;
    }

    protected Label AddBodyText(string text, int height = 70)
    {
        var label = new Label
        {
            Text = text,
            Dock = DockStyle.Top,
            Height = height,
            TextAlign = ContentAlignment.TopLeft,
            ForeColor = AppColors.TextMuted,
            Font = AppFonts.Body,
            Padding = new Padding(0, 4, 0, 0)
        };
        ContentPanel.Controls.Add(label);
        label.BringToFront();
        return label;
    }
}
