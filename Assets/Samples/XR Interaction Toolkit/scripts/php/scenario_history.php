<?php
// ============================================================
//  scenario_history.php
//  GET  ?action=get&user_id=X          — история пользователя
//  GET  ?action=check&user_id=X&scenario_id=Y — прошёл ли юзер
//  POST action=record                   — записать прохождение
// ============================================================

header('Content-Type: application/json; charset=utf-8');
header('Access-Control-Allow-Origin: *');

// --- DB CONFIG (замените на свои данные) ---
$host = '89.218.15.206';
$db   = 'unity_app';
$user = 'root';
$pass = '8520';

try {
    $pdo = new PDO("mysql:host=$host;dbname=$db;charset=utf8", $user, $pass);
    $pdo->setAttribute(PDO::ATTR_ERRMODE, PDO::ERRMODE_EXCEPTION);
} catch (PDOException $e) {
    echo json_encode(['success' => false, 'error' => 'DB connection failed']);
    exit;
}

// --- Создаём таблицу, если не существует ---
$pdo->exec("
    CREATE TABLE IF NOT EXISTS scenario_history (
        id            INT AUTO_INCREMENT PRIMARY KEY,
        user_id       INT NOT NULL,
        scenario_id   INT NOT NULL,
        completed_at  DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
        steps_total   INT NOT NULL DEFAULT 0,
        steps_done    INT NOT NULL DEFAULT 0,
        INDEX idx_user   (user_id),
        INDEX idx_sc_usr (user_id, scenario_id)
    ) ENGINE=InnoDB DEFAULT CHARSET=utf8;
");

// Добавляем столбец scenario_name если его ещё нет (совместимо со старым MySQL)
$colCheck = $pdo->prepare("
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = ? AND TABLE_NAME = 'scenario_history' AND COLUMN_NAME = 'scenario_name'
");
$colCheck->execute([$db]);
if ((int)$colCheck->fetchColumn() === 0) {
    $pdo->exec("ALTER TABLE scenario_history ADD COLUMN scenario_name VARCHAR(255) NOT NULL DEFAULT '';");
}

$method = $_SERVER['REQUEST_METHOD'];

// ===========================================================
//  GET ?action=get&user_id=X  — все прохождения пользователя
// ===========================================================
if ($method === 'GET' && ($_GET['action'] ?? '') === 'get') {
    $userId = (int)($_GET['user_id'] ?? 0);
    if ($userId === 0) {
        echo json_encode(['success' => false, 'error' => 'user_id required']);
        exit;
    }

    $stmt = $pdo->prepare("
        SELECT h.id,
               h.scenario_id,
               CASE 
                   WHEN s.scenario_name IS NOT NULL AND s.scenario_name != '' THEN s.scenario_name
                   WHEN h.scenario_name != '' THEN h.scenario_name
                   ELSE CONCAT('Сценарий #', h.scenario_id)
               END AS scenario_name,
               h.completed_at,
               h.steps_total,
               h.steps_done
        FROM   scenario_history h
        LEFT JOIN custom_scenarios s ON h.scenario_id = s.id
        WHERE  h.user_id = ?
        ORDER BY h.completed_at DESC
        LIMIT 100
    ");
    $stmt->execute([$userId]);
    $rows = $stmt->fetchAll(PDO::FETCH_ASSOC);

    echo json_encode(['success' => true, 'history' => $rows]);
    exit;
}

// ===========================================================
//  GET ?action=check&user_id=X&scenario_id=Y — прошёл ли юзер
// ===========================================================
if ($method === 'GET' && ($_GET['action'] ?? '') === 'check') {
    $userId     = (int)($_GET['user_id']     ?? 0);
    $scenarioId = (int)($_GET['scenario_id'] ?? 0);
    if ($userId === 0 || $scenarioId === 0) {
        echo json_encode(['success' => false, 'error' => 'user_id and scenario_id required']);
        exit;
    }

    $stmt = $pdo->prepare("
        SELECT COUNT(*) AS cnt
        FROM   scenario_history
        WHERE  user_id = ? AND scenario_id = ?
    ");
    $stmt->execute([$userId, $scenarioId]);
    $row = $stmt->fetch(PDO::FETCH_ASSOC);

    echo json_encode([
        'success'   => true,
        'completed' => (int)$row['cnt'] > 0,
        'count'     => (int)$row['cnt']
    ]);
    exit;
}

// ===========================================================
//  POST action=record — записать прохождение
// ===========================================================
if ($method === 'POST') {
    $userId       = (int)($_POST['user_id']       ?? 0);
    $scenarioId   = (int)($_POST['scenario_id']   ?? 0);
    $scenarioName =       $_POST['scenario_name'] ?? '';
    $stepsTotal   = (int)($_POST['steps_total']   ?? 0);
    $stepsDone    = (int)($_POST['steps_done']    ?? $stepsTotal);

    if ($userId === 0 || $scenarioId === 0) {
        echo json_encode(['success' => false, 'error' => 'user_id and scenario_id required']);
        exit;
    }

    $stmt = $pdo->prepare("
        INSERT INTO scenario_history (user_id, scenario_id, scenario_name, steps_total, steps_done)
        VALUES (?, ?, ?, ?, ?)
    ");
    $stmt->execute([$userId, $scenarioId, $scenarioName, $stepsTotal, $stepsDone]);

    echo json_encode(['success' => true, 'insert_id' => (int)$pdo->lastInsertId()]);
    exit;
}

echo json_encode(['success' => false, 'error' => 'Unknown request']);
