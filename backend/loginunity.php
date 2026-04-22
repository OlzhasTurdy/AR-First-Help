<?php
header("Content-Type: application/json");

$conn = new mysqli("localhost", "root", "8520", "unity_app");

$data = json_decode(file_get_contents("php://input"), true);

$email = $data["email"];
$password = $data["password"];

$stmt = $conn->prepare("SELECT id, username FROM users WHERE email = ? AND password = ?");
$stmt->bind_param("ss", $email, $password);
$stmt->execute();
$result = $stmt->get_result();

if ($result->num_rows === 0) {
    echo json_encode([
        "success" => false,
        "message" => "Invalid credentials"
    ]);
    exit();
}

$user = $result->fetch_assoc();

echo json_encode([
    "success" => true,
    "userId" => $user["id"],
    "username" => $user["username"]
]);
?>
