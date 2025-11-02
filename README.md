# Zora Vault Documentation

**Version:** 0.0.1

**Last Updated:** October 24, 2025

---

## 1. Introduction

Zora Vault is a password management solution built on a **zero-knowledge security model**. 🔐

This architecture guarantees that your sensitive data is **encrypted and decrypted exclusively on your device** (client-side). As a result, the Zora Vault server, our team, and any third parties have absolutely no access to the contents of your vault. We only store unintelligible encrypted data blobs.

This document provides a technical overview of the Zora Vault server, detailing its security principles, features, and API specifications. Our goal is to transparently demonstrate the mechanisms that protect your data and ensure your privacy.

---

## 2. Core Features

* **Zero-Knowledge System**: The server has no knowledge of your master password or the contents of your vault. All encryption and decryption operations happen locally on your device. The server only stores the encrypted data, your email, and some non-sensitive metadata.
* **Emergency Logout**: A dedicated API endpoint allows you to remotely log out of one or all of your devices, providing a critical security feature in case a device is lost or stolen.

---

## 3. API Reference

All API endpoints require a valid authentication token in the request header unless otherwise specified.

### Session Management

These endpoints handle user authentication, device verification, and session lifecycle.

* `POST /api/sessions/credentials`
    Authenticates a user's credentials (email and password hash) and returns a temporary token required to proceed with device verification.

* `POST /api/sessions/challenges`
    Issues a cryptographic challenge to a device. This challenge must be decrypted using the device's private key to prove its identity.

* `POST /api/sessions`
    Verifies the device's response to the challenge. If successful, it creates a new user session and returns access and refresh tokens.

* `POST /api/sessions/tokens/refresh`
    Issues a new access token using a valid, unexpired refresh token.

* `DELETE /api/users/me/sessions/{sessionId}`
    Revokes a specific user session (logs out a single device).

#### **Login Flow**
The authentication process is a three-step flow to ensure security:
1.  **Credentials**: The client sends user credentials to `/api/sessions/credentials`.
2.  **Challenge**: The client uses the temporary token to request a challenge from `/api/sessions/challenges`.
3.  **Verification**: The client solves the challenge and sends the response to `/api/sessions` to establish a session.

---

### User Management

These endpoints manage user accounts, profiles, and settings.

* `POST /api/users`
    Registers a new user account. This endpoint performs all necessary validations and securely hashes the master password before storage. Also sends a verification email.

* `GET /api/users/me`
    Retrieves the profile information for the currently authenticated user.

* `GET /api/users/email-verifications?token={token}`
    Marks the email of a user as verified.

* `PATCH /api/users/me`
    Updates specific fields for the current user, such as their username or the main encrypted vault blob.

* `PUT /api/users/me/settings`
    Updates or replaces the settings associated with the authenticated device.

* `DELETE /api/users/me`
    Deletes the current user's account. This is a destructive action and requires re-authentication to confirm.

---

### Vault Management

These endpoints handle the creation and management of individual vault items.

> **Important:** To maintain the zero-knowledge guarantee, all `EncryptedData` sent to these endpoints **must be encrypted on the client-side** before the API call is made.

* `GET /api/users/me/vault-items`
    Retrieves a list of all vault items for the authenticated user. Optionally accepts the query parameter ?deleted=true to include soft-deleted (trashed) items instead of active ones.

* `POST /api/users/me/vault-items`
    Creates a new vault item.

* `GET /api/users/me/vault-items/{itemId}`
    Retrieves a single vault item by its ID.

* `PATCH /api/users/me/vault-items/{vaultItemId}`
    Restores a soft-deleted vault item back to active status.    

* `PUT /api/users/me/vault-items/{itemId}`
    Updates an existing vault item by completely replacing its `EncryptedData` and `Type`.

* `DELETE /api/users/me/vault-items/{itemId}`
    Deletes a specific vault item by its ID.