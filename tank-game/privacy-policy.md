# Privacy Policy

**Last updated:** November 7, 2026
**Game:** T.A.N.K.S.
**Developer:** BEKZATKAZ
**Contact:** bekzatkaz.games@gmail.com

## 1. Introduction

BEKZATKAZ ("we", "us", or "our") is responsible for handling player information in T.A.N.K.S. (the "Game") as described here. The Game uses Unity services for accounts, multiplayer, lobby text chat, leaderboards, content delivery, and diagnostics. It also uses GitHub to host this policy and country.is to determine a country for leaderboard flags.

Before sign-in, the Game retrieves this policy from GitHub. You can read it and quit without signing in. This check contacts GitHub before agreement. The agreement screen blocks Game sign-in until the current policy is accepted; it does not prevent the policy request, engine diagnostics, or startup content requests.

## 2. Information We Collect

- **Accounts:** Unity Authentication creates a player identifier and manages sign-in credentials and your chosen display name. Anonymous sign-in still creates a persistent service account; it does not make associated scores or other player information anonymous. Local authentication credentials can restore your account on later launches. Choose a display name that does not reveal your real name, contact details, or other sensitive information.
- **Multiplayer:** Unity Lobby and Relay process player identifiers, IP addresses, lobby membership and settings, and connection information. Other players see your display name and relevant lobby information. Gameplay actions and state are transmitted to synchronize sessions.
- **Lobby chat:** Unity Lobby stores each member's latest message and message identifier in member-visible player data. Sending a message replaces that value. Connected players display new messages and keep a transcript in memory. Joining players receive current lobby member data, including the latest messages, although the chat screen does not display those earlier messages as history. Messages are accessible to lobby participants. Avoid sharing sensitive information in chat. The Game does not capture microphone audio.
- **Leaderboards:** At the end of a completed match, the Game automatically attempts to submit your elimination and item pickup counts to Unity Leaderboards, associated with your player identifier, display name, and country code. Counts contribute to total, weekly, and daily rankings. Unity also processes IP addresses to operate this service. Other players can view your display name, country flag, score, and rank. Leaderboard results delivered to their Game clients also include your player identifier. Submissions happen even if you never open the leaderboard screen.
- **Country lookup:** When loading leaderboards or submitting match results, the Game requests your IP-based country from `https://api.country.is/`. Country.is and the infrastructure handling that request receive your public IP address and connection information. The Game does not attach your player identifier, display name, or scores to this request. It reads only the returned country code and does not request GPS or precise coordinates. A successful country code is cached in memory for the running Game and sent to Unity with leaderboard scores. Failed lookups may be retried and otherwise use an unknown country. IP-based country estimates can be inaccurate, including when using a VPN.
- **Content delivery:** The Game is configured to download content catalogs and game assets through Unity Cloud Content Delivery and its delivery network. Content requests expose your IP address and request information to the providers delivering the files. Unity states that Cloud Content Delivery irreversibly hashes IP addresses upon ingestion and does not retain them in an identifiable form in that service.
- **Diagnostics:** Unity engine diagnostics are enabled in the current project settings. Technical reports may contain device, operating system, installation identifier, and error information, depending on collection settings. Legacy Cloud Diagnostics crash reporting is disabled in this configuration. These are different Unity diagnostic features.
- **Policy requests:** Every entry launch and retry downloads this document over HTTPS from raw.githubusercontent.com. GitHub and its delivery providers receive your IP address and connection/request information. The Game does not attach Unity credentials, player IDs, or messages to this request.
- **Local data:** The Game saves settings such as language, graphics, and audio preferences locally. It also saves a fingerprint of the accepted policy in local preferences to recognize the accepted version. That acceptance record is per installation and is not uploaded as an account-level consent record. Authentication credentials and downloaded content may also be cached locally. Clearing local data may remove these records.
- **Correspondence:** Privacy requests involve your email address, request details, and information needed for identity verification and a response.

We do not use Unity Analytics or advertising services in the current Game. Engine diagnostics are separate from Unity Analytics.

**Planned features:** Unity Cloud Code (backend game logic) and Unity Cloud Save (persistent progress or settings) are planned, not active player-data flows in the current implementation. We will update this policy with the actual data, purposes, retention, and choices before activating them. Neither service currently stores your policy agreement.

## 3. How We Use Information

We use information to operate accounts and profiles, connect multiplayer sessions, synchronize gameplay, deliver lobby chat and game content, calculate and display rankings and country flags, troubleshoot errors, protect the services, check policy acceptance, and answer privacy requests.

Where a legal basis is required, we rely on performance of requested services for necessary account, multiplayer, and content delivery processing; legitimate interests in operating gameplay rankings and country indicators, reliability, security, and communicating policy changes, subject to your rights and interests; and applicable legal obligations. Where processing requires consent, that consent must meet the applicable requirements. Accepting this policy acknowledges this notice; it does not waive privacy rights or replace any separate consent required by law.

## 4. Third-Party Services

Current providers are Unity (Authentication, Lobby, Relay, Leaderboards, Cloud Content Delivery, and enabled engine diagnostics), GitHub (policy hosting), and country.is (IP-based country lookup). Their infrastructure providers also handle the connections needed to deliver these services. Netcode for GameObjects synchronizes networked gameplay.

Official references:

- [Unity Game Player and App User Privacy Policy](https://unity.com/legal/game-player-and-app-user-privacy-policy)
- [Unity Authentication privacy overview](https://docs.unity.com/en-us/authentication/privacy-and-consent/overview)
- [Unity Leaderboards privacy overview](https://docs.unity.com/en-us/leaderboards/privacy-and-consent/overview)
- [Unity Cloud Content Delivery privacy overview](https://docs.unity.com/en-us/ccd/privacy-and-consent/overview)
- [Unity Diagnostics privacy and consent](https://docs.unity.com/en-us/cloud/developer-data/privacy-and-consent)
- [GitHub Privacy Statement](https://docs.github.com/en/site-policy/privacy-policies/github-general-privacy-statement)
- [Country.is service information](https://country.is/)

Provider policies supplement this notice and do not replace our responsibilities for the Game.

## 5. Data Sharing

We do not sell personal information or use it for targeted advertising. Providers process information for the features described above. Other players receive multiplayer and chat information and can access leaderboard entries, including entries from players outside their lobby. They may independently retain this information, for example in screenshots. Information may also be disclosed when legally required or necessary to protect players and service security.

## 6. Data Retention

Account information is retained by Unity Authentication until deleted through the service's account deletion procedures. Uninstalling the Game does not request that deletion. Multiplayer connection data and service logs follow the relevant provider settings and retention requirements. Ordinary gameplay state is used during sessions; submitted leaderboard scores persist separately.

Unity Leaderboards retains entries until deleted or reset; its default retention has no fixed expiry. If a reset archives a leaderboard, archived entries can remain indefinitely until deleted. Daily and weekly labels describe scoring periods and do not guarantee deletion after a day or a week. Total scores can persist across sessions. Display names and country codes stored with scores can remain with those entries, including older values, until updated or deleted.

Each member's latest message remains in lobby player data until replaced or removed with the player or lobby. The Game does not request a separate server-side chat archive. Its received transcript is held in memory and reset when the lobby changes; the Game does not save that transcript to a chat history file. Provider logs and backups follow Unity's applicable retention practices.

Unity documents a default retention period of 90 days for Diagnostics personal data. Cloud Content Delivery does not retain IP addresses in an identifiable form in that service. GitHub retains request information under its privacy practices. Country.is states that its API does not log requests; the Game's cached country code lasts for the running Game, while country codes submitted with scores follow leaderboard retention.

Local settings and cached data remain until overwritten or removed. The local policy fingerprint remains until replaced by a later acceptance or removed with local preferences. Privacy correspondence is kept as needed to resolve requests and satisfy applicable legal requirements. Cloud Code and Cloud Save are planned and do not currently retain player information for the Game.

## 7. Data Security

We use reasonable measures intended to protect information, including HTTPS policy downloads and provider protections. No electronic storage or transmission method is completely secure.

## 8. Your Privacy Rights

Depending on your location, you may have rights to access, correct, delete, or obtain a portable copy of information; object to or restrict processing; withdraw consent where applicable; and complain to your local data protection authority. Withdrawal does not affect earlier lawful processing.

Contact **bekzatkaz.games@gmail.com** to exercise these rights, including to request access to, correction of, or deletion of leaderboard entries and associated display names and country codes. Include your player identifier if available and relevant details, but never send passwords or authentication tokens. If you do not know your identifier, contact us for help identifying the relevant records. We may need reasonable identity verification and will respond within applicable legal time limits.

We handle requests for the Game and coordinate with relevant providers where needed. Leaderboard requests must address the relevant boards and any retained archives; deleting an Authentication account alone does not automatically delete leaderboard, lobby, or diagnostic information. Legal retention exceptions may apply. Copies independently retained by other players may remain outside our control.

You can decline this policy by quitting before sign-in. The current Game has no separate in-game switch for leaderboard submission or country lookup. Closing or avoiding the leaderboard screen does not stop submissions after completed matches. You can stop using the Game to avoid further Game-initiated submissions and contact us to object to processing or request deletion. Where processing is based on consent, you can withdraw that consent by contacting us.

Removing local acceptance causes a new prompt but does not delete server-side data. Uninstalling or clearing local data does not delete an account or its scores and can prevent recovery of an anonymous account if its credentials are lost.

## 9. Children's Privacy

The Game is not specifically directed at children under 13. Minimum ages and parental permission requirements vary by location. The agreement checkbox is not age verification or verified parental consent. Where legally required, a child's data must not be submitted without those safeguards.

Contact us if you believe a child's information was collected without required permission. We will investigate and take appropriate steps, including coordinating deletion where required.

## 10. International Data Processing

Unity, GitHub, country.is, and their infrastructure providers may process information outside your country, where privacy laws may differ. The linked provider notices explain their practices and available transfer safeguards. For processing on our behalf, applicable legal safeguards are required where international transfer rules apply; you can contact us for information about relevant safeguards.

## 11. Changes to This Privacy Policy

Updates are published here with a revised date. Before initializing Unity Gaming Services or signing in, the Game checks the hosted policy at each entry launch. Changed contents require agreement again on that installation, including for returning players. An unchanged, already accepted policy proceeds to sign-in. If downloading or validating the policy fails, sign-in is blocked and you can retry or quit. Internet access is required.

We provide additional notices or obtain specific consent where legally required before introducing new processing.

## 12. Contact Us

**Developer:** BEKZATKAZ
**Email:** bekzatkaz.games@gmail.com
