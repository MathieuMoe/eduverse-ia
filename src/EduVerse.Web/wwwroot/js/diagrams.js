// Pictures for some questions: the server sends a small specification ("dg:kind:arg:arg"), drawn here as SVG.
// Arguments are parsed as numbers or fixed letters, so no text from the server reaches the markup.
(() => {
    "use strict";
    const lang = document.documentElement.lang || "en";
    const nf = (v) => new Intl.NumberFormat(lang, { maximumFractionDigits: 2 }).format(v);
    const ACCENT = "var(--dg-accent, #7a6cf0)";
    const SOFT = "var(--dg-soft, #e4e1fd)";
    const INK = "currentColor";
    const svg = (w, h, body) => `<svg viewBox="0 0 ${w} ${h}" width="${w}" height="${h}" class="max-w-full h-auto text-foreground" xmlns="http://www.w3.org/2000/svg" font-family="inherit" font-size="13" role="img" aria-hidden="true">${body}</svg>`;
    const text = (x, y, s, extra = "") => `<text x="${x}" y="${y}" fill="${INK}" text-anchor="middle" dominant-baseline="middle" ${extra}>${s}</text>`;
    const int = (s) => { const n = Number.parseInt(s, 10); return Number.isFinite(n) ? n : 0; };
    const num = (s) => { const n = Number.parseFloat(s); return Number.isFinite(n) ? n : 0; };
    const allele = (s) => (String(s).match(/^[Aa]{2}$/) ? s : "??");

    const kinds = {
        pie([n, d]) {
            n = int(n); d = Math.max(1, Math.min(int(d), 24));
            const c = 80, r = 64;
            let body = "";
            for (let i = 0; i < d; i++) {
                const a0 = (i / d) * 2 * Math.PI - Math.PI / 2, a1 = ((i + 1) / d) * 2 * Math.PI - Math.PI / 2;
                const p = (a) => `${(c + r * Math.cos(a)).toFixed(2)} ${(c + r * Math.sin(a)).toFixed(2)}`;
                body += d === 1 ? `<circle cx="${c}" cy="${c}" r="${r}" fill="${i < n ? ACCENT : "none"}" stroke="${INK}" stroke-width="2"/>`
                    : `<path d="M${c} ${c} L${p(a0)} A${r} ${r} 0 0 1 ${p(a1)} Z" fill="${i < n ? ACCENT : "transparent"}" stroke="${INK}" stroke-width="2" stroke-linejoin="round"/>`;
            }
            return svg(160, 160, body);
        },
        grid([p]) {
            p = Math.max(0, Math.min(int(p), 100));
            let body = "";
            for (let i = 0; i < 100; i++) {
                const x = 4 + (i % 10) * 15, y = 4 + Math.floor(i / 10) * 15;
                body += `<rect x="${x}" y="${y}" width="13" height="13" rx="2" fill="${i < p ? ACCENT : SOFT}"/>`;
            }
            return svg(156, 156, body);
        },
        rect([a, b]) {
            a = Math.max(1, int(a)); b = Math.max(1, int(b));
            const s = Math.min(170 / a, 100 / b), w = a * s, h = b * s, x = (220 - w) / 2, y = 14;
            return svg(220, h + 50, `<rect x="${x}" y="${y}" width="${w}" height="${h}" rx="3" fill="${SOFT}" stroke="${ACCENT}" stroke-width="2.5"/>`
                + text(x + w / 2, y + h + 18, `${a} cm`) + text(x + w + 22, y + h / 2, `${b} cm`));
        },
        tri([a, b]) {
            a = Math.max(1, int(a)); b = Math.max(1, int(b));
            const s = Math.min(170 / b, 110 / a), h = a * s, w = b * s, x0 = 40, y0 = 20 + h;
            return svg(240, h + 50, `<path d="M${x0} ${y0} L${x0} ${y0 - h} L${x0 + w} ${y0} Z" fill="${SOFT}" stroke="${ACCENT}" stroke-width="2.5" stroke-linejoin="round"/>`
                + `<path d="M${x0} ${y0 - 14} h14 v14" fill="none" stroke="${INK}" stroke-width="1.5"/>`
                + text(x0 - 18, y0 - h / 2, a) + text(x0 + w / 2, y0 + 16, b) + text(x0 + w / 2 + 14, y0 - h / 2 - 10, "?", 'font-weight="700" font-size="18"'));
        },
        line([x1, y1, x2, y2]) {
            [x1, y1, x2, y2] = [x1, y1, x2, y2].map(int);
            const xs = [Math.min(x1, x2, 0) - 1, Math.max(x1, x2, 0) + 1], ys = [Math.min(y1, y2, 0) - 1, Math.max(y1, y2, 0) + 1];
            const W = 240, H = 180, px = (x) => 16 + ((x - xs[0]) / (xs[1] - xs[0])) * (W - 32), py = (y) => H - 16 - ((y - ys[0]) / (ys[1] - ys[0])) * (H - 32);
            const m = x2 === x1 ? 0 : (y2 - y1) / (x2 - x1), yAt = (x) => y1 + m * (x - x1);
            let body = `<line x1="${px(xs[0])}" y1="${py(0)}" x2="${px(xs[1])}" y2="${py(0)}" stroke="${INK}" stroke-opacity=".5"/><line x1="${px(0)}" y1="${py(ys[0])}" x2="${px(0)}" y2="${py(ys[1])}" stroke="${INK}" stroke-opacity=".5"/>`;
            body += `<line x1="${px(xs[0])}" y1="${py(yAt(xs[0]))}" x2="${px(xs[1])}" y2="${py(yAt(xs[1]))}" stroke="${ACCENT}" stroke-width="2.5"/>`;
            for (const [x, y] of [[x1, y1], [x2, y2]]) {
                body += `<circle cx="${px(x)}" cy="${py(y)}" r="5" fill="${ACCENT}"/>` + text(px(x) + (x === x1 ? -6 : 6), py(y) - 13, `(${x}, ${y})`, `text-anchor="${x === x1 ? "end" : "start"}"`);
            }
            return `<div class="overflow-hidden">${svg(W, H, `<clipPath id="dg-clip"><rect width="${W}" height="${H}"/></clipPath><g clip-path="url(#dg-clip)">${body}</g>`)}</div>`;
        },
        angle([deg]) {
            deg = Math.max(0, Math.min(int(deg), 180));
            const ox = 40, oy = 130, r = 150, rad = (deg * Math.PI) / 180, arc = 40;
            const ex = ox + r * Math.cos(rad), ey = oy - r * Math.sin(rad);
            const ax = ox + arc * Math.cos(rad), ay = oy - arc * Math.sin(rad);
            const mark = deg === 90 ? `<path d="M${ox + 18} ${oy} v-18 h-18" fill="none" stroke="${ACCENT}" stroke-width="2"/>`
                : deg > 0 ? `<path d="M${ox + arc} ${oy} A${arc} ${arc} 0 0 0 ${ax.toFixed(2)} ${ay.toFixed(2)}" fill="none" stroke="${ACCENT}" stroke-width="2.5"/>` : "";
            const lx = ox + (arc + 22) * Math.cos(rad / 2), ly = oy - (arc + 22) * Math.sin(rad / 2);
            return svg(220, 150, `<line x1="${ox}" y1="${oy}" x2="${ox + r}" y2="${oy}" stroke="${INK}" stroke-width="2.5"/><line x1="${ox}" y1="${oy}" x2="${ex.toFixed(2)}" y2="${ey.toFixed(2)}" stroke="${INK}" stroke-width="2.5"/>${mark}${text(lx, ly, `${deg}°`, 'font-weight="600"')}`);
        },
        bars([list]) {
            const v = String(list).split(",").map(int).slice(0, 8);
            const max = Math.max(1, ...v), bw = 30, gap = 14, W = v.length * (bw + gap) + gap, H = 150;
            return svg(W, H, v.map((x, i) => {
                const h = (x / max) * (H - 44), bx = gap + i * (bw + gap);
                return `<rect x="${bx}" y="${H - 22 - h}" width="${bw}" height="${h}" rx="4" fill="${ACCENT}"/>` + text(bx + bw / 2, H - 30 - h, x);
            }).join("") + `<line x1="4" y1="${H - 22}" x2="${W - 4}" y2="${H - 22}" stroke="${INK}" stroke-opacity=".5"/>`);
        },
        polygon([n]) {
            n = Math.max(3, Math.min(int(n), 12));
            const c = 75, r = 62, pts = Array.from({ length: n }, (_, i) => { const a = -Math.PI / 2 + (i * 2 * Math.PI) / n; return `${(c + r * Math.cos(a)).toFixed(1)},${(c + r * Math.sin(a)).toFixed(1)}`; });
            return svg(150, 150, `<polygon points="${pts.join(" ")}" fill="${SOFT}" stroke="${ACCENT}" stroke-width="3" stroke-linejoin="round"/>`);
        },
        clock([h, m]) {
            h = int(h) % 12; m = int(m) % 60;
            const c = 80, hand = (len, a, w) => `<line x1="${c}" y1="${c}" x2="${(c + len * Math.sin(a)).toFixed(2)}" y2="${(c - len * Math.cos(a)).toFixed(2)}" stroke="${w > 4 ? INK : ACCENT}" stroke-width="${w}" stroke-linecap="round"/>`;
            let body = `<circle cx="${c}" cy="${c}" r="70" fill="${SOFT}" stroke="${INK}" stroke-width="3"/>`;
            for (let i = 0; i < 60; i++) {
                const a = (i * Math.PI) / 30, big = i % 5 === 0;
                body += `<line x1="${(c + (big ? 60 : 64) * Math.sin(a)).toFixed(2)}" y1="${(c - (big ? 60 : 64) * Math.cos(a)).toFixed(2)}" x2="${(c + 68 * Math.sin(a)).toFixed(2)}" y2="${(c - 68 * Math.cos(a)).toFixed(2)}" stroke="${INK}" stroke-width="${big ? 2.5 : 1}"/>`;
            }
            for (let i = 1; i <= 12; i++) body += text((c + 48 * Math.sin((i * Math.PI) / 6)).toFixed(1), (c - 48 * Math.cos((i * Math.PI) / 6)).toFixed(1), i, 'font-weight="600"');
            body += hand(34, ((h + m / 60) * Math.PI) / 6, 6) + hand(54, (m * Math.PI) / 30, 3) + `<circle cx="${c}" cy="${c}" r="5" fill="${INK}"/>`;
            return svg(160, 160, body);
        },
        circuit([i, r]) {
            const zig = (x, y) => `<path d="M${x} ${y} l6 -9 l10 18 l10 -18 l10 18 l10 -18 l10 18 l6 -9" fill="none" stroke="${ACCENT}" stroke-width="2.5" stroke-linejoin="round"/>`;
            return svg(240, 160, `<path d="M30 40 H90 M152 40 H210 V120 H150 M110 120 H30 V95 M30 70 V40" fill="none" stroke="${INK}" stroke-width="2"/>`
                + zig(90, 40) + text(121, 18, `${int(r)} Ω`, 'font-weight="600"')
                + `<line x1="16" y1="70" x2="44" y2="70" stroke="${INK}" stroke-width="3"/><line x1="22" y1="95" x2="38" y2="95" stroke="${INK}" stroke-width="5"/>` + text(58, 82, "V ?")
                + `<circle cx="130" cy="120" r="18" fill="var(--layer, #fff)" stroke="${INK}" stroke-width="2"/>` + text(130, 121, "A", 'font-weight="700"') + text(130, 150, `${nf(num(i))} A`));
        },
        resistors([n, r, kind]) {
            n = Math.max(1, Math.min(int(n), 6)); r = int(r);
            const zig = (x, y) => `<path d="M${x} ${y} l5 -8 l8 16 l8 -16 l8 16 l8 -16 l5 8" fill="none" stroke="${ACCENT}" stroke-width="2.5" stroke-linejoin="round"/>`;
            if (kind === "p") {
                const H = 30 + n * 34;
                let body = `<line x1="40" y1="25" x2="40" y2="${H - 25 + 4}" stroke="${INK}" stroke-width="2"/><line x1="200" y1="25" x2="200" y2="${H - 25 + 4}" stroke="${INK}" stroke-width="2"/><line x1="10" y1="${H / 2}" x2="40" y2="${H / 2}" stroke="${INK}" stroke-width="2"/><line x1="200" y1="${H / 2}" x2="230" y2="${H / 2}" stroke="${INK}" stroke-width="2"/>`;
                for (let k = 0; k < n; k++) {
                    const y = 25 + k * 34 + 4;
                    body += `<line x1="40" y1="${y}" x2="99" y2="${y}" stroke="${INK}" stroke-width="2"/>${zig(99, y)}<line x1="141" y1="${y}" x2="200" y2="${y}" stroke="${INK}" stroke-width="2"/>` + text(170, y - 10, `${r} Ω`, 'font-size="11"');
                }
                return svg(240, H + 8, body);
            }
            const W = 40 + n * 62;
            let body = `<line x1="8" y1="50" x2="${W - 8}" y2="50" stroke="${INK}" stroke-width="2"/>`;
            for (let k = 0; k < n; k++) {
                const x = 20 + k * 62 + 10;
                body += `<rect x="${x - 2}" y="38" width="46" height="24" fill="var(--layer, #fff)"/>${zig(x, 50)}` + text(x + 21, 22, `${r} Ω`, 'font-size="11"');
            }
            return svg(W, 80, body);
        },
        punnett([p1, p2]) {
            p1 = allele(p1); p2 = allele(p2);
            const cell = 54, x0 = 46, y0 = 40;
            let body = "";
            for (let i = 0; i < 2; i++) {
                body += text(x0 + cell * i + cell / 2, 22, p2[i], 'font-weight="700" font-size="17"') + text(26, y0 + cell * i + cell / 2, p1[i], 'font-weight="700" font-size="17"');
                for (let j = 0; j < 2; j++) body += `<rect x="${x0 + cell * j}" y="${y0 + cell * i}" width="${cell}" height="${cell}" fill="${SOFT}" stroke="${ACCENT}" stroke-width="2"/>` + text(x0 + cell * j + cell / 2, y0 + cell * i + cell / 2, "?", 'fill-opacity=".45"');
            }
            return svg(170, 160, body);
        },
        market() {
            return svg(220, 170, `<path d="M30 15 V145 H205" fill="none" stroke="${INK}" stroke-width="2"/>`
                + `<path d="M45 30 L190 135" stroke="${ACCENT}" stroke-width="3"/><path d="M45 135 L190 30" stroke="${INK}" stroke-width="3" stroke-dasharray="7 5"/>`
                + `<circle cx="117.5" cy="82.5" r="5" fill="${ACCENT}"/><path d="M30 82.5 H117.5 V145" fill="none" stroke="${INK}" stroke-opacity=".45" stroke-dasharray="3 4"/>`
                + text(18, 16, "P", 'font-weight="700"') + text(208, 158, "Q", 'font-weight="700"') + text(198, 136, "D", 'font-weight="700"') + text(198, 26, "S", 'font-weight="700"') + text(16, 82.5, "P*", 'font-size="11"'));
        },
    };

    /** SVG for a specification, or "" if it is not one. */
    function draw(spec) {
        if (typeof spec !== "string" || !spec.startsWith("dg:")) return "";
        const [kind, ...args] = spec.slice(3).split(":");
        return Object.hasOwn(kinds, kind) ? kinds[kind](args) : "";
    }

    function hydrate(root = document) {
        root.querySelectorAll("[data-diagram]").forEach((el) => { el.innerHTML = draw(el.dataset.diagram); });
    }

    window.Edu = window.Edu || {};
    window.Edu.diagram = draw;
    document.addEventListener("DOMContentLoaded", () => hydrate());
    if (document.readyState !== "loading") hydrate();
})();
