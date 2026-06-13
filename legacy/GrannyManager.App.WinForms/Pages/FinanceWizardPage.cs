using System.Diagnostics;
using System.Globalization;
using GrannyManager.App.Navigation;
using GrannyManager.App.Services;
using GrannyManager.App.Themes;
using GrannyManager.Core.Models;
using GrannyManager.Core.Services;
using GrannyManager.Data.Repositories;

namespace GrannyManager.App.Pages;

public sealed class FinanceWizardPage : BasePage
{
    private readonly CaseFolderService _caseFolderService = new();
    private readonly RecentCasesService _recentCasesService = new();
    private readonly Panel _wizardHost;
    private readonly ToolTip _toolTip = new();

    private string _primaryPersonName = string.Empty;
    private string _caseName = string.Empty;
    private string _caseRootFolder = string.Empty;
    private string _caseSecurityPin = string.Empty;

    private string _pendingPersonName = string.Empty;
    private string _pendingPersonRelationship = string.Empty;
    private string _pendingPersonRole = string.Empty;
    private bool _pendingPersonLivesInHousehold = true;
    private bool _pendingPersonPaysRent;
    private bool _pendingPersonUsesVehicle;
    private bool _pendingPersonReceivesRides;
    private string _pendingPersonNotes = string.Empty;

    public FinanceWizardPage()
        : base(AppPageKey.FinanceWizard, "Finance Setup Wizard", "A friendly guided setup for creating a case, adding household people, and building the first financial picture.")
    {
        ContentPanel.Controls.Clear();

        _caseRootFolder = _caseFolderService.GetDefaultCaseRoot();

        _wizardHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppColors.PanelBackground,
            Padding = new Padding(32, 26, 32, 26),
            AutoScroll = true
        };
        ContentPanel.Controls.Add(_wizardHost);

        ShowStart();
    }

    public override void OnNavigatedTo()
    {
        if (AppState.ActiveCase is not null && string.IsNullOrWhiteSpace(_primaryPersonName))
        {
            _primaryPersonName = AppState.ActiveCase.PrimaryPersonName;
            _caseName = AppState.ActiveCase.DisplayName;
            _caseRootFolder = Path.GetDirectoryName(AppState.ActiveCase.CaseFolderPath) ?? _caseRootFolder;
        }
    }

    private void ShowStart()
    {
        ClearWizard();
        AddTitle("Hello and welcome to your new financial management project!");
        AddParagraph("It's never been easier to manage finances for you, your family and friends, and it's ABSOLUTELY FREE! This wizard will walk you through the first setup so your case starts clean instead of becoming a pile of scattered notes.", 92);

        if (AppState.ActiveCase is null)
        {
            AddPrimaryButton("Start a New Finance Project", new Action(ShowPrimaryPersonStep));
        }
        else
        {
            AddInfoLine($"Active case: {AppState.ActiveCase.DisplayName}");
            AddButtonRow(Button("Continue Setup", new Action(ShowHouseholdIntro)), Button("Finish Later", new Action(GoDashboard)));
        }
    }

    private void ShowPrimaryPersonStep()
    {
        ClearWizard();
        AddTitle("Who are we managing for?");
        AddParagraph("To get started, please type the name of the individual or head of household you are managing for.", 58);
        var nameBox = AddTextBox(_primaryPersonName, "Example: Vanessa Martinez");
        AddButtonRow(Button("Next", () =>
        {
            _primaryPersonName = nameBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(_primaryPersonName))
            {
                MessageBox.Show("Please enter the person or head-of-household name.", "Wizard", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            ShowCaseNameStep();
        }), Button("Back", new Action(ShowStart)));
    }

    private void ShowCaseNameStep()
    {
        ClearWizard();
        AddTitle("Name the project");
        AddParagraph("Now let's give your project a catchy name so you know exactly how to find it later.", 58);
        if (string.IsNullOrWhiteSpace(_caseName))
            _caseName = $"{_primaryPersonName} Finances";
        var caseBox = AddTextBox(_caseName, "Example: Mom Finances");
        AddButtonRow(Button("Next", () =>
        {
            _caseName = caseBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(_caseName))
            {
                MessageBox.Show("Please enter a case / project name.", "Wizard", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            ShowSaveLocationStep();
        }), Button("Back", new Action(ShowPrimaryPersonStep)));
    }

    private void ShowSaveLocationStep()
    {
        ClearWizard();
        AddTitle("Choose where to save it");
        AddParagraph("Great! All we need now is a save location for the project and we're off to the races!", 58);
        var folderBox = AddTextBox(_caseRootFolder, string.Empty, 620);
        var browse = CreateButton("Browse", 120);
        browse.Click += (_, _) =>
        {
            using var dialog = new FolderBrowserDialog { Description = "Choose where Granny Manager cases should be saved" };
            if (Directory.Exists(folderBox.Text)) dialog.SelectedPath = folderBox.Text;
            if (dialog.ShowDialog(this) == DialogResult.OK)
                folderBox.Text = dialog.SelectedPath;
        };
        AddLooseControl(browse);
        AddButtonRow(Button("Let's Get Started!", () =>
        {
            _caseRootFolder = folderBox.Text.Trim();
            if (!CasePinPrompt.TryPromptForNewPin(this, out _caseSecurityPin))
            {
                return;
            }

            CreateCaseAndBegin();
        }), Button("Back", new Action(ShowCaseNameStep)));
    }

    private void CreateCaseAndBegin()
    {
        try
        {
            var profile = _caseFolderService.CreateCase(_caseName, _primaryPersonName, _caseRootFolder, _caseSecurityPin);
            AppState.SetActiveCase(profile);
            _recentCasesService.AddOrUpdate(profile);

            var peopleRepo = new HouseholdPeopleRepository(GetDatabasePath());
            bool alreadyExists = peopleRepo.GetAll().Any(p => string.Equals(p.FullName, _primaryPersonName, StringComparison.OrdinalIgnoreCase));
            if (!alreadyExists)
            {
                peopleRepo.Upsert(new HouseholdPerson
                {
                    FullName = _primaryPersonName,
                    Relationship = "Self",
                    Role = "Head of household / primary case person",
                    LivesInHousehold = true,
                    Notes = "Created by Finance Setup Wizard."
                });
            }

            ShowHouseholdIntro();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Could not create the case.\n\n" + ex.Message, "Wizard", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ShowHouseholdIntro()
    {
        ClearWizard();
        AddTitle($"Let's look at {_primaryPersonNameOrCase()}'s household");
        AddParagraph($"Very good. Let's first take a look at {_primaryPersonNameOrCase()}'s household and the other people living there or involved in the finances.", 76);
        AddButtonRow(Button("Add Person", new Action(ShowPersonBasics)), Button("Skip Household", new Action(ShowIncomeIntro)), Button("Finish Later", new Action(GoDashboard)));
    }

    private void ShowPersonBasics()
    {
        ClearWizard();
        AddTitle("Add a household / case person");
        AddParagraph("Start with the basics. Relationship is how they are connected to the primary person. Role is why they matter to this case.", 72);

        var nameBox = AddLabeledTextBox("Name", _pendingPersonName, "Full legal/preferred name");
        var relationshipBox = AddLabeledTextBox("Relationship", _pendingPersonRelationship, "Example: Son, Daughter, Sister, Trustee");
        var roleBox = AddLabeledTextBox("Role / purpose", _pendingPersonRole, "Example: Lives in home, pays rent, driver, trustee");
        _toolTip.SetToolTip(nameBox, "The person's full name or the name you will recognize later.");
        _toolTip.SetToolTip(relationshipBox, "How this person is related or connected to the primary case person.");
        _toolTip.SetToolTip(roleBox, "Why this person matters to the finance case.");

        AddButtonRow(Button("Next", () =>
        {
            _pendingPersonName = nameBox.Text.Trim();
            _pendingPersonRelationship = relationshipBox.Text.Trim();
            _pendingPersonRole = roleBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(_pendingPersonName))
            {
                MessageBox.Show("Please enter this person's name.", "Wizard", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            ShowPersonDetails();
        }), Button("Back", new Action(ShowHouseholdIntro)));
    }

    private void ShowPersonDetails()
    {
        ClearWizard();
        AddTitle($"Details for {_pendingPersonName}");
        AddParagraph("These details help the dashboard explain who lives in the household, who uses vehicles, and who receives help.", 62);
        var lives = AddCheckBox("Lives in household", _pendingPersonLivesInHousehold);
        var pays = AddCheckBox("Pays rent / contributes", _pendingPersonPaysRent);
        var vehicle = AddCheckBox("Uses household vehicle", _pendingPersonUsesVehicle);
        var rides = AddCheckBox("Receives rides / transport help", _pendingPersonReceivesRides);
        var notesBox = AddLabeledMultiLine("Notes", _pendingPersonNotes);

        AddButtonRow(Button("Next", () =>
        {
            _pendingPersonLivesInHousehold = lives.Checked;
            _pendingPersonPaysRent = pays.Checked;
            _pendingPersonUsesVehicle = vehicle.Checked;
            _pendingPersonReceivesRides = rides.Checked;
            _pendingPersonNotes = notesBox.Text.Trim();
            ShowPersonContribution();
        }), Button("Back", new Action(ShowPersonBasics)));
    }

    private void ShowPersonContribution()
    {
        ClearWizard();
        AddTitle("Financial contribution");
        AddParagraph($"Does {_pendingPersonName} contribute any kind of financial assistance to {_primaryPersonNameOrCase()}?", 40);
        AddWarning("NOTE: This is strictly physical cash transfer to the head of household to help pay bills. Do not enter bills, utilities, formal rent, or other payments this person directly controls. That prevents double-counting.");

        var handling = AddCombo("Contribution income", new[] { "No Contribution", "Select Existing Income Source", "Create Income Source Now" }, "No Contribution");

        AddButtonRow(Button("Next", () =>
        {
            string option = handling.SelectedItem?.ToString() ?? "No Contribution";
            if (option == "Select Existing Income Source")
                ShowPersonSelectContributionIncome();
            else if (option == "Create Income Source Now")
                ShowPersonCreateContributionIncome();
            else
                SavePendingPerson("No Contribution", 0, string.Empty);
        }), Button("Back", new Action(ShowPersonDetails)));
    }

    private void ShowPersonSelectContributionIncome()
    {
        ClearWizard();
        AddTitle("Select contribution income source");
        AddParagraph("Choose the existing income source that represents this person's contribution. This prevents the same money from being counted twice.", 70);

        var incomeSources = LoadIncomeSources();
        if (incomeSources.Count == 0)
        {
            AddWarning("No income sources exist yet. Create one now, or go back and choose No Contribution for now.");
            AddButtonRow(Button("Create Income Source Now", new Action(ShowPersonCreateContributionIncome)), Button("Back", new Action(ShowPersonContribution)));
            return;
        }

        var combo = AddCombo("Linked income source", incomeSources.Select(i => i.Display), incomeSources[0].Display);
        AddButtonRow(Button("Save Person", () =>
        {
            var selected = incomeSources.FirstOrDefault(i => i.Display == combo.SelectedItem?.ToString());
            SavePendingPerson("Select Existing Income Source", selected?.Id ?? 0, selected?.Name ?? string.Empty);
        }), Button("Back", new Action(ShowPersonContribution)));
    }

    private void ShowPersonCreateContributionIncome()
    {
        ClearWizard();
        AddTitle("Create contribution income source");
        AddParagraph($"Create the income record for {_pendingPersonName}'s contribution. The person will link to this income source after it is saved.", 70);

        var sourceName = AddLabeledTextBox("Income source name", $"{_pendingPersonName} contribution", string.Empty);
        var type = AddCombo("Income type", new[] { "Family Contribution", "Rental Income", "Employment / Wages", "Other" }, "Family Contribution");
        var taxes = AddCheckBox("Taxes withheld", false);
        var amount = AddLabeledTextBox("Gross Pay / Payment Amount", string.Empty, "0.00");
        var frequency = AddCombo("Frequency", FrequencyOptions, "Monthly");
        var expected = AddLabeledTextBox("Expected day/date", string.Empty, "Example: 1st, Fridays, every payday");
        var deposit = AddCombo("Deposit method / destination", new[] { "Cash", "Check" }, "Cash");
        var notes = AddLabeledMultiLine("Notes", $"Created from Finance Setup Wizard for {_pendingPersonName}.");

        AddButtonRow(Button("Create Income + Save Person", () =>
        {
            try
            {
                decimal.TryParse(amount.Text.Trim(), out decimal parsedAmount);
                var income = new IncomeSource
                {
                    SourceName = string.IsNullOrWhiteSpace(sourceName.Text) ? $"{_pendingPersonName} contribution" : sourceName.Text.Trim(),
                    IncomeType = type.SelectedItem?.ToString() ?? "Family Contribution",
                    TaxesWithheld = taxes.Checked,
                    Amount = parsedAmount,
                    Frequency = frequency.SelectedItem?.ToString() ?? "Monthly",
                    ExpectedDayOrDate = expected.Text.Trim(),
                    DepositMethod = deposit.SelectedItem?.ToString() ?? "Cash",
                    DepositedToAccount = deposit.SelectedItem?.ToString() ?? "Cash",
                    IsActive = true,
                    Notes = notes.Text.Trim()
                };
                new IncomeSourcesRepository(GetDatabasePath()).Upsert(income);
                SavePendingPerson("Select Existing Income Source", income.Id, income.SourceName);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not create the income source.\n\n" + ex.Message, "Wizard", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }), Button("Back", new Action(ShowPersonContribution)));
    }

    private void SavePendingPerson(string contributionHandling, long linkedIncomeSourceId, string linkedIncomeSourceName)
    {
        try
        {
            var person = new HouseholdPerson
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
                LinkedIncomeSourceName = linkedIncomeSourceName
            };

            new HouseholdPeopleRepository(GetDatabasePath()).Upsert(person);
            ClearPendingPerson();
            ShowAddMorePeople();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Could not save the person.\n\n" + ex.Message, "Wizard", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ShowAddMorePeople()
    {
        ClearWizard();
        AddTitle("GREAT! You just added a member of the household!");
        AddParagraph("Would you like to add more household members or case people now? You can always add or edit people later from the People / Household page.", 74);
        AddButtonRow(Button("Add Another Person", new Action(ShowPersonBasics)), Button("Continue to Income", new Action(ShowIncomeIntro)), Button("Finish Later", new Action(GoDashboard)));
    }

    private void ShowIncomeIntro()
    {
        ClearWizard();
        AddTitle("Income Sources");
        AddParagraph($"Now let's look at money coming in for {_primaryPersonNameOrCase()}. This can include Social Security, pensions, work income, survivor benefits, household contributions, rental payments, and other recurring income.", 82);
        AddButtonRow(Button("Add Income Source", new Action(ShowIncomeAdd)), Button("Skip Income", new Action(ShowBillsIntro)), Button("Back to Household", new Action(ShowHouseholdIntro)));
    }

    private void ShowIncomeAdd()
    {
        ClearWizard();
        AddTitle("Add Income Source");
        var name = AddLabeledTextBox("Source name", string.Empty, "Example: Social Security");
        var type = AddCombo("Income type", new[] { "Social Security", "Pension", "Survivor Benefits", "Disability", "Employment / Wages", "Family Contribution", "Rental Income", "Retirement Account", "Settlement / Lump Sum", "Other" }, "Social Security");
        var taxes = AddCheckBox("Taxes withheld", false);
        var amount = AddLabeledTextBox("Gross Pay / Payment Amount", string.Empty, "0.00");
        var frequency = AddCombo("Frequency", FrequencyOptions, "Monthly");
        var expected = AddLabeledTextBox("Expected day/date", string.Empty, "Example: 3rd of month");
        var deposit = AddCombo("Deposit method / destination", new[] { "Cash", "Check" }, "Cash");
        var notes = AddLabeledMultiLine("Notes", string.Empty);
        AddButtonRow(Button("Save Income", () =>
        {
            decimal.TryParse(amount.Text.Trim(), out decimal parsed);
            new IncomeSourcesRepository(GetDatabasePath()).Upsert(new IncomeSource
            {
                SourceName = name.Text.Trim(),
                IncomeType = type.SelectedItem?.ToString() ?? "Other",
                TaxesWithheld = taxes.Checked,
                Amount = parsed,
                Frequency = frequency.SelectedItem?.ToString() ?? "Monthly",
                ExpectedDayOrDate = expected.Text.Trim(),
                DepositMethod = deposit.SelectedItem?.ToString() ?? "Cash",
                DepositedToAccount = deposit.SelectedItem?.ToString() ?? "Cash",
                Notes = notes.Text.Trim(),
                IsActive = true
            });
            ShowIncomeMore();
        }), Button("Cancel", new Action(ShowIncomeIntro)));
    }

    private void ShowIncomeMore()
    {
        ClearWizard();
        AddTitle("Income saved");
        AddParagraph("Would you like to add another income source, or continue to bills and spending?", 52);
        AddButtonRow(Button("Add Another Income", new Action(ShowIncomeAdd)), Button("Continue to Bills", new Action(ShowBillsIntro)), Button("Finish Later", new Action(GoDashboard)));
    }

    private void ShowBillsIntro()
    {
        ClearWizard();
        AddTitle("Bills / Spending");
        AddParagraph("Next we'll enter regular bills, shared responsibilities, and known monthly costs. This is the main money-going-out section.", 70);
        AddButtonRow(Button("Add Bill / Expense", new Action(ShowBillAdd)), Button("Skip Bills", new Action(ShowAllowanceIntro)), Button("Back to Income", new Action(ShowIncomeIntro)));
    }

    private void ShowBillAdd()
    {
        ClearWizard();
        AddTitle("Add Bill / Expense");
        var name = AddLabeledTextBox("Bill / Expense name", string.Empty, "Example: Electric Bill");
        var cat = AddCombo("Category", new[] { "Housing", "Utilities", "Vehicle", "Insurance", "Food", "Medical", "Debt Payment", "Legal", "Other" }, "Utilities");
        var amount = AddLabeledTextBox("Amount", string.Empty, "0.00");
        var frequency = AddCombo("Frequency", FrequencyOptions, "Monthly");
        var due = AddLabeledTextBox("Due date", string.Empty, "Example: 15th");
        var payer = AddPersonCombo("Who Pays This?");
        var owner = AddPersonCombo("Responsibility / Owner");
        var pastDue = AddLabeledTextBox("Past due amount", string.Empty, "0.00");
        var priority = AddCombo("Priority", new[] { "Low", "Normal", "High", "Urgent" }, "Normal");
        var autopay = AddCheckBox("Autopay", false);
        var notes = AddLabeledMultiLine("Notes", string.Empty);
        AddButtonRow(Button("Save Bill", () =>
        {
            decimal.TryParse(amount.Text.Trim(), out decimal parsed);
            decimal.TryParse(pastDue.Text.Trim(), out decimal pd);
            new BillsRepository(GetDatabasePath()).Upsert(new Bill
            {
                BillName = name.Text.Trim(),
                Category = cat.SelectedItem?.ToString() ?? "Other",
                Amount = parsed,
                Frequency = frequency.SelectedItem?.ToString() ?? "Monthly",
                DueDate = due.Text.Trim(),
                PaidBy = payer.SelectedItem?.ToString() ?? string.Empty,
                ResponsibilityOwner = owner.SelectedItem?.ToString() ?? string.Empty,
                PastDueAmount = pd,
                Priority = priority.SelectedItem?.ToString() ?? "Normal",
                IsAutopay = autopay.Checked,
                Notes = notes.Text.Trim(),
                IsActive = true
            });
            ShowBillsMore();
        }), Button("Cancel", new Action(ShowBillsIntro)));
    }

    private void ShowBillsMore()
    {
        ClearWizard();
        AddTitle("Bill saved");
        AddParagraph("Would you like to add another bill, or continue to Allowance / Savings?", 52);
        AddButtonRow(Button("Add Another Bill", new Action(ShowBillAdd)), Button("Continue to Allowance / Savings", new Action(ShowAllowanceIntro)), Button("Finish Later", new Action(GoDashboard)));
    }

    private void ShowAllowanceIntro()
    {
        ClearWizard();
        AddTitle("Allowance / Savings");
        AddParagraph("This section reserves money for fun-money allowances or savings goals without mixing those amounts into regular bills.", 70);
        AddButtonRow(Button("Add Allowance / Savings", new Action(ShowAllowanceAdd)), Button("Skip Allowance / Savings", new Action(ShowAssetsIntro)), Button("Back to Bills", new Action(ShowBillsIntro)));
    }

    private void ShowAllowanceAdd()
    {
        ClearWizard();
        AddTitle("Add Allowance / Savings");
        var name = AddLabeledTextBox("Name / purpose", string.Empty, "Example: Grocery buffer, Nail fund, Emergency savings");
        var type = AddCombo("Type", new[] { "Allowance", "Savings" }, "Allowance");
        var amount = AddLabeledTextBox("Amount", string.Empty, "0.00");
        var frequency = AddCombo("Frequency", FrequencyOptions, "Monthly");
        var whereMethod = AddCombo("Where / account / envelope", new[] { "Cash / Envelope", "Select Bank Account", "Create Bank Account Now", "Other" }, "Cash / Envelope");
        var bankLabel = AddLabel("Bank account");
        var bankCombo = AddBankAssetCombo();
        var otherWhereLabel = AddLabel("Other location / envelope");
        var otherWhere = AddTextBox(string.Empty, "Example: cash envelope, prepaid card, lockbox");
        var notes = AddLabeledMultiLine("Notes", string.Empty);

        void RefreshWhereControls()
        {
            string method = whereMethod.SelectedItem?.ToString() ?? "Cash / Envelope";
            bool showBank = method == "Select Bank Account";
            bool showOther = method == "Other";
            bankLabel.Visible = showBank;
            bankCombo.Visible = showBank;
            otherWhereLabel.Visible = showOther;
            otherWhere.Visible = showOther;
            ReflowWizardControls();
        }

        whereMethod.SelectedIndexChanged += (_, _) =>
        {
            string method = whereMethod.SelectedItem?.ToString() ?? "Cash / Envelope";
            if (method == "Create Bank Account Now")
            {
                long newId = CreateBankAccountFromWizard();
                ReloadBankCombo(bankCombo, newId);
                whereMethod.SelectedItem = "Select Bank Account";
            }
            RefreshWhereControls();
        };
        RefreshWhereControls();

        AddButtonRow(Button("Save Allowance / Savings", () =>
        {
            decimal.TryParse(amount.Text.Trim(), out decimal parsed);
            var item = new AllowanceSavingsItem
            {
                ItemName = name.Text.Trim(),
                ItemType = type.SelectedItem?.ToString() ?? "Allowance",
                Amount = parsed,
                Frequency = frequency.SelectedItem?.ToString() ?? "Monthly",
                StorageMethod = whereMethod.SelectedItem?.ToString() ?? "Cash / Envelope",
                Notes = notes.Text.Trim(),
                IsActive = true
            };

            if (!ApplyWizardAllowanceWhere(item, bankCombo, otherWhere))
                return;

            new AllowanceSavingsRepository(GetDatabasePath()).Save(item);
            ShowAllowanceMore();
        }), Button("Cancel", new Action(ShowAllowanceIntro)));
        ReflowWizardControls();
    }

    private void ShowAllowanceMore()
    {
        ClearWizard();
        AddTitle("Allowance / Savings saved");
        AddParagraph("Would you like to add another allowance or savings goal, or continue to Assets?", 52);
        AddButtonRow(Button("Add Another", new Action(ShowAllowanceAdd)), Button("Continue to Assets", new Action(ShowAssetsIntro)), Button("Finish Later", new Action(GoDashboard)));
    }

    private void ShowAssetsIntro()
    {
        ClearWizard();
        AddTitle("Assets");
        AddParagraph("Assets are real-world things such as vehicles, properties, bank accounts, valuable items, or other things the case person owns, controls, or is responsible for.", 78);
        AddButtonRow(Button("Add Asset", new Action(ShowAssetType)), Button("Skip Assets", new Action(ShowDebtsIntro)), Button("Back to Allowance / Savings", new Action(ShowAllowanceIntro)));
    }

    private void ShowAssetType()
    {
        ClearWizard();
        AddTitle("Choose asset type");
        var type = AddCombo("Asset type", new[] { "Vehicle", "Property", "Bank", "Valuable Item", "Other" }, "Vehicle");
        AddButtonRow(Button("Next", new Action(() => ShowAssetAdd(type.SelectedItem?.ToString() ?? "Other"))), Button("Back", new Action(ShowAssetsIntro)));
    }

    private void ShowAssetAdd(string assetType)
    {
        ClearWizard();
        AddTitle($"Add {assetType} Asset");
        var name = AddLabeledTextBox("Asset name", string.Empty, "Example: 2021 Jeep Compass");
        var owner = AddPersonCombo("Owner / responsible person");
        var value = AddLabeledTextBox("Estimated value", string.Empty, "0.00");
        var status = AddLabeledTextBox("Status", "Active / In Use", string.Empty);
        var location = AddLabeledTextBox("Location / institution", string.Empty, string.Empty);

        TextBox? extra1 = null, extra2 = null, extra3 = null, extra4 = null;
        if (assetType == "Vehicle")
        {
            extra1 = AddLabeledTextBox("Year", string.Empty, "2021");
            extra2 = AddLabeledTextBox("Make", string.Empty, "Jeep");
            extra3 = AddLabeledTextBox("Model", string.Empty, "Compass");
            extra4 = AddLabeledTextBox("Primary driver", string.Empty, string.Empty);
        }
        else if (assetType == "Property")
        {
            extra1 = AddLabeledTextBox("Property type", string.Empty, "House, condo, land");
            extra2 = AddLabeledTextBox("Property address", string.Empty, string.Empty);
            extra3 = AddLabeledTextBox("Occupants", string.Empty, string.Empty);
            extra4 = AddLabeledTextBox("HOA / management", string.Empty, string.Empty);
        }
        else if (assetType == "Bank")
        {
            extra1 = AddLabeledTextBox("Institution name", string.Empty, "Bank / credit union");
            extra2 = AddLabeledTextBox("Account nickname", string.Empty, "Checking, Savings");
            extra3 = AddLabeledTextBox("Current balance / value", string.Empty, "0.00");
        }
        else if (assetType == "Valuable Item")
        {
            extra1 = AddLabeledTextBox("Description", string.Empty, string.Empty);
            extra2 = AddLabeledTextBox("Serial / identifier", string.Empty, string.Empty);
            extra3 = AddLabeledTextBox("Storage location", string.Empty, string.Empty);
        }
        else
        {
            extra1 = AddLabeledTextBox("Details", string.Empty, string.Empty);
        }

        var notes = AddLabeledMultiLine("Notes", string.Empty);
        AddButtonRow(Button("Save Asset", () =>
        {
            decimal.TryParse(value.Text.Trim(), out decimal parsedValue);
            decimal.TryParse(extra3?.Text.Trim(), out decimal parsedBalance);
            var asset = new AssetItem
            {
                AssetName = name.Text.Trim(),
                AssetType = assetType,
                Owner = owner.SelectedItem?.ToString() ?? string.Empty,
                EstimatedValue = parsedValue,
                Status = status.Text.Trim(),
                LocationOrInstitution = location.Text.Trim(),
                Notes = notes.Text.Trim(),
                IsActive = true
            };
            if (assetType == "Vehicle") { asset.VehicleYear = extra1?.Text.Trim() ?? string.Empty; asset.VehicleMake = extra2?.Text.Trim() ?? string.Empty; asset.VehicleModel = extra3?.Text.Trim() ?? string.Empty; asset.PrimaryDriver = extra4?.Text.Trim() ?? string.Empty; }
            else if (assetType == "Property") { asset.PropertyType = extra1?.Text.Trim() ?? string.Empty; asset.PropertyAddress = extra2?.Text.Trim() ?? string.Empty; asset.Occupants = extra3?.Text.Trim() ?? string.Empty; asset.HoaOrManagement = extra4?.Text.Trim() ?? string.Empty; }
            else if (assetType == "Bank") { asset.InstitutionName = extra1?.Text.Trim() ?? string.Empty; asset.AccountNickname = extra2?.Text.Trim() ?? string.Empty; asset.CurrentBalanceValue = parsedBalance; }
            else if (assetType == "Valuable Item") { asset.ValuableDescription = extra1?.Text.Trim() ?? string.Empty; asset.SerialOrIdentifier = extra2?.Text.Trim() ?? string.Empty; asset.StorageLocation = extra3?.Text.Trim() ?? string.Empty; }
            else { asset.OtherDetails = extra1?.Text.Trim() ?? string.Empty; }
            new AssetsRepository(GetDatabasePath()).Upsert(asset);
            ShowAssetsMore();
        }), Button("Cancel", new Action(ShowAssetsIntro)));
    }

    private void ShowAssetsMore()
    {
        ClearWizard();
        AddTitle("Asset saved");
        AddParagraph("Would you like to add another asset, or continue to Debts?", 52);
        AddButtonRow(Button("Add Another Asset", new Action(ShowAssetType)), Button("Continue to Debts", new Action(ShowDebtsIntro)), Button("Finish Later", new Action(GoDashboard)));
    }

    private void ShowDebtsIntro()
    {
        ClearWizard();
        AddTitle("Debts");
        AddParagraph("Debts are obligations owed to creditors, collectors, agencies, family members, or other parties. If the monthly payment is already in Bills, link it later so it does not get counted twice.", 88);
        AddButtonRow(Button("Add Debt", new Action(ShowDebtAdd)), Button("Skip Debts", new Action(ShowDocumentsIntro)), Button("Back to Assets", new Action(ShowAssetsIntro)));
    }

    private void ShowDebtAdd()
    {
        ClearWizard();
        AddTitle("Add Debt");
        var name = AddLabeledTextBox("Debt name", string.Empty, "Example: IRS payment plan");
        var type = AddCombo("Debt type", new[] { "Credit Card", "Vehicle Loan", "Personal Loan", "IRS / Tax", "Medical", "Collection", "Family Loan", "Other" }, "Other");
        var creditor = AddLabeledTextBox("Creditor / collector", string.Empty, string.Empty);
        var balance = AddLabeledTextBox("Current balance", string.Empty, "0.00");
        var minimum = AddLabeledTextBox("Minimum payment", string.Empty, "0.00");
        var frequency = AddCombo("Payment frequency", FrequencyOptions, "Monthly");
        var owner = AddPersonCombo("Responsibility / Owner");
        var paidBy = AddPersonCombo("Who Pays This?");
        var paymentTracking = AddCombo("Monthly payment tracking", new[] { "Not Linked", "Select Existing Bill", "Create Bill Now" }, "Not Linked");
        var linkedBillLabel = AddLabel("Linked bill / expense");
        var linkedBillCombo = AddBillCombo();
        var status = AddCombo("Status", new[] { "Current", "Past Due", "In Collections", "Disputed", "Unknown" }, "Current");
        var priority = AddCombo("Priority", new[] { "Low", "Normal", "High", "Urgent" }, "Normal");
        var notes = AddLabeledMultiLine("Notes", string.Empty);

        void RefreshDebtBillControls()
        {
            string trackingChoice = paymentTracking.SelectedItem?.ToString() ?? "Not Linked";
            bool showLinkedBill = trackingChoice == "Select Existing Bill";
            linkedBillLabel.Visible = showLinkedBill;
            linkedBillCombo.Visible = showLinkedBill;
            ReflowWizardControls();
        }

        paymentTracking.SelectedIndexChanged += (_, _) => RefreshDebtBillControls();
        RefreshDebtBillControls();

        AddButtonRow(Button("Save Debt", () =>
        {
            decimal.TryParse(balance.Text.Trim(), out decimal bal);
            decimal.TryParse(minimum.Text.Trim(), out decimal min);

            string trackingChoice = paymentTracking.SelectedItem?.ToString() ?? "Not Linked";
            long linkedBillId = 0;
            string linkedBillName = string.Empty;

            if (trackingChoice == "Create Bill Now")
            {
                var createdBill = CreateBillForDebtWizard(name.Text.Trim(), min, frequency.SelectedItem?.ToString() ?? "Monthly", owner.SelectedItem?.ToString() ?? string.Empty, paidBy.SelectedItem?.ToString() ?? string.Empty);
                if (createdBill is null)
                    return;

                ReloadBillCombo(linkedBillCombo, createdBill.Id);
                paymentTracking.SelectedItem = "Select Existing Bill";
                trackingChoice = "Select Existing Bill";
                linkedBillId = createdBill.Id;
                linkedBillName = createdBill.Name;
                RefreshDebtBillControls();
            }

            if (trackingChoice == "Select Existing Bill")
            {
                if (linkedBillCombo.SelectedItem is not BillListItem linkedBill || linkedBill.Id <= 0)
                {
                    MessageBox.Show("Select the bill/payment linked to this debt, or choose Create Bill Now.", "Wizard", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                linkedBillId = linkedBill.Id;
                linkedBillName = linkedBill.Name;
            }

            new DebtsRepository(GetDatabasePath()).Upsert(new Debt
            {
                DebtName = name.Text.Trim(),
                DebtType = type.SelectedItem?.ToString() ?? "Other",
                CreditorCollector = creditor.Text.Trim(),
                CurrentBalance = bal,
                MinimumPayment = min,
                PaymentFrequency = frequency.SelectedItem?.ToString() ?? "Monthly",
                ResponsibilityOwner = owner.SelectedItem?.ToString() ?? string.Empty,
                PaidBy = paidBy.SelectedItem?.ToString() ?? string.Empty,
                PaymentTracking = trackingChoice,
                LinkedBillId = linkedBillId,
                LinkedBillName = linkedBillName,
                Status = status.SelectedItem?.ToString() ?? "Current",
                Priority = priority.SelectedItem?.ToString() ?? "Normal",
                Notes = notes.Text.Trim(),
                IsActive = true
            });
            ShowDebtsMore();
        }), Button("Cancel", new Action(ShowDebtsIntro)));
        ReflowWizardControls();
    }

    private void ShowDebtsMore()
    {
        ClearWizard();
        AddTitle("Debt saved");
        AddParagraph("Would you like to add another debt, or continue to Documents?", 52);
        AddButtonRow(Button("Add Another Debt", new Action(ShowDebtAdd)), Button("Continue to Documents", new Action(ShowDocumentsIntro)), Button("Finish Later", new Action(GoDashboard)));
    }

    private void ShowDocumentsIntro()
    {
        ClearWizard();
        AddTitle("Documents");
        AddParagraph("Documents keep important PDFs, photos, letters, statements, and records attached to the case. You can tag files now so search can find them later.", 78);
        AddButtonRow(Button("Add Document", new Action(ShowDocumentAdd)), Button("Skip Documents", new Action(ShowFinish)), Button("Back to Debts", new Action(ShowDebtsIntro)));
    }

    private void ShowDocumentAdd()
    {
        ClearWizard();
        AddTitle("Add Document");
        var path = AddLabeledTextBox("File", string.Empty, string.Empty, 620);
        var browse = CreateButton("Browse", 120);
        browse.Click += (_, _) =>
        {
            using var dialog = new OpenFileDialog { Title = "Choose a document to copy into this case", Filter = "Documents|*.pdf;*.jpg;*.jpeg;*.png;*.txt;*.doc;*.docx;*.xls;*.xlsx|All files|*.*" };
            if (dialog.ShowDialog(this) == DialogResult.OK) path.Text = dialog.FileName;
        };
        AddLooseControl(browse);
        var title = AddLabeledTextBox("Title", string.Empty, string.Empty);
        var category = AddCombo("Category", new[] { "Taxes", "Legal", "Property", "Vehicle", "Insurance", "Banking", "Income", "Bills", "Debt", "Other" }, "Other");
        var tags = AddLabeledTextBox("Tags / Keywords (separate with commas)", string.Empty, "Example: IRS, truck, registration, loan");
        var notes = AddLabeledMultiLine("Notes", string.Empty);
        AddButtonRow(Button("Save Document", () =>
        {
            try
            {
                if (!System.IO.File.Exists(path.Text.Trim()))
                {
                    MessageBox.Show("Please choose a valid file.", "Wizard", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                string cat = category.SelectedItem?.ToString() ?? "Other";
                string folder = Path.Combine(AppState.ActiveCase!.CaseFolderPath, "documents", SanitizeFolderName(cat));
                Directory.CreateDirectory(folder);
                string source = path.Text.Trim();
                string dest = GetUniqueFilePath(Path.Combine(folder, Path.GetFileName(source)));
                System.IO.File.Copy(source, dest);
                new DocumentsRepository(GetDatabasePath()).Upsert(new DocumentRecord
                {
                    Title = string.IsNullOrWhiteSpace(title.Text) ? Path.GetFileNameWithoutExtension(source) : title.Text.Trim(),
                    Category = cat,
                    Tags = tags.Text.Trim(),
                    OriginalFileName = Path.GetFileName(source),
                    SourceFilePath = source,
                    StoredFilePath = dest,
                    Notes = notes.Text.Trim(),
                    LinkedRecordType = "None",
                    IsActive = true
                });
                ShowDocumentsMore();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not save the document.\n\n" + ex.Message, "Wizard", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }), Button("Cancel", new Action(ShowDocumentsIntro)));
    }

    private void ShowDocumentsMore()
    {
        ClearWizard();
        AddTitle("Document saved");
        AddParagraph("Would you like to add another document, or finish the setup wizard?", 52);
        AddButtonRow(Button("Add Another Document", new Action(ShowDocumentAdd)), Button("Finish Setup", new Action(ShowFinish)), Button("Finish Later", new Action(GoDashboard)));
    }

    private void ShowFinish()
    {
        ClearWizard();
        AddTitle("Setup complete for now");
        AddParagraph("Great work. Your case now has a guided foundation. You can continue polishing details from the left-side pages, and any questions can later be opened from the Question Mark button at the top of the application.", 96);
        AddButtonRow(Button("Go to Dashboard", new Action(GoDashboard)), Button("Add More Household People", new Action(ShowHouseholdIntro)), Button("Add More Documents", new Action(ShowDocumentsIntro)));
    }

    private void GoDashboard()
    {
        Parent?.FindForm()?.Invoke(new Action(() =>
        {
            var form = Parent?.FindForm();
            var navigationField = form?.GetType().GetField("_navigation", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (navigationField?.GetValue(form) is NavigationService navigation)
                navigation.NavigateTo(AppPageKey.Dashboard);
        }));
    }

    private void ClearWizard(bool showJumpBar = true)
    {
        _wizardHost.SuspendLayout();
        _wizardHost.Controls.Clear();
        _wizardHost.AutoScrollPosition = Point.Empty;
        _wizardHost.ResumeLayout();

        if (showJumpBar && AppState.ActiveCase is not null)
            AddWizardJumpDropdown();
    }

    private void AddWizardJumpDropdown()
    {
        var row = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Width = 940,
            Height = 42,
            BackColor = AppColors.PanelBackground,
            Margin = new Padding(0, 0, 0, 12)
        };

        var label = new Label
        {
            Text = "Jump to setup section",
            AutoSize = false,
            Width = 170,
            Height = 30,
            ForeColor = AppColors.TextMuted,
            Font = AppFonts.Body,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(0, 3, 10, 0)
        };

        var combo = new ComboBox
        {
            Width = 320,
            Height = 30,
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = AppColors.SearchBoxBackground,
            ForeColor = AppColors.TextPrimary,
            Font = AppFonts.Body,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(0, 3, 0, 0)
        };

        combo.Items.Add(new WizardJumpItem("Choose a section...", null));
        combo.Items.Add(new WizardJumpItem("Household / People", new Action(ShowHouseholdIntro)));
        combo.Items.Add(new WizardJumpItem("Income Sources", new Action(ShowIncomeIntro)));
        combo.Items.Add(new WizardJumpItem("Bills / Spending", new Action(ShowBillsIntro)));
        combo.Items.Add(new WizardJumpItem("Allowance / Savings", new Action(ShowAllowanceIntro)));
        combo.Items.Add(new WizardJumpItem("Assets", new Action(ShowAssetsIntro)));
        combo.Items.Add(new WizardJumpItem("Debts", new Action(ShowDebtsIntro)));
        combo.Items.Add(new WizardJumpItem("Documents", new Action(ShowDocumentsIntro)));
        combo.Items.Add(new WizardJumpItem("Finish Setup", new Action(ShowFinish)));
        combo.SelectedIndex = 0;

        combo.SelectedIndexChanged += (_, _) =>
        {
            if (combo.SelectedItem is not WizardJumpItem item || item.Action is null)
                return;

            BeginInvoke(new Action(item.Action));
        };

        row.Controls.Add(label);
        row.Controls.Add(combo);
        AddLooseControl(row);
    }

    private void AddTitle(string text)
    {
        AddLooseControl(new Label { Text = text, AutoSize = false, Width = 880, Height = 42, ForeColor = AppColors.TextPrimary, Font = AppFonts.PageTitle, TextAlign = ContentAlignment.MiddleLeft });
    }

    private void AddParagraph(string text, int height)
    {
        AddLooseControl(new Label { Text = text, AutoSize = false, Width = 920, Height = height, ForeColor = AppColors.TextMuted, Font = AppFonts.Body, TextAlign = ContentAlignment.TopLeft });
    }

    private void AddWarning(string text)
    {
        AddLooseControl(new Label { Text = text, AutoSize = false, Width = 920, Height = 68, ForeColor = AppColors.Warning, Font = AppFonts.Body, TextAlign = ContentAlignment.TopLeft });
    }

    private void AddInfoLine(string text)
    {
        AddLooseControl(new Label { Text = text, AutoSize = false, Width = 920, Height = 32, ForeColor = AppColors.Good, Font = AppFonts.BodyBold, TextAlign = ContentAlignment.MiddleLeft });
    }

    private void AddSmallMuted(string text)
    {
        AddLooseControl(new Label { Text = text, AutoSize = false, Width = 780, Height = 38, ForeColor = AppColors.TextSubtle, Font = AppFonts.Body, TextAlign = ContentAlignment.TopLeft });
    }

    private TextBox AddLabeledTextBox(string label, string value, string placeholder, int width = 420)
    {
        var lbl = AddLabel(label);
        var box = AddTextBox(value, placeholder, width);
        return box;
    }

    private TextBox AddTextBox(string value, string placeholder, int width = 420)
    {
        var box = new TextBox { Width = width, Height = 28, AutoSize = false, Text = value, BackColor = AppColors.SearchBoxBackground, ForeColor = AppColors.TextPrimary, BorderStyle = BorderStyle.FixedSingle, Font = AppFonts.Body };
        if (!string.IsNullOrWhiteSpace(placeholder)) _toolTip.SetToolTip(box, placeholder);
        AddLooseControl(box);
        return box;
    }

    private TextBox AddLabeledMultiLine(string label, string value)
    {
        AddLabel(label);
        var box = new TextBox { Width = 720, Height = 86, Multiline = true, ScrollBars = ScrollBars.Vertical, Text = value, BackColor = AppColors.SearchBoxBackground, ForeColor = AppColors.TextPrimary, BorderStyle = BorderStyle.FixedSingle, Font = AppFonts.Body };
        AddLooseControl(box);
        return box;
    }

    private Label AddLabel(string text)
    {
        var lbl = new Label { Text = text, AutoSize = false, Width = 720, Height = 28, ForeColor = AppColors.TextMuted, Font = AppFonts.Body, TextAlign = ContentAlignment.BottomLeft, Margin = new Padding(0, 10, 0, 0) };
        AddLooseControl(lbl);
        return lbl;
    }

    private ComboBox AddCombo(string label, IEnumerable<string> options, string selected)
    {
        AddLabel(label);
        var combo = new ComboBox { Width = 420, Height = 28, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = AppColors.SearchBoxBackground, ForeColor = AppColors.TextPrimary, Font = AppFonts.Body, FlatStyle = FlatStyle.Flat };
        foreach (string option in options) combo.Items.Add(option);
        int idx = combo.Items.IndexOf(selected);
        combo.SelectedIndex = idx >= 0 ? idx : (combo.Items.Count > 0 ? 0 : -1);
        AddLooseControl(combo);
        return combo;
    }

    private ComboBox AddPersonCombo(string label)
    {
        var names = new List<string> { $"Self ({_primaryPersonNameOrCase()})" };
        try
        {
            names.AddRange(new HouseholdPeopleRepository(GetDatabasePath()).GetAll().Select(p => p.FullName).Where(n => !string.IsNullOrWhiteSpace(n) && !names.Contains(n)).OrderBy(n => n));
        }
        catch { }
        names.Add("Other");
        return AddCombo(label, names, names[0]);
    }

    private CheckBox AddCheckBox(string text, bool isChecked)
    {
        var box = new CheckBox { Text = text, AutoSize = false, Width = 760, Height = 30, Checked = isChecked, ForeColor = AppColors.TextPrimary, BackColor = AppColors.PanelBackground, Font = AppFonts.Body };
        AddLooseControl(box);
        return box;
    }

    private Button CreateButton(string text, int width = 150)
    {
        var button = new Button { Text = text, Width = width, Height = 40, FlatStyle = FlatStyle.Flat, BackColor = AppColors.SearchButtonBackground, ForeColor = AppColors.TextPrimary, Font = AppFonts.SearchButton, Cursor = Cursors.Hand, Margin = new Padding(0, 8, 12, 0) };
        button.FlatAppearance.BorderColor = AppColors.Border;
        button.FlatAppearance.MouseOverBackColor = AppColors.SidebarButtonHover;
        button.FlatAppearance.MouseDownBackColor = AppColors.SidebarButtonSelected;
        return button;
    }

    private void AddPrimaryButton(string text, Action action)
    {
        var button = CreateButton(text, 230);
        button.BackColor = Color.FromArgb(38, 138, 76);
        button.FlatAppearance.BorderColor = Color.FromArgb(72, 190, 116);
        button.Click += (_, _) => action();
        AddLooseControl(button);
    }

    // These explicit overloads are intentional. They give C# a real target type for
    // button lambdas used inside tuple arguments, e.g. ("Next", () => ShowNext()).
    // The older params-only version cannot reliably infer Action from those lambdas.
    private static WizardButton Button(string text, Action action)
    {
        return new WizardButton(text, action);
    }

    private void AddButtonRow(params WizardButton[] buttons)
    {
        var row = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Width = 940, Height = 54, BackColor = AppColors.PanelBackground, Margin = new Padding(0, 14, 0, 0) };
        foreach (var spec in buttons)
        {
            var button = CreateButton(spec.Text, Math.Max(140, Math.Min(260, spec.Text.Length * 10 + 60)));
            button.Click += (_, _) => spec.Action();
            row.Controls.Add(button);
        }
        AddLooseControl(row);
    }

    private void AddLooseControl(Control control)
    {
        int y = 0;
        if (_wizardHost.Controls.Count > 0)
        {
            Control last = _wizardHost.Controls[_wizardHost.Controls.Count - 1];
            y = last.Bottom + 6;
        }
        control.Location = new Point(0, y);
        _wizardHost.Controls.Add(control);
    }



    private ComboBox AddBillCombo(long selectedId = 0)
    {
        var combo = new ComboBox { Width = 520, Height = 28, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = AppColors.SearchBoxBackground, ForeColor = AppColors.TextPrimary, Font = AppFonts.Body, FlatStyle = FlatStyle.Flat };
        ReloadBillCombo(combo, selectedId);
        AddLooseControl(combo);
        return combo;
    }

    private void ReloadBillCombo(ComboBox combo, long selectedId = 0)
    {
        combo.Items.Clear();
        var bills = LoadBillItems();
        if (bills.Count == 0)
        {
            combo.Items.Add(new BillListItem(0, "No bills yet"));
            combo.SelectedIndex = 0;
            return;
        }

        foreach (var bill in bills)
            combo.Items.Add(bill);

        int selectedIndex = 0;
        for (int i = 0; i < combo.Items.Count; i++)
        {
            if (combo.Items[i] is BillListItem item && item.Id == selectedId)
            {
                selectedIndex = i;
                break;
            }
        }
        combo.SelectedIndex = selectedIndex;
    }

    private List<BillListItem> LoadBillItems()
    {
        try
        {
            return new BillsRepository(GetDatabasePath()).GetAll()
                .Where(b => b.IsActive)
                .Select(b => new BillListItem(b.Id, b.BillName, $"{b.BillName} - {b.MonthlyEquivalent:C2}/mo"))
                .ToList();
        }
        catch
        {
            return new List<BillListItem>();
        }
    }

    private BillListItem? CreateBillForDebtWizard(string debtName, decimal amount, string frequency, string owner, string paidBy)
    {
        using var form = new Form
        {
            Text = "Create Bill for Debt Payment",
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MinimizeBox = false,
            MaximizeBox = false,
            ClientSize = new Size(440, 250),
            BackColor = AppColors.PanelBackground,
            ForeColor = AppColors.TextPrimary
        };

        var nameLabel = WizardDialogLabel("Bill / expense name", 18, 18);
        var nameBox = WizardDialogTextBox(18, 42, 390);
        nameBox.Text = string.IsNullOrWhiteSpace(debtName) ? "Debt Payment" : debtName + " Payment";

        var amountLabel = WizardDialogLabel("Payment amount", 18, 78);
        var amountBox = WizardDialogTextBox(18, 102, 180);
        amountBox.Text = amount <= 0m ? string.Empty : amount.ToString("0.##", CultureInfo.CurrentCulture);

        var frequencyLabel = WizardDialogLabel("Frequency", 18, 138);
        var frequencyCombo = new ComboBox { Location = new Point(18, 162), Size = new Size(220, 28), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = AppColors.SearchBoxBackground, ForeColor = AppColors.TextPrimary, Font = AppFonts.Body, FlatStyle = FlatStyle.Flat };
        frequencyCombo.Items.AddRange(FrequencyOptions.Cast<object>().ToArray());
        frequencyCombo.SelectedItem = FrequencyOptions.Contains(frequency) ? frequency : "Monthly";

        var ok = CreateButton("Create Bill", 110);
        ok.Location = new Point(210, 202);
        var cancel = CreateButton("Cancel", 90);
        cancel.Location = new Point(330, 202);

        form.Controls.AddRange(new Control[] { nameLabel, nameBox, amountLabel, amountBox, frequencyLabel, frequencyCombo, ok, cancel });
        form.AcceptButton = ok;
        form.CancelButton = cancel;
        cancel.Click += (_, _) => form.DialogResult = DialogResult.Cancel;

        BillListItem? created = null;
        ok.Click += (_, _) =>
        {
            string billName = nameBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(billName))
            {
                MessageBox.Show(form, "Enter a bill name.", "Create Bill", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            decimal.TryParse(amountBox.Text.Trim(), NumberStyles.Currency, CultureInfo.CurrentCulture, out decimal billAmount);
            var bill = new Bill
            {
                BillName = billName,
                Category = "Debt Payment",
                Amount = billAmount,
                Frequency = frequencyCombo.SelectedItem?.ToString() ?? "Monthly",
                PaidBy = paidBy,
                ResponsibilityOwner = owner,
                Priority = "Normal",
                IsActive = true,
                Notes = "Created from Finance Setup Wizard while adding a linked debt."
            };
            long id = new BillsRepository(GetDatabasePath()).Upsert(bill);
            created = new BillListItem(id, bill.BillName, $"{bill.BillName} - {bill.MonthlyEquivalent:C2}/mo");
            form.DialogResult = DialogResult.OK;
        };

        return form.ShowDialog(this) == DialogResult.OK ? created : null;
    }

    private ComboBox AddBankAssetCombo(long selectedId = 0)
    {
        var combo = new ComboBox { Width = 520, Height = 28, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = AppColors.SearchBoxBackground, ForeColor = AppColors.TextPrimary, Font = AppFonts.Body, FlatStyle = FlatStyle.Flat };
        ReloadBankCombo(combo, selectedId);
        AddLooseControl(combo);
        return combo;
    }

    private void ReloadBankCombo(ComboBox combo, long selectedId = 0)
    {
        combo.Items.Clear();
        var banks = LoadBankAssets();
        if (banks.Count == 0)
        {
            combo.Items.Add(new BankAssetListItem(0, "No bank accounts yet"));
            combo.SelectedIndex = 0;
            return;
        }

        foreach (var bank in banks)
            combo.Items.Add(bank);

        int selectedIndex = 0;
        for (int i = 0; i < combo.Items.Count; i++)
        {
            if (combo.Items[i] is BankAssetListItem item && item.Id == selectedId)
            {
                selectedIndex = i;
                break;
            }
        }
        combo.SelectedIndex = selectedIndex;
    }

    private List<BankAssetListItem> LoadBankAssets()
    {
        try
        {
            return new AssetsRepository(GetDatabasePath()).GetAll()
                .Where(a => a.IsActive && a.AssetType.Equals("Bank", StringComparison.OrdinalIgnoreCase))
                .Select(a => new BankAssetListItem(a.Id, BuildBankDisplayName(a)))
                .ToList();
        }
        catch
        {
            return new List<BankAssetListItem>();
        }
    }

    private static string BuildBankDisplayName(AssetItem asset)
    {
        string institution = string.IsNullOrWhiteSpace(asset.InstitutionName) ? asset.LocationOrInstitution : asset.InstitutionName;
        string nickname = string.IsNullOrWhiteSpace(asset.AccountNickname) ? asset.AssetName : asset.AccountNickname;
        if (!string.IsNullOrWhiteSpace(institution) && !string.IsNullOrWhiteSpace(nickname) && !nickname.Contains(institution, StringComparison.OrdinalIgnoreCase))
            return $"{asset.AssetName} ({institution} - {nickname})";
        return string.IsNullOrWhiteSpace(asset.AssetName) ? "Bank Account" : asset.AssetName;
    }

    private bool ApplyWizardAllowanceWhere(AllowanceSavingsItem item, ComboBox bankCombo, TextBox otherWhere)
    {
        string method = item.StorageMethod;
        item.LinkedBankAssetId = 0;
        item.LinkedBankAssetName = string.Empty;

        if (method == "Select Bank Account")
        {
            if (bankCombo.SelectedItem is not BankAssetListItem bank || bank.Id <= 0)
            {
                MessageBox.Show("Select a bank account, or choose Create Bank Account Now.", "Wizard", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            item.LinkedBankAssetId = bank.Id;
            item.LinkedBankAssetName = bank.Name;
            item.WhereStored = bank.Name;
            return true;
        }

        if (method == "Other")
        {
            item.WhereStored = otherWhere.Text.Trim();
            return true;
        }

        item.WhereStored = "Cash / Envelope";
        return true;
    }

    private long CreateBankAccountFromWizard()
    {
        using var form = new Form
        {
            Text = "Create Bank Account Asset",
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MinimizeBox = false,
            MaximizeBox = false,
            ClientSize = new Size(440, 290),
            BackColor = AppColors.PanelBackground,
            ForeColor = AppColors.TextPrimary
        };

        var nameLabel = WizardDialogLabel("Asset name", 18, 18);
        var nameBox = WizardDialogTextBox(18, 42, 390);
        var institutionLabel = WizardDialogLabel("Bank / institution", 18, 78);
        var institutionBox = WizardDialogTextBox(18, 102, 390);
        var nicknameLabel = WizardDialogLabel("Account nickname", 18, 138);
        var nicknameBox = WizardDialogTextBox(18, 162, 390);
        var balanceLabel = WizardDialogLabel("Current balance / value (optional)", 18, 198);
        var balanceBox = WizardDialogTextBox(18, 222, 180);
        var ok = CreateButton("Create", 90);
        ok.Location = new Point(214, 236);
        var cancel = CreateButton("Cancel", 90);
        cancel.Location = new Point(316, 236);

        form.Controls.AddRange(new Control[] { nameLabel, nameBox, institutionLabel, institutionBox, nicknameLabel, nicknameBox, balanceLabel, balanceBox, ok, cancel });
        form.AcceptButton = ok;
        form.CancelButton = cancel;
        cancel.Click += (_, _) => form.DialogResult = DialogResult.Cancel;

        long newId = 0;
        ok.Click += (_, _) =>
        {
            string assetName = nameBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(assetName))
            {
                MessageBox.Show(form, "Enter a bank account asset name.", "Create Bank Account", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            decimal.TryParse(balanceBox.Text.Trim(), NumberStyles.Currency, CultureInfo.CurrentCulture, out decimal balance);
            var asset = new AssetItem
            {
                AssetName = assetName,
                AssetType = "Bank",
                Owner = AppState.ActiveCase?.PrimaryPersonName ?? _primaryPersonNameOrCase(),
                Status = "Active / In Use",
                LocationOrInstitution = institutionBox.Text.Trim(),
                InstitutionName = institutionBox.Text.Trim(),
                AccountNickname = nicknameBox.Text.Trim(),
                CurrentBalanceValue = balance,
                EstimatedValue = balance,
                IsActive = true
            };
            newId = new AssetsRepository(GetDatabasePath()).Upsert(asset);
            form.DialogResult = DialogResult.OK;
        };

        return form.ShowDialog(this) == DialogResult.OK ? newId : 0;
    }

    private static Label WizardDialogLabel(string text, int x, int y)
    {
        return new Label { Text = text, Location = new Point(x, y), Size = new Size(390, 20), ForeColor = AppColors.TextMuted, Font = AppFonts.Body };
    }

    private static TextBox WizardDialogTextBox(int x, int y, int width)
    {
        return new TextBox { Location = new Point(x, y), Size = new Size(width, 26), BackColor = AppColors.SearchBoxBackground, ForeColor = AppColors.TextPrimary, BorderStyle = BorderStyle.FixedSingle, Font = AppFonts.Body };
    }

    private void SetPreviousLabelVisible(Control inputControl, bool visible)
    {
        int idx = _wizardHost.Controls.GetChildIndex(inputControl);
        for (int i = idx + 1; i < _wizardHost.Controls.Count; i++)
        {
            if (_wizardHost.Controls[i] is Label label)
            {
                label.Visible = visible;
                return;
            }
        }
    }

    private void ReflowWizardControls()
    {
        int y = 0;
        for (int i = 0; i < _wizardHost.Controls.Count; i++)
        {
            var control = _wizardHost.Controls[i];
            if (!control.Visible)
                continue;
            control.Location = new Point(control.Left, y);
            y = control.Bottom + 6;
        }
    }

    private List<IncomeSourceListItem> LoadIncomeSources()
    {
        try
        {
            return new IncomeSourcesRepository(GetDatabasePath()).GetAll()
                .Where(i => i.IsActive)
                .Select(i => new IncomeSourceListItem(i.Id, i.SourceName, $"{i.SourceName} - {i.MonthlyEquivalent:C2}/mo"))
                .ToList();
        }
        catch
        {
            return new List<IncomeSourceListItem>();
        }
    }

    private string GetDatabasePath()
    {
        if (AppState.ActiveCase is null)
            throw new InvalidOperationException("No active case is open.");
        return Path.Combine(AppState.ActiveCase.CaseFolderPath, "data.db");
    }

    private string _primaryPersonNameOrCase()
    {
        if (!string.IsNullOrWhiteSpace(_primaryPersonName)) return _primaryPersonName;
        if (!string.IsNullOrWhiteSpace(AppState.ActiveCase?.PrimaryPersonName)) return AppState.ActiveCase.PrimaryPersonName;
        return "the head of household";
    }

    private void ClearPendingPerson()
    {
        _pendingPersonName = string.Empty; _pendingPersonRelationship = string.Empty; _pendingPersonRole = string.Empty; _pendingPersonLivesInHousehold = true; _pendingPersonPaysRent = false; _pendingPersonUsesVehicle = false; _pendingPersonReceivesRides = false; _pendingPersonNotes = string.Empty;
    }

    private static string SanitizeFolderName(string input)
    {
        char[] invalid = Path.GetInvalidFileNameChars();
        string cleaned = new(input.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
        return string.IsNullOrWhiteSpace(cleaned) ? "Other" : cleaned.Trim();
    }

    private static string GetUniqueFilePath(string requestedPath)
    {
        if (!System.IO.File.Exists(requestedPath)) return requestedPath;
        string folder = Path.GetDirectoryName(requestedPath) ?? string.Empty;
        string name = Path.GetFileNameWithoutExtension(requestedPath);
        string ext = Path.GetExtension(requestedPath);
        for (int i = 2; i < 1000; i++)
        {
            string candidate = Path.Combine(folder, $"{name} ({i}){ext}");
            if (!System.IO.File.Exists(candidate)) return candidate;
        }
        return Path.Combine(folder, $"{name} {DateTime.Now:yyyyMMddHHmmss}{ext}");
    }

    private static readonly string[] FrequencyOptions =
    {
        "Weekly", "Every 2 weeks", "Twice monthly", "Monthly", "Quarterly", "Yearly", "One-time / irregular"
    };


    private sealed class WizardJumpItem
    {
        public WizardJumpItem(string text, Action? action)
        {
            Text = text;
            Action = action;
        }

        public string Text { get; }
        public Action? Action { get; }
        public override string ToString() => Text;
    }


    private sealed class WizardButton
    {
        public WizardButton(string text, Action action)
        {
            Text = text;
            Action = action;
        }

        public string Text { get; }
        public Action Action { get; }
    }



    private sealed class BillListItem
    {
        public BillListItem(long id, string name, string? display = null)
        {
            Id = id;
            Name = name;
            Display = string.IsNullOrWhiteSpace(display) ? name : display;
        }

        public long Id { get; }
        public string Name { get; }
        public string Display { get; }
        public override string ToString() => Display;
    }

    private sealed class BankAssetListItem
    {
        public BankAssetListItem(long id, string name)
        {
            Id = id;
            Name = name;
        }

        public long Id { get; }
        public string Name { get; }
        public override string ToString() => Name;
    }

    private sealed class IncomeSourceListItem
    {
        public IncomeSourceListItem(long id, string name, string display)
        {
            Id = id; Name = name; Display = display;
        }
        public long Id { get; }
        public string Name { get; }
        public string Display { get; }
    }
}
