# Zora Vault: A Zero-Knowledge Security Architecture

**Document Type: Technical Whitepaper**
**Version: 1.1**
**Last Updated: October 16, 2025**

---

## 1. Abstract

Zora Vault is a password management solution engineered on a foundational zero-knowledge security model. This architecture ensures that all sensitive user data is processed exclusively on the client-side, rendering it inaccessible to the Zora Vault service infrastructure or any unauthorized third party. This document provides a detailed technical specification of the cryptographic protocols, feature implementations, and the core security principles that govern the Zora Vault ecosystem. Our objective is to transparently detail the mechanisms that guarantee end-to-end encryption and user data sovereignty.

---

## 2. Core Cryptographic Architecture: End-to-End Encryption (E2EE) Protocol

The security of Zora Vault is rooted in a client-side cryptographic protocol that ensures no unencrypted vault data is ever transmitted from the user's device. The server's role is intentionally limited to storing encrypted data blobs and facilitating authentication through a salted hash comparison.

The cryptographic flow is detailed below.

### **2.1. Master Key Generation and Derivation**

The entire security chain is bootstrapped from a user-provided Master Password, which is never stored or transmitted. A robust cryptographic key is derived from it using the following process on the client device.

1.  **Initial Inputs**:
    * **Password**: The user's Master Password.
    * **Salt**: The user's email address. Utilizing a unique, non-secret salt for each user mitigates pre-computation attacks, such as those employing rainbow tables.

2.  **Key Stretching**:
    To increase resilience against brute-force and dictionary attacks, a key stretching algorithm is employed to derive a high-entropy `Master Key`.
    * **Algorithm**: `PBKDF2-SHA256` (Password-Based Key Derivation Function 2 with HMAC-SHA256 as the pseudorandom function).
    * **Iteration Count**: A default of `100,000` iterations is enforced. This value is configurable by the user in advanced settings, with an upper bound of `600,000`, to balance security requirements with device performance.

3.  **Output: The 256-bit Master Key**
    The output is a 256-bit `Master Key` that exists only in the client's volatile memory (RAM) for the duration of an active session. It is the root of trust for all subsequent cryptographic operations.

### **2.2. Derivation of Specialized Keys**

Adhering to the cryptographic principle of key separation, the `Master Key` is not used directly for data encryption. Instead, it serves as input to a Key Derivation Function (KDF) to generate purpose-specific keys.

* **Derivation Algorithm**: `HKDF-SHA256` (HMAC-based Key Derivation Function). HKDF is used for its "extract-then-expand" paradigm, ensuring that the derived keys are cryptographically strong and independent.

From the `Master Key`, two distinct keys are derived:

1.  **Stretched Master Key (Symmetric Encryption Key)**: A 256-bit key used for the symmetric encryption and decryption of the user's local vault data and other sensitive keys.
2.  **Master Password Hash (Authentication Key)**: The `Master Key` is processed again through `PBKDF2-SHA256`, but with a fixed **single iteration**. This produces a secure hash used solely for authentication with the Zora Vault server. This design allows for rapid server-side verification without imposing the high computational cost of the full iteration count, thus mitigating Denial-of-Service (DoS) attack vectors against the authentication endpoint.

### **2.3. Asymmetric Cryptography for Secure Sharing**

For functionalities requiring secure data exchange between users (e.g., sharing credentials within an organization), an asymmetric cryptosystem is employed.

1.  **Key Pair Generation**: Upon account creation, the client generates a 2048-bit `RSA` key pair.
    * **Public Key**: Used to encrypt data intended for the user (specifically, symmetric keys for shared collections). This key is distributed to other users via the Zora Vault server.
    * **Private Key**: Kept confidential and used to decrypt data encrypted with the corresponding public key.

2.  **Private Key Protection**: The RSA private key is a highly sensitive asset. It is encrypted client-side using `AES-256` (in CBC mode with a random IV) with the `Stretched Master Key` before being transmitted to the server for storage. This results in a **Protected Private Key** blob. Consequently, the private key can only be decrypted and used by the client after the user provides their Master Password.

### **2.4. Vault Encryption**

The user's vault, which contains all records (logins, secure notes, etc.), is encrypted using a multi-layered symmetric encryption scheme.

1.  **Data Encryption Key (DEK)**: A unique 256-bit symmetric key (`Generated Symmetric Key` in the diagram) is randomly generated on the client. This key is used to encrypt all individual items within the vault via the `AES-256` algorithm. Each encryption operation uses a unique, randomly generated Initialization Vector (IV) to prevent cryptographic patterns.
2.  **Key Encryption Key (KEK)**: The Data Encryption Key (DEK) itself is then encrypted using the `Stretched Master Key` (which acts as the KEK). This process yields the **Protected Symmetric Key**.

### **2.5. Data Synchronization with the Cloud**

Upon completion of the client-side cryptographic operations, only the following artifacts are persisted on the Zora Vault servers:

* User's email address (identifier).
* The single-iteration `Master Password Hash` (for authentication).
* The `Protected Symmetric Key` (encrypted DEK).
* The `Protected Private Key` (encrypted RSA private key).
* The user's vault data, fully encrypted with the DEK.

This architecture guarantees that the Zora Vault service has zero knowledge of the user's Master Password and zero access to their decrypted vault contents.

---

## 3. Technical Features and Implementations

* **Multi-Platform Client Architecture**: Native and web-based clients for major operating systems (Windows, macOS, Linux, Android, iOS) and browser extensions, all implementing the same core cryptographic protocol.
* **Password Strength Auditing Engine**: A client-side module that analyzes vault data for weak, reused, or compromised passwords. It cross-references password hashes against public data breach corpuses (e.g., via a k-Anonymity API like Have I Been Pwned's) without exposing the passwords themselves.
* **Secure Sharing Protocol**: Implements an E2EE protocol for sharing vault items. When an item is shared, its symmetric key is encrypted using the recipient's public RSA key and transmitted to them. Only the recipient, using their protected private key, can decrypt the shared item's key.
* **Time-Based One-Time Password (TOTP) Authenticator**: The client can securely store TOTP seeds and generate 6-digit codes locally, functioning as a 2FA authenticator. The seeds are treated as sensitive data and are encrypted within the vault.
* **Emergency Access**: Implements a time-locked key exchange protocol. A designated emergency contact can request access, initiating a user-defined waiting period. If the user does not veto the request within this period, the contact is granted a key, encrypted with their own public key, allowing them to decrypt a read-only copy of the vault.

---

## 4. Passkey Support on Android 13 via the "Personal Bridge" Protocol

**Problem Statement**: Native Passkey (FIDO2/WebAuthn) credential provider support for third-party managers is only available on Android 14+. On Android 13 and below, this functionality is restricted to the Google Password Manager, creating a significant adoption barrier.

**Zora Vault's Solution: The Personal Bridge**

To provide a seamless Passkey experience on these legacy systems, Zora Vault implements a proprietary protocol named "Personal Bridge." This system forwards WebAuthn API requests from the Android 13 device to a more capable, user-owned device for processing.

**Technical Workflow:**

1.  **Request Interception**: The Zora Vault Autofill Service on Android 13 is designed to detect and intercept JavaScript `navigator.credentials.get()` or `create()` calls (WebAuthn API requests) initiated by the browser.
2.  **Secure Channel Establishment**: Upon interception, the app initiates a secure, E2EE channel to a secondary "bridge" device (e.g., a user's desktop with the Zora Vault browser extension or a phone running Android 14+). This channel is established over a local network via WebRTC (using a DTLS-SRTP handshake) or via Bluetooth LE using custom GATT services with an application-layer encryption protocol.
3.  **Challenge Forwarding**: The `challenge` and `relyingPartyId` from the WebAuthn request are serialized and transmitted through the secure channel to the bridge device.
4.  **Remote Authentication and Signing**: The Zora Vault client on the bridge device receives the challenge and invokes the platform's native Passkey APIs. The user authenticates on the bridge device (e.g., via biometrics). The device's authenticator signs the challenge using the stored Passkey private key.
5.  **Attestation Response Relay**: The signed client data JSON and authenticator data (the attestation) are sent back to the Android 13 device through the secure channel.
6.  **Response Injection**: The Zora Vault Autofill Service on the Android 13 device injects the received cryptographic response back into the browser's pending promise, successfully completing the Passkey authentication flow.

This mechanism, while complex internally, provides a transparent user experience and effectively backports modern authentication standards to unsupported operating systems.

---

## 5. Technical Roadmap and Objectives

### **Short-Term Objectives**

1.  **Third-Party Cryptographic Audit**: Commission and publish a full security assessment from a reputable firm (e.g., Trail of Bits, Cure53) to independently validate our E2EE implementation and identify potential vulnerabilities.
2.  **Client-Side Code Open-Sourcing**: Release the source code for all client applications under an appropriate open-source license (e.g., GPLv3) to foster community review and transparency.
3.  **Infrastructure Scaling**: Refactor backend infrastructure to support >10 million active users, ensuring high availability and low latency (<100ms for vault sync operations) via a globally distributed architecture.

---

## 6. Conclusion

Zora Vault is architected to provide uncompromising security through a rigorously implemented zero-knowledge and end-to-end encryption model. By handling all cryptographic operations on the client-side, the platform ensures that user data remains private and secure under all circumstances. Innovations like the Personal Bridge protocol demonstrate our commitment to pushing the boundaries of security accessibility. Our public roadmap and commitment to open-sourcing client code underscore our core belief in transparency as a prerequisite for trust in any security-focused product.