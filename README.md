# Home & Family Finance Manager

Home & Family Finance Manager is a local-first, Windows desktop application for organizing family and household finances, especially when helping manage finances for a parent, relative, friend, or head of household. The app is designed to keep important financial information, documents, bills, income sources, debts, assets, credentials, and household details organized in one place without requiring online accounts, subscriptions, or cloud storage.

## Current Status
This project is currently in active development and is not yet a final public release.

Current working focus:
- Finance Manager
- Case-based financial organization
- Household and people tracking
- Income sources
- Bills and spending
- Allowance and savings
- Assets
- Debts
- Documents
- Credential vault

The Trust Manager side is planned for a future phase and is currently not part of the active release.

## Main Features

### Case-Based Organization
Each financial project is stored as a separate case.

A case includes:
- Case name
- Primary person / head of household
- Case folder
- Local database
- Documents folder
- Exports folder
- Case PIN protection

### Finance Setup Wizard
The Finance Setup Wizard helps new users get started with a guided walkthrough.

The wizard can help create:
- A new case
- Primary person information
- Household members
- Income sources
- Bills and spending
- Allowance and savings entries
- Assets
- Debts
- Documents

### Dashboard
The dashboard gives a quick overview of the active case.

It includes:
- Monthly income
- Known expenses
- Allowance
- Savings
- Remaining or deficit amount
- Household contribution visibility
- Recent cases
- Important documents quick-launch list

### People / Household
Track people involved in the case.

This can include:
- Household members
- Family members
- Helpers
- Trustees or responsible parties
- People contributing income
- People using vehicles or receiving rides

Household contributions are linked to Income Sources to avoid double-counting.

### Income Sources
Track money coming in.

Examples:
- Social Security
- Pension
- Survivor benefits
- Disability
- Employment / wages
- Family contribution
- Rental income
- Retirement account
- Settlement / lump sum
- Other

Income sources support:
- Frequency
- Tax-withheld clarity
- Deposit method
- Bank account asset linking

### Bills / Spending
Track recurring or known expenses.

Examples:
- Mortgage or rent
- Utilities
- Insurance
- Vehicle payments
- Phone bills
- Internet
- IRS payment plans
- Other regular spending

Bills include:
- Who pays this?
- Responsibility / owner
- Frequency
- Past due amount
- Priority
- Active or inactive status

Bills paid by someone other than the primary case person do not reduce the primary person’s remaining money.

### Fuel and Grocery Calculators
Bills / Spending includes calculators for:
- Fuel receipts
- Grocery receipts

Users can add receipt dates and totals. The app calculates an estimated monthly average and updates the related bill estimate automatically.

### Allowance / Savings
Track planned money set aside for:
- Personal allowance
- Savings
- Envelopes
- Bank accounts
- Cash reserves

Allowance and savings can link to Bank-type assets.

### Assets
Track owned or controlled assets.

Supported first-pass asset types:
- Vehicle
- Property
- Bank account
- Valuable item
- Other

Assets can link to:
- Bills / Spending
- Income Sources

This helps avoid duplicating financial numbers while still keeping real-world details organized.

### Debts
Track obligations and outstanding debts.

Debts can include:
- Creditor or collector
- Balance
- Minimum payment
- Responsibility / owner
- Who pays this?
- Linked monthly bill payment

Debt payments can link to Bills / Spending so they are not counted twice.

### Documents
Store and organize important files inside the case folder.

Document features include:
- Copy files into the case
- Category-based document folders
- Tags / keywords
- Linked records
- Important document flag
- Dashboard quick-launch list
- Print-friendly PDF exports

Documents are organized under the case folder by category.

Example:
    <case folder>/documents/<category>/

### Credential Vault
The Credential Vault stores account login information for the active case.

Credential records can include:
- Account name
- Website
- Username
- Password
- Recovery information
- Notes
- Linked record

Sensitive values are encrypted before storage.

Vault behavior includes:
- Passwords hidden by default
- Copy username
- Copy password
- Clipboard auto-clear
- Reveal password only after entering the active case PIN

### Live Search
The top search bar provides fast live search across the active case.

Search categories include:
- People
- Income Sources
- Bills / Spending
- Allowance / Savings
- Assets
- Debts
- Documents
- Credential records

Search results are categorized and clickable. Clicking a result opens the matching profile or record.

## Local-First Design
This app is designed to run locally.

Current design goals:
- No required online accounts
- No cloud dependency
- No subscriptions
- No analytics
- No automatic internet connection
- Case data stored locally
- Documents copied locally
- Exports created locally

Some future optional features may use internet access, such as market or crypto lookup, but those should remain opt-in.

## Security Notes
This project includes a basic case PIN system and an encrypted credential vault.

Important distinction:
- The case PIN prevents the app from opening and displaying a case without the PIN.
- Credential vault secrets are encrypted.
- Full database encryption is planned for future improvement.
- Full document encryption is planned for future improvement.
- Case documents on disk are not fully encrypted yet unless future document encryption is implemented.

Planned security improvements:
- Optional PIN-required document opening
- Optional password-protected PDF exports
- Optional encrypted copied PDFs
- Stronger vault/master-password system
- Better backup and restore controls

## Technology
Built with:
- C#
- WinForms
- .NET 10
- SQLite
- Local file storage

## Project Structure
Typical project layout:

    HomeFamilyFinanceManager/
    ├── GrannyManager.App/
    ├── GrannyManager.Core/
    ├── GrannyManager.Data/
    ├── GrannyManager.Security/
    ├── GrannyManager.Tests/
    └── README.md

Case data is stored outside the application folder, usually under the user’s Documents folder.

Example:

    Documents/
    └── GrannyManager Cases/
        └── Example Case/
            ├── Example_Case.gmcase
            ├── data.db
            ├── documents/
            ├── exports/
            ├── imports/
            └── backups/

## Development Notes
This project is currently being built as a personal and family-use finance organization tool.

Primary goals:
- Keep the interface simple
- Keep records easy to find
- Avoid double-counting money
- Make setup friendly for new users
- Make documents and credentials easier to manage
- Keep data local and private

## Planned Future Work
Planned improvements include:
- Trust Manager section
- Stronger document encryption
- Optional password-protected PDF exports
- More complete help system
- Backup and restore
- More detailed reporting
- Better inactive record styling
- Expanded asset-specific forms
- Improved portable EXE publishing
- Full first public test build polish

## License
This project is proprietary and all rights are reserved.

No permission is granted to copy, modify, distribute, sublicense, sell, publish, host, or use this software or any portion of it in another project without prior written permission from the copyright holder.

Copyright © 2026 Aaron Marine. All rights reserved.

