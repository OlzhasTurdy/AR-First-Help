<?php
$host = "localhost";
$db_name = "unity_app";
$username = "root"; // Обычно root
$password = "8520";

try {
    $pdo = new PDO("mysql:host=$host;dbname=$db_name;charset=utf8", $username, $password);
    $pdo->setAttribute(PDO::ATTR_ERRMODE, PDO::ERRMODE_EXCEPTION);
} catch (PDOException $e) {
    die("Ошибка подключения: " . $e->getMessage());
}

// Получаем данные из запроса (поддерживаем и GET для проверки, и POST для действий)
$user_id = isset($_REQUEST['user_id']) ? (int)$_REQUEST['user_id'] : 0;
$scenario_id = isset($_REQUEST['scenario_id']) ? (int)$_REQUEST['scenario_id'] : 0;
$action = isset($_REQUEST['action']) ? $_REQUEST['action'] : '';

if ($user_id <= 0 || $scenario_id <= 0) {
    die("Некорректные ID");
}

// 1. ПРОВЕРКА (Нужна для StartCoroutine в Unity)
if ($action == "check") {
    $stmt = $pdo->prepare("SELECT id FROM likes WHERE user_id = ? AND scenario_id = ?");
    $stmt->execute([$user_id, $scenario_id]);
    
    if ($stmt->fetch()) {
        echo "1"; // Лайкнуто
    } else {
        echo "0"; // Не лайкнуто
    }
}

// 2. ПОСТАВИТЬ ЛАЙК
else if ($action == "like") {
    // Используем INSERT IGNORE, чтобы не было ошибки при дубликате
    $stmt = $pdo->prepare("INSERT IGNORE INTO likes (user_id, scenario_id) VALUES (?, ?)");
    $stmt->execute([$user_id, $scenario_id]);
    
    // Опционально: обновляем счетчик в таблице custom_scenarios для быстрой выборки
    if ($stmt->rowCount() > 0) {
        $update = $pdo->prepare("UPDATE custom_scenarios SET likes = likes + 1 WHERE id = ?");
        $update->execute([$scenario_id]);
    }
    echo "success_liked";
}

// 3. УБРАТЬ ЛАЙК
else if ($action == "unlike") {
    $stmt = $pdo->prepare("DELETE FROM likes WHERE user_id = ? AND scenario_id = ?");
    $stmt->execute([$user_id, $scenario_id]);
    
    // Если запись была удалена, уменьшаем счетчик
    if ($stmt->rowCount() > 0) {
        $update = $pdo->prepare("UPDATE custom_scenarios SET likes = GREATEST(0, likes - 1) WHERE id = ?");
        $update->execute([$scenario_id]);
    }
    echo "success_unliked";
}
?>