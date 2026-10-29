# Knowledge base

Source documents for the RAG assistant. The company and its policies are fictional, set in Nepal (NPR, Nepali fiscal year, Sunday to Friday working week). Legal details such as leave minimums, SSF rates and VAT follow the Labour Act and tax rules as understood when written; check them with HR or legal before using this for anything real.

## Layout

```
Documents/
  HR/    leave, remote work, payroll, expenses, onboarding, benefits, conduct,
         public holidays, festival allowance, SSF, income tax and PAN, domestic travel, emergencies
  IT/    VPN, passwords, MFA, laptops, printers, Wi-Fi, software, email, security, support,
         power backup and outages, Nepali language and Unicode
  Finance/  purchasing, vendor payments, corporate cards, budgets, deadlines, petty cash, tax, customer billing, contacts,
            fiscal year and statutory deadlines, VAT/PAN bills and TDS, banking and foreign currency
```

## Document format

The admin uploads these files on the Knowledge page. `title` and `department` come first, then sections under `##` headings. The `---` lines are optional, and there is no `# Title` line, because the title is already at the top:

```markdown
---
title: Leave Policy
department: HR
---

## Annual leave entitlement
...
```

- `department` must equal the department chosen in the upload form (names are compared ignoring case); otherwise the file is discarded.
- Upload mode **Add** keeps existing knowledge (a file with the same name replaces its earlier sections). **Flush and Add** deletes all existing knowledge first.
- Each `##` section is one retrieval chunk, so it must make sense on its own: name the topic in the section text instead of relying on the heading above it.

## Retrieval test questions (HR)

Answerable (expected source in brackets):

| Question | Expected answer | Source |
|---|---|---|
| How many days of annual leave do I get? | 18 days per year | Leave Policy |
| Do I need a doctor's note for sick leave? | Yes, if more than 3 consecutive working days | Leave Policy |
| How much notice do I give for a week of leave? | At least 2 weeks for more than 5 days | Leave Policy |
| How much unused annual leave can I accumulate? | Up to 90 days | Leave Policy |
| How long is paternity leave? | 15 days | Leave Policy |
| When do I get paid? | Last working day of the month | Payroll and Salary |
| By when must I change my bank details? | The 15th of the month | Payroll and Salary |
| How many days a week must I be in the office? | At least 3 | Remote Work Policy |
| Can I work from another country? | Only with written HR approval | Remote Work Policy |
| What is the hotel limit for business trips in Kathmandu? | NPR 8,000 per night | Expense Reimbursement |
| How long do I have to submit an expense claim? | 30 days | Expense Reimbursement |
| How long is probation? | 3 months | Onboarding and Probation |
| Does insurance cover my spouse? | Yes, spouse and up to 2 children | Health Insurance and Benefits |
| What is the learning budget? | NPR 60,000 per year | Health Insurance and Benefits |
| What is the notice period for managers? | 60 days | Code of Conduct and Working Hours |

Not covered (the assistant should say it does not know, and offer to raise a ticket):

- Does the company offer stock options?
- Can I take a sabbatical?
- Is there relocation assistance?
- Does the company pay for a gym membership beyond the wellness allowance?
- What is the policy on jury duty leave?

## Retrieval test questions (IT)

Answerable (expected source in brackets):

| Question | Expected answer | Source |
|---|---|---|
| I can't connect to the VPN, what should I do? | Check internet, restart FortiClient, check password/MFA, try a hotspot | VPN Access |
| What is the VPN address? | vpn.ticket.com on port 443 | VPN Access |
| How do I reset my forgotten password? | Self-service at password.ticket.com | Password Reset and Account Lockout |
| My account is locked, how long until I can try again? | 15 minutes after 5 failed attempts | Password Reset and Account Lockout |
| How long must my password be? | At least 12 characters | Password Reset and Account Lockout |
| I got a new phone, how do I move my authenticator? | Enable backup in Authenticator before wiping the old phone | Multi-Factor Authentication |
| I got an MFA prompt I didn't start | Deny it, tell IT, change the password | Multi-Factor Authentication |
| My laptop was stolen | Report to IT immediately (extension 100), file a police report | Laptops and Hardware |
| How often are laptops replaced? | Every 3 years | Laptops and Hardware |
| How do I add the office printer? | Software Center "Office Printers" or \\print.ticket.com | Printers and Scanning |
| My print job isn't coming out | Release it with your access card at the printer | Printers and Scanning |
| What is the guest Wi-Fi password? | At reception, changes weekly | Wi-Fi and Network |
| Wi-Fi won't connect after a password change | Forget the network and reconnect | Wi-Fi and Network |
| How do I install Zoom? | Software Center | Software Installation |
| Can I get administrator rights? | No, raise a software request | Software Installation |
| What is the maximum email attachment size? | 25 MB | Email and Calendar |
| I clicked a phishing link | Tell IT immediately, change password | Email and Calendar |
| Can I use a USB drive? | Blocked by default, use OneDrive or ask IT | Security and Data Protection |
| How fast does IT respond to a P1 issue? | Within 1 hour | IT Support and Service Levels |
| What are IT support hours? | 08:00 to 20:00, Monday to Friday | IT Support and Service Levels |

Not covered (the assistant should say it does not know, and offer to raise a ticket):

- Do you support Linux laptops?
- How do I set up a company mobile phone?
- Can I install a personal VPN on my work laptop?
- How do I get access to the data warehouse?
- What is the Wi-Fi password at a partner company office?

## Retrieval test questions (Finance)

Answerable (expected source in brackets):

| Question | Expected answer | Source |
|---|---|---|
| When do I need a purchase request? | For purchases above NPR 10,000 paid by the company | Purchase Requests and Procurement |
| Who approves a purchase of NPR 500,000? | Manager and department head | Budgets and Approval Limits |
| Who must approve spending above NPR 1,000,000? | Also the CFO | Budgets and Approval Limits |
| How many quotes do I need for a large purchase? | 3 quotes above NPR 500,000 | Purchase Requests and Procurement |
| Why was the vendor's invoice returned? | It had no valid PO number | Purchase Requests and Procurement |
| Where should vendors send invoices? | ap@ticket.com | Vendor Invoices and Payments |
| When are vendors paid? | Weekly payment run on Thursday, 30-day terms | Vendor Invoices and Payments |
| How do I add a new vendor? | Registration form to ap@ticket.com, up to 5 working days | Vendor Invoices and Payments |
| What is the corporate card limit? | NPR 300,000 per month by default | Corporate Credit Card |
| By when must I reconcile card transactions? | The 5th of the following month | Corporate Credit Card |
| My corporate card was stolen | Call the bank to block it, then tell finance | Corporate Credit Card |
| When does the financial year start? | 1 Shrawan | Budgets and Approval Limits |
| Who can sign a contract? | Only the CFO and the CEO | Budgets and Approval Limits |
| What is the deadline for expenses to be paid this month? | Approved before the 20th | Month-End and Finance Deadlines |
| How much petty cash can I spend at once? | NPR 5,000 per purchase | Petty Cash and Cash Advances |
| How much travel advance can I get? | Up to 70 percent of estimated cost | Petty Cash and Cash Advances |
| How long does finance keep records? | 7 years | Tax and Compliance |
| How do I request a customer invoice? | Finance portal or billing@ticket.com, 2 working days | Customer Invoicing and Collections |
| When do customers get an overdue reminder? | At 7 days overdue | Customer Invoicing and Collections |
| Who do I email about vendor payments? | ap@ticket.com | Finance Support and Contacts |

Not covered (the assistant should say it does not know, and offer to raise a ticket):

- How are foreign exchange gains and losses booked?
- Can the company pay a vendor in cryptocurrency?
- How do I apply for a salary advance?
- What is the depreciation policy for laptops?
- Can I get a loan from the company?

## Retrieval test questions (Nepal-specific)

Answerable (expected source in brackets):

| Question | Expected answer | Source |
|---|---|---|
| What is our weekly day off? | Saturday | Code of Conduct and Working Hours |
| How many public holidays do we get? | At least 13, following the government list | Public Holidays and Festivals |
| Do women get a holiday for Teej? | Yes, Haritalika Teej | Public Holidays and Festivals |
| How long is the Dashain break? | Typically about a week, announced yearly | Public Holidays and Festivals |
| How much is the Dashain allowance? | One month of basic salary | Festival Allowance and Bonus |
| Do I get the Dashain allowance if I joined mid-year? | Yes, pro-rated by months worked | Festival Allowance and Bonus |
| How many days of mourning leave do I get? | 13 days | Leave Policy |
| How long is maternity leave? | 98 days, fully paid | Leave Policy |
| What are the SSF contribution rates? | 11 percent employee, 20 percent company | Social Security Fund and Gratuity |
| Do I get gratuity separately? | No, covered by the company's SSF share | Social Security Fund and Gratuity |
| Do I need a PAN? | Yes, give it to HR on the first day | Income Tax, TDS and PAN for Employees |
| By when do I submit my tax declaration? | End of Poush | Income Tax, TDS and PAN for Employees |
| When do I get my tax certificate? | After the fiscal year ends in Ashadh | Income Tax, TDS and PAN for Employees |
| What should I do during an earthquake? | Drop, cover, hold on, then use the stairs to the assembly point | Emergencies and Business Continuity |
| What do I do during a bandh? | Work from home, manager confirms by 08:00 | Emergencies and Business Continuity |
| What is the ambulance number? | 102 | Emergencies and Business Continuity |
| What is the hotel limit in Pokhara? | NPR 5,000 per night | Domestic Travel Policy |
| How much do I get per km for my own motorcycle? | NPR 10 per km | Domestic Travel Policy |
| What bills are accepted for expenses? | A VAT or PAN bill showing the vendor's PAN | VAT Bills, PAN Bills and TDS |
| What is the VAT rate? | 13 percent | VAT Bills, PAN Bills and TDS |
| When does the fiscal year start and end? | 1 Shrawan to the end of Ashadh | Nepali Fiscal Year and Statutory Deadlines |
| When are VAT and TDS filed? | Normally by the 25th of the following BS month | Nepali Fiscal Year and Statutory Deadlines |
| Can I pay for a foreign subscription with my personal card? | No, raise a purchase request | Banking, Digital Payments and Foreign Currency |
| Are eSewa or Khalti payments reimbursed? | Yes, with a screenshot and a VAT or PAN bill | Banking, Digital Payments and Foreign Currency |
| My home internet is down | Restart the router, use a mobile hotspot, work from the office if long | Power Backup and Internet Outages |
| Is mobile data reimbursed during an outage? | Up to NPR 500 per month | Power Backup and Internet Outages |
| How do I type in Nepali? | Add Nepali (Nepal) in Language settings | Nepali Language and Unicode Support |
| How do I convert Preeti text? | Preeti to Unicode converter on the intranet | Nepali Language and Unicode Support |

Not covered (the assistant should say it does not know, and offer to raise a ticket):

- What are the income tax slabs for this fiscal year?
- Which hospitals are in the insurance network?
- Does the company provide a vehicle or a driver?
- How is the statutory bonus calculated for my exact salary?

## Questions that need the right category

Some questions touch both sets, so retrieval should consider all documents unless the category is clear:

- "I'm working from home, what do I need?" (HR Remote Work Policy and IT VPN Access)
- "I'm leaving the company, what do I return?" (HR Code of Conduct and IT Laptops and Hardware, and Finance Corporate Credit Card)
- "I bought a monitor cable myself, how do I get the money back?" (HR Expense Reimbursement, Finance Purchase Requests)
- "When will my expense claim be paid?" (HR Expense Reimbursement, Finance Month-End and Finance Deadlines)
- "How do I get a travel advance?" (HR Expense Reimbursement, Finance Petty Cash and Cash Advances)
- "The power is out and I have a call in 10 minutes" (IT Power Backup and Internet Outages, HR Remote Work Policy)
- "I need to pay a foreign vendor" (Finance Banking and Foreign Currency, Purchase Requests and Procurement)
- "Where do I deduct my insurance premium?" (HR Income Tax, TDS and PAN, HR Health Insurance and Benefits)
