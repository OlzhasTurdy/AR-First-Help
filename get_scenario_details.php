<?php
$host = 'localhost';
$db   = 'unity_app';
$user = 'root';
$pass = '8520';

$conn = new mysqli($host, $user, $pass, $db);
$scenario_id = (int)$_GET['id'];

// 1. Получаем инфо о сценарии и авторе
$info_sql = "SELECT s.scenario_name, s.likes, s.views, u.username 
             FROM custom_scenarios s 
             JOIN users u ON s.user_id = u.id 
             WHERE s.id = $scenario_id";
$info_res = $conn->query($info_sql);
$info = $info_res->fetch_assoc();

// 2. Получаем список комментариев
$comm_sql = "SELECT c.comment_text, u.username 
             FROM comments c 
             JOIN users u ON c.user_id = u.id 
             WHERE c.scenario_id = $scenario_id 
             ORDER BY c.created_at DESC";
$comm_res = $conn->query($comm_sql);
$comments = array();
while($row = $comm_res->fetch_assoc()) {
    $comments[] = $row;
}

// Отдаем всё одним объектом
echo json_encode(array("info" => $info, "comments" => $comments));
$conn->close();
?>