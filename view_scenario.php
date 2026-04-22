<?php
$servername = "localhost";
$username = "root";
$password = "8520";
$dbname = "unity_app";

$conn = new mysqli($servername, $username, $password, $dbname);

$scenario_id = isset($_POST['scenario_id']) ? intval($_POST['scenario_id']) : 0;

if ($scenario_id > 0) {
    // Увеличиваем счетчик просмотров на 1
    $conn->query("UPDATE custom_scenarios SET views = views + 1 WHERE id = $scenario_id");
    echo json_encode(["success" => true]);
}
$conn->close();
?>