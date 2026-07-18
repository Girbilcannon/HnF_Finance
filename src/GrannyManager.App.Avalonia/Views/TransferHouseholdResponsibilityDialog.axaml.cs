using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using GrannyManager.Application.Services;
using GrannyManager.Core.Models;

namespace GrannyManager.App.Avalonia.Views
{
    public partial class TransferHouseholdResponsibilityDialog : Window
    {
        private readonly TextBlock? _affectedRecordsTextBlock;
        private readonly TextBlock? _validationTextBlock;
        private readonly ComboBox? _transferTargetComboBox;
        private readonly List<HouseholdPerson> _targets = new();

        public TransferHouseholdResponsibilityDialog()
        {
            InitializeComponent();

            _affectedRecordsTextBlock = this.FindControl<TextBlock>("AffectedRecordsTextBlock");
            _validationTextBlock = this.FindControl<TextBlock>("ValidationTextBlock");
            _transferTargetComboBox = this.FindControl<ComboBox>("TransferTargetComboBox");

            var cancelButton = this.FindControl<Button>("CancelButton");
            if (cancelButton is not null)
                cancelButton.Click += (_, _) => Close(false);

            var transferButton = this.FindControl<Button>("TransferButton");
            if (transferButton is not null)
                transferButton.Click += (_, _) => TransferAndClose();
        }

        public HouseholdPerson? SelectedTransferTarget { get; private set; }

        public void SetMode(HouseholdInactiveImpactPreview preview, IReadOnlyList<HouseholdPerson> transferTargets)
        {
            if (_affectedRecordsTextBlock is not null)
            {
                var income = preview.IncomeSources.Count == 0 ? "None" : string.Join(", ", preview.IncomeSources);
                var bills = preview.Bills.Count == 0 ? "None" : string.Join(", ", preview.Bills);
                var debts = preview.Debts.Count == 0 ? "None" : string.Join(", ", preview.Debts);

                _affectedRecordsTextBlock.Text =
                    $"Income to deactivate: {income}\n" +
                    $"Bills needing transfer: {bills}\n" +
                    $"Debts needing transfer: {debts}";
            }

            _targets.Clear();
            _targets.Add(new HouseholdPerson
            {
                Id = 0,
                FullName = "Self (Primary Person)",
                IsActive = true
            });

            if (transferTargets is not null)
                _targets.AddRange(transferTargets.Where(person => person.IsActive));

            if (_transferTargetComboBox is not null)
            {
                _transferTargetComboBox.Items.Clear();

                foreach (var target in _targets)
                    _transferTargetComboBox.Items.Add(new ComboBoxItem { Content = target.FullName, Tag = target });

                _transferTargetComboBox.SelectedIndex = 0;
            }
        }

        private void TransferAndClose()
        {
            if (_validationTextBlock is not null)
                _validationTextBlock.Text = string.Empty;

            if (_transferTargetComboBox?.SelectedItem is not ComboBoxItem item ||
                item.Tag is not HouseholdPerson target)
            {
                if (_validationTextBlock is not null)
                    _validationTextBlock.Text = "Choose who should take over payment responsibility.";
                return;
            }

            SelectedTransferTarget = target;
            Close(true);
        }
    }
}
