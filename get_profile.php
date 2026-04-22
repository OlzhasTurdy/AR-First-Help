<?php
header('Content-Type: application/json; charset=utf-8');

$servername = "localhost";
$username = "root";
$password = "8520";
$dbname = "unity_app";

$user_id = isset($_POST['user_id']) ? intval($_POST['user_id']) : 0;

if ($user_id <= 0) {
    echo json_encode(["success" => false, "message" => "Invalid User ID"]);
    exit;
}

$conn = new mysqli($servername, $username, $password, $dbname);
$conn->set_charset("utf8mb4");

if ($conn->connect_error) {
    die(json_encode(["success" => false, "message" => "Connection failed"]));
}

// Достаем всю нужную информацию о пользователе
$stmt = $conn->prepare("SELECT username, role, likes, finished, profile_pic_url FROM users WHERE id = ?");
$stmt->bind_param("i", $user_id);
$stmt->execute();
$result = $stmt->get_result();

if ($result->num_rows > 0) {
    $row = $result->fetch_assoc();
    
    // Определяем имя файла из БД
    $fileName = $row["profile_pic_url"];
    
    // Если в базе пусто или NULL — принудительно ставим default.png
    if (empty($fileName)) {
        $fileName = "default.png";
    }

    // Собираем полную рабочую ссылку для Unity
    // Теперь Unity получит: https://autoreduce.kz/avatars/default.png 
    // вместо просто "default.png"
    $full_avatar_url = "https://autoreduce.kz/avatars/" . $fileName;

    echo json_encode([
        "success" => true,
        "username" => $row["username"],
        "role" => $row["role"],
        "likes" => intval($row["likes"]),
        "finished" => intval($row["finished"]),
        "profile_pic_url" => $full_avatar_url 
    ]);
} else {
    echo json_encode(["success" => false, "message" => "User not found"]);
}

$stmt->close();
$conn->close();
?>