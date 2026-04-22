<?php
$conn = new mysqli('localhost', 'root', '8520', 'unity_app');
$id = (int)$_GET['id'];

// Получаем инфо о сценарии + имя автора через JOIN
$sql = "SELECT s.scenario_name, s.likes, s.views, u.username 
        FROM custom_scenarios s 
        JOIN users u ON s.user_id = u.id 
        WHERE s.id = ?";

$stmt = $conn->prepare($sql);
$stmt->bind_param("i", $id);
$stmt->execute();
echo json_encode($stmt->get_result()->fetch_assoc());
?>