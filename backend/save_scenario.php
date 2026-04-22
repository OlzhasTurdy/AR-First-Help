<?php
// Настройки подключения к БД
$host = 'localhost';
$db   = 'unity_app';
$user = 'root';
$pass = '8520';

// Подключение к БД
$conn = new mysqli($host, $user, $pass, $db);
if ($conn->connect_error) {
    die("Connection failed: " . $conn->connect_error);
}

// ИЗМЕНЕНИЕ 1: Проверяем, пришел ли не только JSON, но и user_id
if (isset($_POST['scenario_json']) && isset($_POST['user_id'])) {
    
    // Получаем данные
    $json_string = $_POST['scenario_json'];
    
    // ИЗМЕНЕНИЕ 2: Забираем user_id и приводим его к числу (int) для безопасности
    $user_id = (int)$_POST['user_id']; 
    
    // Декодируем JSON, чтобы вытащить scenarioName
    $data = json_decode($json_string, true);
    
    if ($data && isset($data['scenarioName'])) {
        $name = $data['scenarioName'];
        
        // ИЗМЕНЕНИЕ 3: Добавляем user_id в SQL запрос
        $stmt = $conn->prepare("INSERT INTO custom_scenarios (user_id, scenario_name, json_data) VALUES (?, ?, ?)");
        
        // ИЗМЕНЕНИЕ 4: Меняем "ss" на "iss" 
        // i = integer (для user_id), s = string (для name), s = string (для json_data)
        $stmt->bind_param("iss", $user_id, $name, $json_string);
        
        if ($stmt->execute()) {
            echo "Success";
        } else {
            echo "Error: " . $stmt->error;
        }
        $stmt->close();
    } else {
        echo "Error: Invalid JSON format";
    }
} else {
    // ИЗМЕНЕНИЕ 5: Уточняем текст ошибки
    echo "Error: No data received or user_id missing"; 
}

$conn->close();
?>