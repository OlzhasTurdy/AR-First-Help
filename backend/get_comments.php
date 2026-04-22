<?php
$conn = new mysqli('localhost', 'root', '8520', 'unity_app');
if ($conn->connect_error) die("Connection failed: " . $conn->connect_error);

$scenario_id = isset($_GET['scenario_id']) ? (int)$_GET['scenario_id'] : 0;

// JOIN с таблицей users, чтобы достать username
$sql = "SELECT c.id, c.comment_text, c.created_at, u.username 
        FROM comments c 
        JOIN users u ON c.user_id = u.id 
        WHERE c.scenario_id = ? 
        ORDER BY c.created_at DESC";

$stmt = $conn->prepare($sql);
$stmt->bind_param("i", $scenario_id);
$stmt->execute();
$result = $stmt->get_result();

$comments = array();
while($row = $result->fetch_assoc()) {
    $comments[] = $row;
}

echo json_encode(array("items" => $comments));
$stmt->close();
$conn->close();
?>