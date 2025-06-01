document.addEventListener("DOMContentLoaded", function () {
    const userId = parseInt(document.body.getAttribute("data-user-id")) || null;

    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/chathub", { withCredentials: true })
        .build();

    connection.start()
        .then(() => console.log("✅ SignalR connected"))
        .catch(err => console.error("❌ SignalR failed:", err.toString()));

    const chatInput = document.getElementById("chat-input");
    const chatMessages = document.getElementById("chat-messages");
    const chatbot = document.getElementById("chatbot-container");
    const icon = document.getElementById("chat-toggle-icon");
    const close = document.getElementById("chat-close");
    const sendButton = document.getElementById("chat-send");

    icon?.addEventListener("click", () => {
        if (!userId || isNaN(userId)) {
            alert("⚠️ Bạn cần đăng nhập để sử dụng chatbot.");
            return;
        }
        chatbot.style.display = "flex";
        icon.style.display = "none";
    });

    close?.addEventListener("click", () => {
        chatbot.style.display = "none";
        icon.style.display = "flex";
    });

    connection.on("ReceiveMessage", function (data) {
        if (isJson(data.response)) {
            const products = JSON.parse(data.response);
            showProductList(products);
        } else {
            appendMessage("Bot", data.response);
        }
    });

    function sendMessage() {
        const message = chatInput.value.trim();
        if (!message) return;

        appendMessage("Bạn", message);

        connection.invoke("SendMessage", message).catch(err => {
            console.error("❌ SendMessage error:", err);
            appendMessage("Bot", "⚠️ Có lỗi xảy ra khi gửi tin.");
        });

        chatInput.value = "";
    }

    chatInput?.addEventListener("keypress", function (e) {
        if (e.key === "Enter") sendMessage();
    });

    sendButton?.addEventListener("click", sendMessage);

    function appendMessage(sender, text) {
        const div = document.createElement("div");
        div.innerHTML = `<strong>${sender}:</strong> ${text}`;
        chatMessages.appendChild(div);
        chatMessages.scrollTop = chatMessages.scrollHeight;
    }

    function showProductList(products) {
        const div = document.createElement("div");
        div.innerHTML = `<strong>Bot:</strong> Dưới đây là một số sản phẩm bạn quan tâm:<br>`;
        products.forEach(p => {
            div.innerHTML += `
                <div style="margin: 5px 0; display: flex; gap: 8px; align-items: center;">
                    <img src="${p.image}" alt="${p.name}" width="50" height="50" style="border-radius: 6px;">
                    <span>${p.name} - ${p.price.toLocaleString()} VNĐ</span>
                </div>
            `;
        });
        chatMessages.appendChild(div);
        chatMessages.scrollTop = chatMessages.scrollHeight;
    }

    function isJson(str) {
        try { JSON.parse(str); } catch { return false; }
        return true;
    }
});
