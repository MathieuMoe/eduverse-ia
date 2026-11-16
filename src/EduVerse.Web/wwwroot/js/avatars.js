// Illustrated historical figures (original drawings, not portraits): SVG with three mouth shapes and blinking eyes.
// The container gets data-mouth="closed|mid|open" while the figure speaks; the CSS in app.css shows the right mouth.
(() => {
    "use strict";
    const SKIN = { curie: "#f3d6c1", leonardo: "#e9c3a3", newton: "#f1d0b7", lovelace: "#f6dccb", galileo: "#ecc7a8", darwin: "#efcdb3", cervantes: "#e8c09f", franklin: "#f4d8c4", ibnbattuta: "#c99a72", smith: "#f2d2bc", noether: "#f1d4bf", champollion: "#f0cfb5" };

    const mouths = (x, y, lip = "#a4505a") => `
        <g class="av-mouth av-mouth-closed"><path d="M${x - 9} ${y} q9 5 18 0" fill="none" stroke="${lip}" stroke-width="2.6" stroke-linecap="round"/></g>
        <g class="av-mouth av-mouth-mid"><ellipse cx="${x}" cy="${y + 1}" rx="8" ry="4" fill="#5a2430"/><path d="M${x - 8} ${y} q8 -3 16 0" fill="none" stroke="${lip}" stroke-width="2"/></g>
        <g class="av-mouth av-mouth-open"><ellipse cx="${x}" cy="${y + 2}" rx="9" ry="7.5" fill="#5a2430"/><ellipse cx="${x}" cy="${y + 6}" rx="5" ry="3" fill="#d8707c"/></g>`;

    const eyes = (y, color = "#2c2230") => `
        <g class="av-eyes">
            <ellipse cx="82" cy="${y}" rx="5.5" ry="6.5" fill="#fff"/><ellipse cx="118" cy="${y}" rx="5.5" ry="6.5" fill="#fff"/>
            <circle class="av-pupil" cx="83" cy="${y + 1}" r="3.6" fill="${color}"/><circle class="av-pupil" cx="119" cy="${y + 1}" r="3.6" fill="${color}"/>
            <circle cx="84.5" cy="${y - 0.5}" r="1.1" fill="#fff"/><circle cx="120.5" cy="${y - 0.5}" r="1.1" fill="#fff"/>
        </g>`;

    const face = (key, extra = "") => `
        <ellipse cx="62" cy="112" rx="8" ry="12" fill="${SKIN[key]}"/><ellipse cx="138" cy="112" rx="8" ry="12" fill="${SKIN[key]}"/>
        <ellipse cx="100" cy="108" rx="38" ry="46" fill="${SKIN[key]}"/>
        <ellipse cx="78" cy="126" rx="7" ry="4" fill="#e88c8c" opacity=".35"/><ellipse cx="122" cy="126" rx="7" ry="4" fill="#e88c8c" opacity=".35"/>
        <path d="M100 108 q-4 12 0 15 q3 1 5 -1" fill="none" stroke="#b07a62" stroke-width="2" stroke-linecap="round"/>${extra}`;

    const art = {
        curie: () => `
            <circle cx="100" cy="110" r="100" fill="#dfeee4"/>
            <path d="M40 220 q4 -52 60 -60 q56 8 60 60 Z" fill="#2d2a33"/>
            <path d="M88 160 h24 v12 q-12 8 -24 0 Z" fill="#2d2a33"/><rect x="90" y="146" width="20" height="16" fill="${SKIN.curie}"/>
            <path d="M88 168 q12 10 24 0" fill="none" stroke="#e8e2ea" stroke-width="2"/>
            <circle cx="100" cy="48" r="20" fill="#6b5446"/>
            ${face("curie")}
            <path d="M62 104 q-2 -50 38 -52 q40 2 38 52 q-6 -26 -38 -30 q-32 4 -38 30 Z" fill="#7a6152"/>
            <path d="M100 56 v20" stroke="#5e4a3e" stroke-width="2"/>
            ${eyes(104)}
            <path d="M74 92 q8 -5 16 -1 M110 91 q8 -4 16 1" fill="none" stroke="#5e4a3e" stroke-width="2.4" stroke-linecap="round"/>
            ${mouths(100, 133)}
            <g transform="translate(150 150)"><path d="M6 0 h12 v14 l12 24 q2 6 -5 6 h-26 q-7 0 -5 -6 l12 -24 Z" fill="#fff" fill-opacity=".85" stroke="#4a6a58" stroke-width="2"/><path d="M2 34 h28 l2 4 q1 4 -4 4 h-24 q-5 0 -4 -4 Z" fill="#7be0a5"/></g>`,
        leonardo: () => `
            <circle cx="100" cy="110" r="100" fill="#f6e7d6"/>
            <path d="M36 220 q4 -54 64 -62 q60 8 64 62 Z" fill="#7d2e2b"/>
            <path d="M58 100 q-12 60 10 82 q-6 -40 4 -70 Z M142 100 q12 60 -10 82 q6 -40 -4 -70 Z" fill="#d9d4cc"/>
            ${face("leonardo")}
            <path d="M64 124 q2 52 36 58 q34 -6 36 -58 q-8 18 -18 20 q-8 -6 -18 -6 q-10 0 -18 6 q-10 -2 -18 -20 Z" fill="#e2ddd5"/>
            <path d="M80 152 q20 30 40 0 M86 160 q14 24 28 0" fill="none" stroke="#c9c3ba" stroke-width="1.6"/>
            <path d="M84 131 q16 -7 32 0 q-16 3 -32 0 Z" fill="#bdb6ac"/>
            <path d="M58 92 q0 -40 42 -42 q42 2 42 42 q-10 -16 -42 -18 q-32 2 -42 18 Z" fill="#d9d4cc"/>
            <path d="M52 78 q48 -44 96 0 q-48 -10 -96 0 Z" fill="#8c3a2c"/><ellipse cx="100" cy="66" rx="40" ry="16" fill="#9c4433"/>
            ${eyes(104, "#3a4a5c")}
            <path d="M72 92 q9 -6 18 -2 M110 90 q9 -4 18 2" fill="none" stroke="#bdb6ac" stroke-width="3" stroke-linecap="round"/>
            ${mouths(100, 141, "#9a4f4f")}
            <g transform="translate(150 152)" fill="none" stroke="#8c5a2c" stroke-width="2.2"><circle cx="14" cy="14" r="13"/><path d="M14 1 v26 M1 14 h26 M5 5 l18 18 M23 5 l-18 18"/></g>`,
        newton: () => `
            <circle cx="100" cy="110" r="100" fill="#e3ebf6"/>
            <path d="M36 220 q4 -54 64 -62 q60 8 64 62 Z" fill="#6b4b33"/>
            <path d="M90 160 h20 l-10 22 Z" fill="#fff"/><path d="M86 166 q14 14 28 0 l-4 18 q-10 6 -20 0 Z" fill="#f4f1ea"/>
            <g fill="#cdc8c0"><circle cx="58" cy="120" r="14"/><circle cx="54" cy="142" r="14"/><circle cx="60" cy="162" r="13"/><circle cx="142" cy="120" r="14"/><circle cx="146" cy="142" r="14"/><circle cx="140" cy="162" r="13"/></g>
            ${face("newton")}
            <path d="M58 104 q-6 -56 42 -58 q48 2 42 58 q-8 -30 -42 -32 q-34 2 -42 32 Z" fill="#d8d3cb"/>
            <g fill="#e4e0d9"><circle cx="70" cy="66" r="10"/><circle cx="88" cy="54" r="11"/><circle cx="112" cy="54" r="11"/><circle cx="130" cy="66" r="10"/></g>
            ${eyes(104)}
            <path d="M73 93 q8 -4 17 -1 M110 92 q9 -3 17 1" fill="none" stroke="#9a8f80" stroke-width="2.6" stroke-linecap="round"/>
            ${mouths(100, 133)}
            <g transform="translate(152 152)"><circle cx="14" cy="18" r="14" fill="#d9483b"/><path d="M14 5 q2 -6 6 -7" stroke="#6b4b33" stroke-width="2.4" fill="none"/><ellipse cx="22" cy="2" rx="6" ry="3" fill="#5aa05a"/></g>`,
        lovelace: () => `
            <circle cx="100" cy="110" r="100" fill="#ece7fb"/>
            <path d="M38 220 q4 -50 62 -58 q58 8 62 58 Z" fill="#4b4a8f"/>
            <path d="M74 168 q26 18 52 0 q-6 14 -26 16 q-20 -2 -26 -16 Z" fill="#f6dccb"/><rect x="90" y="146" width="20" height="22" fill="${SKIN.lovelace}"/>
            <path d="M70 172 q30 20 60 0" fill="none" stroke="#c9c6f3" stroke-width="3"/>
            <g fill="#3b2a26"><ellipse cx="60" cy="122" rx="9" ry="16"/><ellipse cx="57" cy="146" rx="8" ry="13"/><ellipse cx="140" cy="122" rx="9" ry="16"/><ellipse cx="143" cy="146" rx="8" ry="13"/></g>
            ${face("lovelace")}
            <path d="M60 106 q-4 -54 40 -56 q44 2 40 56 q-10 -30 -40 -32 q-30 2 -40 32 Z" fill="#4a332d"/>
            <path d="M100 52 q-26 4 -32 40 M100 52 q26 4 32 40" fill="none" stroke="#3b2a26" stroke-width="2"/>
            <ellipse cx="100" cy="46" rx="22" ry="10" fill="#3b2a26"/>
            ${eyes(104)}
            <path d="M74 92 q8 -5 16 -2 M110 90 q8 -3 16 2" fill="none" stroke="#3b2a26" stroke-width="2.4" stroke-linecap="round"/>
            ${mouths(100, 133, "#b3485a")}
            <g transform="translate(150 150)" fill="#8b88d8" stroke="#4b4a8f" stroke-width="2"><path d="M16 2 l3 5 6 -1 1 6 5 3 -3 5 3 5 -5 3 -1 6 -6 -1 -3 5 -5 -3 -5 3 -3 -5 -6 1 -1 -6 -5 -3 3 -5 -3 -5 5 -3 1 -6 6 1 Z" transform="translate(-2 2)"/><circle cx="14" cy="18" r="5" fill="#ece7fb"/></g>`,
        galileo: () => `
            <circle cx="100" cy="110" r="100" fill="#e4e1f6"/>
            <path d="M36 220 q4 -54 64 -62 q60 8 64 62 Z" fill="#3a3346"/>
            <path d="M74 162 q26 14 52 0 l-4 10 q-22 10 -44 0 Z" fill="#f2efe6"/>
            ${face("galileo")}
            <path d="M64 120 q0 46 36 50 q36 -4 36 -50 q-8 16 -18 18 q-8 -6 -18 -6 q-10 0 -18 6 q-10 -2 -18 -18 Z" fill="#9b8f84"/>
            <path d="M84 131 q16 -7 32 0 q-16 3 -32 0 Z" fill="#857a70"/>
            <path d="M62 100 q-2 -40 38 -44 q40 4 38 44 q-8 -22 -18 -26 q-20 -4 -40 0 q-10 4 -18 26 Z" fill="#8a7d72"/>
            <path d="M70 74 q30 -16 60 0" fill="none" stroke="#a69b90" stroke-width="3"/>
            ${eyes(104, "#3e3226")}
            <path d="M72 92 q9 -6 18 -2 M110 90 q9 -4 18 2" fill="none" stroke="#6e6258" stroke-width="3" stroke-linecap="round"/>
            ${mouths(100, 141, "#8a4a4a")}
            <g transform="translate(142 150) rotate(-30)"><rect x="0" y="0" width="40" height="11" rx="3" fill="#b5843f" stroke="#6b4b23" stroke-width="2"/><rect x="38" y="-2" width="8" height="15" rx="2" fill="#6b4b23"/></g>`,
        darwin: () => `
            <circle cx="100" cy="110" r="100" fill="#e2f0e2"/>
            <path d="M36 220 q4 -54 64 -62 q60 8 64 62 Z" fill="#2f3b33"/>
            ${face("darwin")}
            <path d="M60 116 q-2 70 40 74 q42 -4 40 -74 q-6 22 -18 26 q-10 -8 -22 -8 q-12 0 -22 8 q-12 -4 -18 -26 Z" fill="#ece9e2"/>
            <path d="M84 156 q16 26 32 0 M78 146 q22 34 44 0" fill="none" stroke="#d3cfc6" stroke-width="1.6"/>
            <path d="M84 131 q16 -7 32 0 q-16 3 -32 0 Z" fill="#dcd8cf"/>
            <path d="M62 112 q-6 -22 6 -36 q4 14 -6 36 Z M138 112 q6 -22 -6 -36 q-4 14 6 36 Z" fill="#e6e2da"/>
            ${eyes(104, "#3a4a5c")}
            <path d="M70 92 q10 -9 22 -3 M108 89 q12 -6 22 3" fill="none" stroke="#e6e2da" stroke-width="5" stroke-linecap="round"/>
            ${mouths(100, 141, "#9a4f4f")}
            <g transform="translate(146 156)"><ellipse cx="18" cy="16" rx="18" ry="12" fill="#7a9a5a" stroke="#4d6638" stroke-width="2"/><path d="M6 12 l8 -6 8 6 8 -6" fill="none" stroke="#4d6638" stroke-width="2"/><circle cx="38" cy="14" r="6" fill="#8fae6b" stroke="#4d6638" stroke-width="2"/></g>`,
        cervantes: () => `
            <circle cx="100" cy="110" r="100" fill="#f7ead2"/>
            <path d="M36 220 q4 -54 64 -62 q60 8 64 62 Z" fill="#2e2a2c"/>
            <g fill="#faf7f0" stroke="#d9d2c4" stroke-width="1.5"><ellipse cx="100" cy="166" rx="52" ry="14"/><path d="M52 166 q8 8 16 0 q8 8 16 0 q8 8 16 0 q8 8 16 0 q8 8 16 0 q8 8 16 0" fill="none"/></g>
            ${face("cervantes")}
            <path d="M88 144 q12 22 24 0 q-12 6 -24 0 Z" fill="#4a3a30"/>
            <path d="M80 131 q8 -6 20 -2 q12 -4 20 2 q-10 4 -20 0 q-10 4 -20 0 Z" fill="#4a3a30"/>
            <path d="M62 104 q-4 -48 38 -50 q42 2 38 50 q-6 -28 -38 -30 q-32 2 -38 30 Z" fill="#4a3a30"/>
            ${eyes(104)}
            <path d="M73 93 q8 -5 17 -2 M110 91 q9 -3 17 2" fill="none" stroke="#3a2c24" stroke-width="2.6" stroke-linecap="round"/>
            ${mouths(100, 137, "#9a4f4f")}
            <g transform="translate(146 146)"><rect x="0" y="10" width="30" height="34" rx="3" fill="#8c3a2c"/><rect x="4" y="14" width="22" height="26" fill="#f4ecd8"/><path d="M34 -4 q-10 18 -14 34" stroke="#3a2c24" stroke-width="2" fill="none"/><path d="M34 -4 q6 10 -6 22 q-2 -12 6 -22 Z" fill="#f2efe6" stroke="#9a8f80"/></g>`,
        franklin: () => `
            <circle cx="100" cy="110" r="100" fill="#e3eef8"/>
            <path d="M36 220 q4 -54 64 -62 q60 8 64 62 Z" fill="#fbfbfd"/>
            <path d="M82 160 l18 30 18 -30" fill="#4a6fa5"/><path d="M78 158 l22 18 22 -18 l-4 -4 -18 12 -18 -12 Z" fill="#e7e7ee"/>
            <path d="M58 220 v-40 M142 220 v-40" stroke="#d6d6e0" stroke-width="2"/>
            ${face("franklin")}
            <path d="M60 110 q-8 -60 40 -60 q48 0 40 60 q-4 -14 -10 -20 q-2 -16 -30 -20 q-28 4 -32 22 q-6 6 -8 18 Z" fill="#3b2b24"/>
            <g fill="#3b2b24"><circle cx="62" cy="104" r="8"/><circle cx="138" cy="104" r="8"/><circle cx="64" cy="118" r="6"/><circle cx="136" cy="118" r="6"/></g>
            ${eyes(106)}
            <path d="M74 94 q8 -5 16 -2 M110 92 q8 -3 16 2" fill="none" stroke="#3b2b24" stroke-width="2.4" stroke-linecap="round"/>
            ${mouths(100, 134, "#b3485a")}
            <g transform="translate(150 140)" fill="none" stroke-width="3" stroke-linecap="round"><path d="M4 0 q24 12 0 24 q-24 12 0 24" stroke="#4a6fa5"/><path d="M24 0 q-24 12 0 24 q24 12 0 24" stroke="#d9483b"/><path d="M8 6 h12 M8 18 h12 M8 30 h12 M8 42 h12" stroke="#9a9ab0" stroke-width="2"/></g>`,
        ibnbattuta: () => `
            <circle cx="100" cy="110" r="100" fill="#fbecd6"/>
            <path d="M36 220 q4 -54 64 -62 q60 8 64 62 Z" fill="#8a5a34"/>
            <path d="M80 158 q20 12 40 0 l-4 62 h-32 Z" fill="#f3ead8"/>
            ${face("ibnbattuta")}
            <path d="M66 122 q2 40 34 44 q32 -4 34 -44 q-6 14 -16 16 q-8 -6 -18 -6 q-10 0 -18 6 q-10 -2 -16 -16 Z" fill="#2f2622"/>
            <path d="M84 131 q16 -7 32 0 q-16 3 -32 0 Z" fill="#2f2622"/>
            <path d="M54 90 q-4 -46 46 -48 q50 2 46 48 q-20 -10 -46 -10 q-26 0 -46 10 Z" fill="#f7f3ea" stroke="#d9cfbd" stroke-width="2"/>
            <path d="M60 70 q40 -16 80 0 M58 82 q42 -14 84 0" fill="none" stroke="#d9cfbd" stroke-width="2"/>
            ${eyes(104, "#2f2622")}
            <path d="M73 93 q8 -5 17 -2 M110 91 q9 -3 17 2" fill="none" stroke="#2f2622" stroke-width="2.6" stroke-linecap="round"/>
            ${mouths(100, 141, "#7a3a3a")}
            <g transform="translate(148 150)"><circle cx="16" cy="16" r="15" fill="#f3ead8" stroke="#8a5a34" stroke-width="2.5"/><path d="M16 4 l4 12 -4 12 -4 -12 Z" fill="#d9483b"/><path d="M16 16 l-4 12 4 0 Z" fill="#4a6fa5"/></g>`,
        smith: () => `
            <circle cx="100" cy="110" r="100" fill="#efe9f7"/>
            <path d="M36 220 q4 -54 64 -62 q60 8 64 62 Z" fill="#5a4a6b"/>
            <path d="M88 158 h24 l-12 24 Z" fill="#fff"/><path d="M84 164 q16 12 32 0 l-4 16 q-12 6 -24 0 Z" fill="#f4f1ea"/>
            <g fill="#ebe7e0" stroke="#d6d0c6" stroke-width="1.5"><rect x="52" y="104" width="16" height="10" rx="5"/><rect x="52" y="116" width="16" height="10" rx="5"/><rect x="132" y="104" width="16" height="10" rx="5"/><rect x="132" y="116" width="16" height="10" rx="5"/></g>
            ${face("smith")}
            <path d="M60 104 q-4 -52 40 -54 q44 2 40 54 q-6 -28 -40 -30 q-34 2 -40 30 Z" fill="#ebe7e0"/>
            <path d="M138 70 q14 4 14 20 q-6 -6 -14 -6 Z" fill="#ebe7e0"/>
            ${eyes(104)}
            <path d="M73 93 q8 -4 17 -1 M110 92 q9 -3 17 1" fill="none" stroke="#a89f92" stroke-width="2.6" stroke-linecap="round"/>
            ${mouths(100, 133)}
            <g transform="translate(150 152)"><circle cx="16" cy="16" r="15" fill="#e8b84a" stroke="#a57a1c" stroke-width="2.5"/><circle cx="16" cy="16" r="9" fill="none" stroke="#a57a1c" stroke-width="1.5"/></g>`,
        noether: () => `
            <circle cx="100" cy="110" r="100" fill="#e6f1f0"/>
            <path d="M36 220 q4 -54 64 -62 q60 8 64 62 Z" fill="#3d4a5a"/>
            <path d="M82 158 q18 12 36 0 l-2 10 q-16 8 -32 0 Z" fill="#e7e2d6"/>
            ${face("noether")}
            <path d="M60 108 q-6 -58 40 -58 q46 0 40 58 q-8 -30 -40 -34 q-32 4 -40 34 Z" fill="#3a2c26"/>
            <path d="M100 52 v22" stroke="#2a201c" stroke-width="2"/>
            <path d="M62 108 q-4 14 2 22 M138 108 q4 14 -2 22" fill="none" stroke="#3a2c26" stroke-width="7" stroke-linecap="round"/>
            ${eyes(104)}
            <g fill="none" stroke="#3a3a3a" stroke-width="2.4"><circle cx="82" cy="104" r="11"/><circle cx="118" cy="104" r="11"/><path d="M93 104 h14 M71 102 l-9 -3 M129 102 l9 -3"/></g>
            <path d="M73 88 q8 -4 17 -1 M110 87 q9 -3 17 1" fill="none" stroke="#3a2c26" stroke-width="2.4" stroke-linecap="round"/>
            ${mouths(100, 134, "#b3485a")}
            <g transform="translate(150 150)" fill="none" stroke="#3d7a74" stroke-width="2.5" stroke-linecap="round"><path d="M16 0 v32 M2 8 l28 16 M2 24 l28 -16"/><path d="M12 2 l4 4 4 -4 M12 30 l4 -4 4 4"/></g>`,
        champollion: () => `
            <circle cx="100" cy="110" r="100" fill="#f8eccf"/>
            <path d="M36 220 q4 -54 64 -62 q60 8 64 62 Z" fill="#2c3a5a"/>
            <path d="M84 150 h32 v18 q-16 8 -32 0 Z" fill="#fff"/><path d="M86 168 q14 10 28 0 l-6 14 q-8 4 -16 0 Z" fill="#f4f1ea"/>
            ${face("champollion")}
            <path d="M62 128 q-4 -16 2 -26 q4 14 4 30 Z M138 128 q4 -16 -2 -26 q-4 14 -4 30 Z" fill="#2a1f1a"/>
            <g fill="#2a1f1a"><circle cx="70" cy="70" r="12"/><circle cx="86" cy="60" r="13"/><circle cx="104" cy="58" r="13"/><circle cx="122" cy="62" r="12"/><circle cx="134" cy="76" r="10"/><circle cx="64" cy="86" r="9"/><circle cx="138" cy="92" r="8"/></g>
            ${eyes(104)}
            <path d="M73 92 q8 -5 17 -2 M110 90 q9 -3 17 2" fill="none" stroke="#2a1f1a" stroke-width="2.6" stroke-linecap="round"/>
            ${mouths(100, 133)}
            <g transform="translate(150 140)"><rect x="0" y="0" width="28" height="48" rx="14" fill="#f3e2b0" stroke="#a57a1c" stroke-width="2.5"/><circle cx="14" cy="12" r="4" fill="#a57a1c"/><path d="M8 22 h12 M14 22 v8 M8 36 q6 -6 12 0" stroke="#a57a1c" stroke-width="2" fill="none"/></g>`,
    };

    /** SVG of a figure, "" for an unknown key. */
    function avatar(key) {
        return Object.hasOwn(art, key)
            ? `<svg viewBox="0 0 200 220" class="av w-full h-auto" xmlns="http://www.w3.org/2000/svg" role="img" aria-hidden="true"><defs><clipPath id="av-${key}"><circle cx="100" cy="110" r="100"/></clipPath></defs><g clip-path="url(#av-${key})"><g class="av-head">${art[key]()}</g></g></svg>`
            : "";
    }

    function hydrate(root = document) {
        root.querySelectorAll("[data-avatar]").forEach((el) => {
            el.innerHTML = avatar(el.dataset.avatar);
            el.dataset.mouth = el.dataset.mouth || "closed";
        });
    }

    window.Edu = window.Edu || {};
    window.Edu.avatar = avatar;
    if (document.readyState !== "loading") hydrate(); else document.addEventListener("DOMContentLoaded", () => hydrate());
})();
