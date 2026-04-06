const LANGS = ["jp", "en", "cn", "zhtw", "kr"];

function localNameOf(element) {
    const raw = (element.localName || element.tagName || "").toLowerCase();
    const idx = raw.indexOf(":");
    return idx >= 0 ? raw.slice(idx + 1) : raw;
}

function childElementsByName(parent, name) {
    const out = [];
    for (const child of Array.from(parent.children || [])) {
        if (localNameOf(child) === name) {
            out.push(child)
        };
    }
    return out;
}

function firstChildByName(parent, name) {
    for (const child of Array.from(parent.children || [])) {
        if (localNameOf(child) === name) {
            return child;
        };
    }
    return null;
}

function textForLang(textElement, lang) {
    function pick(attrName) {
        const v = textElement.getAttribute(attrName);
        return (v && v.trim() !== "") ? v : "";
    }

    return pick(lang) || pick("en") || pick("jp") || "";
}

function applyStyleAttrs(target, styleElement) {
    const size = styleElement.getAttribute("value");
    const color = styleElement.getAttribute("color");
    const bold = styleElement.getAttribute("bold");
    const italic = styleElement.getAttribute("italic");

    if (size) {
        target.style.fontSize = `${size}px`
    };
    if (color) {
        target.style.color = color;
    }
    if (bold === "true") {
        target.style.fontWeight = "bold";
    }
    if (italic === "true") {
        target.style.fontStyle = "italic";
    }
}

function renderInlineNode(node, lang) {
    const name = localNameOf(node);

    if (name === "text") {
        return document.createTextNode(textForLang(node, lang));
    }

    if (name === "style") {
        const span = document.createElement("span");
        span.className = "help-inline-style";
        applyStyleAttrs(span, node);

        for (const child of Array.from(node.children || [])) {
            span.appendChild(renderInlineNode(child, lang));
        }
        return span;
    }

    const fallback = document.createTextNode("");
    return fallback;
}

function appendInlineChildren(host, source, lang) {
    for (const child of Array.from(source.children || [])) {
        host.appendChild(renderInlineNode(child, lang));
    }
}

function resolveImageSrc(src) {
    if (!src) {
        return "";
    }
    if (/^(https?:|data:|\/|\.\.\/|\.\/)/i.test(src)) {
        return src;
    }
    return `../Texture/${src}`;
}

function renderPage(pageElement, lang) {
    const section = document.createElement("section");
    section.className = "help-page";

    const title = firstChildByName(pageElement, "title");
    if (title) {
        const h2 = document.createElement("h2");
        appendInlineChildren(h2, title, lang);
        section.appendChild(h2);
    }

    for (const child of Array.from(pageElement.children || [])) {
        const name = localNameOf(child);
        if (name === "title") {
            continue;
        }

        if (name === "topic") {
            const h3 = document.createElement("h3");
            appendInlineChildren(h3, child, lang);
            section.appendChild(h3);
            continue;
        }

        if (name === "p") {
            const p = document.createElement("p");
            appendInlineChildren(p, child, lang);
            section.appendChild(p);
            continue;
        }

        if (name === "style") {
            const p = document.createElement("p");
            p.appendChild(renderInlineNode(child, lang));
            section.appendChild(p);
            continue;
        }

        if (name === "pair") {
            const dl = document.createElement("dl");
            const kids = Array.from(child.children || []);
            for (let i = 0; i < kids.length; i += 2) {
                const keyElement = kids[i];
                const valElement = kids[i + 1];
                if (!keyElement || localNameOf(keyElement) !== "key") {
                    continue;
                }
                const dt = document.createElement("dt");
                appendInlineChildren(dt, keyElement, lang);
                dl.appendChild(dt);
                if (valElement && localNameOf(valElement) === "value") {
                    const dd = document.createElement("dd");
                    appendInlineChildren(dd, valElement, lang);
                    dl.appendChild(dd);
                }
            }
            section.appendChild(dl);
            continue;
        }

        if (name === "list") {
            const ul = document.createElement("ul");
            for (const item of childElementsByName(child, "item")) {
                const li = document.createElement("li");
                appendInlineChildren(li, item, lang);
                ul.appendChild(li);
            }
            section.appendChild(ul);
            continue;
        }

        if (name === "qa") {
            const qa = document.createElement("div");
            qa.className = "help-qa";
            const kids = Array.from(child.children || []);
            for (let i = 0; i < kids.length; i += 2) {
                const qElement = kids[i];
                const aElement = kids[i + 1];
                if (!qElement || localNameOf(qElement) !== "q") {
                    continue;
                }

                const q = document.createElement("div");
                q.className = "help-q";
                q.textContent = "Q: ";
                appendInlineChildren(q, qElement, lang);
                qa.appendChild(q);

                if (aElement && localNameOf(aElement) === "a") {
                    const a = document.createElement("div");
                    a.className = "help-a";
                    a.textContent = "A: ";
                    appendInlineChildren(a, aElement, lang);
                    qa.appendChild(a);
                }
            }
            section.appendChild(qa);
            continue;
        }

        if (name === "link") {
            const a = document.createElement("a");
            a.className = "help-link";
            a.href = child.getAttribute("href") || "#";
            a.target = "_blank";
            a.rel = "noopener noreferrer";
            appendInlineChildren(a, child, lang);
            section.appendChild(a);
            continue;
        }

        if (name === "image") {
            const img = document.createElement("img");
            img.className = "help-image";
            img.alt = "help image";
            img.src = resolveImageSrc(child.getAttribute("src"));
            section.appendChild(img);
        }

        if(name === "br") {
            const br = document.createElement("br");
            const value = child.getAttribute("value");
            if (value) {
                const count = Number.parseInt(value, 10);
                for (let i = 0; i < count; i++) {
                    section.appendChild(br.cloneNode());
                }
            } else {
                section.appendChild(br);
            }
        }
    }

    return section;
}

async function loadSourceDoc() {
    const sourceUrl = new URL("help.xhtml", globalThis.location.href).toString();
    try {
        const res = await fetch(sourceUrl, { cache: "no-store" });
        if (!res.ok) {
            throw new Error(`HTTP ${res.status}`);
        }
        const text = await res.text();
        const xml = new DOMParser().parseFromString(text, "application/xml");
        if (xml.getElementsByTagName("parsererror").length > 0) {
            throw new Error("parsererror");
        }
        return xml;
    } catch (ex) {
        console.error(ex);
        return document;
    }
}

function findPages(docLike) {
    const pages = [];
    const all = docLike.getElementsByTagName("*");
    for (const el of Array.from(all)) {
        if (localNameOf(el) === "page") {
            pages.push(el);
        }
    }
    return pages;
}

function buildShell() {
    document.body.innerHTML = "";

    const app = document.createElement("div");
    app.className = "help-app";

    const toolbar = document.createElement("div");
    toolbar.className = "help-toolbar";
    const label = document.createElement("label");
    label.textContent = "Language";
    label.setAttribute("for", "help-lang");

    const select = document.createElement("select");
    select.id = "help-lang";
    select.className = "help-lang";
    for (const lang of LANGS) {
        const opt = document.createElement("option");
        opt.value = lang;
        opt.textContent = lang.toUpperCase();
        if (lang === "jp") opt.selected = true;
        select.appendChild(opt);
    }

    toolbar.appendChild(label);
    toolbar.appendChild(select);

    const content = document.createElement("main");
    content.className = "help-content";

    app.appendChild(toolbar);
    app.appendChild(content);
    document.body.appendChild(app);

    return { select, content };
}

document.addEventListener("DOMContentLoaded", async () => {
    const source = await loadSourceDoc();
    const pages = findPages(source);
    const shell = buildShell();

    function rerender() {
        const lang = shell.select.value;
        shell.content.innerHTML = "";
        for (const page of pages) {
            shell.content.appendChild(renderPage(page, lang));
        }
    }

    shell.select.addEventListener("change", rerender);
    rerender();
});
