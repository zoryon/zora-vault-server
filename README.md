# Zora Vault

**Documentation**

**Version: 0.0.1**

**Last Updated: October 24, 2025**

---

## 1. Abstract

Zora Vault is a password management solution engineered on a foundational zero-knowledge security model. 

This architecture ensures that all sensitive user data is processed exclusively on the client-side, rendering it inaccessible to the Zora Vault service infrastructure or any unauthorized third party. 

This document provides a detailed technical specification of the cryptographic protocols, feature implementations, and the core security principles that govern the Zora Vault Server. 

Our objective is to transparently detail the mechanisms that guarantee encryption and user data sovereignty.

---

## 2. Technical Features

* **Zero Knowledge System**: All user's vault items are encrypted and decrypted exclusively on the client side. The server only stores encrypted blobs. This ensures that Zora Vault has zero knowledge of user data, besides email and few metadata
* **Emergency Logout**: A designated emergency endpoint which allows users to log out of one or more devices remotely (requires authentication).

---

## 3. List of all API Endpoints


---
