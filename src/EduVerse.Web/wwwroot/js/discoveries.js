// Original illustrations of what each historical figure discovered or did (dossiers). Drawn as SVG, no external images.
(() => {
    "use strict";
    const INK = "currentColor";
    const svg = (body, label) => `<svg viewBox="0 0 320 200" class="w-full h-auto text-foreground" xmlns="http://www.w3.org/2000/svg" font-family="inherit" font-size="12" role="img" aria-label="${label || ""}">${body}</svg>`;
    const t = (x, y, s, extra = "") => `<text x="${x}" y="${y}" fill="${INK}" text-anchor="middle" dominant-baseline="middle" ${extra}>${s}</text>`;

    const art = {
        curie: () => svg(`
            <rect x="20" y="40" width="70" height="80" rx="8" fill="#e7f6ee" stroke="#2f6f4f" stroke-width="2"/>${t(55, 62, "84", 'font-size="11"')}${t(55, 88, "Po", 'font-size="28" font-weight="700" fill="#2f6f4f"')}${t(55, 110, "polonium", 'font-size="10"')}
            <rect x="230" y="40" width="70" height="80" rx="8" fill="#e7f6ee" stroke="#2f6f4f" stroke-width="2"/>${t(265, 62, "88", 'font-size="11"')}${t(265, 88, "Ra", 'font-size="28" font-weight="700" fill="#2f6f4f"')}${t(265, 110, "radium", 'font-size="10"')}
            <circle cx="160" cy="80" r="16" fill="#7be0a5"/><circle cx="160" cy="80" r="26" fill="none" stroke="#7be0a5" stroke-dasharray="3 5"/><circle cx="160" cy="80" r="38" fill="none" stroke="#7be0a5" stroke-opacity=".6" stroke-dasharray="3 6"/>
            <g stroke="#2f6f4f" stroke-width="2" fill="none"><path d="M160 112 q-6 20 -30 40"/><path d="M160 112 v44"/><path d="M160 112 q6 20 30 40"/></g>
            ${t(126, 166, "α")}${t(160, 168, "γ")}${t(194, 166, "β")}${t(160, 190, "1898", 'font-size="11" fill-opacity=".6"')}`),
        leonardo: () => svg(`
            <rect x="0" y="0" width="320" height="200" fill="#f7ecd6"/>
            <g stroke="#6b4a2a" stroke-width="2" fill="none" stroke-linecap="round">
              <path d="M160 110 L40 60 Q80 70 100 96 Q120 80 160 110 Z" fill="#efe0c2"/><path d="M160 110 L280 60 Q240 70 220 96 Q200 80 160 110 Z" fill="#efe0c2"/>
              <path d="M40 60 L160 110 M60 70 L150 106 M80 78 L140 102 M280 60 L160 110 M260 70 L170 106 M240 78 L180 102"/>
              <ellipse cx="160" cy="124" rx="34" ry="10"/><path d="M160 110 v-30 M150 84 h20"/><path d="M140 134 l-10 30 M180 134 l10 30 M126 164 h12 M182 164 h12"/>
            </g>
            <text x="300" y="186" fill="#6b4a2a" font-size="12" text-anchor="end" font-style="italic" transform="scale(-1 1) translate(-320 0)">ali e volo</text>`),
        newton: () => svg(`
            <rect x="0" y="0" width="320" height="200" fill="#1f2333"/>
            <path d="M0 96 L130 96" stroke="#fff" stroke-width="5"/>
            <path d="M130 60 L170 140 L90 140 Z" fill="#cfe3ff" fill-opacity=".35" stroke="#e8f0ff" stroke-width="2" transform="translate(20 -20)"/>
            ${["#ff4d4d", "#ff9f40", "#ffe14d", "#4ddf6a", "#4da6ff", "#6a5cff", "#b04dff"].map((c, i) => `<path d="M152 98 L310 ${40 + i * 18}" stroke="${c}" stroke-width="5"/>`).join("")}
            ${t(70, 82, "white light", 'fill="#fff" font-size="11"')}`),
        lovelace: () => svg(`
            <g stroke="#4b4a8f" stroke-width="2" fill="#dcdafa">
              <circle cx="80" cy="90" r="40"/><circle cx="80" cy="90" r="12" fill="#fff"/><circle cx="150" cy="70" r="26"/><circle cx="150" cy="70" r="8" fill="#fff"/><circle cx="130" cy="140" r="22"/><circle cx="130" cy="140" r="7" fill="#fff"/>
            </g>
            <g fill="#4b4a8f">${Array.from({ length: 12 }, (_, i) => { const a = (i * Math.PI) / 6; return `<rect x="${(80 + 44 * Math.cos(a) - 4).toFixed(1)}" y="${(90 + 44 * Math.sin(a) - 4).toFixed(1)}" width="8" height="8" transform="rotate(${i * 30} ${(80 + 44 * Math.cos(a)).toFixed(1)} ${(90 + 44 * Math.sin(a)).toFixed(1)})"/>`; }).join("")}</g>
            <g transform="translate(200 30)"><rect width="100" height="140" rx="6" fill="#f4ecd8" stroke="#8a5a34" stroke-width="2"/>
              ${Array.from({ length: 6 }, (_, r) => Array.from({ length: 4 }, (_, c) => (r * 7 + c * 3) % 5 < 2 ? `<circle cx="${20 + c * 20}" cy="${20 + r * 20}" r="5" fill="#8a5a34"/>` : "").join("")).join("")}
            </g>${t(250, 186, "Note G · B₇", 'font-size="11"')}`),
        galileo: () => svg(`
            <rect x="0" y="0" width="320" height="200" fill="#151a2e"/>
            ${[[30, "* *", "O", "*", "Jan 7"], [74, "", "O", "* * *", "Jan 8"], [118, "* *", "O", "", "Jan 10"], [162, "*", "O", "* * *", "Jan 13"]].map(([y, l, o, r, d]) =>
                `<text x="160" y="${y}" fill="#fff" text-anchor="end" font-size="18" dx="-14">${l}</text><circle cx="160" cy="${y - 5}" r="9" fill="#f2d7a6"/><text x="160" y="${y}" fill="#fff" font-size="18" dx="14">${r}</text><text x="20" y="${y}" fill="#9aa3c7" font-size="11">${d} 1610</text>`).join("")}
            <text x="300" y="190" fill="#9aa3c7" font-size="10" text-anchor="end">Jupiter · Sidereus Nuncius</text>`),
        darwin: () => svg(`
            ${[["#8b6b4a", 0, 18, 12], ["#6f8a4e", 1, 12, 8], ["#a36a3c", 2, 22, 16], ["#5c6f7a", 3, 8, 4]].map(([c, i, len, h]) => {
                const x = 40 + i * 76;
                return `<g transform="translate(${x} 40)"><circle cx="24" cy="30" r="22" fill="${c}"/><circle cx="32" cy="24" r="4" fill="#fff"/><circle cx="33" cy="24" r="2" fill="#222"/><path d="M44 26 l${len} ${h / 2} l-${len} ${h / 2} Z" fill="#f2b24a" stroke="#7a5420"/></g>`;
            }).join("")}
            <g stroke="${INK}" stroke-width="2" fill="none"><path d="M160 190 V160 M160 160 L80 120 M160 160 L240 120 M80 120 L60 100 M80 120 L120 100 M240 120 L200 100 M240 120 L270 100"/></g>
            ${t(160, 120, "I think", 'font-style="italic" font-size="13"')}`),
        cervantes: () => svg(`
            <rect x="0" y="0" width="320" height="200" fill="#f8edd6"/>
            <path d="M0 160 Q160 140 320 160 V200 H0 Z" fill="#d9c79c"/>
            <g transform="translate(210 40)"><path d="M20 120 L30 30 H50 L60 120 Z" fill="#fff" stroke="#6b4a2a" stroke-width="2"/><path d="M28 34 h24 l-12 -14 Z" fill="#8c3a2c"/>
              <g stroke="#6b4a2a" stroke-width="3" transform="rotate(20 40 40)"><path d="M40 40 L40 -10 M40 40 L90 40 M40 40 L40 90 M40 40 L-10 40"/></g>
              <g fill="#efe0c2" stroke="#6b4a2a" transform="rotate(20 40 40)"><rect x="34" y="-8" width="12" height="40"/><rect x="48" y="34" width="40" height="12"/><rect x="34" y="48" width="12" height="40"/><rect x="-8" y="34" width="40" height="12"/></g></g>
            <g transform="translate(40 110)" stroke="#3a2c24" stroke-width="3" fill="none" stroke-linecap="round"><path d="M10 40 l14 -24 l16 4 l8 -14 M48 6 l70 -40"/><circle cx="18" cy="44" r="6"/><circle cx="44" cy="44" r="6"/></g>
            ${t(90, 30, "1605", 'font-size="16" font-weight="700" fill="#8c3a2c"')}`),
        franklin: () => svg(`
            <rect x="10" y="20" width="150" height="160" rx="6" fill="#1b1b22"/>
            ${Array.from({ length: 9 }, (_, i) => { const d = (i - 4) * 14; return `<ellipse cx="${85 + d}" cy="${100 + d * 1.3}" rx="5" ry="3" fill="#e8e8f0" fill-opacity="${0.9 - Math.abs(i - 4) * 0.12}"/><ellipse cx="${85 - d}" cy="${100 + d * 1.3}" rx="5" ry="3" fill="#e8e8f0" fill-opacity="${0.9 - Math.abs(i - 4) * 0.12}"/>`; }).join("")}
            <ellipse cx="85" cy="34" rx="16" ry="5" fill="#e8e8f0"/><ellipse cx="85" cy="166" rx="16" ry="5" fill="#e8e8f0"/>
            <g fill="none" stroke-width="4" stroke-linecap="round"><path d="M200 20 q60 20 0 40 q-60 20 0 40 q60 20 0 40 q-60 20 0 40" stroke="#4a6fa5"/><path d="M260 20 q-60 20 0 40 q60 20 0 40 q-60 20 0 40 q60 20 0 40" stroke="#d9483b"/></g>
            <g stroke="#9a9ab0" stroke-width="2">${[30, 50, 70, 90, 110, 130, 150, 170].map((y) => `<path d="M212 ${y} h36"/>`).join("")}</g>
            ${t(85, 192, "Photo 51 · 1952", 'font-size="10"')}`),
        ibnbattuta: () => svg(`
            <rect x="0" y="0" width="320" height="200" rx="8" fill="#e3f0f6"/>
            <path d="M20 40 q40 -20 90 0 q30 30 0 60 q-20 50 -60 70 q-30 -40 -30 -130 Z M120 40 q60 -20 180 0 q10 50 -40 80 q-40 10 -60 -10 q-40 -10 -80 -70 Z" fill="#f3e6c8" stroke="#c9b48a"/>
            <path d="M44 62 Q90 70 132 84 Q170 100 214 104 Q250 92 286 70 M132 84 Q100 120 80 140" fill="none" stroke="#d9483b" stroke-width="2.5" stroke-dasharray="6 5"/>
            ${[[44, 62, "Tangier"], [132, 84, "Mecca"], [214, 104, "Delhi"], [286, 70, "China"], [80, 140, "Mali"]].map(([x, y, n]) => `<circle cx="${x}" cy="${y}" r="5" fill="#8a5a34"/>${t(x, y - 12, n, 'font-size="11" font-weight="600"')}`).join("")}
            ${t(160, 186, "1325 → 1354", 'font-size="11"')}`),
        smith: () => svg(`
            ${["〰", "│", "✂", "▲"].map((s, i) => `<g transform="translate(${16 + i * 64} 40)"><circle cx="26" cy="20" r="12" fill="#f2d2bc"/><path d="M8 56 q18 -26 36 0 Z" fill="#5a4a6b"/><rect x="4" y="64" width="44" height="30" rx="6" fill="#efe9f7" stroke="#5a4a6b"/>${t(26, 80, s, 'font-size="16"')}</g>${i < 3 ? `<path d="M${68 + i * 64} 104 h10" stroke="${INK}" stroke-width="2"/>` : ""}`).join("")}
            <g transform="translate(270 50)">${Array.from({ length: 30 }, (_, i) => `<path d="M${(i % 6) * 7} ${Math.floor(i / 6) * 14} v12" stroke="#a57a1c" stroke-width="2"/><circle cx="${(i % 6) * 7}" cy="${Math.floor(i / 6) * 14}" r="2" fill="#a57a1c"/>`).join("")}</g>
            ${t(80, 160, "1 → 20", 'font-size="14"')}${t(220, 160, "10 → 48 000", 'font-size="14" font-weight="700"')}`),
        noether: () => svg(`
            ${[["⟳", "rotation", "L", 50], ["→", "space", "p", 160], ["⏱", "time", "E", 270]].map(([s, n, q, x]) => `<g>
              <circle cx="${x}" cy="70" r="36" fill="#e6f1f0" stroke="#3d7a74" stroke-width="2"/>${t(x, 66, s, 'font-size="26"')}${t(x, 92, n, 'font-size="10"')}
              <path d="M${x} 110 v22" stroke="#3d7a74" stroke-width="2"/><path d="M${x - 5} 126 l5 8 l5 -8" fill="#3d7a74"/>
              <rect x="${x - 34}" y="140" width="68" height="34" rx="8" fill="#3d7a74"/>${t(x, 157, `${q} = const`, 'fill="#fff" font-size="12" font-weight="600"')}</g>`).join("")}`),
        champollion: () => svg(`
            <path d="M40 20 L280 20 L300 190 L20 190 Z" fill="#6c6a70"/>
            <rect x="52" y="30" width="216" height="40" fill="#7c7a80"/><g fill="none" stroke="#f2efe6" stroke-width="2" stroke-linecap="round">
              <circle cx="72" cy="50" r="7"/><circle cx="72" cy="50" r="1.5" fill="#f2efe6"/>
              <path d="M88 54 q4 -6 8 0 t8 0 t8 0"/>
              <path d="M124 58 l6 -14 l6 14 Z"/>
              <path d="M146 58 v-16 q6 2 4 8"/>
              <path d="M162 56 q8 -14 16 -6 l4 -4 M166 56 l-2 4 M174 56 l2 4"/>
              <path d="M222 44 h20 M222 50 l6 8 M236 50 l-6 8"/><path d="M228 42 v-2"/>
              <path d="M246 44 q6 -4 8 4 q-4 8 -8 0"/>
              <path d="M190 58 h14 M197 58 v-14 l-6 4 M197 44 l6 4"/>
            </g>
            <rect x="48" y="76" width="224" height="46" fill="#77757b"/>${Array.from({ length: 3 }, (_, i) => `<path d="M60 ${88 + i * 12} q40 -6 80 0 t80 0 t40 0" stroke="#e9e5da" fill="none"/>`).join("")}
            <rect x="44" y="128" width="232" height="54" fill="#727076"/>${t(160, 145, "ΠΤΟΛΕΜΑΙΟΣ", 'fill="#f2efe6" font-size="14" letter-spacing="2"')}${t(160, 166, "ΒΑΣΙΛΕΥΟΝΤΟΣ ΤΟΥ ΝΕΟΥ", 'fill="#e9e5da" font-size="10"')}
            <rect x="214" y="34" width="46" height="32" rx="16" fill="none" stroke="#f3e2b0" stroke-width="2.5"/>`),
    };

    function hydrate(root = document) {
        root.querySelectorAll("[data-discovery]").forEach((el) => {
            const f = art[el.dataset.discovery];
            if (f) el.innerHTML = f().replace('aria-label=""', `aria-label="${(el.dataset.label || "").replace(/"/g, "&quot;")}"`);
        });
    }

    window.Edu = window.Edu || {};
    window.Edu.discovery = (key) => (art[key] ? art[key]() : "");
    if (document.readyState !== "loading") hydrate(); else document.addEventListener("DOMContentLoaded", () => hydrate());
})();
