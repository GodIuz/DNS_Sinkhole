# Socket & Script - Security Hub (DNS Sinkhole & Dashboard)

Το **Socket & Script - Security Hub** είναι μια self-hosted, full-stack εφαρμογή ασφαλείας και ιδιωτικότητας δικτύου. Συνδυάζει έναν custom **DNS Sinkhole Server** γραμμένο σε C#, ένα **ASP.NET Core Web API** για τη διαχείριση και μετάδοση στατιστικών σε πραγματικό χρόνο, και ένα σύγχρονο **Angular Dashboard** για την οπτικοποίηση της δικτυακής κίνησης και των αναχαιτίσεων.

Επιπλέον, ενσωματώνει έναν advanced CLI Browser αυτοματοποιημένο μέσω **Playwright** με υποστήριξη για Burner Mode (RAM-only) και Ad-blocking σε επίπεδο browser.

---

## 🚀 Χαρακτηριστικά (Features)

- **Custom DNS Sinkhole:** Ακούει στην πόρτα 53, αναλύει τα πακέτα και μπλοκάρει trackers και διαφημίσεις επιστρέφοντας IP `0.0.0.0` (Sinkholing) βασισμένο σε αυτοματοποιημένες blocklists.
- **DNS-over-HTTPS (DoH):** Προωθεί τα ασφαλή αιτήματα στην Cloudflare (`1.0.0.1`) μέσω κρυπτογραφημένου HTTPS, παρακάμπτοντας τοπικούς περιορισμούς του router.
- **ASP.NET Core Web API:** Background server που εκθέτει real-time endpoints (`/api/stats`) για την τροφοδοσία του frontend.
- **Angular 17+ Dashboard:** Μοντέρνο dark-themed UI με χρήση **Signals** και **Reactive Effects** για live updates χωρίς περιττά re-renders.
- **Δυναμικά Γραφήματα:** Ενσωμάτωση του **Chart.js** (Donut Chart) για την οπτική απεικόνιση των καθαρών έναντι των μπλοκαρισμένων αιτημάτων.
- **Playwright CLI Browser:**
  - *Mode 1 (Persistent Profile):* Καθημερινός ad-free browser.
  - *Mode 2 (Burner Mode):* Απόλυτη ανωνυμία, τρέχει αποκλειστικά στη RAM με μηδενικό αποτύπωμα στον δίσκο.
- **Windows Desktop Notifications:** Άμεση ενημέρωση μέσω native Windows Toasts όταν εντοπίζεται και μπλοκάρεται μια κρίσιμη απειλή.

---

## 🛠️ Τεχνολογικό Stack (Tech Stack)

### Backend (C# / .NET)
- .NET 8.0 / Windows Target SDK
- ASP.NET Core (Minimal APIs & CORS Policy)
- Playwright .NET (Browser Automation)
- DNS (Open-source DNS protocol library)
- Microsoft Toolkit Uwp Notifications

### Frontend (Angular)
- Angular 17+ (Standalone Components, Signals, New Control Flow `@for`)
- Chart.js / NgCharts (Data Visualization)
- HttpClient (RxJS Polling / API Integration)

---

## 📦 Δομή Project (Project Structure)

```text
├── DNS_Sinkhole/                  # C# Backend & CLI App
│   ├── Program.cs                 # ASP.NET Core & CLI Menu Setup
│   ├── SinkholeResolver.cs        # DNS Filtering Logic
│   ├── DohRequestResolver.cs      # Cloudflare DoH Proxy
│   ├── StatsStore.cs              # Thread-safe RAM Logging
│   └── DNS_Sinkhole.csproj
│
└── socket-script-dashboard/       # Angular Frontend


    ├── src/
    │   ├── app/
    │   │   ├── app.ts             # Reactive Signals & Chart Logic
    │   │   ├── app.html           # Dashboard Layout
    │   │   └── app.css            # Dark Theme UI Styles
    └── package.json
