using GrannyManager.App.Themes;
using GrannyManager.Core.Models;
using GrannyManager.Core.Services;

namespace GrannyManager.App.Services;

public static class CasePinPrompt
{
    public static bool TryPromptForNewPin(IWin32Window owner, out string pin)
    {
        pin = string.Empty;
        using var dialog = new PinDialog(
            "Create Case Security PIN",
            "Create a 4-digit PIN for this case. You will need this PIN before the case can be opened later.",
            showConfirm: true);

        if (dialog.ShowDialog(owner) != DialogResult.OK)
            return false;

        pin = dialog.Pin;
        return true;
    }

    public static bool VerifyCasePin(IWin32Window owner, CaseProfile profile, CaseFolderService service)
    {
        if (profile is null)
            return false;

        if (!profile.HasSecurityPin)
            return true;

        for (int attempt = 0; attempt < 3; attempt++)
        {
            using var dialog = new PinDialog(
                "Unlock Case",
                $"Enter the 4-digit PIN for:\r\n{profile.DisplayName}",
                showConfirm: false);

            if (dialog.ShowDialog(owner) != DialogResult.OK)
                return false;

            if (service.VerifySecurityPin(profile, dialog.Pin))
                return true;

            MessageBox.Show(owner, "That PIN did not match this case.", "Incorrect PIN", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        return false;
    }

    public static bool TryChangePin(IWin32Window owner, CaseProfile profile, CaseFolderService service)
    {
        if (profile is null)
            return false;

        if (profile.HasSecurityPin && !VerifyCasePin(owner, profile, service))
            return false;

        if (!TryPromptForNewPin(owner, out string newPin))
            return false;

        service.SetSecurityPin(profile, newPin);
        service.SaveCase(profile);
        MessageBox.Show(owner, "Case PIN updated.", "Security PIN", MessageBoxButtons.OK, MessageBoxIcon.Information);
        return true;
    }

    private sealed class PinDialog : Form
    {
        private readonly TextBox _pinBox;
        private readonly TextBox? _confirmBox;

        public PinDialog(string title, string message, bool showConfirm)
        {
            Text = title;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(420, showConfirm ? 230 : 180);
            BackColor = AppColors.PanelBackground;
            ForeColor = AppColors.TextPrimary;
            Font = AppFonts.Body;

            var label = new Label
            {
                Text = message,
                Location = new Point(18, 16),
                Size = new Size(384, 52),
                ForeColor = AppColors.TextPrimary,
                Font = AppFonts.Body
            };
            Controls.Add(label);

            var pinLabel = new Label
            {
                Text = "4-digit PIN",
                Location = new Point(18, 76),
                Size = new Size(160, 22),
                ForeColor = AppColors.TextMuted,
                Font = AppFonts.Body
            };
            Controls.Add(pinLabel);

            _pinBox = CreatePinBox(18, 100);
            Controls.Add(_pinBox);

            var nextY = 136;
            if (showConfirm)
            {
                var confirmLabel = new Label
                {
                    Text = "Confirm PIN",
                    Location = new Point(18, 136),
                    Size = new Size(160, 22),
                    ForeColor = AppColors.TextMuted,
                    Font = AppFonts.Body
                };
                Controls.Add(confirmLabel);

                _confirmBox = CreatePinBox(18, 160);
                Controls.Add(_confirmBox);
                nextY = 194;
            }

            var okButton = CreateButton("OK", 226, nextY);
            okButton.Click += (_, _) => Accept(showConfirm);
            Controls.Add(okButton);

            var cancelButton = CreateButton("Cancel", 316, nextY);
            cancelButton.Click += (_, _) => DialogResult = DialogResult.Cancel;
            Controls.Add(cancelButton);

            AcceptButton = okButton;
            CancelButton = cancelButton;
        }

        public string Pin => _pinBox.Text.Trim();

        private static TextBox CreatePinBox(int x, int y)
        {
            return new TextBox
            {
                Location = new Point(x, y),
                Size = new Size(120, 26),
                MaxLength = 4,
                UseSystemPasswordChar = true,
                BackColor = AppColors.SearchBoxBackground,
                ForeColor = AppColors.TextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Font = AppFonts.Body
            };
        }

        private static Button CreateButton(string text, int x, int y)
        {
            var button = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(78, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = AppColors.SearchButtonBackground,
                ForeColor = AppColors.TextPrimary,
                Font = AppFonts.BodyBold
            };
            button.FlatAppearance.BorderColor = AppColors.Border;
            return button;
        }

        private void Accept(bool showConfirm)
        {
            var pin = _pinBox.Text.Trim();
            if (pin.Length != 4 || pin.Any(c => !char.IsDigit(c)))
            {
                MessageBox.Show(this, "Enter a 4-digit numeric PIN.", "PIN required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                _pinBox.Focus();
                return;
            }

            if (showConfirm && _confirmBox is not null && !string.Equals(pin, _confirmBox.Text.Trim(), StringComparison.Ordinal))
            {
                MessageBox.Show(this, "The PIN entries do not match.", "PIN mismatch", MessageBoxButtons.OK, MessageBoxIcon.Information);
                _confirmBox.Focus();
                return;
            }

            DialogResult = DialogResult.OK;
        }
    }
}
