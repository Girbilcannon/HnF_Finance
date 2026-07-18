using System;
using Avalonia.Controls;
using GrannyManager.Core.Models;

namespace GrannyManager.App.Avalonia.Views
{
    public partial class AssetItemDialog : Window
    {
        private readonly TextBlock? _dialogTitleTextBlock;
        private readonly TextBlock? _validationTextBlock;
        private readonly TextBox? _assetNameTextBox;
        private readonly ComboBox? _assetTypeComboBox;
        private readonly TextBox? _estimatedValueTextBox;
        private readonly StackPanel? _bankFieldsPanel;
        private readonly TextBox? _institutionNameTextBox;
        private readonly ComboBox? _accountTypeComboBox;
        private readonly TextBox? _accountLastFourTextBox;
        private readonly CheckBox? _isActiveCheckBox;
        private readonly TextBox? _notesTextBox;

        private AssetItem _asset = new();

        public AssetItemDialog()
        {
            InitializeComponent();

            _dialogTitleTextBlock = this.FindControl<TextBlock>("DialogTitleTextBlock");
            _validationTextBlock = this.FindControl<TextBlock>("ValidationTextBlock");
            _assetNameTextBox = this.FindControl<TextBox>("AssetNameTextBox");
            _assetTypeComboBox = this.FindControl<ComboBox>("AssetTypeComboBox");
            _estimatedValueTextBox = this.FindControl<TextBox>("EstimatedValueTextBox");
            _bankFieldsPanel = this.FindControl<StackPanel>("BankFieldsPanel");
            _institutionNameTextBox = this.FindControl<TextBox>("InstitutionNameTextBox");
            _accountTypeComboBox = this.FindControl<ComboBox>("AccountTypeComboBox");
            _accountLastFourTextBox = this.FindControl<TextBox>("AccountLastFourTextBox");
            _isActiveCheckBox = this.FindControl<CheckBox>("IsActiveCheckBox");
            _notesTextBox = this.FindControl<TextBox>("NotesTextBox");

            if (_assetTypeComboBox is not null)
                _assetTypeComboBox.SelectionChanged += (_, _) => RefreshBankVisibility();

            var cancelButton = this.FindControl<Button>("CancelButton");
            if (cancelButton is not null)
                cancelButton.Click += (_, _) => Close(false);

            var saveButton = this.FindControl<Button>("SaveButton");
            if (saveButton is not null)
                saveButton.Click += (_, _) => SaveAndClose();
        }

        public AssetItem Asset => _asset;

        public void SetMode(string title, AssetItem asset)
        {
            _asset = asset ?? new AssetItem();

            if (_dialogTitleTextBlock is not null)
                _dialogTitleTextBlock.Text = title;

            if (_assetNameTextBox is not null)
                _assetNameTextBox.Text = _asset.AssetName;

            SelectComboValue(_assetTypeComboBox, string.IsNullOrWhiteSpace(_asset.AssetType) ? "Bank Account" : _asset.AssetType);

            if (_estimatedValueTextBox is not null)
                _estimatedValueTextBox.Text = _asset.EstimatedValue <= 0 ? string.Empty : _asset.EstimatedValue.ToString("0.##");

            if (_institutionNameTextBox is not null)
                _institutionNameTextBox.Text = _asset.InstitutionName;

            SelectComboValue(_accountTypeComboBox, string.IsNullOrWhiteSpace(_asset.AccountType) ? "Checking" : _asset.AccountType);

            if (_accountLastFourTextBox is not null)
                _accountLastFourTextBox.Text = _asset.AccountLastFour;

            if (_isActiveCheckBox is not null)
                _isActiveCheckBox.IsChecked = _asset.IsActive;

            if (_notesTextBox is not null)
                _notesTextBox.Text = _asset.Notes;

            RefreshBankVisibility();
        }

        private void RefreshBankVisibility()
        {
            if (_bankFieldsPanel is not null)
                _bankFieldsPanel.IsVisible = GetComboValue(_assetTypeComboBox, "Bank Account") == "Bank Account";
        }

        private void SaveAndClose()
        {
            if (_validationTextBlock is not null)
                _validationTextBlock.Text = string.Empty;

            var assetName = _assetNameTextBox?.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(assetName))
            {
                if (_validationTextBlock is not null)
                    _validationTextBlock.Text = "Enter an asset name before saving.";
                return;
            }

            var value = 0m;
            var valueText = _estimatedValueTextBox?.Text?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(valueText) && !decimal.TryParse(valueText, out value))
            {
                if (_validationTextBlock is not null)
                    _validationTextBlock.Text = "Estimated value must be a valid number.";
                return;
            }

            var assetType = GetComboValue(_assetTypeComboBox, "Bank Account");
            _asset.AssetName = assetName;
            _asset.AssetType = assetType;
            _asset.EstimatedValue = value;
            _asset.InstitutionName = _institutionNameTextBox?.Text?.Trim() ?? string.Empty;
            _asset.AccountType = assetType == "Bank Account" ? GetComboValue(_accountTypeComboBox, "Checking") : string.Empty;
            _asset.AccountLastFour = assetType == "Bank Account" ? (_accountLastFourTextBox?.Text?.Trim() ?? string.Empty) : string.Empty;
            _asset.IsActive = _isActiveCheckBox?.IsChecked == true;
            _asset.Notes = _notesTextBox?.Text?.Trim() ?? string.Empty;

            Close(true);
        }

        private static void SelectComboValue(ComboBox? comboBox, string value)
        {
            if (comboBox is null)
                return;

            for (var index = 0; index < comboBox.ItemCount; index++)
            {
                if (comboBox.Items[index] is ComboBoxItem item &&
                    string.Equals(item.Content?.ToString(), value, StringComparison.OrdinalIgnoreCase))
                {
                    comboBox.SelectedIndex = index;
                    return;
                }
            }

            comboBox.SelectedIndex = 0;
        }

        private static string GetComboValue(ComboBox? comboBox, string fallback)
        {
            if (comboBox?.SelectedItem is ComboBoxItem item)
                return item.Content?.ToString() ?? fallback;

            return fallback;
        }
    }
}
