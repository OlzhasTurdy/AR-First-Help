<?php
header("Content-Type: application/json");

$conn = new mysqli("localhost", "root", "8520", "unity_app");

$data = json_decode(file_get_contents("php://input"), true);

$username = $data["username"];
$email = $data["email"];
$password = $data["password"];

if (!$username || !$email || !$password) {
    echo json_encode([
        "success" => false,
        "message" => "All fields required"
    ]);
    exit();
}

$check = $conn->prepare("SELECT id FROM users WHERE email = ?");
$check->bind_param("s", $email);
$check->execute();
$check->store_result();

if ($check->num_rows > 0) {
    echo json_encode([
        "success" => false,
        "message" => "User already exists"
    ]);
    exit();
}

$stmt = $conn->prepare("INSERT INTO users (username, email, password) VALUES (?, ?, ?)");
$stmt->bind_param("sss", $username, $email, $password);
$stmt->execute();

echo json_encode([
    "success" => true,
    "userId" => $stmt->insert_id,
    "username" => $username
]);
?>
