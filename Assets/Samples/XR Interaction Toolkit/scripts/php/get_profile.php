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

    // 1. Получаем основные данные пользователя
    // Если столбцов role или profile_pic_url нет в вашей базе, 
    // PDO может выдать ошибку. Вы можете их убрать из SELECT, если их нет.
    $stmtUser = $pdo->prepare("
        SELECT username, role, profile_pic_url 
        FROM users 
        WHERE id = ?
    ");
    $stmtUser->execute([$userId]);
    $userData = $stmtUser->fetch(PDO::FETCH_ASSOC);

    if (!$userData) {
        echo json_encode(['success' => false, 'message' => 'User not found']);
        exit;
    }

    // 2. Считаем ОБЩЕЕ КОЛИЧЕСТВО ЛАЙКОВ, которые поставил этот пользователь
    $stmtLikes = $pdo->prepare("SELECT COUNT(*) FROM likes WHERE user_id = ?");
    $stmtLikes->execute([$userId]);
    $likesCount = (int)$stmtLikes->fetchColumn();

    // 3. Считаем КОЛИЧЕСТВО ПРОЙДЕННЫХ СЦЕНАРИЕВ из scenario_history
    $stmtFinished = $pdo->prepare("SELECT COUNT(*) FROM scenario_history WHERE user_id = ?");
    $stmtFinished->execute([$userId]);
    $finishedCount = (int)$stmtFinished->fetchColumn();

    // Возвращаем JSON
    echo json_encode([
        'success'         => true,
        'username'        => $userData['username'] ?? 'User',
        'role'            => $userData['role'] ?? 'user',
        'profile_pic_url' => $userData['profile_pic_url'] ?? '',
        'likes'           => $likesCount,
        'finished'        => $finishedCount
    ]);

} catch (PDOException $e) {
    // В случае, если столбцов role или profile_pic_url нет, попробуем запросить только username
    if (strpos($e->getMessage(), "Unknown column") !== false) {
        $stmtUser = $pdo->prepare("SELECT username FROM users WHERE id = ?");
        $stmtUser->execute([$userId]);
        $userData = $stmtUser->fetch(PDO::FETCH_ASSOC);
        
        $stmtLikes = $pdo->prepare("SELECT COUNT(*) FROM likes WHERE user_id = ?");
        $stmtLikes->execute([$userId]);
        $likesCount = (int)$stmtLikes->fetchColumn();
        
        $stmtFinished = $pdo->prepare("SELECT COUNT(*) FROM scenario_history WHERE user_id = ?");
        $stmtFinished->execute([$userId]);
        $finishedCount = (int)$stmtFinished->fetchColumn();

        echo json_encode([
            'success'         => true,
            'username'        => $userData['username'] ?? 'User',
            'role'            => 'user',
            'profile_pic_url' => '',
            'likes'           => $likesCount,
            'finished'        => $finishedCount
        ]);
    } else {
        echo json_encode(['success' => false, 'message' => 'DB Error: ' . $e->getMessage()]);
    }
}
?>
