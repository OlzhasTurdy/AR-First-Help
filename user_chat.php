<?php
ini_set('display_errors', 1);
ini_set('display_startup_errors', 1);
error_reporting(E_ALL);

require 'db1.php';

$user_email = $_GET['email'] ?? 'test_user@example.com'; 

$stmt = $pdo->prepare("SELECT id FROM support_chats WHERE user_email = ? AND status = 'open' LIMIT 1");
$stmt->execute([$user_email]);
$chat = $stmt->fetch();

if (!$chat) {
    $stmt = $pdo->prepare("INSERT INTO support_chats (user_email) VALUES (?)");
    $stmt->execute([$user_email]);
    $chat_id = $pdo->lastInsertId();
} else {
    $chat_id = $chat['id'];
}
?>
<!DOCTYPE html>
<html lang="ru">
<head>
    <meta charset="UTF-8">
    <title>Поддержка Smart Tutor</title>
    <style>
        body { font-family: Arial, sans-serif; background: #f0f2f5; display: flex; justify-content: center; padding-top: 50px; }
        .chat-container { width: 400px; background: white; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); padding: 20px; }
        #chat-box { height: 350px; overflow-y: auto; display: flex; flex-direction: column; margin-bottom: 15px; padding-right: 5px; }
        .msg { padding: 8px 12px; border-radius: 15px; margin-bottom: 8px; max-width: 80%; word-wrap: break-word; }
        .msg.user { background: #0084ff; color: white; align-self: flex-end; border-bottom-right-radius: 2px; }
        .msg.admin { background: #e4e6eb; color: black; align-self: flex-start; border-bottom-left-radius: 2px; }
        .time { font-size: 0.7em; opacity: 0.7; display: block; margin-top: 5px; text-align: right; }
        .input-group { display: flex; gap: 10px; }
        input[type="text"] { flex: 1; padding: 10px; border: 1px solid #ccc; border-radius: 20px; outline: none; }
        button { padding: 10px 20px; border: none; background: #0084ff; color: white; border-radius: 20px; cursor: pointer; }
    </style>
</head>
<body>
    <div class="chat-container">
        <h3>Чат с поддержкой</h3>
        <div id="chat-box"></div>
        <div class="input-group">
            <input type="text" id="message-input" placeholder="Напишите сообщение..." onkeypress="handleEnter(event)">
            <button onclick="sendMessage()">Отправить</button>
        </div>
    </div>

    <script>
        const chatId = <?php echo $chat_id; ?>;
        const chatBox = document.getElementById('chat-box');
        const messageInput = document.getElementById('message-input');
        let lastMessageCount = 0;

        function loadMessages() {
            fetch(`chat_api.php?action=get_messages&chat_id=${chatId}`)
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

        function sendMessage() {
            const text = messageInput.value.trim();
            if (!text) return;

            const formData = new FormData();
            formData.append('action', 'send_message');
            formData.append('chat_id', chatId);
            formData.append('sender', 'user');
            formData.append('message', text);

            messageInput.value = '';
            
            fetch('chat_api.php', { method: 'POST', body: formData })
                .then(() => loadMessages());
        }

        function handleEnter(e) {
            if (e.key === 'Enter') sendMessage();
        }

        setInterval(loadMessages, 2000);
        loadMessages();
    </script>
</body>
</html>