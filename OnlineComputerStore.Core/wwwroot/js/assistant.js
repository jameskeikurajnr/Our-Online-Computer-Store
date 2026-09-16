(function () {
    const button = document.getElementById("assistant-button");
    const panel = document.getElementById("assistant-panel");
    const closeBtn = document.getElementById("assistant-close");
    const messages = document.getElementById("assistant-messages");
    const input = document.getElementById("assistant-input");
    const sendBtn = document.getElementById("assistant-send");
    const chipRow = document.getElementById("assistant-chip-row");

    if (!button || !panel) return;

    function toggle() { panel.classList.toggle("open"); }
    button.addEventListener("click", toggle);
    closeBtn?.addEventListener("click", toggle);

    // The last few turns of this conversation, mirrored to the server on
    // every request so the assistant can follow up naturally ("what about in
    // black?") instead of answering each message cold. Capped client-side
    // too so an unusually long chat session doesn't grow this forever — the
    // server only looks at the most recent handful anyway.
    const history = [];
    const MAX_HISTORY = 12;

    function pushHistory(role, text) {
        history.push({ role: role, text: text });
        if (history.length > MAX_HISTORY) history.shift();
    }

    function scrollToBottom() {
        messages.scrollTop = messages.scrollHeight;
    }

    function appendMessage(text, who) {
        const div = document.createElement("div");
        div.className = "msg " + who;
        div.textContent = text;
        messages.appendChild(div);
        scrollToBottom();
        return div;
    }

    function appendNote(text) {
        const div = document.createElement("div");
        div.className = "msg note";
        div.textContent = text;
        messages.appendChild(div);
        scrollToBottom();
    }

    function showTyping() {
        const div = document.createElement("div");
        div.className = "msg ai typing-wrap";
        div.innerHTML = '<span class="typing"><span></span><span></span><span></span></span>';
        messages.appendChild(div);
        scrollToBottom();
        return div;
    }

    function formatPrice(value) {
        const n = Number(value) || 0;
        return "$" + n.toLocaleString("en-US", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    }

    // Product cards are built entirely from fields the server already
    // resolved against the real Products table (see AiAssistantService) —
    // never from raw AI text — so every field here is set with textContent
    // or a plain attribute, not innerHTML. Cards render as a horizontally
    // scrollable row of compact photo-cards (photo on top, details below)
    // so 2-3 real product photos are visible at once, more reachable by
    // scrolling the row.
    function appendProductCards(products) {
        if (!products || !products.length) return;

        const wrap = document.createElement("div");
        wrap.className = "product-cards";

        products.forEach(function (p) {
            const card = document.createElement("a");
            card.className = "product-card";
            card.href = p.url;
            card.target = "_blank";
            card.rel = "noopener";

            const thumb = document.createElement("span");
            thumb.className = "product-thumb";
            if (p.imageUrl) {
                const img = document.createElement("img");
                img.src = p.imageUrl;
                img.alt = "";
                img.onerror = function () { thumb.innerHTML = fallbackIconSvg(); };
                thumb.appendChild(img);
            } else {
                thumb.innerHTML = fallbackIconSvg();
            }

            const body = document.createElement("span");
            body.className = "product-card-body";

            const name = document.createElement("span");
            name.className = "product-name";
            name.textContent = p.name;

            const meta = document.createElement("span");
            meta.className = "product-meta";
            const dot = document.createElement("span");
            dot.className = "stock-dot " + (p.stockQuantity > 0 ? "in" : "out");
            meta.appendChild(dot);
            meta.appendChild(document.createTextNode(p.stockQuantity > 0 ? "In stock" : "Out of stock"));

            const price = document.createElement("span");
            price.className = "product-price";
            price.textContent = formatPrice(p.price);

            body.appendChild(name);
            body.appendChild(meta);
            body.appendChild(price);

            card.appendChild(thumb);
            card.appendChild(body);
            wrap.appendChild(card);
        });

        messages.appendChild(wrap);

        // Only hint at scrolling when there are more cards than comfortably
        // fit in view at once — with 1-2 products the whole row already
        // shows without scrolling.
        if (products.length > 2) {
            const note = document.createElement("p");
            note.className = "scroll-fade-note";
            note.textContent = "← scroll for more →";
            messages.appendChild(note);
        }

        scrollToBottom();
    }

    function fallbackIconSvg() {
        return '<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><rect x="3" y="3" width="18" height="18" rx="2" /><circle cx="9" cy="9" r="2" /><path d="m21 15-5-5L5 21" /></svg>';
    }

    async function sendText(text) {
        text = (text || "").trim();
        if (!text) return;

        chipRow?.remove();

        appendMessage(text, "user");
        // Snapshot the history BEFORE adding this turn — the server already
        // appends `text` as the newest user turn itself, so sending it a
        // second time inside `history` would make the model see the same
        // message twice.
        const priorHistory = history.slice();
        pushHistory("user", text);
        input.value = "";

        const typingEl = showTyping();

        try {
            const response = await fetch("/assistant/ask", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ text: text, history: priorHistory })
            });
            const data = await response.json();

            typingEl.remove();

            if (data.response) {
                appendMessage(data.response, "ai");
                pushHistory("assistant", data.response);
            }

            appendProductCards(data.products);

            if (data.flagged) {
                appendNote("Noted — I've let the team know you're after this.");
            }
        } catch {
            typingEl.remove();
            appendMessage("Sorry, I couldn't reach the assistant just now.", "ai");
        }
    }

    sendBtn.addEventListener("click", () => sendText(input.value));
    input.addEventListener("keydown", (e) => { if (e.key === "Enter") sendText(input.value); });

    chipRow?.addEventListener("click", (e) => {
        const chip = e.target.closest(".chip");
        if (chip) sendText(chip.dataset.prompt || chip.textContent);
    });
})();
