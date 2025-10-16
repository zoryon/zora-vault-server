# Android 13+ BLE FIDO2 Password Manager – Full Specification (Centralized Zero-Knowledge)

---

## 1. Project Overview

A cross-platform password manager designed for **Android 13+**, with:

- Centralized encrypted storage (zero-knowledge server)
- BLE-based FIDO2 authentication for external devices
- Multi-device compatibility
- Modern, mobile-first UI
- Full passkey support within the app
- Zero-knowledge authentication (master password never stored on server)

**Key design principle:** All sensitive data is encrypted **client-side**; the server only stores encrypted data, public keys, and metadata.

--- 

## 2. Core Features

### 2.1 Vault & Password Storage

- **Vault items**: usernames, passwords, URLs, notes, attachments
- **Encryption**:
  - AES-256-GCM, client-side
  - Master password → Argon2id → encryption key
- **Features**:
  - Folders, tags, favorites
  - Versioning and history
  - Password generator (customizable length, symbols, letters, digits)
- **Temporary in-memory cache** for offline access (encrypted, wiped on app exit)

---

### 2.2 FIDO2 Credentials

- Stored **encrypted on server**; private keys remain on device Keystore
- Each credential contains:
  - Public key (for verification and multi-device sharing)
  - Metadata: account name, service, creation date
- Enables **WebAuthn challenge-response** entirely client-side
- Multi-device: any authorized device can participate in BLE FIDO2 login

---

### 2.3 BLE FIDO2 Authenticator

- Acts as a **mini FIDO2 authenticator** over BLE
- **Workflow**:
  1. External device scans for BLE service
  2. App advertises BLE service UUID
  3. Device sends challenge + account ID
  4. App fetches encrypted credential from centralized DB
  5. Prompts user approval
  6. Signs challenge with private key in Keystore
  7. Sends signed response back to external device
  8. Device verifies using public key
- Optional BLE pairing or short-lived PIN for extra security
- Works **even if server temporarily unreachable**, if credential cached in-memory

---

### 2.4 Centralized Zero-Knowledge Server

- Stores:
  - Encrypted credentials (AES ciphertext)
  - Metadata: account info, device IDs, timestamps
  - Public keys for FIDO2 verification
- Responsible for:
  - Multi-device synchronization
  - Device registration & management
  - Audit logging
  - Push notifications for BLE login requests
- **Server cannot decrypt vault items**; all decryption occurs on client

---

### 2.5 User Authentication (Zero-Knowledge)

**No master password is stored on the server.**

**Flow:**

1. **Account creation**:
   - Client generates random salt
   - Derives authentication key:  
     `AuthKey = Argon2id(MasterPassword + Salt)`
   - Computes a server verifier: e.g., `AuthVerifier = HMAC(AuthKey, ServerChallenge)`  
   - Server stores only `AuthVerifier` + `Salt`  
   - Master password **never leaves the device**
   
2. **Login**:
   - Client derives `AuthKey` from master password + stored salt
   - Performs challenge-response with server verifier
   - Server confirms correctness without knowing master password

**Optional**: Use **SRP (Secure Remote Password protocol)** for fully standardized zero-knowledge authentication.

---

### 2.6 User Experience

- Modern Material Design 3 UI
- Vault unlock via:
  - Biometric (Fingerprint/FaceID)
  - Master password
- Credential search, filters, favorites
- Notifications for incoming BLE login requests
- Credential selection interface with account preview
- Optional offline cache (encrypted, wiped on timeout)

---

## 3. Architecture

### 3.1 Mobile App (Android 13+)

- **React Native frontend**:
  - UI components: `react-native-paper` or `dripsy`
  - In-memory encrypted cache for temporary offline access
- **Native Android modules**:
  - FIDO2/WebAuthn crypto
  - Keystore/StrongBox integration
  - BLE peripheral & central roles
  - BLE challenge-response handler
- **Workflow**:
  1. App receives BLE login request
  2. Fetches encrypted credential from server
  3. Decrypts using master password-derived key
  4. Prompts user approval
  5. Signs challenge and sends response

### 3.2 External Device

- BLE central role
- Sends challenge to mobile app
- Receives signed challenge
- Verifies using public key
- No need for server during direct BLE authentication if credential cached

### 3.3 Backend Server

- C# ASP.NET Core
- Database: MySQL (Oracle HeatWave)
- Stores:
  - Encrypted vault items
  - Metadata
  - Public keys for FIDO2
  - Device registration & audit logs
- Zero-knowledge: server never sees decrypted vault items or master passwords
- TLS 1.3 for all communications

---

## 4. Cryptography

- **Vault encryption**: AES-256-GCM
- **Master password derivation**: Argon2id
- **FIDO2 private keys**: stored in Android Keystore / StrongBox
- **Challenge-response over BLE**:
  - Random 32–64 byte challenges
  - Signed locally using Keystore private key
- **Server security**:
  - TLS 1.3
  - No plaintext sensitive data

---

## 5. BLE Protocol Specification

- **Service UUID**: `0000FIDO-0000-1000-8000-00805F9B34FB` (example)
- **Characteristics**:
  1. `ChallengeRequest` (write)
  2. `SignedResponse` (notify)
  3. `RequestMetadata` (read)
- **Flow**:
  1. Device scans → discovers BLE service
  2. Writes challenge
  3. App fetches encrypted credential from server
  4. Prompts user approval
  5. Signs challenge → notifies back
  6. Device verifies signature

---

## 6. Database Schema (Centralized, MySQL HeatWave)

### **Users**
- `user_id` (UUID, PK)
- `email` (unique)
- `username` (unique)
- `salt` (random per-user, for Argon2id)
- `auth_verifier` (HMAC or SRP verifier)
- `created_at`, `last_login`

### **VaultItems**
- `vault_item_id` (UUID, PK)
- `user_id` (FK → Users)
- `type` (password/note/attachment)
- `encrypted_data` (BLOB, AES-256-GCM)
- `created_at`, `updated_at`

### **Passkeys**
- `passkey_id` (UUID, PK)
- `user_id` (FK → Users)
- `vault_item_id` (optional FK → VaultItems)
- `public_key` (PEM/DER)
- `credential_id` (FIDO2 ID)
- `device_name` (optional)
- `created_at`

### **Devices**
- `device_id` (UUID, PK)
- `user_id` (FK → Users)
- `device_name`
- `last_seen`
- `public_key` (optional BLE auth)
- `created_at`

### **AuditLog**
- `audit_id` (UUID, PK)
- `user_id` (FK → Users)
- `device_id` (FK → Devices, optional)
- `action` (string)
- `timestamp`
- `metadata` (JSON)

---

## 7. Implementation Notes

- **Backend**: C# ASP.NET Core + MySQL HeatWave  
- **Frontend**: React Native + Android native modules (Keystore/BLE/FIDO2)  
- **Libraries**:
  - `Pomelo.EntityFrameworkCore.MySql` for C# MySQL ORM
  - `libsodium-net` or `BouncyCastle.Crypto` for AES-GCM (if needed)
- AES-256-GCM and Argon2id handled on **mobile client**
- FIDO2 private keys stored **only in Keystore**, never sent to server
- BLE handled via `react-native-ble-plx` or custom native module

---

## 8. Roadmap (High-Level)

1. **Phase 0 – Planning**: architecture, DB schema, cryptography, UI mockups  
2. **Phase 1 – Backend**: DB tables, REST/GraphQL endpoints, zero-knowledge auth  
3. **Phase 2 – Client Skeleton**: login screen, vault display, encrypted storage  
4. **Phase 3 – FIDO2 Credential Handling**: create, encrypt, store public key  
5. **Phase 4 – BLE FIDO2 Login**: challenge-response flow  
6. **Phase 5 – Multi-device Sync & Audit**  
7. **Phase 6 – UX Polishing & Security Enhancements**  
8. **Phase 7 – Android 14+ system passkey integration (optional)**  
9. **Phase 8 – Deployment & Maintenance**

---

## 9. Security Considerations

- Private keys **never leave device**  
- Master password never stored server-side  
- Signed challenges prevent replay attacks  
- BLE pairing/bonding optional but recommended  
- TLS for all communication  
- Auto-lock, auto-clear clipboard, self-destruct on repeated failed attempts  

---

## 10. References

- Android Keystore / StrongBox  
- WebAuthn / FIDO2 / CTAP2  
- Argon2id key derivation  
- AES-256-GCM encryption  
- Zero-knowledge authentication principles  
- BLE GATT specification  
- Bitwarden open-source vault design
