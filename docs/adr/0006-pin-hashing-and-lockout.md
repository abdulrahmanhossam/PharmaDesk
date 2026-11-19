# ADR 0006: PIN-Based Authentication and 5-Attempt Lockout Policy

## Status
Accepted

## Context
Community pharmacies require rapid operator switching at the point of sale. Traditional alphanumeric passwords or complex multi-factor authentication disrupt counter workflow when cashiers alternate transactions quickly. However, simple numeric PINs are susceptible to brute-force guessing if not aggressively rate-limited and securely stored.

## Decision
We implement a **secure, local PIN-based authentication system**:
1. **User Selection & Numeric PIN**: The login screen displays active team members in a clean dropdown. The user selects their name and enters their numeric PIN.
2. **Cryptographic Hashing**: PINs are never stored in plain text. We utilize ASP.NET Core Identity's `PasswordHasher<TeamMember>` (PBKDF2 with HMAC-SHA256, random salt per user) or `BCrypt.Net-Next` to hash PINs into `TeamMembers.PinHash` (stored in `string(200)`).
3. **5-Attempt Lockout Mechanism**:
   - `TeamMembers.FailedPinCount` (int, default 0) tracks consecutive failed attempts.
   - Upon the 5th consecutive failure, `LockedUntil` is stamped with `DateTime.UtcNow.AddMinutes(5)`, and `FailedPinCount` is reset.
   - If a login attempt occurs while `DateTime.UtcNow < LockedUntil`, authentication is rejected immediately with the remaining minutes displayed.
   - Upon successful PIN verification, `FailedPinCount` is reset to `0` and `LockedUntil` is cleared to `null`.
4. **Session Cookie**: Successful authentication issues an encrypted HTTP cookie (`CookieAuthenticationDefaults.AuthenticationScheme`) carrying claims: `NameIdentifier` (ID), `Name` (Full Name), and `Role` (`Admin` or `Staff`).

### Codebase Alignment & Discrepancies
- `PharmaDesk/Controllers/AccountController.cs` implements this lockout and hashing logic using `PasswordHasher<TeamMember>`.
- In `AccountController.cs`, comparisons use `DateTime.Now` rather than `DateTime.UtcNow`. To avoid timezone and daylight savings skew, UTC should be used uniformly across the lockout calculations.
- `DbSeeder.cs` seeds the initial administrator account with default PIN `1234` hashed via `PasswordHasher<TeamMember>`.

## Consequences
### Positive
- Fast, friendly login experience tailored for touchscreens and numeric keypads at pharmacy counters.
- 5-minute lockout on 5 failed tries reduces a 4-digit PIN search space to an impractical brute-force attack rate on local terminals.
- Industry-standard PBKDF2 hashing protects user credentials against direct database inspection.

### Negative / Risks
- If an administrator locks themselves out, they must wait 5 minutes before retrying.
- A physical keyboard or numeric keypad is required at the counter.
