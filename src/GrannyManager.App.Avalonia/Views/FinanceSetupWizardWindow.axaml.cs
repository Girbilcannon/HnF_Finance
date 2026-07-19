using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using GrannyManager.App.Avalonia.ViewModels.Sections;
using GrannyManager.Application.Services;
using GrannyManager.Core.Models;
using GrannyManager.Data.Database;
using GrannyManager.Data.Repositories;

using FolderPickerOpenOptions = global::Avalonia.Platform.Storage.FolderPickerOpenOptions;
using FilePickerOpenOptions = global::Avalonia.Platform.Storage.FilePickerOpenOptions;
using FilePickerFileType = global::Avalonia.Platform.Storage.FilePickerFileType;
namespace GrannyManager.App.Avalonia.Views
{
    public partial class FinanceSetupWizardWindow : Window
    {
        private static readonly string[] FrequencyOptions =
        {
            "Weekly",
            "Every 2 Weeks",
            "Twice Monthly",
            "Monthly",
            "Quarterly",
            "Yearly",
            "One Time"
        };

        private readonly DashboardViewModel _dashboard;
        private string _primaryPersonName = string.Empty;
        private string _caseManagerName = string.Empty;
        private string _caseName = string.Empty;
        private string _caseRootFolder = string.Empty;
        private string _casePin = string.Empty;
        private string _confirmCasePin = string.Empty;
        private long _primaryPersonId;

        private string _pendingPersonName = string.Empty;
        private string _pendingPersonRelationship = string.Empty;
        private string _pendingPersonRole = string.Empty;
        private bool _pendingPersonLivesInHousehold = true;
        private bool _pendingPersonPaysRent;
        private bool _pendingPersonUsesVehicle;
        private bool _pendingPersonReceivesRides;
        private string _pendingPersonNotes = string.Empty;

        private bool _isUpdatingJumpCombo;
        private readonly List<ComboBox> _wizardBankAccountCombos = new();

        public FinanceSetupWizardWindow(DashboardViewModel dashboard)
        {
            _dashboard = dashboard ?? throw new ArgumentNullException(nameof(dashboard));
            InitializeComponent();

            _caseRootFolder = _dashboard.DefaultCaseRootFolder;
            InitializeJumpCombo();
            ShowStart();
        }

        private void InitializeJumpCombo()
        {
            JumpComboBox.ItemsSource = new[]
            {
                "Choose a section...",
                "Household / People",
                "Income Sources",
                "Bills / Spending",
                "Allowance / Savings",
                "Assets",
                "Debts",
                "Finish Setup"
            };

            JumpComboBox.SelectedIndex = 0;
            JumpBarBorder.IsVisible = _dashboard.HasActiveCase;
        }

        private void JumpComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingJumpCombo)
                return;

            var value = JumpComboBox.SelectedItem?.ToString() ?? string.Empty;
            if (value == "Choose a section...")
                return;

            ResetJumpCombo();

            switch (value)
            {
                case "Household / People":
                    NotifyWizardDataChanged();
                    ShowHouseholdIntro();
                    break;
                case "Income Sources":
                    ShowIncomeIntro();
                    break;
                case "Bills / Spending":
                    ShowBillsIntro();
                    break;
                case "Allowance / Savings":
                    ShowAllowanceIntro();
                    break;
                case "Assets":
                    ShowAssetsIntro();
                    break;
                case "Debts":
                    ShowDebtsIntro();
                    break;
                case "Finish Setup":
                    NotifyWizardDataChanged();
            ShowFinish();
                    break;
            }
        }

        private void ResetJumpCombo()
        {
            _isUpdatingJumpCombo = true;
            JumpComboBox.SelectedIndex = 0;
            _isUpdatingJumpCombo = false;
        }

        private void CloseButton_Click(object? sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ShowStart()
        {
            ClearWizard(showJump: false);
            AddTitle("Hello and welcome to your new financial management project!");
            AddParagraph("We will start with a few basics, then build the case step by step.");
            AddButtonRow(
                Button("Start a New Finance Project", ShowPrimaryPersonStep, primary: true),
                Button("Continue Setup", () =>
                {
                    if (_dashboard.HasActiveCase)
                    {
                        NotifyWizardDataChanged();
                        ShowHouseholdIntro();
                    }
                    else
                    {
                        ShowValidation("Create or open a case before continuing setup.");
                    }
                }),
                Button("Finish Later", () => Close()));
        }

        private void ShowPrimaryPersonStep()
        {
            ClearWizard(showJump: false);
            AddTitle("Who are we managing for?");
            AddParagraph("Enter the person or head-of-household name. The wizard will automatically create this person as the primary household record.");

            var nameBox = AddTextBox(_primaryPersonName, "Example: John Doe");

            AddButtonRow(
                Button("Next", () =>
                {
                    _primaryPersonName = Clean(nameBox.Text);
                    if (string.IsNullOrWhiteSpace(_primaryPersonName))
                    {
                        ShowValidation("Please enter the person or head-of-household name.");
                        return;
                    }

                    ShowCaseManagerStep();
                }, primary: true),
                Button("Back", ShowStart));
        }


        private void ShowCaseManagerStep()
        {
            ClearWizard(showJump: false);
            AddTitle("Who is managing this case?");
            AddParagraph("Enter the person responsible for managing this finance case. This name is used on the dashboard and case profile.");

            var managerBox = AddTextBox(_caseManagerName, "Example: Jane Doe");

            AddButtonRow(
                Button("Next", () =>
                {
                    _caseManagerName = Clean(managerBox.Text);
                    if (string.IsNullOrWhiteSpace(_caseManagerName))
                    {
                        ShowValidation("Please enter the name of the person managing this case.");
                        return;
                    }

                    ShowCaseNameStep();
                }, primary: true),
                Button("Back", ShowPrimaryPersonStep));
        }

        private void ShowCaseNameStep()
        {
            ClearWizard(showJump: false);
            AddTitle("Name the project");
            AddParagraph("Give your project a clear name so it is easy to find later.");

            if (string.IsNullOrWhiteSpace(_caseName))
                _caseName = $"{_primaryPersonName} Finances";

            var caseBox = AddTextBox(_caseName, "Example: Mom Finances");

            AddButtonRow(
                Button("Next", () =>
                {
                    _caseName = Clean(caseBox.Text);
                    if (string.IsNullOrWhiteSpace(_caseName))
                    {
                        ShowValidation("Please enter a case / project name.");
                        return;
                    }

                    ShowSaveLocationStep();
                }, primary: true),
                Button("Back", ShowCaseManagerStep));
        }

        private void ShowSaveLocationStep()
        {
            ClearWizard(showJump: false);
            AddTitle("Choose where to save it");
            AddParagraph("Choose the folder where case folders should be created.");

            var folderBox = AddTextBox(_caseRootFolder, string.Empty, 620);
            var browseButton = Button("Browse", async () =>
            {
                var folders = await StorageProvider.OpenFolderPickerAsync(
                    new FolderPickerOpenOptions
                    {
                        Title = "Choose where Home & Family Finance Manager cases should be saved",
                        AllowMultiple = false
                    });

                var selected = folders.FirstOrDefault();
                if (selected is not null)
                    folderBox.Text = selected.Path.LocalPath;
            });

            WizardHost.Children.Add(browseButton);

            AddButtonRow(
                Button("Next", () =>
                {
                    _caseRootFolder = Clean(folderBox.Text);
                    if (string.IsNullOrWhiteSpace(_caseRootFolder))
                    {
                        ShowValidation("Choose where the case should be saved.");
                        return;
                    }

                    ShowCasePinStep();
                }, primary: true),
                Button("Back", ShowCaseNameStep));
        }

        private void ShowCasePinStep()
        {
            ClearWizard(showJump: false);
            AddTitle("Create the case PIN");
            AddParagraph("This PIN protects the case. It is required for opening protected areas and revealing sensitive information.");

            var pinBox = AddTextBox(_casePin, "4 digits", 220, password: true, maxLength: 4);
            var confirmBox = AddTextBox(_confirmCasePin, "Confirm PIN", 220, password: true, maxLength: 4);

            AddWarning("Write this PIN down somewhere safe. This local app does not have cloud account recovery.");

            AddButtonRow(
                Button("Let's Get Started!", () =>
                {
                    _casePin = Clean(pinBox.Text);
                    _confirmCasePin = Clean(confirmBox.Text);

                    if (_casePin.Length != 4 || _casePin.Any(c => !char.IsDigit(c)))
                    {
                        ShowValidation("Security PIN must be exactly 4 digits.");
                        return;
                    }

                    if (!string.Equals(_casePin, _confirmCasePin, StringComparison.Ordinal))
                    {
                        ShowValidation("Security PIN and confirm PIN do not match.");
                        return;
                    }

                    CreateCaseAndBegin();
                }, primary: true),
                Button("Back", ShowSaveLocationStep));
        }

        private void CreateCaseAndBegin()
        {
            var request = new NewCaseRequest
            {
                PrimaryPersonName = _primaryPersonName,
                CaseName = _caseName,
                CaseManagerName = _caseManagerName,
                CaseRootFolder = _caseRootFolder,
                SecurityPin = _casePin,
                ConfirmSecurityPin = _confirmCasePin
            };

            if (!_dashboard.TryCreateCase(request, out var message))
            {
                ShowValidation(message);
                return;
            }

            // Do not create the primary person as a Household row here.
            // The active case already represents Primary / Self, and other sections use that virtual option.
            _primaryPersonId = 0;
            JumpBarBorder.IsVisible = true;
            ShowPrimaryIncomeIntro();
        }


        private void ShowPrimaryIncomeIntro()
        {
            ClearWizard();
            AddTitle($"{PrimaryPersonOrCase()}'s income");
            AddParagraph("Before adding other household members, let's set up the primary person's main income source. This gives the case a useful starting point right away.");

            AddButtonRow(
                Button("Add Primary Income", () => ShowIncomeAdd(
                    ShowPrimaryIncomeIntro,
                    () => ShowPrimaryIncomeMore(),
                    0,
                    "Self (Primary Person)",
                    $"{PrimaryPersonOrCase()} Income",
                    "Social Security"), primary: true),
                Button("Skip Primary Income", ShowHouseholdIntro),
                Button("Finish Later", () => Close()));
        }

        private void ShowPrimaryIncomeMore()
        {
            ClearWizard();
            AddTitle("Primary income saved");
            AddParagraph("Would you like to add another income source for the primary person, or continue to household members?");

            AddButtonRow(
                Button("Add Another Primary Income", () => ShowIncomeAdd(
                    ShowPrimaryIncomeIntro,
                    ShowPrimaryIncomeMore,
                    0,
                    "Self (Primary Person)",
                    $"{PrimaryPersonOrCase()} Income",
                    "Social Security"), primary: true),
                Button("Continue to Household", ShowHouseholdIntro),
                Button("Finish Later", () => Close()));
        }

        private void ShowHouseholdIntro()
        {
            ClearWizard();
            AddTitle($"Let's look at {PrimaryPersonOrCase()}'s household");
            AddParagraph($"Very good. Let's first look at {PrimaryPersonOrCase()}'s household and the other people living there or involved in the finances.");

            AddButtonRow(
                Button("Add Person", ShowPersonBasics, primary: true),
                Button("Skip Household", ShowIncomeIntro),
                Button("Finish Later", () => Close()));
        }

        private void ShowPersonBasics()
        {
            ClearWizard();
            AddTitle("Add a household / case person");
            AddParagraph("Start with the basics. Relationship is how they are connected to the primary person. Role is why they matter to this case.");

            var nameBox = AddField("Name", _pendingPersonName, "Full legal/preferred name");
            var relationshipBox = AddField("Relationship", _pendingPersonRelationship, "Example: Son, Daughter, Sister, Trustee");
            var roleBox = AddField("Role / purpose", _pendingPersonRole, "Example: Lives in home, pays rent, driver, trustee");

            AddButtonRow(
                Button("Next", () =>
                {
                    _pendingPersonName = Clean(nameBox.Text);
                    _pendingPersonRelationship = Clean(relationshipBox.Text);
                    _pendingPersonRole = Clean(roleBox.Text);

                    if (string.IsNullOrWhiteSpace(_pendingPersonName))
                    {
                        ShowValidation("Please enter this person's name.");
                        return;
                    }

                    ShowPersonDetails();
                }, primary: true),
                Button("Back", ShowHouseholdIntro));
        }

        private void ShowPersonDetails()
        {
            ClearWizard();
            AddTitle($"Details for {_pendingPersonName}");
            AddParagraph("These details help explain who lives in the household, who uses vehicles, and who receives help.");

            var lives = AddCheckBox("Lives in household", _pendingPersonLivesInHousehold);
            var pays = AddCheckBox("Pays rent / contributes", _pendingPersonPaysRent);
            var vehicle = AddCheckBox("Uses household vehicle", _pendingPersonUsesVehicle);
            var rides = AddCheckBox("Receives rides / transport help", _pendingPersonReceivesRides);
            var notesBox = AddMultiLine("Notes", _pendingPersonNotes);

            AddButtonRow(
                Button("Next", () =>
                {
                    _pendingPersonLivesInHousehold = lives.IsChecked == true;
                    _pendingPersonPaysRent = pays.IsChecked == true;
                    _pendingPersonUsesVehicle = vehicle.IsChecked == true;
                    _pendingPersonReceivesRides = rides.IsChecked == true;
                    _pendingPersonNotes = Clean(notesBox.Text);
                    ShowPersonContribution();
                }, primary: true),
                Button("Back", ShowPersonBasics));
        }

        private void ShowPersonContribution()
        {
            ClearWizard();
            AddTitle("Financial contribution");
            AddParagraph($"Does {_pendingPersonName} contribute any kind of physical cash/check/direct deposit assistance to {PrimaryPersonOrCase()}?");
            AddWarning("Only enter money transferred into the case/household budget. Do not enter bills, utilities, formal rent, or other payments this person directly controls, because that can double-count money.");

            var incomeSources = LoadIncomeSourceChoices();
            var options = new List<string> { "No Contribution" };
            options.AddRange(incomeSources.Select(i => i.Display));

            var existingIncome = AddCombo("Existing contribution income source", options.ToArray(), "No Contribution");

            var addIncomeButton = Button("+ Add Income Source", ShowPersonCreateContributionIncome, primary: true);
            WizardHost.Children.Add(addIncomeButton);

            AddButtonRow(
                Button("Save Person", () =>
                {
                    var selectedText = SelectedText(existingIncome, "No Contribution");
                    if (selectedText == "No Contribution")
                    {
                        SavePendingPerson("No Contribution", 0, string.Empty);
                        return;
                    }

                    var selected = incomeSources.FirstOrDefault(i => i.Display == selectedText);
                    SavePendingPerson("Select Income Source", selected?.Id ?? 0, selected?.Name ?? string.Empty);
                }, primary: true),
                Button("Back", ShowPersonDetails));
        }

        private void ShowPersonSelectContributionIncome()
        {
            ClearWizard();
            AddTitle("Select contribution income source");
            AddParagraph("Choose the existing income source that represents this person's contribution. This prevents the same money from being counted twice.");

            var incomeSources = LoadIncomeSourceChoices();
            if (incomeSources.Count == 0)
            {
                AddWarning("No income sources exist yet. Create one now, or go back and choose No Contribution for now.");
                AddButtonRow(Button("Create Income Source Now", ShowPersonCreateContributionIncome, primary: true), Button("Back", ShowPersonContribution));
                return;
            }

            var combo = AddCombo("Linked income source", incomeSources.Select(i => i.Display).ToArray(), incomeSources[0].Display);

            AddButtonRow(
                Button("Save Person", () =>
                {
                    var selected = incomeSources.FirstOrDefault(i => i.Display == SelectedText(combo, string.Empty));
                    SavePendingPerson("Select Income Source", selected?.Id ?? 0, selected?.Name ?? string.Empty);
                }, primary: true),
                Button("Back", ShowPersonContribution));
        }

        private void ShowPersonCreateContributionIncome()
        {
            ClearWizard();
            AddTitle("Create contribution income source");
            AddParagraph($"Create the income record for {_pendingPersonName}'s contribution. After saving, the wizard returns here and saves {_pendingPersonName} with the contribution linked.");

            var sourceName = AddField("Income source name", $"{_pendingPersonName} contribution", string.Empty);
            var type = AddCombo("Income type", new[] { "Family Contribution", "Rental Income", "Employment / Wages", "Other" }, "Family Contribution");
            var taxes = AddCheckBox("Taxes withheld", false);
            var amount = AddField("Gross Pay / Payment Amount", string.Empty, "0.00");
            var frequency = AddCombo("Frequency", FrequencyOptions, "Monthly");
            var expected = AddField("Expected day/date", string.Empty, "Example: 1st, Fridays, every payday");

            var deposit = AddCombo("Deposit destination", new[] { "Cash/Check", "Select Bank Account", "Select Multiple Bank Accounts", "Other" }, "Cash/Check");
            var singleBank = AddSingleBankAccountPanel($"{_pendingPersonName} contribution account");
            var multipleBank = AddMultipleBankDepositPanel($"{_pendingPersonName} contribution account");
            var otherDeposit = AddOtherDepositPanel("Other deposit destination / notes", "Example: prepaid card, money order, cash app");

            deposit.SelectionChanged += (_, _) => RefreshWizardDepositPanels(deposit, singleBank.Panel, multipleBank.Panel, otherDeposit.Panel, amount);
            RefreshWizardDepositPanels(deposit, singleBank.Panel, multipleBank.Panel, otherDeposit.Panel, amount);

            var notes = AddMultiLine("Notes", $"Created from Finance Setup Wizard for {_pendingPersonName}.");

            AddButtonRow(
                Button("Create Income + Save Person", () =>
                {
                    try
                    {
                        long bankId = 0;
                        string bankName = string.Empty;
                        var depositChoice = SelectedText(deposit, "Cash/Check");

                        if (depositChoice == "Select Bank Account")
                        {
                            var selectedBank = GetSelectedBankAccount(singleBank.ComboBox);
                            if (selectedBank is null)
                            {
                                ShowValidation("Choose a bank account or use + Create Bank Account.");
                                return;
                            }

                            bankId = selectedBank.Id;
                            bankName = selectedBank.Name;
                        }
                        else if (depositChoice == "Other")
                        {
                            var other = Clean(otherDeposit.TextBox.Text);
                            if (!string.IsNullOrWhiteSpace(other))
                                depositChoice = other;
                        }
                        else if (depositChoice == "Select Multiple Bank Accounts")
                        {
                            bankId = GetFirstMultipleBankDepositId(multipleBank.Lines);
                            bankName = BuildMultipleBankDepositText(multipleBank.Lines);

                            if (string.IsNullOrWhiteSpace(bankName))
                            {
                                ShowValidation("Choose at least one bank account and enter deposit amounts.");
                                return;
                            }
                        }

                        var trueIncomeAmount = depositChoice == "Select Multiple Bank Accounts"
                            ? SumMultipleBankDepositAmounts(multipleBank.Lines)
                            : ParseMoney(amount.Text);

                        if (depositChoice == "Select Multiple Bank Accounts" && trueIncomeAmount <= 0m)
                        {
                            ShowValidation("Enter a deposit amount for each selected bank account.");
                            return;
                        }

                        var income = new IncomeSource
                        {
                            SourceName = string.IsNullOrWhiteSpace(sourceName.Text) ? $"{_pendingPersonName} contribution" : Clean(sourceName.Text),
                            IncomeType = SelectedText(type, "Family Contribution"),
                            TaxesWithheld = taxes.IsChecked == true,
                            Amount = trueIncomeAmount,
                            Frequency = SelectedText(frequency, "Monthly"),
                            ExpectedDayOrDate = Clean(expected.Text),
                            DepositDestination = depositChoice,
                            LinkedBankAssetId = bankId,
                            LinkedBankAssetName = bankName,
                            LinkedHouseholdPersonName = _pendingPersonName,
                            IsActive = true,
                            Notes = Clean(notes.Text)
                        };

                        new IncomeSourcesRepository(GetDatabasePath()).Upsert(income);
                        SavingsBankAccountSyncService.Sync(GetDatabasePath());
                        AppDataChangeNotifier.NotifyIncomeSourcesChanged();
                        AppDataChangeNotifier.NotifyAllowanceSavingsChanged();
                        SavePendingPerson("Select Income Source", income.Id, income.SourceName);
                    }
                    catch (Exception ex)
                    {
                        ShowValidation("Could not create the income source: " + ex.Message);
                    }
                }, primary: true),
                Button("Back", ShowPersonContribution));
        }

        private void SavePendingPerson(string contributionHandling, long linkedIncomeSourceId, string linkedIncomeSourceName)
        {
            try
            {
                new HouseholdPeopleRepository(GetDatabasePath()).Upsert(new HouseholdPerson
                {
                    FullName = _pendingPersonName,
                    Relationship = _pendingPersonRelationship,
                    Role = _pendingPersonRole,
                    LivesInHousehold = _pendingPersonLivesInHousehold,
                    PaysRent = _pendingPersonPaysRent,
                    UsesHouseholdVehicle = _pendingPersonUsesVehicle,
                    ReceivesRides = _pendingPersonReceivesRides,
                    Notes = _pendingPersonNotes,
                    ContributionHandling = contributionHandling,
                    LinkedIncomeSourceId = linkedIncomeSourceId,
                    LinkedIncomeSourceName = linkedIncomeSourceName,
                    IsActive = true
                });

                AppDataChangeNotifier.NotifyHouseholdChanged();
                ClearPendingPerson();
                NotifyWizardDataChanged();
            ShowAddMorePeople();
            }
            catch (Exception ex)
            {
                ShowValidation("Could not save the person: " + ex.Message);
            }
        }

        private void ShowAddMorePeople()
        {
            ClearWizard();
            AddTitle("GREAT! You just added a member of the household!");
            AddParagraph("Would you like to add more household members or case people now? You can always add or edit people later from the Household page.");

            AddButtonRow(
                Button("Add Another Person", ShowPersonBasics, primary: true),
                Button("Continue to Income", ShowIncomeIntro),
                Button("Finish Later", () => Close()));
        }

        private void ShowIncomeIntro()
        {
            ClearWizard();
            AddTitle("Income Sources");
            AddParagraph($"Now let's look at money coming in for {PrimaryPersonOrCase()}. This can include Social Security, pensions, work income, survivor benefits, household contributions, rental payments, and other recurring income.");

            AddButtonRow(
                Button("Add Income Source", ShowIncomeAdd, primary: true),
                Button("Skip Income", ShowBillsIntro),
                Button("Back to Household", ShowHouseholdIntro));
        }

        private void ShowIncomeAdd()
        {
            ShowIncomeAdd(ShowIncomeIntro, ShowIncomeMore, 0, string.Empty, string.Empty, "Social Security");
        }

        private void ShowIncomeAdd(Action backAction, Action savedAction, long linkedHouseholdPersonId, string linkedHouseholdPersonName, string defaultSourceName, string defaultIncomeType)
        {
            ClearWizard();
            AddTitle("Add Income Source");

            var name = AddField("Source name", defaultSourceName, "Example: Social Security");
            var type = AddCombo("Income type", new[] { "Social Security", "Pension", "Survivor Benefits", "Disability", "Employment / Wages", "Family Contribution", "Rental Income", "Retirement Account", "Settlement / Lump Sum", "Other" }, defaultIncomeType);
            var taxes = AddCheckBox("Taxes withheld", false);
            var amount = AddField("Gross Pay / Payment Amount", string.Empty, "0.00");
            var frequency = AddCombo("Frequency", FrequencyOptions, "Monthly");
            var expected = AddField("Expected day/date", string.Empty, "Example: 3rd of month");

            var deposit = AddCombo("Deposit destination", new[] { "Cash/Check", "Select Bank Account", "Select Multiple Bank Accounts", "Other" }, "Cash/Check");
            var defaultBankName = string.IsNullOrWhiteSpace(linkedHouseholdPersonName) ? "Bank Account" : $"{linkedHouseholdPersonName} bank account";
            var singleBank = AddSingleBankAccountPanel(defaultBankName);
            var multipleBank = AddMultipleBankDepositPanel(defaultBankName);
            var otherDeposit = AddOtherDepositPanel("Other deposit destination / notes", "Optional");

            deposit.SelectionChanged += (_, _) => RefreshWizardDepositPanels(deposit, singleBank.Panel, multipleBank.Panel, otherDeposit.Panel, amount);
            RefreshWizardDepositPanels(deposit, singleBank.Panel, multipleBank.Panel, otherDeposit.Panel, amount);

            var notes = AddMultiLine("Notes", string.Empty);

            AddButtonRow(
                Button("Save Income", () =>
                {
                    long bankId = 0;
                    string bankName = string.Empty;
                    var depositChoice = SelectedText(deposit, "Cash/Check");

                    if (depositChoice == "Select Bank Account")
                    {
                        var selectedBank = GetSelectedBankAccount(singleBank.ComboBox);
                        if (selectedBank is null)
                        {
                            ShowValidation("Choose a bank account or use + Create Bank Account.");
                            return;
                        }

                        bankId = selectedBank.Id;
                        bankName = selectedBank.Name;
                    }
                    else if (depositChoice == "Other")
                    {
                        var other = Clean(otherDeposit.TextBox.Text);
                        if (!string.IsNullOrWhiteSpace(other))
                            depositChoice = other;
                    }
                    else if (depositChoice == "Select Multiple Bank Accounts")
                    {
                        bankId = GetFirstMultipleBankDepositId(multipleBank.Lines);
                        bankName = BuildMultipleBankDepositText(multipleBank.Lines);

                        if (string.IsNullOrWhiteSpace(bankName))
                        {
                            ShowValidation("Choose at least one bank account and enter deposit amounts.");
                            return;
                        }
                    }

                    var trueIncomeAmount = depositChoice == "Select Multiple Bank Accounts"
                        ? SumMultipleBankDepositAmounts(multipleBank.Lines)
                        : ParseMoney(amount.Text);

                    if (depositChoice == "Select Multiple Bank Accounts" && trueIncomeAmount <= 0m)
                    {
                        ShowValidation("Enter a deposit amount for each selected bank account.");
                        return;
                    }

                    var incomeSourceToSave = new IncomeSource
                    {
                        SourceName = Clean(name.Text),
                        IncomeType = SelectedText(type, "Other"),
                        TaxesWithheld = taxes.IsChecked == true,
                        Amount = trueIncomeAmount,
                        Frequency = SelectedText(frequency, "Monthly"),
                        ExpectedDayOrDate = Clean(expected.Text),
                        DepositDestination = depositChoice,
                        LinkedBankAssetId = bankId,
                        LinkedBankAssetName = bankName,
                        LinkedHouseholdPersonId = linkedHouseholdPersonId,
                        LinkedHouseholdPersonName = linkedHouseholdPersonName,
                        Notes = Clean(notes.Text),
                        IsActive = true
                    };

                    new IncomeSourcesRepository(GetDatabasePath()).Upsert(incomeSourceToSave);
                    SavingsBankAccountSyncService.Sync(GetDatabasePath());
                    AppDataChangeNotifier.NotifyIncomeSourcesChanged();
                    AppDataChangeNotifier.NotifyAllowanceSavingsChanged();
                    savedAction();
                }, primary: true),
                Button("Cancel", backAction));
        }

        private void ShowIncomeMore()
        {
            ClearWizard();
            AddTitle("Income saved");
            AddParagraph("Would you like to add another income source, or continue to bills and spending?");
            AddButtonRow(Button("Add Another Income", ShowIncomeAdd, primary: true), Button("Continue to Bills", ShowBillsIntro), Button("Finish Later", () => Close()));
        }

        private void ShowBillsIntro()
        {
            ClearWizard();
            AddTitle("Bills / Spending");
            AddParagraph("Next we'll enter regular bills, shared responsibilities, and known monthly costs. This is the main money-going-out section.");

            AddButtonRow(Button("Add Bill / Expense", ShowBillAdd, primary: true), Button("Skip Bills", ShowAllowanceIntro), Button("Back to Income", ShowIncomeIntro));
        }

        private void ShowBillAdd()
        {
            ClearWizard();
            AddTitle("Add Bill / Expense");

            var name = AddField("Bill / Expense name", string.Empty, "Example: Electric Bill");
            var cat = AddCombo("Category", new[] { "Housing", "Utilities", "Vehicle", "Insurance", "Food", "Medical", "Debt Payment", "Legal", "Other" }, "Utilities");
            var amount = AddField("Amount", string.Empty, "0.00");
            var frequency = AddCombo("Frequency", FrequencyOptions, "Monthly");
            var due = AddField("Due date", string.Empty, "Example: 15th");
            var payer = AddPersonCombo("Who Pays This?");
            var owner = AddPersonCombo("Responsibility / Owner");
            var pastDue = AddField("Past due amount", string.Empty, "0.00");
            var priority = AddCombo("Priority", new[] { "Low", "Normal", "High", "Urgent" }, "Normal");
            var autopay = AddCheckBox("Autopay", false);
            var notes = AddMultiLine("Notes", string.Empty);

            AddButtonRow(
                Button("Save Bill", () =>
                {
                    new BillsRepository(GetDatabasePath()).Upsert(new Bill
                    {
                        BillName = Clean(name.Text),
                        Category = SelectedText(cat, "Other"),
                        Amount = ParseMoney(amount.Text),
                        Frequency = SelectedText(frequency, "Monthly"),
                        DueDate = Clean(due.Text),
                        PaidBy = SelectedText(payer, "Self (Primary Person)"),
                        ResponsibilityOwner = SelectedText(owner, "Self (Primary Person)"),
                        PastDueAmount = ParseMoney(pastDue.Text),
                        Priority = SelectedText(priority, "Normal"),
                        IsAutopay = autopay.IsChecked == true,
                        PaymentMethod = "Cash/Check",
                        Notes = Clean(notes.Text),
                        IsActive = true
                    });

                    AppDataChangeNotifier.NotifyBillsChanged();
                    NotifyWizardDataChanged();
                    ShowBillsMore();
                }, primary: true),
                Button("Cancel", ShowBillsIntro));
        }

        private void ShowBillsMore()
        {
            ClearWizard();
            AddTitle("Bill saved");
            AddParagraph("Would you like to add another bill, or continue to Allowance / Savings?");
            AddButtonRow(Button("Add Another Bill", ShowBillAdd, primary: true), Button("Continue to Allowance / Savings", ShowAllowanceIntro), Button("Finish Later", () => Close()));
        }

        private void ShowAllowanceIntro()
        {
            ClearWizard();
            AddTitle("Allowance / Savings");
            AddParagraph("This section reserves money for fun-money allowances or savings goals without mixing those amounts into regular bills.");

            AddButtonRow(Button("Add Allowance / Savings", ShowAllowanceAdd, primary: true), Button("Skip Allowance / Savings", ShowAssetsIntro), Button("Back to Bills", ShowBillsIntro));
        }

        private void ShowAllowanceAdd()
        {
            ClearWizard();
            AddTitle("Add Allowance / Savings");

            var name = AddField("Name / purpose", string.Empty, "Example: Grocery buffer, emergency savings");
            var type = AddCombo("Type", new[] { "Allowance", "Savings" }, "Allowance");
            var amount = AddField("Amount", string.Empty, "0.00");
            var frequency = AddCombo("Frequency", FrequencyOptions, "Monthly");

            var storage = AddCombo("Storage / Account", new[] { "Cash/Check", "Select Bank Account", "Select Multiple Bank Accounts", "Other" }, "Cash/Check");
            var singleBank = AddSingleBankAccountPanel("Savings Account");
            var multipleBank = AddMultipleBankDepositPanel("Savings Account");
            var otherStorage = AddOtherDepositPanel("Other storage notes", "Example: safe, prepaid card, family-held cash");

            storage.SelectionChanged += (_, _) => RefreshWizardDepositPanels(storage, singleBank.Panel, multipleBank.Panel, otherStorage.Panel, amount);
            RefreshWizardDepositPanels(storage, singleBank.Panel, multipleBank.Panel, otherStorage.Panel, amount);

            var notes = AddMultiLine("Notes", string.Empty);

            AddButtonRow(
                Button("Save Allowance / Savings", () =>
                {
                    var method = SelectedText(storage, "Cash/Check");
                    long bankId = 0;
                    string bankName = string.Empty;
                    var itemAmount = ParseMoney(amount.Text);

                    if (method == "Select Bank Account")
                    {
                        var selectedBank = GetSelectedBankAccount(singleBank.ComboBox);
                        if (selectedBank is null)
                        {
                            ShowValidation("Choose a bank account or use + Create Bank Account.");
                            return;
                        }

                        bankId = selectedBank.Id;
                        bankName = selectedBank.Name;
                    }
                    else if (method == "Select Multiple Bank Accounts")
                    {
                        bankId = GetFirstMultipleBankDepositId(multipleBank.Lines);
                        bankName = BuildMultipleBankDepositText(multipleBank.Lines);
                        itemAmount = SumMultipleBankDepositAmounts(multipleBank.Lines);

                        if (string.IsNullOrWhiteSpace(bankName) || itemAmount <= 0m)
                        {
                            ShowValidation("Choose at least one bank account and enter deposit amounts.");
                            return;
                        }
                    }
                    else if (method == "Other")
                    {
                        var other = Clean(otherStorage.TextBox.Text);
                        if (!string.IsNullOrWhiteSpace(other))
                            method = other;
                    }

                    var item = new AllowanceSavingsItem
                    {
                        ItemName = Clean(name.Text),
                        ItemType = SelectedText(type, "Allowance"),
                        Amount = itemAmount,
                        Frequency = SelectedText(frequency, "Monthly"),
                        StorageMethod = method,
                        WhereStored = !string.IsNullOrWhiteSpace(bankName) ? bankName : method,
                        LinkedBankAssetId = bankId,
                        LinkedBankAssetName = bankName,
                        Notes = Clean(notes.Text),
                        IsActive = true
                    };

                    new AllowanceSavingsRepository(GetDatabasePath()).Upsert(item);
                    AppDataChangeNotifier.NotifyAllowanceSavingsChanged();
                    NotifyWizardDataChanged();
                    ShowAllowanceMore();
                }, primary: true),
                Button("Cancel", ShowAllowanceIntro));
        }

        private void ShowAllowanceMore()
        {
            ClearWizard();
            AddTitle("Allowance / Savings saved");
            AddParagraph("Would you like to add another allowance or savings goal, or continue to Assets?");
            AddButtonRow(Button("Add Another", ShowAllowanceAdd, primary: true), Button("Continue to Assets", ShowAssetsIntro), Button("Finish Later", () => Close()));
        }

        private void ShowAssetsIntro()
        {
            ClearWizard();
            AddTitle("Assets");
            AddParagraph("Assets are real-world things such as vehicles, properties, bank accounts, valuable items, or other things the case person owns, controls, or is responsible for.");

            AddButtonRow(Button("Add Asset", ShowAssetType, primary: true), Button("Skip Assets", ShowDebtsIntro), Button("Back to Allowance / Savings", ShowAllowanceIntro));
        }

        private void ShowAssetType()
        {
            ClearWizard();
            AddTitle("Choose asset type");

            var type = AddCombo("Asset type", new[] { "Vehicle", "Property", "Bank Account", "Valuable Item", "Other" }, "Vehicle");

            AddButtonRow(Button("Next", () => ShowAssetAdd(SelectedText(type, "Other")), primary: true), Button("Back", ShowAssetsIntro));
        }

        private void ShowAssetAdd(string assetType)
        {
            ClearWizard();
            AddTitle($"Add {assetType} Asset");

            var name = AddField("Asset name", string.Empty, "Example: 2021 Jeep Compass");
            var value = AddField("Estimated value", string.Empty, "0.00");
            var institution = AddField("Location / institution", string.Empty, string.Empty);
            var accountType = AddField("Account type / details", string.Empty, "Checking, vehicle, property, item details");
            var lastFour = AddField("Last four / identifier", string.Empty, string.Empty);
            var notes = AddMultiLine("Notes", string.Empty);

            AddButtonRow(
                Button("Save Asset", () =>
                {
                    new AssetsRepository(GetDatabasePath()).Upsert(new AssetItem
                    {
                        AssetName = Clean(name.Text),
                        AssetType = assetType,
                        EstimatedValue = ParseMoney(value.Text),
                        InstitutionName = Clean(institution.Text),
                        AccountType = Clean(accountType.Text),
                        AccountLastFour = Clean(lastFour.Text),
                        Notes = Clean(notes.Text),
                        IsActive = true
                    });

                    AppDataChangeNotifier.NotifyAssetsChanged();
                    NotifyWizardDataChanged();
                    ShowAssetsMore();
                }, primary: true),
                Button("Cancel", ShowAssetsIntro));
        }

        private AssetItem? CreateBankAccountFromWizard(string accountName, string accountType, decimal estimatedBalance)
        {
            if (string.IsNullOrWhiteSpace(accountName))
            {
                ShowValidation("Enter a bank account name before saving.");
                return null;
            }

            var asset = new AssetItem
            {
                AssetName = accountName.Trim(),
                AssetType = "Bank Account",
                AccountType = string.IsNullOrWhiteSpace(accountType) ? "Checking" : accountType.Trim(),
                EstimatedValue = estimatedBalance,
                Notes = "Created by Finance Setup Wizard.",
                IsActive = true
            };

            new AssetsRepository(GetDatabasePath()).Upsert(asset);
            SavingsBankAccountSyncService.Sync(GetDatabasePath());
            AppDataChangeNotifier.NotifyAssetsChanged();
            AppDataChangeNotifier.NotifyAllowanceSavingsChanged();
            return asset;
        }

        private void ShowAssetsMore()
        {
            ClearWizard();
            AddTitle("Asset saved");
            AddParagraph("Would you like to add another asset, or continue to Debts?");
            AddButtonRow(Button("Add Another Asset", ShowAssetType, primary: true), Button("Continue to Debts", ShowDebtsIntro), Button("Finish Later", () => Close()));
        }

        private void ShowDebtsIntro()
        {
            ClearWizard();
            AddTitle("Debts");
            AddParagraph("Debts are obligations owed to creditors, collectors, agencies, family members, or other parties. If the monthly payment is already in Bills, link it later so it does not get counted twice.");

            AddButtonRow(Button("Add Debt", ShowDebtAdd, primary: true), Button("Skip Debts", ShowFinish), Button("Back to Assets", ShowAssetsIntro));
        }

        private void ShowDebtAdd()
        {
            ClearWizard();
            AddTitle("Add Debt");

            var name = AddField("Debt name", string.Empty, "Example: IRS payment plan");
            var type = AddCombo("Debt type", new[] { "Credit Card", "Auto Loan", "Personal Loan", "IRS / Tax", "Medical", "Collection", "Family Loan", "Other" }, "Other");
            var creditor = AddField("Creditor / collector", string.Empty, string.Empty);
            var balance = AddField("Current balance", string.Empty, "0.00");
            var minimum = AddField("Minimum payment", string.Empty, "0.00");
            var frequency = AddCombo("Payment frequency", FrequencyOptions, "Monthly");
            var owner = AddPersonCombo("Responsibility / Owner");
            var paidBy = AddPersonCombo("Who Pays This?");
            var status = AddCombo("Status", new[] { "Current", "Past Due", "In Collections", "Disputed", "Unknown" }, "Current");
            var priority = AddCombo("Priority", new[] { "Low", "Normal", "High", "Urgent" }, "Normal");
            var notes = AddMultiLine("Notes", string.Empty);

            AddButtonRow(
                Button("Save Debt", () =>
                {
                    new DebtsRepository(GetDatabasePath()).Upsert(new Debt
                    {
                        DebtName = Clean(name.Text),
                        DebtType = SelectedText(type, "Other"),
                        CreditorCollector = Clean(creditor.Text),
                        CurrentBalance = ParseMoney(balance.Text),
                        MinimumPayment = ParseMoney(minimum.Text),
                        PaymentFrequency = SelectedText(frequency, "Monthly"),
                        ResponsibilityOwner = SelectedText(owner, "Self (Primary Person)"),
                        PaidBy = SelectedText(paidBy, "Self (Primary Person)"),
                        Status = SelectedText(status, "Current"),
                        Priority = SelectedText(priority, "Normal"),
                        PaymentTracking = "Not Linked",
                        Notes = Clean(notes.Text),
                        IsActive = true
                    });

                    AppDataChangeNotifier.NotifyDebtsChanged();
                    NotifyWizardDataChanged();
                    ShowDebtsMore();
                }, primary: true),
                Button("Cancel", ShowDebtsIntro));
        }

        private void ShowDebtsMore()
        {
            ClearWizard();
            AddTitle("Debt saved");
            AddParagraph("Would you like to add another debt, or finish the setup wizard?");
            AddButtonRow(Button("Add Another Debt", ShowDebtAdd, primary: true), Button("Finish Setup", ShowFinish), Button("Finish Later", () => Close()));
        }

        private void ShowDocumentsIntro()
        {
            NotifyWizardDataChanged();
            ShowFinish();
        }

        private void ShowDocumentAdd()
        {
            NotifyWizardDataChanged();
            ShowFinish();
        }

        private void ShowDocumentsMore()
        {
            NotifyWizardDataChanged();
            ShowFinish();
        }

        private void ShowFinish()
        {
            ClearWizard();
            AddTitle("Setup complete for now");
            AddParagraph("Great work. Your case now has a guided foundation. Documents can be added later from the Documents section, where the full importer, folder choices, tags, links, and PDF options are available.");
            AddButtonRow(Button("Go to Dashboard", () => Close(), primary: true), Button("Add More Household People", ShowHouseholdIntro), Button("Add More Debts", ShowDebtsIntro));
        }

        private void NotifyWizardDataChanged()
        {
            AppDataChangeNotifier.NotifyAllFinanceChanged();
        }

        private void ClearPendingPerson()
        {
            _pendingPersonName = string.Empty;
            _pendingPersonRelationship = string.Empty;
            _pendingPersonRole = string.Empty;
            _pendingPersonLivesInHousehold = true;
            _pendingPersonPaysRent = false;
            _pendingPersonUsesVehicle = false;
            _pendingPersonReceivesRides = false;
            _pendingPersonNotes = string.Empty;
        }

        private string GetDatabasePath()
        {
            if (!_dashboard.HasActiveCase || string.IsNullOrWhiteSpace(_dashboard.ActiveCasePath))
                throw new InvalidOperationException("No active case is open.");

            return CaseDatabaseLocator.GetDatabasePathForCaseFolder(_dashboard.ActiveCasePath);
        }

        private string PrimaryPersonOrCase()
        {
            if (!string.IsNullOrWhiteSpace(_primaryPersonName))
                return _primaryPersonName;

            if (!string.IsNullOrWhiteSpace(_dashboard.ActiveCasePrimaryPerson) && _dashboard.ActiveCasePrimaryPerson != "None")
                return _dashboard.ActiveCasePrimaryPerson;

            return "the case person";
        }

        private IReadOnlyList<ChoiceItem> LoadIncomeSourceChoices()
        {
            try
            {
                return new IncomeSourcesRepository(GetDatabasePath())
                    .GetAll()
                    .Select(i => new ChoiceItem(i.Id, i.SourceName, $"{i.SourceName} ({i.IncomeType})"))
                    .ToList();
            }
            catch
            {
                return Array.Empty<ChoiceItem>();
            }
        }

        private ComboBox AddPersonCombo(string label)
        {
            var names = new List<string> { "Self (Primary Person)" };

            try
            {
                foreach (var person in new HouseholdPeopleRepository(GetDatabasePath()).GetAll())
                {
                    if (!string.IsNullOrWhiteSpace(person.FullName) && !names.Contains(person.FullName))
                        names.Add(person.FullName);
                }
            }
            catch
            {
            }

            return AddCombo(label, names.ToArray(), names[0]);
        }


        private (StackPanel Panel, ComboBox ComboBox) AddSingleBankAccountPanel(string defaultBankName)
        {
            var panel = new StackPanel
            {
                Spacing = 8,
                HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Left
            };

            panel.Children.Add(Label("Bank account"));

            var combo = CreateBankAccountComboControl();
            combo.Width = 440;
            combo.HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Left;
            panel.Children.Add(combo);

            AddInlineBankAccountCreator(panel, defaultBankName, created =>
            {
                RefreshAllWizardBankAccountCombos();
                combo.SelectedItem = created.DisplayName;
            });

            WizardHost.Children.Add(panel);
            return (panel, combo);
        }

        private (StackPanel Panel, List<MultiBankDepositLine> Lines) AddMultipleBankDepositPanel(string defaultBankName)
        {
            var panel = new StackPanel
            {
                Spacing = 8,
                HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Left
            };

            panel.Children.Add(Label("Bank accounts / deposit amounts"));

            var host = new StackPanel
            {
                Spacing = 8,
                Width = 620,
                HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Left
            };

            var lines = new List<MultiBankDepositLine>();

            void AddLine(long selectedId = 0)
            {
                var row = new Grid
                {
                    Width = 620,
                    ColumnDefinitions = new ColumnDefinitions("*,160,Auto"),
                    ColumnSpacing = 8,
                    HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Left
                };

                var combo = CreateBankAccountComboControl();
                Grid.SetColumn(combo, 0);
                row.Children.Add(combo);

                if (selectedId > 0)
                {
                    var account = LoadBankAccountChoices().FirstOrDefault(item => item.Id == selectedId);
                    if (account is not null)
                        combo.SelectedItem = account.Display;
                }

                var amountBox = new TextBox
                {
                    Watermark = "Deposit Amount",
                    Height = 34,
                    Padding = new Thickness(10, 6),
                    Background = Brush.Parse("#0F1B2A"),
                    Foreground = Brushes.White,
                    BorderBrush = Brush.Parse("#30445F")
                };

                Grid.SetColumn(amountBox, 1);
                row.Children.Add(amountBox);

                var remove = Button("🗑", () =>
                {
                    host.Children.Remove(row);
                    var line = lines.FirstOrDefault(item => item.Row == row);
                    if (line is not null)
                        lines.Remove(line);
                });

                remove.Width = 42;
                remove.Padding = new Thickness(0);
                remove.Background = Brush.Parse("#8B2E2E");
                remove.BorderBrush = Brush.Parse("#B84A4A");

                Grid.SetColumn(remove, 2);
                row.Children.Add(remove);

                host.Children.Add(row);
                lines.Add(new MultiBankDepositLine(row, combo, amountBox));
            }

            AddLine();
            panel.Children.Add(host);

            panel.Children.Add(Button("+ Add Bank Account Line", () => AddLine(), primary: true));

            AddInlineBankAccountCreator(panel, defaultBankName, created =>
            {
                RefreshAllWizardBankAccountCombos();
                AddLine(created.Id);
            });

            WizardHost.Children.Add(panel);
            return (panel, lines);
        }

        private (StackPanel Panel, TextBox TextBox) AddOtherDepositPanel(string label, string watermark)
        {
            var panel = new StackPanel
            {
                Spacing = 8
            };

            panel.Children.Add(Label(label));

            var box = new TextBox
            {
                Watermark = watermark,
                Width = 440,
                Height = 38,
                Padding = new Thickness(12, 7),
                Background = Brush.Parse("#0F1B2A"),
                Foreground = Brushes.White,
                BorderBrush = Brush.Parse("#30445F"),
                HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Left
            };

            panel.Children.Add(box);
            WizardHost.Children.Add(panel);
            return (panel, box);
        }

        private void RefreshWizardDepositPanels(ComboBox deposit, StackPanel singleBankPanel, StackPanel multipleBankPanel, StackPanel otherPanel, TextBox amountBox)
        {
            var destination = SelectedText(deposit, "Cash/Check");

            singleBankPanel.IsVisible = destination == "Select Bank Account";
            multipleBankPanel.IsVisible = destination == "Select Multiple Bank Accounts";
            otherPanel.IsVisible = destination == "Other" || destination == "Cash/Check";

            amountBox.IsEnabled = destination != "Select Multiple Bank Accounts";
        }

        private void AddInlineBankAccountCreator(StackPanel parent, string defaultBankName, Action<AssetItem> onSaved)
        {
            var form = new Border
            {
                Background = Brush.Parse("#0F1B2A"),
                BorderBrush = Brush.Parse("#30445F"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12),
                IsVisible = false
            };

            var formStack = new StackPanel
            {
                Spacing = 8
            };

            var nameBox = new TextBox
            {
                Text = defaultBankName,
                Watermark = "Bank account name",
                Height = 36,
                Padding = new Thickness(10, 6),
                Background = Brush.Parse("#122238"),
                Foreground = Brushes.White,
                BorderBrush = Brush.Parse("#30445F")
            };

            var typeCombo = new ComboBox
            {
                ItemsSource = new[] { "Checking", "Savings", "Money Market", "Other" },
                SelectedItem = "Checking",
                Height = 36,
                Background = Brush.Parse("#122238"),
                Foreground = Brushes.White,
                BorderBrush = Brush.Parse("#30445F")
            };

            var balanceBox = new TextBox
            {
                Watermark = "Current estimated balance",
                Height = 36,
                Padding = new Thickness(10, 6),
                Background = Brush.Parse("#122238"),
                Foreground = Brushes.White,
                BorderBrush = Brush.Parse("#30445F")
            };

            formStack.Children.Add(Label("Bank account name"));
            formStack.Children.Add(nameBox);
            formStack.Children.Add(Label("Account type"));
            formStack.Children.Add(typeCombo);
            formStack.Children.Add(Label("Current estimated balance"));
            formStack.Children.Add(balanceBox);

            formStack.Children.Add(Button("Save Bank Account", () =>
            {
                var created = CreateBankAccountFromWizard(
                    Clean(nameBox.Text),
                    SelectedText(typeCombo, "Checking"),
                    ParseMoney(balanceBox.Text));

                if (created is null)
                    return;

                onSaved(created);
                form.IsVisible = false;
            }, primary: true));

            form.Child = formStack;

            parent.Children.Add(Button("+ Create Bank Account", () => form.IsVisible = true, primary: true));
            parent.Children.Add(form);
        }

        private static void AddBankChoiceToCombo(ComboBox combo, AssetItem asset)
        {
            var display = asset.DisplayName;

            foreach (var item in combo.Items)
            {
                if (string.Equals(item?.ToString(), display, StringComparison.OrdinalIgnoreCase))
                    return;
            }

            combo.Items.Add(display);
        }

        private ComboBox AddBankAccountCombo(string label)
        {
            var accounts = LoadBankAccountChoices();
            var options = new List<string> { "Choose account" };
            options.AddRange(accounts.Select(a => a.Display));
            return AddCombo(label, options.ToArray(), "Choose account");
        }

        private IReadOnlyList<ChoiceItem> LoadBankAccountChoices()
        {
            try
            {
                return new AssetsRepository(GetDatabasePath())
                    .GetBankAccounts()
                    .Select(a => new ChoiceItem(a.Id, a.AssetName, a.DisplayName))
                    .ToList();
            }
            catch
            {
                return Array.Empty<ChoiceItem>();
            }
        }

        private ChoiceItem? GetSelectedBankAccount(ComboBox combo)
        {
            var selected = SelectedText(combo, "Choose account");
            if (selected == "Choose account")
                return null;

            return LoadBankAccountChoices().FirstOrDefault(a => a.Display == selected);
        }

        private List<MultiBankDepositLine> AddMultipleBankDepositLines(string label)
        {
            WizardHost.Children.Add(Label(label));

            var host = new StackPanel
            {
                Spacing = 8,
                Width = 620,
                HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Left
            };

            var lines = new List<MultiBankDepositLine>();

            void AddLine()
            {
                var row = new Grid
                {
                    Width = 620,
                    ColumnDefinitions = new ColumnDefinitions("*,160,Auto"),
                    ColumnSpacing = 8,
                    HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Left
                };

                var combo = CreateBankAccountComboControl();
                Grid.SetColumn(combo, 0);
                row.Children.Add(combo);

                var amountBox = new TextBox
                {
                    Watermark = "Deposit Amount",
                    Height = 34,
                    Padding = new Thickness(10, 6),
                    Background = Brush.Parse("#0F1B2A"),
                    Foreground = Brushes.White,
                    BorderBrush = Brush.Parse("#30445F")
                };

                Grid.SetColumn(amountBox, 1);
                row.Children.Add(amountBox);

                var remove = Button("🗑", () =>
                {
                    host.Children.Remove(row);
                    var line = lines.FirstOrDefault(item => item.Row == row);
                    if (line is not null)
                        lines.Remove(line);
                });

                remove.Width = 42;
                remove.Padding = new Thickness(0);
                remove.Background = Brush.Parse("#8B2E2E");
                remove.BorderBrush = Brush.Parse("#B84A4A");

                Grid.SetColumn(remove, 2);
                row.Children.Add(remove);

                host.Children.Add(row);
                lines.Add(new MultiBankDepositLine(row, combo, amountBox));
            }

            AddLine();
            WizardHost.Children.Add(host);

            var addLineButton = Button("+ Add Bank Account Line", AddLine, primary: true);
            WizardHost.Children.Add(addLineButton);

            return lines;
        }

        private ComboBox CreateBankAccountComboControl()
        {
            var combo = new ComboBox
            {
                ItemsSource = null,
                Width = 360,
                Height = 38,
                Background = Brush.Parse("#0F1B2A"),
                Foreground = Brushes.White,
                BorderBrush = Brush.Parse("#30445F"),
                HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Left
            };

            _wizardBankAccountCombos.Add(combo);
            RefreshWizardBankAccountCombo(combo);
            return combo;
        }

        private void RefreshAllWizardBankAccountCombos()
        {
            foreach (var combo in _wizardBankAccountCombos.ToList())
                RefreshWizardBankAccountCombo(combo);
        }

        private void RefreshWizardBankAccountCombo(ComboBox combo)
        {
            var previousSelection = combo.SelectedItem?.ToString() ?? string.Empty;
            combo.Items.Clear();
            combo.Items.Add("Choose account");

            foreach (var account in LoadBankAccountChoices())
                combo.Items.Add(account.Display);

            if (!string.IsNullOrWhiteSpace(previousSelection) && ComboContains(combo, previousSelection))
                combo.SelectedItem = previousSelection;
            else
                combo.SelectedIndex = 0;
        }

        private static bool ComboContains(ComboBox combo, string value)
        {
            foreach (var item in combo.Items)
            {
                if (string.Equals(item?.ToString(), value, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private string BuildMultipleBankDepositText(IEnumerable<MultiBankDepositLine> lines)
        {
            var accounts = LoadBankAccountChoices();
            var values = new List<string>();

            foreach (var line in lines)
            {
                var selected = line.ComboBox.SelectedItem?.ToString() ?? string.Empty;
                if (selected == "Choose account" || string.IsNullOrWhiteSpace(selected))
                    continue;

                var account = accounts.FirstOrDefault(item => item.Display == selected);
                var amount = line.AmountTextBox.Text?.Trim() ?? string.Empty;

                if (account is null)
                    continue;

                values.Add(string.IsNullOrWhiteSpace(amount) ? account.Name : $"{account.Name}: ${amount}");
            }

            return string.Join(", ", values);
        }

        private decimal SumMultipleBankDepositAmounts(IEnumerable<MultiBankDepositLine> lines)
        {
            return lines.Sum(line => ParseMoney(line.AmountTextBox.Text));
        }

        private long GetFirstMultipleBankDepositId(IEnumerable<MultiBankDepositLine> lines)
        {
            var accounts = LoadBankAccountChoices();

            foreach (var line in lines)
            {
                var selected = line.ComboBox.SelectedItem?.ToString() ?? string.Empty;
                var account = accounts.FirstOrDefault(item => item.Display == selected);
                if (account is not null)
                    return account.Id;
            }

            return 0;
        }

        private void ClearWizard(bool showJump = true)
        {
            WizardHost.Children.Clear();
            StatusTextBlock.Text = string.Empty;
            JumpBarBorder.IsVisible = showJump && _dashboard.HasActiveCase;
            ResetJumpCombo();
        }

        private void AddTitle(string text)
        {
            WizardHost.Children.Add(new TextBlock
            {
                Text = text,
                Foreground = Brushes.White,
                FontSize = 25,
                FontWeight = FontWeight.SemiBold,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 2)
            });
        }

        private void AddParagraph(string text)
        {
            WizardHost.Children.Add(new TextBlock
            {
                Text = text,
                Foreground = Brush.Parse("#AFC0D3"),
                FontSize = 14,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 780
            });
        }

        private void AddWarning(string text)
        {
            WizardHost.Children.Add(new Border
            {
                Background = Brush.Parse("#2B2434"),
                BorderBrush = Brush.Parse("#6C527A"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(14),
                Child = new TextBlock
                {
                    Text = text,
                    Foreground = Brush.Parse("#EACDFF"),
                    FontSize = 12,
                    TextWrapping = TextWrapping.Wrap
                }
            });
        }

        private void AddInfo(string text)
        {
            WizardHost.Children.Add(new Border
            {
                Background = Brush.Parse("#0F1B2A"),
                BorderBrush = Brush.Parse("#26384F"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(14),
                Child = new TextBlock
                {
                    Text = text,
                    Foreground = Brush.Parse("#DDE7F3"),
                    FontSize = 12,
                    TextWrapping = TextWrapping.Wrap
                }
            });
        }

        private TextBox AddField(string label, string value, string watermark, double width = 440)
        {
            WizardHost.Children.Add(Label(label));
            var box = AddTextBox(value, watermark, width);
            return box;
        }

        private TextBox AddTextBox(string value, string watermark, double width = 440, bool password = false, int maxLength = 0)
        {
            var box = new TextBox
            {
                Text = value,
                Watermark = watermark,
                Width = width,
                Height = 38,
                Padding = new Thickness(12, 7),
                Background = Brush.Parse("#0F1B2A"),
                Foreground = Brushes.White,
                BorderBrush = Brush.Parse("#30445F"),
                HorizontalAlignment = HorizontalAlignment.Left
            };

            if (password)
                box.PasswordChar = '●';

            if (maxLength > 0)
                box.MaxLength = maxLength;

            WizardHost.Children.Add(box);
            return box;
        }

        private TextBox AddMultiLine(string label, string value)
        {
            WizardHost.Children.Add(Label(label));
            var box = new TextBox
            {
                Text = value,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                Width = 620,
                Height = 92,
                Padding = new Thickness(12, 8),
                Background = Brush.Parse("#0F1B2A"),
                Foreground = Brushes.White,
                BorderBrush = Brush.Parse("#30445F"),
                HorizontalAlignment = HorizontalAlignment.Left
            };
            WizardHost.Children.Add(box);
            return box;
        }

        private ComboBox AddCombo(string label, string[] options, string selected)
        {
            WizardHost.Children.Add(Label(label));
            var combo = new ComboBox
            {
                ItemsSource = options,
                SelectedItem = options.Contains(selected) ? selected : options.FirstOrDefault(),
                Width = 360,
                Height = 38,
                Background = Brush.Parse("#0F1B2A"),
                Foreground = Brushes.White,
                BorderBrush = Brush.Parse("#30445F"),
                HorizontalAlignment = HorizontalAlignment.Left
            };

            WizardHost.Children.Add(combo);
            return combo;
        }

        private CheckBox AddCheckBox(string text, bool value)
        {
            var check = new CheckBox
            {
                Content = text,
                IsChecked = value,
                Foreground = Brushes.White,
                FontSize = 14,
                Margin = new Thickness(0, 2, 0, 2)
            };

            WizardHost.Children.Add(check);
            return check;
        }

        private TextBlock Label(string text)
        {
            return new TextBlock
            {
                Text = text,
                Foreground = Brush.Parse("#9EB4CC"),
                FontSize = 12,
                FontWeight = FontWeight.SemiBold,
                Margin = new Thickness(0, 4, 0, -6)
            };
        }

        private Button Button(string text, Action action, bool primary = false)
        {
            var button = new Button
            {
                Content = text,
                Height = 40,
                Padding = new Thickness(18, 0),
                Background = Brush.Parse(primary ? "#2E8B57" : "#17263A"),
                Foreground = Brushes.White,
                BorderBrush = Brush.Parse(primary ? "#3FAF70" : "#30445F"),
                FontWeight = primary ? FontWeight.SemiBold : FontWeight.Normal,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center
            };

            button.Click += (_, _) => action();
            return button;
        }

        private void AddButtonRow(params Button[] buttons)
        {
            var row = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 10,
                Margin = new Thickness(0, 10, 0, 0)
            };

            foreach (var button in buttons)
                row.Children.Add(button);

            WizardHost.Children.Add(row);
        }

        private void ShowValidation(string message)
        {
            StatusTextBlock.Text = message ?? string.Empty;
        }

        private static string Clean(string? value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();

        private static string SelectedText(ComboBox combo, string fallback)
        {
            return combo.SelectedItem?.ToString() ?? fallback;
        }

        private static decimal ParseMoney(string? value)
        {
            if (decimal.TryParse(Clean(value).Replace("$", string.Empty).Replace(",", string.Empty), out var parsed))
                return parsed;

            return 0m;
        }

        private static string SanitizeFolderName(string value)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var cleaned = new string(value.Select(c => invalid.Contains(c) ? '_' : c).ToArray()).Trim();
            return string.IsNullOrWhiteSpace(cleaned) ? "General" : cleaned;
        }

        private static string GetUniqueFilePath(string requestedPath)
        {
            if (!File.Exists(requestedPath))
                return requestedPath;

            var folder = Path.GetDirectoryName(requestedPath) ?? string.Empty;
            var name = Path.GetFileNameWithoutExtension(requestedPath);
            var ext = Path.GetExtension(requestedPath);

            for (var i = 2; i < 1000; i++)
            {
                var candidate = Path.Combine(folder, $"{name} {i}{ext}");
                if (!File.Exists(candidate))
                    return candidate;
            }

            return Path.Combine(folder, $"{name} {DateTime.Now:yyyyMMddHHmmss}{ext}");
        }

        private sealed class MultiBankDepositLine
        {
            public MultiBankDepositLine(Grid row, ComboBox comboBox, TextBox amountTextBox)
            {
                Row = row;
                ComboBox = comboBox;
                AmountTextBox = amountTextBox;
            }

            public Grid Row { get; }
            public ComboBox ComboBox { get; }
            public TextBox AmountTextBox { get; }
        }

        private sealed class ChoiceItem
        {
            public ChoiceItem(long id, string name, string display)
            {
                Id = id;
                Name = name;
                Display = display;
            }

            public long Id { get; }
            public string Name { get; }
            public string Display { get; }
        }
    }
}
