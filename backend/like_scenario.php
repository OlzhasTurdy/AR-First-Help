<?php
// like_scenario.php
header('Content-Type: application/json; charset=utf-8');

$host = 'localhost';
$db   = 'unity_app';
$user = 'root';
$pass = '8520';

$conn = new mysqli($host, $user, $pass, $db);
if ($conn->connect_error) {
    die(json_encode(["status" => "error", "message" => "DB Connection failed"]));
}

$user_id = isset($_POST['user_id']) ? (int)$_POST['user_id'] : 0;
$scenario_id = isset($_POST['scenario_id']) ? (int)$_POST['scenario_id'] : 0;

if ($user_id <= 0 || $scenario_id <= 0) {
    echo json_encode(["status" => "error", "message" => "Invalid IDs. User: $user_id, Scenario: $scenario_id"]);
    exit;
}

// Проверяем, есть ли уже лайк
$check = $conn->prepare("SELECT id FROM likes WHERE user_id = ? AND scenario_id = ?");
$check->bind_param("ii", $user_id, $scenario_id);
$check->execute();
$res = $check->get_result();

if ($res->num_rows > 0) {
    // Лайк есть — удаляем
    $del = $conn->prepare("DELETE FROM likes WHERE user_id = ? AND scenario_id = ?");
    $del->bind_param("ii", $user_id, $scenario_id);
    $del->execute();
    echo json_encode(["status" => "unliked"]);
} else {
    // Лайка нет — добавляем
    $ins = $conn->prepare("INSERT INTO likes (user_id, scenario_id) VALUES (?, ?)");
    $ins->bind_param("ii", $user_id, $scenario_id);
    if ($ins->execute()) {
        echo json_encode(["status" => "liked"]);
    } else {
        echo json_encode(["status" => "error", "message" => $conn->error]);
    }
}
$conn->close();
?>