<?php
$conn = new mysqli('localhost', 'root', '8520', 'unity_app');
if ($conn->connect_error) die("Connection failed: " . $conn->connect_error);

if (isset($_POST['scenario_id']) && isset($_POST['user_id']) && isset($_POST['comment_text'])) {
    $scenario_id = (int)$_POST['scenario_id'];
    $user_id = (int)$_POST['user_id'];
    $text = $_POST['comment_text'];

    $stmt = $conn->prepare("INSERT INTO comments (scenario_id, user_id, comment_text) VALUES (?, ?, ?)");
    $stmt->bind_param("iis", $scenario_id, $user_id, $text);
    
    if($stmt->execute()) {
        echo "Success";
    } else {
        echo "Error: " . $stmt->error;
    }
    $stmt->close();
} else {
    echo "Missing data";
}
$conn->close();
?>