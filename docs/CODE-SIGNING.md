# Code-Signatur

Der Release-Workflow kann EXE und Setup auf zwei Arten signieren. Ist nichts eingerichtet,
wird unsigniert veröffentlicht. Der integrierte Updater akzeptiert ab der ersten signierten Version
nur noch Updates vom selben Herausgeber.

| Weg | Kosten | Gültig auf | Geeignet für |
|---|---|---|---|
| **A: Eigenes Zertifikat** | kostenlos | nur PCs, denen das Zertifikat per GPO/Intune bekannt gemacht wurde | interne Verteilung (empfohlen) |
| **B: Azure Artifact Signing** | 9,99 $/Monat | überall (öffentlich vertrauenswürdig) | Weitergabe an Kunden/Externe |

## A: Kostenlos – eigenes Zertifikat (interne Verteilung)

Auf Firmen-PCs, die das Zertifikat kennen, zeigt Windows „Netz, Technik, Büro GbR“ als Herausgeber und
der Updater prüft die Signatur. Auf fremden PCs gilt die Signatur als nicht vertrauenswürdig – dort
verhält sich das Setup wie unsigniert.

1. **Zertifikat erzeugen** (einmalig, auf deinem PC, in PowerShell im Repository-Ordner):
   ```powershell
   .\tools\new-signing-certificate.ps1 -OutputPath C:\Sicher\Codesigning
   ```
   Das Skript fragt nach einem Passwort und legt `NTB-PW-Codesigning.pfx` (geheim, gut sichern),
   `NTB-PW-Codesigning.pfx.base64.txt` und `NTB-PW-Codesigning.cer` (öffentlich) an. Gültigkeit: 5 Jahre.
2. **GitHub-Secrets** anlegen (Repository → Settings → Secrets and variables → Actions → *New repository secret*):
   - `SIGNING_PFX_BASE64` = Inhalt von `NTB-PW-Codesigning.pfx.base64.txt` (danach die Textdatei löschen)
   - `SIGNING_PFX_PASSWORD` = das gewählte Passwort
3. **Zertifikat an die Firmen-PCs verteilen** – die `.cer`-Datei in zwei Speicher:
   - *Gruppenrichtlinie:* Computerkonfiguration → Richtlinien → Windows-Einstellungen → Sicherheitseinstellungen →
     Richtlinien für öffentliche Schlüssel → **Vertrauenswürdige Stammzertifizierungsstellen** und
     **Vertrauenswürdige Herausgeber** → Importieren.
   - *Intune:* Geräte → Konfiguration → Profil „Vertrauenswürdiges Zertifikat“ (Speicher: Computer-Zertifikatspeicher – Stamm).
   - *Einzelner PC ohne Domäne* (PowerShell als Administrator):
     ```powershell
     Import-Certificate -FilePath .\NTB-PW-Codesigning.cer -CertStoreLocation Cert:\LocalMachine\Root
     Import-Certificate -FilePath .\NTB-PW-Codesigning.cer -CertStoreLocation Cert:\LocalMachine\TrustedPublisher
     ```
4. Beim nächsten Release wird automatisch signiert. Der Build prüft diesen Weg bei jedem Push bereits mit einem Test-Zertifikat.

**Wichtig:** Wird die PFX-Datei verloren oder ausgetauscht, akzeptieren bereits signierte Installationen
keine Updates mit dem neuen Zertifikat über den integrierten Updater – dann das Setup einmal manuell installieren.

## B: Azure Artifact Signing (öffentlich vertrauenswürdig)

Azure Artifact Signing (früher „Trusted Signing“) hat Vorrang vor Weg A, sobald die Variable
`ARTIFACT_SIGNING_ACCOUNT` gesetzt ist.

- Windows zeigt den geprüften Firmennamen als Herausgeber – auf jedem PC.
- **SmartScreen warnt anfangs trotzdem.** Das Vertrauen baut sich über Downloads und Zeit auf
  (laut Microsoft typischerweise mehrere Wochen). Es ist an die geprüfte Identität gebunden, nicht an ein einzelnes Zertifikat.

Kosten: Basic-Tarif **9,99 $ pro Monat** (5.000 Signaturen inklusive – pro Release werden 2 benötigt).

## B.1 Azure vorbereiten (einmalig, im Azure-Portal)

1. Azure-Abonnement auswählen oder anlegen.
2. Unter **Abonnements → Ressourcenanbieter** den Anbieter `Microsoft.CodeSigning` registrieren.
3. **Artifact Signing-Konto** erstellen:
   - Region: **West Europe** (Endpunkt `https://weu.codesigning.azure.net`)
   - Tarif: **Basic**
   - Name: z. B. `ntbpwsigning` (3–24 Zeichen, weltweit eindeutig)
4. Sich selbst die Rolle **Artifact Signing Identity Verifier** auf dem Konto zuweisen
   (Zugriffssteuerung (IAM) → Rollenzuweisung hinzufügen).
5. **Identitätsprüfung** anlegen: Konto → *Identity validations* → *New identity* → **Public** → **Organization**.
   - Rechtlich korrekter Firmenname, Anschrift, Website, primäre und sekundäre E-Mail.
   - Microsoft prüft die Organisation anhand öffentlicher Register bzw. eingereichter Nachweise.
     Für eine GbR ohne Handelsregistereintrag können zusätzliche Unterlagen angefordert werden
     (z. B. Gewerbeanmeldung, Gesellschaftsvertrag, Steuerbescheinigung). Die Prüfung dauert einige Werktage.
   - Der Name im Zertifikat ist immer der geprüfte Rechtsname und kann nicht frei gewählt werden.
6. Nach erfolgreicher Prüfung ein **Zertifikatprofil** anlegen: Konto → *Certificate profiles* → *Create* → **Public Trust**,
   Name z. B. `ntbpwpublic`, die Identitätsprüfung auswählen.

## B.2 Zugriff für GitHub einrichten (OpenID Connect, ohne Passwort)

1. In **Microsoft Entra ID → App-Registrierungen** eine neue App anlegen, z. B. `ntbpw-github-signing`.
   Notieren: **Anwendungs-ID (Client-ID)** und **Verzeichnis-ID (Mandanten-ID)**.
2. In der App unter **Zertifikate & Geheimnisse → Verbundanmeldeinformationen** hinzufügen:
   - Szenario: **GitHub Actions deploying Azure resources**
   - Organisation: `tarek-muna`, Repository: `ntb-pw-pdf-watcher`
   - Entitätstyp: **Environment**, Name: `release`
3. Auf dem **Zertifikatprofil** der App die Rolle **Artifact Signing Certificate Profile Signer** zuweisen
   (in älteren Oberflächen „Trusted Signing Certificate Profile Signer“).

## B.3 GitHub konfigurieren

Im Repository unter **Settings → Secrets and variables → Actions**:

| Art | Name | Wert |
|---|---|---|
| Secret | `AZURE_CLIENT_ID` | Client-ID der App-Registrierung |
| Secret | `AZURE_TENANT_ID` | Mandanten-ID |
| Secret | `AZURE_SUBSCRIPTION_ID` | ID des Azure-Abonnements |
| Variable | `ARTIFACT_SIGNING_ENDPOINT` | `https://weu.codesigning.azure.net/` |
| Variable | `ARTIFACT_SIGNING_ACCOUNT` | Name des Signing-Kontos, z. B. `ntbpwsigning` |
| Variable | `ARTIFACT_SIGNING_PROFILE` | Name des Zertifikatprofils, z. B. `ntbpwpublic` |

Die Umgebung `release` wird beim ersten Release-Lauf automatisch angelegt. Optional kann man sie unter
**Settings → Environments** vorab erstellen und z. B. auf den Branch `main` bzw. Tags `v*` beschränken.

## Prüfen

Beim nächsten Release (`RELEASE.cmd`) signiert der Workflow:

1. `NTB-PW PDF-Watcher.exe` vor dem Verpacken,
2. `NTB-PW-PDF-Watcher-Setup-<Version>.exe` nach dem Verpacken,

und bricht ab, wenn eine Signatur ungültig ist. Lokal prüfen:

```powershell
Get-AuthenticodeSignature .\NTB-PW-PDF-Watcher-Setup-<Version>.exe | Format-List Status, SignerCertificate
```

## Hinweise

- Die Deinstallationsdatei `unins000.exe` von Inno Setup wird derzeit nicht signiert.
- Zertifikate von Artifact Signing sind nur wenige Tage gültig; durch den Zeitstempel bleiben die Signaturen dauerhaft gültig.
- Fällt Artifact Signing aus oder wird das Konto gekündigt, die Variable `ARTIFACT_SIGNING_ACCOUNT` löschen –
  dann werden Releases wieder unsigniert erstellt. Bereits signierte Installationen akzeptieren dann aber keine
  unsignierten Updates mehr über den integrierten Updater (Setup manuell installieren).
