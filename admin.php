<?php
// admin.php
// ВНИМАНИЕ: На реальном сервере закройте этот файл паролем!
?>
<!DOCTYPE html>
<html lang="ru">
<head>
    <meta charset="UTF-8">
    <title>Панель Администратора - Поддержка</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 0; display: flex; height: 100vh; background: #f0f2f5; }
        .sidebar { width: 300px; background: white; border-right: 1px solid #ddd; overflow-y: auto; }
        .chat-area { flex: 1; display: flex; flex-direction: column; padding: 20px; }
        
        .chat-item { padding: 15px; border-bottom: 1px solid #eee; cursor: pointer; transition: background 0.2s; }
        .chat-item:hover { background: #f9f9f9; }
        .chat-item.active { background: #e6f2ff; border-left: 4px solid #0084ff; }
        .chat-item .email { font-weight: bold; margin-bottom: 5px; word-break: break-all; }
        .chat-item .status { font-size: 0.8em; color: gray; }
        
        #chat-box { flex: 1; background: white; padding: 20px; border-radius: 10px; overflow-y: auto; display: flex; flex-direction: column; margin-bottom: 20px; box-shadow: 0 1px 3px rgba(0,0,0,0.1); }
        .msg { padding: 10px 15px; border-radius: 15px; margin-bottom: 10px; max-width: 70%; }
        .msg.admin { background: #0084ff; color: white; align-self: flex-end; border-bottom-right-radius: 2px; }
        .msg.user { background: #e4e6eb; color: black; align-self: flex-start; border-bottom-left-radius: 2px; }
        .time { font-size: 0.7em; opacity: 0.7; display: block; margin-top: 5px; text-align: right; }
        
        .input-area { display: flex; gap: 10px; }
        input[type="text"] { flex: 1; padding: 15px; border: 1px solid #ccc; border-radius: 5px; outline: none; }
        button { padding: 15px 30px; border: none; background: #28a745; color: white; border-radius: 5px; cursor: pointer; font-weight: bold; }
        button:disabled { background: #ccc; cursor: not-allowed; }
    </style>
</head>
<body>

    <div class="sidebar" id="chat-list">
        </div>

    <div class="chat-area">
        <h2 id="current-chat-title">Выберите чат</h2>
        <div id="chat-box">
            </div>
        <div class="input-area">
            <input type="text" id="message-input" placeholder="Введите ответ..." disabled onkeypress="handleEnter(event)">
            <button id="send-btn" onclick="sendMessage()" disabled>Отправить</button>
        </div>
    </div>

    <script>
        let currentChatId = null;
        const chatListDiv = document.getElementById('chat-list');
        const chatBox = document.getElementById('chat-box');
        const titleDiv = document.getElementById('current-chat-title');
        const messageInput = document.getElementById('message-input');
        const sendBtn = document.getElementById('send-btn');
        let lastMessageCount = 0;

        // Загрузка списка пользователей (чатов) слева
        function loadChatList() {
            fetch('chat_api.php?action=get_chats')
                .then(res => res.json())
                .then(data => {
                    chatListDiv.innerHTML = '<h3 style="padding: 15px; margin: 0; background: #fafafa; border-bottom: 1px solid #ddd;">Диалоги</h3>';
                    data.forEach(chat => {
                        const div = document.createElement('div');
                        div.className = `chat-item ${currentChatId === chat.id ? 'active' : ''}`;
                        div.onclick = () => selectChat(chat.id, chat.user_email);
                        div.innerHTML = `
                            <div class="email">${chat.user_email}</div>
                            <div class="status">ID: ${chat.id} | ${chat.date}</div>
                        `;
                        chatListDiv.appendChild(div);
                    });
                });
        }

        // Выбор конкретного чата
        function selectChat(id, email) {
            currentChatId = id;
            titleDiv.innerText = `Чат с: ${email}`;
            messageInput.disabled = false;
            sendBtn.disabled = false;
            lastMessageCount = 0; // Сброс счетчика для нового чата
            chatBox.innerHTML = '';
            loadChatList(); // Обновляем выделение в списке
            loadMessages();
        }

        // Загрузка сообщений выбранного чата
        function loadMessages() {
            if (!currentChatId) return;
            fetch(`chat_api.php?action=get_messages&chat_id=${currentChatId}`)
                .then(res => res.json())
                .then(data => {
                    if (data.length !== lastMessageCount) {
                        chatBox.innerHTML = '';
                        data.forEach(msg => {
                            const div = document.createElement('div');
                            div.className = `msg ${msg.sender}`;
                            div.innerHTML = `${msg.message} <span class="time">${msg.time}</span>`;
                            chatBox.appendChild(div);
                        });
                        chatBox.scrollTop = chatBox.scrollHeight;
                        lastMessageCount = data.length;
                    }
                });
        }

        // Отправка ответа админа
        function sendMessage() {
            if (!currentChatId) return;
            const text = messageInput.value.trim();
            if (!text) return;

            const formData = new FormData();
            formData.append('action', 'send_message');
            formData.append('chat_id', currentChatId);
            formData.append('sender', 'admin'); // Отправляем как админ
            formData.append('message', text);

            messageInput.value = '';

            fetch('chat_api.php', { method: 'POST', body: formData })
                .then(() => loadMessages());
        }

        function handleEnter(e) {
            if (e.key === 'Enter') sendMessage();
        }

        // Запускаем циклы обновления
        setInterval(loadChatList, 5000); // Обновляем список чатов каждые 5 сек
        setInterval(loadMessages, 2000); // Обновляем текущие сообщения каждые 2 сек
        
        loadChatList(); // Первичная загрузка
    </script>
</body>
</html>