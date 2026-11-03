// Lucide-Icons: ersetzt alle <i data-lucide="name"></i> durch das passende SVG.
// Strichstärke 1.5 wie im Design-System, die Größe kommt aus site.css (.lucide)
lucide.createIcons({ attrs: { 'stroke-width': 1.5 } });

// Filter als Auswahlliste (data-auto-absenden): schickt das Formular sofort ab, wenn sich die Auswahl ändert
document.querySelectorAll('[data-auto-absenden]').forEach(function (auswahl) {
    auswahl.addEventListener('change', function () {
        auswahl.form.submit();
    });
});

// Formular mit data-bestaetigen (z. B. Ausleihe ablehnen oder zurückziehen): erst nach einer Rückfrage abschicken
document.querySelectorAll('form[data-bestaetigen]').forEach(function (formular) {
    formular.addEventListener('submit', function (e) {
        if (!window.confirm(formular.dataset.bestaetigen)) {
            e.preventDefault();
        }
    });
});

// Ein Formular mit method="post" nur einmal abschicken, sonst bucht z. B. ein Doppelklick auf "Einbuchen" zweimal.
// Der Handler hängt am document und läuft daher nach denen am Formular: Haben Validierung oder Rückfrage das Absenden abgebrochen,
// bleibt das Formular frei. Die Knöpfe werden nicht deaktiviert, sonst fehlte der Wert des gedrückten Knopfs im Formular
document.addEventListener('submit', function (e) {
    var formular = e.target;
    if (e.defaultPrevented || formular.method !== 'post') {
        return;
    }
    if (formular.dataset.abgeschickt) {
        e.preventDefault();
        return;
    }
    formular.dataset.abgeschickt = 'true';
});

// Kommt man mit "Zurück" auf eine Seite aus dem Cache des Browsers, sind die Formulare wieder frei
window.addEventListener('pageshow', function (e) {
    if (e.persisted) {
        document.querySelectorAll('form[data-abgeschickt]').forEach(function (formular) {
            delete formular.dataset.abgeschickt;
        });
    }
});

// Tabellenzeile mit data-href: ein Klick irgendwo in der Zeile öffnet den Link (Links und Buttons in der Zeile gehen vor)
document.querySelectorAll('tr[data-href]').forEach(function (zeile) {
    zeile.addEventListener('click', function (e) {
        if (!e.target.closest('a, button, input, select, label')) {
            window.location.href = zeile.dataset.href;
        }
    });
});

// Dark Mode: der Schalter in der Seitenleiste (data-farbmodus-schalter) wechselt zwischen hell und dunkel und merkt sich die Wahl im Browser.
// Beim Laden setzt schon das Skript im <head> von _Layout.cshtml den Modus; hier nur noch das Umschalten
var farbmodusSchalter = document.querySelectorAll('[data-farbmodus-schalter]');

// Für Screenreader: aria-checked an allen Schaltern (im Profil steht er mobil ein zweites Mal) passend zum Modus setzen
function farbmodusAnzeigen() {
    var dunkel = document.documentElement.getAttribute('data-bs-theme') === 'dark';
    farbmodusSchalter.forEach(function (schalter) {
        schalter.setAttribute('aria-checked', String(dunkel));
    });
}

farbmodusSchalter.forEach(function (schalter) {
    schalter.addEventListener('click', function () {
        var modus = 'dark';
        if (document.documentElement.getAttribute('data-bs-theme') === 'dark') {
            modus = 'light';
        }
        document.documentElement.setAttribute('data-bs-theme', modus);
        try {
            localStorage.setItem('farbmodus', modus);
        } catch (e) {
            // Ohne Zugriff auf den Speicher gilt der Modus nur bis zum nächsten Seitenaufruf
        }
        farbmodusAnzeigen();
    });
});

farbmodusAnzeigen();
