<?php
header('Content-Type: application/json; charset=utf-8');
header('Access-Control-Allow-Origin: *');

// --- DB CONFIG ---
$host = '89.218.15.206';
$db   = 'unity_app';
$user = 'root';
$pass = '8520';

$userId = (int)($_POST['user_id'] ?? 0);

if ($userId === 0) {
    echo json_encode(['success' => false, 'message' => 'User ID required']);
    exit;
}

try {
    $pdo = new PDO("mysql:host=$host;dbname=$db;charset=utf8", $user, $pass);
    $pdo->setAttribute(PDO::ATTR_ERRMODE, PDO::ERRMODE_EXCEPTION);

    // 1. Считаем количество лайков
    $stmtLikes = $pdo->prepare("SELECT COUNT(*) FROM likes WHERE user_id = ?");
    $stmtLikes->execute([$userId]);
    $likesCount = (int)$stmtLikes->fetchColumn();

    // 2. Считаем количество пройденных сценариев
    $stmtFinished = $pdo->prepare("SELECT COUNT(*) FROM scenario_history WHERE user_id = ?");
    $stmtFinished->execute([$userId]);
    $finishedCount = (int)$stmtFinished->fetchColumn();

    echo json_encode([
        'success'  => true,
        'likes'    => $likesCount,
        'finished' => $finishedCount
    ]);

} catch (PDOException $e) {
    echo json_encode(['success' => false, 'message' => 'DB Error: ' . $e->getMessage()]);
}
?>
