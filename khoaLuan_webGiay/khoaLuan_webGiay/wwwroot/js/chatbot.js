document.addEventListener("DOMContentLoaded", function () {
    const userId = parseInt(document.body.getAttribute("data-user-id")) || null;
    const userFullName = document.body.getAttribute("data-user-name") || "Bạn";

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

        // Gọi API lấy lịch sử chat
        fetch("/ChatHistories/GetUserChatHistory")
            .then(res => res.json())
            .then(data => {
                if (Array.isArray(data)) {
                    data.forEach(chat => {
                        const isUser = chat.sender === "user";
                        appendMessage(
                            chat.sender === "user" ? userFullName : "Bot",
                            chat.message,
                            chat.sender === "user",
                            chat.sentAt
                        );
                        });
                }
            })
            .catch(err => {
                console.error("❌ Lỗi tải lịch sử chat:", err);
            });
    });

    close?.addEventListener("click", () => {
        chatbot.style.display = "none";
        icon.style.display = "flex";
    });

    connection.on("ReceiveMessage", function (data) {
        const isUser = data.sender === "user";
        const senderName = isUser ? userFullName : "Bot";
        appendMessage(senderName, data.message, isUser, data.sentAt);
    });

    function sendMessage() {
        const message = chatInput.value.trim();
        if (!message) return;

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

    fetch("/ChatHistories/GetUserChatHistory")
        .then(res => res.json())
        .then(data => {
            if (Array.isArray(data) && data.length > 0) {
                data.forEach(chat => {
                    const isUser = chat.sender === "user";
                    appendMessage(
                        isUser ? userFullName : "Bot",
                        chat.message,
                        isUser,
                        chat.sentAt
                    );
                });
            } else {
                // Không có lịch sử → gửi lời chào từ bot
                appendMessage("Bot", "Milion Sneaker xin chào bạn 👋. Mình có thể hỗ trợ gì hôm nay?", false);
            }
        });

    function appendMessage(sender, text, isUser = false, sentTime = null) {
        const div = document.createElement("div");

        const date = sentTime ? new Date(sentTime) : new Date();
        const today = new Date();
        const isSameDay = date.toDateString() === today.toDateString();

        const timestamp = isSameDay
            ? date.toLocaleTimeString("vi-VN", { hour: '2-digit', minute: '2-digit' })
            : date.toLocaleDateString("vi-VN", { day: '2-digit', month: '2-digit', year: 'numeric' }) +
            " " +
            date.toLocaleTimeString("vi-VN", { hour: '2-digit', minute: '2-digit' });

        const avatarUrl = `/img/chat/chatbot.jpg`;

        div.style.display = "flex";
        div.style.alignItems = "flex-start";
        div.style.marginBottom = "10px";
        div.style.flexDirection = isUser ? "row-reverse" : "row";

        div.innerHTML = `
        ${!isUser ? `<img src="${avatarUrl}" alt="avatar" style="
            width: 32px; height: 32px; border-radius: 50%; margin-right: 10px;">` : ""}
        <div style="
            background: ${isUser ? "#d1ecf1" : "#f8f9fa"};
            padding: 8px 12px;
            border-radius: 12px;
            max-width: 220px;
            text-align: ${isUser ? "right" : "left"};
        ">
            <div style="font-weight: bold;">${sender}</div>
            <div>${text}</div>
            <div style="text-align: ${isUser ? "left" : "right"}; font-size: 11px; color: #666;">${timestamp}</div>
        </div>
    `;

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