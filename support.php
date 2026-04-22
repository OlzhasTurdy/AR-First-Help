<?php
// index.php
require 'db1.php';
$msg = '';

if ($_SERVER["REQUEST_METHOD"] == "POST") {
    $email = $_POST['email'];
    $subject = $_POST['subject'];
    $message = $_POST['message'];

    $stmt = $pdo->prepare("INSERT INTO support_tickets (user_email, subject, message) VALUES (?, ?, ?)");
    if ($stmt->execute([$email, $subject, $message])) {
        $msg = "Ваше сообщение успешно отправлено! Мы ответим вам на email.";
    } else {
        $msg = "Ошибка при отправке сообщения.";
    }
}
?>

<!DOCTYPE html>
<html lang="ru">
<head>
    <meta charset="UTF-8">
    <title>Служба поддержки</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 40px; max-width: 600px; }
        input, textarea { width: 100%; padding: 10px; margin: 10px 0; box-sizing: border-box; }
        button { padding: 10px 20px; background-color: #007bff; color: white; border: none; cursor: pointer; }
        .success { color: green; font-weight: bold; }
    </style>
</head>
<body>
    <h2>Служба поддержки приложения</h2>
    <?php if($msg): ?>
        <p class="success"><?= htmlspecialchars($msg) ?></p>
    <?php endif; ?>
    
    <form method="POST" action="">
        <label>Ваш Email (указанный при регистрации):</label>
        <input type="email" name="email" required placeholder="example@mail.com">
        
        <label>Тема обращения:</label>
        <input type="text" name="subject" required placeholder="Например: Проблема с авторизацией">
        
        <label>Сообщение:</label>
        <textarea name="message" rows="5" required placeholder="Опишите вашу проблему..."></textarea>
        
        <button type="submit">Отправить</button>
    </form>
</body>
</html>