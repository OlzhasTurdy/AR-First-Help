<?php
// Указываем, что сервер возвращает JSON
header('Content-Type: application/json; charset=utf-8');

// 1. НАСТРОЙКИ ПОДКЛЮЧЕНИЯ К БАЗЕ ДАННЫХ
// (Замените на ваши реальные данные от хостинга)
$servername = "localhost";
$username = "root"; 
$password = "8520";
$dbname = "unity_app";

// 2. ПОЛУЧАЕМ user_id ОТ UNITY
// Проверяем, пришел ли POST-запрос с полем user_id. Если нет, ставим 0.
$user_id = isset($_POST['user_id']) ? intval($_POST['user_id']) : 0;

// Если user_id некорректный, просто возвращаем пустой список
if ($user_id <= 0) {
    echo json_encode(["items" => []]);
    exit;
}

// 3. ПОДКЛЮЧАЕМСЯ К БАЗЕ
$conn = new mysqli($servername, $username, $password, $dbname);
$conn->set_charset("utf8mb4"); // Чтобы русские символы отображались корректно

if ($conn->connect_error) {
    die(json_encode(["error" => "Ошибка подключения: " . $conn->connect_error]));
}

// 4. ИЩЕМ СЦЕНАРИИ ПОЛЬЗОВАТЕЛЯ
// Используем подготовленные выражения (prepare) — это ВАЖНО для защиты от взлома (SQL-инъекций)
$stmt = $conn->prepare("SELECT id, scenario_name, json_data FROM scenarios WHERE user_id = ?");
$stmt->bind_param("i", $user_id); // "i" означает integer (целое число)
$stmt->execute();
$result = $stmt->get_result();

$items = array();

if ($result->num_rows > 0) {
    // Проходимся по всем найденным строкам и добавляем их в массив
    while($row = $result->fetch_assoc()) {
        $items[] = array(
            "id" => intval($row["id"]),
            "scenario_name" => $row["scenario_name"],
            "json_data" => $row["json_data"]
            "likes" => intval($row["likes"]),   // Добавляем это
            "views" => intval($row["views"])    // И это
        );
    }
}

// Закрываем соединение
$stmt->close();
$conn->close();

// 5. ОТПРАВЛЯЕМ ОТВЕТ В UNITY
// Упаковываем массив в объект с ключом "items", чтобы Unity смог это распарсить
echo json_encode(["items" => $items]);
?>