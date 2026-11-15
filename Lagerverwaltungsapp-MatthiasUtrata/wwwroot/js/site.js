// Lucide-Icons ersetzen alle <i data-lucide="name"></i> durch das passende SVG.
﻿// Die Strichstärke ist 1.5 wie im Design-System, die Größe kommt aus site.css (.lucide).
lucide.createIcons({ attrs: { 'stroke-width': 1.5 } });

// Eine Auswahlliste mit data-auto-absenden schickt das Formular sofort bei Änderung ab.
document.querySelectorAll('[data-auto-absenden]').forEach(function (auswahl) {
    auswahl.addEventListener('change', function () {
        auswahl.form.submit();
    });
});

// Ein Formular mit data-bestaetigen (z. B. Ausleihe ablehnen oder zurückziehen) wird erst nach einer Rückfrage abgeschickt.
document.querySelectorAll('form[data-bestaetigen]').forEach(function (formular) {
    formular.addEventListener('submit', function (e) {
        if (!window.confirm(formular.dataset.bestaetigen)) {
            e.preventDefault();
        }
    });
});

// Ein POST-Formular wird nur einmal abgeschickt, damit ein Doppelklick nicht doppelt bucht.
// Der Handler läuft nach denen am Formular, ein abgebrochenes Absenden (Validierung, Rückfrage) lässt das Formular frei.
// Die Knöpfe bleiben aktiv, sonst fehlt der Wert des gedrückten Knopfs.
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

// Kommt man mit "Zurück" auf eine Seite aus dem Cache des Browsers, sind die Formulare wieder frei.
window.addEventListener('pageshow', function (e) {
    if (e.persisted) {
        document.querySelectorAll('form[data-abgeschickt]').forEach(function (formular) {
            delete formular.dataset.abgeschickt;
        });
    }
});

// Ein Klick in eine Tabellenzeile mit data-href öffnet den Link (Links und Buttons in der Zeile gehen vor).
document.querySelectorAll('tr[data-href]').forEach(function (zeile) {
    zeile.addEventListener('click', function (e) {
        if (!e.target.closest('a, button, input, select, label')) {
            window.location.href = zeile.dataset.href;
        }
    });
});

// Der Schalter in der Seitenleiste (data-farbmodus-schalter) wechselt zwischen hell und dunkel, der Browser merkt sich die Wahl.
// Den Modus setzt beim Laden schon das Skript im <head> von _Layout.cshtml, hier wird nur umgeschaltet.
var farbmodusSchalter = document.querySelectorAll('[data-farbmodus-schalter]');

// Für Screenreader wird aria-checked an allen Schaltern passend zum Modus gesetzt (im Profil gibt es mobil einen zweiten).
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
            // Ohne Zugriff auf den Speicher gilt der Modus nur bis zum nächsten Seitenaufruf.
        }
        farbmodusAnzeigen();
    });
});

farbmodusAnzeigen();
