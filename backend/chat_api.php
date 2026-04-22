<?php
// chat_api.php
require 'db1.php';
header('Content-Type: application/json');

$action = $_GET['action'] ?? $_POST['action'] ?? '';

// Получение списка всех чатов (для админа)
if ($action === 'get_chats') {
    $stmt = $pdo->query("SELECT id, user_email, status, DATE_FORMAT(created_at, '%d.%m %H:%i') as date FROM support_chats ORDER BY status DESC, id DESC");
    $chats = $stmt->fetchAll();
    echo json_encode($chats);
    exit;
}

// Получение сообщений конкретного чата
if ($action === 'get_messages') {
    $chat_id = (int)($_GET['chat_id'] ?? 0);
    
    $stmt = $pdo->prepare("SELECT sender, message, DATE_FORMAT(created_at, '%H:%i') as time FROM support_messages WHERE chat_id = ? ORDER BY id ASC");
    $stmt->execute([$chat_id]);
    $messages = $stmt->fetchAll();
    
    echo json_encode($messages);
    exit;
}

// Отправка нового сообщения
if ($action === 'send_message') {
    $chat_id = (int)($_POST['chat_id'] ?? 0);
    $sender = $_POST['sender'] ?? 'user'; // 'user' или 'admin'
    $message = trim($_POST['message'] ?? '');
    
    if (!empty($message) && $chat_id > 0) {
        $stmt = $pdo->prepare("INSERT INTO support_messages (chat_id, sender, message) VALUES (?, ?, ?)");
        $stmt->execute([$chat_id, $sender, $message]);
        echo json_encode(['status' => 'success']);
    } else {
        echo json_encode(['status' => 'error', 'msg' => 'Пустое сообщение или нет ID чата']);
    }
    exit;
}
?>