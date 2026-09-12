(function () {
    const button = document.getElementById("assistant-button");
    const panel = document.getElementById("assistant-panel");
    const closeBtn = document.getElementById("assistant-close");
    const messages = document.getElementById("assistant-messages");
    const input = document.getElementById("assistant-input");
    const sendBtn = document.getElementById("assistant-send");

    if (!button || !panel) return;

    function toggle() { panel.classList.toggle("open"); }
    button.addEventListener("click", toggle);
    closeBtn?.addEventListener("click", toggle);

    function appendMessage(text, who) {
        const div = document.createElement("div");
        div.className = "msg " + who;
        div.textContent = text;
        messages.appendChild(div);
        messages.scrollTop = messages.scrollHeight;
    }

    async function send() {
        const text = input.value.trim();
        if (!text) return;
        appendMessage(text, "user");
        input.value = "";

        try {
            const response = await fetch("/assistant/ask", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ text })
            });
            const data = await response.json();
            appendMessage(data.response, "ai");
        } catch {
            appendMessage("Sorry, I couldn't reach the assistant just now.", "ai");
        }
    }

    sendBtn.addEventListener("click", send);
    input.addEventListener("keydown", (e) => { if (e.key === "Enter") send(); });
})();
