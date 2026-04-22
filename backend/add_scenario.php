<?php
// Подключаем базу данных
require_once 'db_config.php';

// Проверяем, пришли ли данные
if (!isset($_POST['user_id'])) {
    die("Error: No user_id provided.");
}

$user_id = (int)$_POST['user_id'];

// Проверяем роль в базе данных
$check_sql = "SELECT role FROM users WHERE id = $user_id";
$result = $conn->query($check_sql);

if ($result->num_rows > 0) {
    $user = $result->fetch_assoc();

    if ($user['role'] !== 'root') {
        die("Error: Access denied. Only root can create scenarios.");
    }
} else {
    die("Error: User not found.");
}

// Если код дошел сюда — значит пользователь root.
// Здесь идет твой старый код записи сценария (INSERT INTO custom_scenarios...)
echo "Access granted. Saving scenario...";
?>