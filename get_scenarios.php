<?php
header('Content-Type: application/json; charset=utf-8');

$host = 'localhost';
$db   = 'unity_app';
$user = 'root';
$pass = '8520';

$conn = new mysqli($host, $user, $pass, $db);
$conn->set_charset("utf8mb4");

if ($conn->connect_error) {
    die(json_encode(array("error" => "Connection failed: " . $conn->connect_error)));
}

// Получаем ID пользователя из GET запроса (если он есть)
$current_user_id = isset($_GET['user_id']) ? (int)$_GET['user_id'] : 0;

// Сложный запрос:
// COUNT(DISTINCT l.id) - считаем уникальные лайки из таблицы likes
// MAX(IF(l.user_id = ?, 1, 0)) - проверяем, есть ли среди лайкающих наш user_id
$sql = "SELECT 
            s.id, 
            s.scenario_name, 
            s.json_data, 
            s.views,
            COUNT(DISTINCT l.id) AS likes_count,
            MAX(IF(l.user_id = ?, 1, 0)) AS is_liked_by_me
        FROM custom_scenarios s
        LEFT JOIN likes l ON s.id = l.scenario_id
        GROUP BY s.id
        ORDER BY s.created_at DESC";

$stmt = $conn->prepare($sql);
$stmt->bind_param("i", $current_user_id);
$stmt->execute();
$result = $stmt->get_result();

$scenarios = array();

while($row = $result->fetch_assoc()) {
    $scenarios[] = array(
        "id" => (int)$row['id'],
        "scenario_name" => $row['scenario_name'],
        "json_data" => $row['json_data'],
        "views" => (int)$row['views'],
        "likes" => (int)$row['likes_count'],
        "isLiked" => (bool)$row['is_liked_by_me']
    );
}

echo json_encode(array("items" => $scenarios));

$stmt->close();
$conn->close();
?>