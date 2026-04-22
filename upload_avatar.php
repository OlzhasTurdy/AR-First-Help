<?php
$servername = "localhost";
$username = "root";
$password = "8520";
$dbname = "unity_app";

$conn = new mysqli($servername, $username, $password, $dbname);

// Проверяем, переданы ли данные
if (!isset($_POST['user_id']) || !isset($_FILES['avatar'])) {
    die(json_encode(["success" => false, "message" => "Missing data"]));
}

$user_id = intval($_POST['user_id']);
$image = $_FILES['avatar'];

$upload_dir = 'avatars/';
if (!file_exists($upload_dir)) {
    mkdir($upload_dir, 0777, true);
}

// Генерируем имя файла на основе ID пользователя
$file_extension = pathinfo($image['name'], PATHINFO_EXTENSION);
$file_name = "user_" . $user_id . "." . $file_extension;
$target_path = $upload_dir . $file_name;

if (move_uploaded_file($image['tmp_name'], $target_path)) {
    // Сохраняем в БД только имя файла (например, user_1.jpg)
    // Это гибче, так как домен может измениться
    $stmt = $conn->prepare("UPDATE users SET profile_pic_url = ? WHERE id = ?");
    $stmt->bind_param("si", $file_name, $user_id);
    
    if ($stmt->execute()) {
        $full_url = "https://autoreduce.kz/" . $target_path;
        echo json_encode(["success" => true, "url" => $full_url]);
    } else {
        echo json_encode(["success" => false, "message" => "DB Error"]);
    }
    $stmt->close();
} else {
    echo json_encode(["success" => false, "message" => "Upload failed"]);
}

$conn->close();
?>