# Booking Payment Behavior (Customer Account Wallet Model)

This document defines the uniform financial processing rules for all booking payments (Other Services, Transportation, Visa, VFS, Holidays, Hotels, Flights) in the Sales Portal.

---

## 1. CORE FINANCIAL PRINCIPLES

The customer (Head) financial state is tracked using a single balance: `Wallet` (which represents the customer's running account balance).

There are only two situations that modify the Wallet balance:
1. **Wallet Bookings**: Creating a booking with `PaymentMethod == "wallet"` decreases the Wallet balance:
   `Wallet -= BookingAmount`
2. **Wallet Deposits**: Creating a receipt with `Service == "wallet_deposit"` increases the Wallet balance when approved:
   `Wallet += DepositAmount`

No other booking payment methods (cash, online, bank transfer) or receipt types affect the Wallet balance.

---

## 2. WALLET STATUS AND INTERPRETATION

- **Wallet > 0**: Customer has prepaid/advance credit.
- **Wallet = 0**: Customer account is settled.
- **Wallet < 0**: Customer owes money to the company (outstanding balance). Negative balances are expected and fully supported. No booking fails due to a negative wallet balance; there is NO insufficient balance check.

---

## 3. EVENT BEHAVIORS

| Event | Wallet | Description |
| :--- | :--- | :--- |
| **Booking Creation (Wallet)** | `Wallet -= BookingAmount` | Decrements the wallet balance for the booking amount. |
| **Booking Creation (Cash/Online/Bank)** | *No Change* | Does not modify the wallet. |
| **Receipt Approval (Wallet Deposit)** | `Wallet += DepositAmount` | Increments the wallet balance when verified by the accountant. |
| **Receipt Reversal (Wallet Deposit)** | `Wallet -= DepositAmount` | Decrements the wallet balance if an approved wallet deposit is cancelled or failed. |
| **Receipt Approval/Reversal (Other)** | *No Change* | Does not modify the wallet. |

---

## 4. RECEIPT ALLOCATION

Receipt Allocation is **strictly a non-financial operation** used to match payment receipts to specific bookings for record-keeping and updating booking payment statuses (e.g., marking a booking as `paid`).

**Receipt Allocation does NOT modify the Wallet balance.** The Wallet balance is updated solely upon receipt approval/reversal for wallet deposits.
