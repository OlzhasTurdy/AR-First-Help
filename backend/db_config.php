<?php
$servername = "localhost";
$username = "root";
$password = "8520"; // Твой пароль из прошлых скриптов
$dbname = "unity_app";

// Создаем подключение
$conn = new mysqli($servername, $username, $password, $dbname);

// Проверка подключения
if ($conn->connect_error) {
    die("Connection failed: " . $conn->connect_error);
}

// Устанавливаем кодировку UTF8, чтобы кириллица не превращалась в знаки вопроса
$conn->set_charset("utf8");
?>