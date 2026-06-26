using System;
using Avalonia.Controls;
using GrannyManager.Core.Models;

namespace GrannyManager.App.Avalonia.Views
{
    public partial class HouseholdPersonDialog : Window
    {
        private readonly TextBlock? _dialogTitleTextBlock;
        private readonly TextBlock? _validationTextBlock;
        private readonly TextBox? _fullNameTextBox;
        private readonly TextBox? _relationshipTextBox;
        private readonly TextBox? _roleTextBox;
        private readonly CheckBox? _livesInHouseholdCheckBox;
        private readonly CheckBox? _paysRentCheckBox;
        private readonly CheckBox? _usesHouseholdVehicleCheckBox;
        private readonly CheckBox? _receivesRidesCheckBox;
        private readonly ComboBox? _contributionHandlingComboBox;
        private readonly Grid? _linkedIncomeSourcePanel;
        private readonly ComboBox? _linkedIncomeSourceComboBox;
        private readonly CheckBox? _isActiveCheckBox;
        private readonly TextBox? _notesTextBox;

        private HouseholdPerson _person = new();

        public HouseholdPersonDialog()
        {
            InitializeComponent();

            _dialogTitleTextBlock = this.FindControl<TextBlock>("DialogTitleTextBlock");
            _validationTextBlock = this.FindControl<TextBlock>("ValidationTextBlock");
            _fullNameTextBox = this.FindControl<TextBox>("FullNameTextBox");
            _relationshipTextBox = this.FindControl<TextBox>("RelationshipTextBox");
            _roleTextBox = this.FindControl<TextBox>("RoleTextBox");
            _livesInHouseholdCheckBox = this.FindControl<CheckBox>("LivesInHouseholdCheckBox");
            _paysRentCheckBox = this.FindControl<CheckBox>("PaysRentCheckBox");
            _usesHouseholdVehicleCheckBox = this.FindControl<CheckBox>("UsesHouseholdVehicleCheckBox");
            _receivesRidesCheckBox = this.FindControl<CheckBox>("ReceivesRidesCheckBox");
            _contributionHandlingComboBox = this.FindControl<ComboBox>("ContributionHandlingComboBox");
            _linkedIncomeSourcePanel = this.FindControl<Grid>("LinkedIncomeSourcePanel");
            _linkedIncomeSourceComboBox = this.FindControl<ComboBox>("LinkedIncomeSourceComboBox");
            _isActiveCheckBox = this.FindControl<CheckBox>("IsActiveCheckBox");
            _notesTextBox = this.FindControl<TextBox>("NotesTextBox");

            if (_contributionHandlingComboBox is not null)
                _contributionHandlingComboBox.SelectionChanged += (_, _) => RefreshContributionVisibility();

            var cancelButton = this.FindControl<Button>("CancelButton");
            if (cancelButton is not null)
                cancelButton.Click += (_, _) => Close(false);

            var saveButton = this.FindControl<Button>("SaveButton");
            if (saveButton is not null)
                saveButton.Click += (_, _) => SaveAndClose();
        }

        public HouseholdPerson Person => _person;

        public void SetMode(string title, HouseholdPerson person)
        {
            _person = person ?? new HouseholdPerson();

            if (_dialogTitleTextBlock is not null)
                _dialogTitleTextBlock.Text = title;

            if (_fullNameTextBox is not null)
                _fullNameTextBox.Text = _person.FullName;

            if (_relationshipTextBox is not null)
                _relationshipTextBox.Text = _person.Relationship;

            if (_roleTextBox is not null)
                _roleTextBox.Text = _person.Role;

            if (_livesInHouseholdCheckBox is not null)
                _livesInHouseholdCheckBox.IsChecked = _person.LivesInHousehold;

            if (_paysRentCheckBox is not null)
                _paysRentCheckBox.IsChecked = _person.PaysRent;

            if (_usesHouseholdVehicleCheckBox is not null)
                _usesHouseholdVehicleCheckBox.IsChecked = _person.UsesHouseholdVehicle;

            if (_receivesRidesCheckBox is not null)
                _receivesRidesCheckBox.IsChecked = _person.ReceivesRides;

            SelectContributionHandling(string.IsNullOrWhiteSpace(_person.ContributionHandling)
                ? "No Contribution"
                : _person.ContributionHandling);

            if (_linkedIncomeSourceComboBox is not null)
                _linkedIncomeSourceComboBox.SelectedIndex = 0;

            if (_isActiveCheckBox is not null)
                _isActiveCheckBox.IsChecked = _person.IsActive;

            if (_notesTextBox is not null)
                _notesTextBox.Text = _person.Notes;

            RefreshContributionVisibility();
        }

        private void SelectContributionHandling(string value)
        {
            if (_contributionHandlingComboBox is null)
                return;

            var target = value.Trim();
            for (var index = 0; index < _contributionHandlingComboBox.ItemCount; index++)
            {
                if (_contributionHandlingComboBox.Items[index] is ComboBoxItem item &&
                    string.Equals(item.Content?.ToString(), target, StringComparison.OrdinalIgnoreCase))
                {
                    _contributionHandlingComboBox.SelectedIndex = index;
                    return;
                }
            }

            _contributionHandlingComboBox.SelectedIndex = 0;
        }

        private string GetContributionHandling()
        {
            if (_contributionHandlingComboBox?.SelectedItem is ComboBoxItem item)
                return item.Content?.ToString() ?? "No Contribution";

            return "No Contribution";
        }

        private void RefreshContributionVisibility()
        {
            if (_linkedIncomeSourcePanel is not null)
                _linkedIncomeSourcePanel.IsVisible = GetContributionHandling() == "Select Income Source";
        }

        private void SaveAndClose()
        {
            if (_validationTextBlock is not null)
                _validationTextBlock.Text = string.Empty;

            var fullName = _fullNameTextBox?.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(fullName))
            {
                if (_validationTextBlock is not null)
                    _validationTextBlock.Text = "Enter a name before saving.";
                return;
            }

            _person.FullName = fullName;
            _person.Relationship = _relationshipTextBox?.Text?.Trim() ?? string.Empty;
            _person.Role = _roleTextBox?.Text?.Trim() ?? string.Empty;
            _person.LivesInHousehold = _livesInHouseholdCheckBox?.IsChecked == true;
            _person.PaysRent = _paysRentCheckBox?.IsChecked == true;
            _person.UsesHouseholdVehicle = _usesHouseholdVehicleCheckBox?.IsChecked == true;
            _person.ReceivesRides = _receivesRidesCheckBox?.IsChecked == true;
            _person.ContributionHandling = GetContributionHandling();
            _person.MonthlyContribution = 0m;

            if (_person.ContributionHandling != "Select Income Source")
            {
                _person.LinkedIncomeSourceId = 0;
                _person.LinkedIncomeSourceName = string.Empty;
            }

            _person.IsActive = _isActiveCheckBox?.IsChecked == true;
            _person.Notes = _notesTextBox?.Text?.Trim() ?? string.Empty;

            Close(true);
        }
    }
}
