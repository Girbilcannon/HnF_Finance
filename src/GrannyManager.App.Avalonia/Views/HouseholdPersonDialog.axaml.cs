using System;
using Avalonia.Controls;
using GrannyManager.Core.Models;
using System.Collections.Generic;

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
        private readonly Grid? _createIncomeSourcePanel;
        private readonly ComboBox? _linkedIncomeSourceComboBox;
        private readonly Button? _createIncomeSourceFromHouseholdButton;
        private readonly List<IncomeSource> _incomeSources = new();
        private readonly List<HouseholdPerson> _householdPeopleForIncomeDialog = new();
        private readonly CheckBox? _isActiveCheckBox;
        private readonly TextBox? _notesTextBox;

        private Func<IncomeSource>? _createBlankIncomeSource;
        private Func<IncomeSource, bool>? _saveIncomeSource;
        private Func<IReadOnlyList<IncomeSource>>? _reloadIncomeSources;

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
            _createIncomeSourcePanel = this.FindControl<Grid>("CreateIncomeSourcePanel");
            _linkedIncomeSourceComboBox = this.FindControl<ComboBox>("LinkedIncomeSourceComboBox");
            _createIncomeSourceFromHouseholdButton = this.FindControl<Button>("CreateIncomeSourceFromHouseholdButton");
            _isActiveCheckBox = this.FindControl<CheckBox>("IsActiveCheckBox");
            _notesTextBox = this.FindControl<TextBox>("NotesTextBox");

            if (_contributionHandlingComboBox is not null)
                _contributionHandlingComboBox.SelectionChanged += (_, _) => RefreshContributionVisibility();

            if (_createIncomeSourceFromHouseholdButton is not null)
                _createIncomeSourceFromHouseholdButton.Click += async (_, _) => await CreateIncomeSourceFromHouseholdAsync();

            var cancelButton = this.FindControl<Button>("CancelButton");
            if (cancelButton is not null)
                cancelButton.Click += (_, _) => Close(false);

            var saveButton = this.FindControl<Button>("SaveButton");
            if (saveButton is not null)
                saveButton.Click += (_, _) => SaveAndClose();
        }

        public HouseholdPerson Person => _person;

        public void SetMode(
            string title,
            HouseholdPerson person,
            IReadOnlyList<IncomeSource>? incomeSources = null,
            IReadOnlyList<HouseholdPerson>? householdPeople = null,
            Func<IncomeSource>? createBlankIncomeSource = null,
            Func<IncomeSource, bool>? saveIncomeSource = null,
            Func<IReadOnlyList<IncomeSource>>? reloadIncomeSources = null)
        {
            _createBlankIncomeSource = createBlankIncomeSource;
            _saveIncomeSource = saveIncomeSource;
            _reloadIncomeSources = reloadIncomeSources;

            _incomeSources.Clear();
            if (incomeSources is not null)
                _incomeSources.AddRange(incomeSources);

            _householdPeopleForIncomeDialog.Clear();
            if (householdPeople is not null)
                _householdPeopleForIncomeDialog.AddRange(householdPeople);

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

            PopulateIncomeSources(_person.LinkedIncomeSourceId);

            if (_isActiveCheckBox is not null)
                _isActiveCheckBox.IsChecked = _person.IsActive;

            if (_notesTextBox is not null)
                _notesTextBox.Text = _person.Notes;

            RefreshContributionVisibility();
        }

        private void PopulateIncomeSources(long selectedId)
        {
            if (_linkedIncomeSourceComboBox is null)
                return;

            _linkedIncomeSourceComboBox.Items.Clear();
            _linkedIncomeSourceComboBox.Items.Add(new ComboBoxItem { Content = "Select income source", Tag = 0L });

            foreach (var source in _incomeSources)
                _linkedIncomeSourceComboBox.Items.Add(new ComboBoxItem { Content = source.SourceName, Tag = source.Id });

            _linkedIncomeSourceComboBox.SelectedIndex = 0;

            for (var index = 1; index < _linkedIncomeSourceComboBox.ItemCount; index++)
            {
                if (_linkedIncomeSourceComboBox.Items[index] is ComboBoxItem item &&
                    item.Tag is long id &&
                    id == selectedId)
                {
                    _linkedIncomeSourceComboBox.SelectedIndex = index;
                    return;
                }
            }
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
            var mode = GetContributionHandling();

            if (_linkedIncomeSourcePanel is not null)
                _linkedIncomeSourcePanel.IsVisible = mode == "Select Income Source";

            if (_createIncomeSourcePanel is not null)
                _createIncomeSourcePanel.IsVisible = mode == "Add Income Source";
        }


        private async System.Threading.Tasks.Task CreateIncomeSourceFromHouseholdAsync()
        {
            if (_createBlankIncomeSource is null || _saveIncomeSource is null)
                return;

            var owner = this.Owner as Window;
            if (owner is null)
                return;

            var source = _createBlankIncomeSource();

            if (string.IsNullOrWhiteSpace(source.SourceName) && !string.IsNullOrWhiteSpace(_fullNameTextBox?.Text))
                source.SourceName = $"{_fullNameTextBox.Text.Trim()} Contribution";

            if (_person.Id > 0)
            {
                source.LinkedHouseholdPersonId = _person.Id;
                source.LinkedHouseholdPersonName = _person.FullName;
            }

            var dialog = new IncomeSourceDialog();
            dialog.SetMode("Add Income Source", source, _householdPeopleForIncomeDialog);

            var result = await dialog.ShowDialog<bool>(owner);
            if (!result)
                return;

            if (!_saveIncomeSource(dialog.Source))
                return;

            _incomeSources.Clear();
            if (_reloadIncomeSources is not null)
                _incomeSources.AddRange(_reloadIncomeSources());

            PopulateIncomeSources(dialog.Source.Id);
            SelectContributionHandling("Select Income Source");
            RefreshContributionVisibility();
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

            if (_person.ContributionHandling == "Select Income Source" &&
                _linkedIncomeSourceComboBox?.SelectedItem is ComboBoxItem selectedIncome &&
                selectedIncome.Tag is long incomeId &&
                incomeId > 0)
            {
                _person.LinkedIncomeSourceId = incomeId;
                _person.LinkedIncomeSourceName = selectedIncome.Content?.ToString() ?? string.Empty;
            }
            else
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
