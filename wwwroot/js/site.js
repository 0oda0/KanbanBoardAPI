// Конфигурация API
const API_BASE_URL = window.location.origin; // Базовый URL API (текущий домен)
const AUTH_TOKEN_KEY = 'kanban_auth_token'; // Ключ для хранения токена авторизации в localStorage

// DOM элементы
let authModal, taskModal; // Модальные окна для авторизации и задач

/**
 * Инициализация приложения при загрузке страницы
 */
document.addEventListener('DOMContentLoaded', () => {
    initModals(); // Инициализация модальных окон
    checkAuthStatus(); // Проверка статуса авторизации
    setupEventListeners(); // Настройка обработчиков событий
    initSortable(); // Инициализация drag-and-drop функционала
});

/**
 * Инициализация модальных окон с помощью Bootstrap
 */
function initModals() {
    authModal = new bootstrap.Modal('#authModal');
    taskModal = new bootstrap.Modal('#taskModal');
}

/**
 * Проверка статуса авторизации пользователя
 */
async function checkAuthStatus() {
    const token = localStorage.getItem(AUTH_TOKEN_KEY);
    if (token) {
        try {
            await fetchCurrentUser(); // Загрузка данных текущего пользователя
            await loadTasks(); // Загрузка задач пользователя
        } catch (error) {
            console.error('Ошибка проверки авторизации:', error);
            handleLogout(); // Выход при ошибке
        }
    }
}

/**
 * Настройка обработчиков событий для элементов интерфейса
 */
function setupEventListeners() {
    // Обработчики форм авторизации
    document.getElementById('loginForm')?.addEventListener('submit', handleLogin);
    document.getElementById('registerForm')?.addEventListener('submit', handleRegister);
    document.getElementById('logout-btn')?.addEventListener('click', handleLogout);

    // Обработчики для работы с задачами
    document.getElementById('saveTaskBtn')?.addEventListener('click', saveTask);
    document.getElementById('deleteTaskBtn')?.addEventListener('click', deleteTask);

    // Кнопки добавления задач в разных статусах
    document.querySelectorAll('.add-task-btn').forEach(btn => {
        btn.addEventListener('click', () => {
            const status = btn.dataset.status;
            openTaskModal(null, status); // Открытие модального окна для новой задачи
        });
    });
}

// ========== АВТОРИЗАЦИЯ ========== //

/**
 * Обработка входа пользователя
 * @param {Event} e - Событие отправки формы
 */
async function handleLogin(e) {
    e.preventDefault();
    const form = e.target;
    const formData = new FormData(form);

    try {
        showLoader(form);

        const response = await fetch(`${API_BASE_URL}/api/auth/login`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                username: formData.get('username').trim(),
                password: formData.get('password')
            })
        });

        if (!response.ok) {
            const errorData = await response.json().catch(() => ({}));
            throw new Error(errorData.message || 'Login failed');
        }

        const { data } = await response.json();
        localStorage.setItem(AUTH_TOKEN_KEY, data.token);

        authModal.hide();
        form.reset();

        await fetchCurrentUser();
        await loadTasks();

        showAlert('Login successful', 'success');
    } catch (error) {
        showAlert(error.message || 'Login failed', 'danger');
    } finally {
        hideLoader(form);
    }
}

/**
 * Обработка регистрации нового пользователя
 * @param {Event} e - Событие отправки формы
 */
async function handleRegister(e) {
    e.preventDefault();
    const form = e.target;
    const formData = new FormData(form);

    const userData = {
        username: formData.get('username').trim(),
        email: formData.get('email').trim(),
        password: formData.get('password'),
        roleId: 1
    };

    // Валидация данных
    if (!userData.username || !userData.email || !userData.password) {
        showAlert('Все поля обязательны для заполнения', 'danger');
        return;
    }

    if (userData.password.length < 6) {
        showAlert('Пароль должен содержать минимум 6 символов', 'danger');
        return;
    }

    try {
        showLoader(form);

        const response = await fetch(`${API_BASE_URL}/api/auth/register`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(userData)
        });

        const data = await parseResponse(response);

        if (!response.ok) {
            throw new Error(data.message || 'Ошибка регистрации');
        }

        showAlert('Регистрация успешна! Теперь войдите.', 'success');
        document.getElementById('login-tab').click(); // Переключение на вкладку входа
        form.reset();
    } catch (error) {
        showAlert(error.message, 'danger');
    } finally {
        hideLoader(form);
    }
}

/**
 * Выход пользователя из системы
 */
function handleLogout() {
    localStorage.removeItem(AUTH_TOKEN_KEY);
    document.getElementById('user-info').classList.add('d-none');
    document.getElementById('login-section').classList.remove('d-none');
    clearTasks();
    showAlert('Вы успешно вышли из системы', 'info');
}

/**
 * Загрузка данных текущего пользователя
 */
async function fetchCurrentUser() {
    try {
        const response = await fetch(`${API_BASE_URL}/api/auth/me`, {
            headers: getAuthHeaders()
        });

        if (!response.ok) {
            throw new Error('Failed to fetch user data');
        }

        const { data } = await response.json();

        if (!data) {
            throw new Error('Invalid user data received');
        }

        // Обновление UI с данными пользователя
        document.getElementById('username').textContent = data.username;
        document.getElementById('user-info').classList.remove('d-none');
        document.getElementById('login-section').classList.add('d-none');
    } catch (error) {
        console.error('Error fetching current user:', error);
        handleLogout();
        throw error;
    }
}

// ========== РАБОТА С ЗАДАЧАМИ ========== //

/**
 * Загрузка задач пользователя
 */
async function loadTasks() {
    try {
        const response = await fetch(`${API_BASE_URL}/api/tasks`, {
            headers: getAuthHeaders()
        });

        const data = await parseResponse(response);

        if (!data || !Array.isArray(data)) {
            throw new Error('Invalid tasks data received');
        }

        renderTasks(data);
    } catch (error) {
        console.error('Error loading tasks:', error);
        showAlert(error.message || 'Failed to load tasks', 'danger');
    }
}

/**
 * Отображение задач в соответствующих колонках
 * @param {Array} tasks - Массив задач
 */
function renderTasks(tasks) {
    clearTasks();

    tasks.forEach(task => {
        const container = document.querySelector(`.tasks-container[data-status="${task.status}"]`);
        if (container) {
            container.appendChild(createTaskElement(task));
        }
    });
}

/**
 * Создание DOM-элемента задачи
 * @param {Object} task - Объект задачи
 * @returns {HTMLElement} Элемент задачи
 */
function createTaskElement(task) {
    const taskElement = document.createElement('div');
    taskElement.className = 'card task-card mb-3';
    taskElement.dataset.taskId = task.taskId;

    const deadlineDate = task.deadline ? new Date(task.deadline) : null;
    const deadlineText = deadlineDate ?
        `Дедлайн: ${deadlineDate.toLocaleDateString()}` : 'Без дедлайна';

    taskElement.innerHTML = `
        <div class="card-body">
            <h5 class="card-title">${escapeHtml(task.title)}</h5>
            <p class="card-text">${escapeHtml(task.description || '')}</p>
            <div class="d-flex justify-content-between align-items-center">
                <small class="text-muted">${deadlineText}</small>
                <div>
                    <button class="btn btn-sm btn-outline-primary edit-btn">✏️</button>
                    <button class="btn btn-sm btn-outline-danger delete-btn">🗑️</button>
                </div>
            </div>
        </div>
    `;

    // Назначение обработчиков для кнопок редактирования и удаления
    taskElement.querySelector('.edit-btn').addEventListener('click', () => openTaskModal(task));
    taskElement.querySelector('.delete-btn').addEventListener('click', () => confirmDeleteTask(task.taskId));

    return taskElement;
}

/**
 * Открытие модального окна для создания/редактирования задачи
 * @param {Object|null} task - Объект задачи или null для новой задачи
 * @param {string|null} status - Статус для новой задачи
 */
function openTaskModal(task, status = null) {
    const isEdit = !!task;
    const form = document.getElementById('taskForm');

    document.getElementById('taskModalTitle').textContent = isEdit ? 'Редактировать задачу' : 'Новая задача';
    form.reset();

    if (isEdit) {
        document.getElementById('taskId').value = task.taskId;
        document.getElementById('taskTitle').value = task.title;
        document.getElementById('taskDescription').value = task.description || '';
        document.getElementById('taskStatus').value = task.status;

        if (task.deadline) {
            const deadline = new Date(task.deadline);
            document.getElementById('taskDeadline').value = deadline.toISOString().slice(0, 16);
        }
    } else if (status) {
        document.getElementById('taskStatus').value = status;
    }

    document.getElementById('deleteTaskBtn').classList.toggle('d-none', !isEdit);
    taskModal.show();
}

/**
 * Сохранение задачи (создание или обновление)
 */
async function saveTask() {
    const form = document.getElementById('taskForm');
    const taskId = document.getElementById('taskId').value;
    const isEdit = !!taskId;

    const taskData = {
        title: document.getElementById('taskTitle').value.trim(),
        description: document.getElementById('taskDescription').value.trim(),
        status: document.getElementById('taskStatus').value,
        deadline: document.getElementById('taskDeadline').value || null
    };

    // Валидация
    if (!taskData.title) {
        showAlert('Название задачи обязательно', 'danger');
        return;
    }

    try {
        showLoader(form);

        const url = `${API_BASE_URL}/api/tasks${isEdit ? `/${taskId}` : ''}`;
        const method = isEdit ? 'PUT' : 'POST';

        const response = await fetch(url, {
            method,
            headers: getAuthHeaders(),
            body: JSON.stringify(taskData)
        });

        const data = await parseResponse(response);

        if (!response.ok) {
            throw new Error(data.message || 'Ошибка сохранения задачи');
        }

        taskModal.hide();
        await loadTasks();
        showAlert(`Задача ${isEdit ? 'обновлена' : 'создана'} успешно`, 'success');
    } catch (error) {
        showAlert(error.message, 'danger');
    } finally {
        hideLoader(form);
    }
}

/**
 * Подтверждение удаления задачи
 * @param {string} taskId - ID задачи
 */
function confirmDeleteTask(taskId) {
    if (confirm('Вы уверены, что хотите удалить эту задачу?')) {
        deleteTask(taskId);
    }
}

/**
 * Удаление задачи
 * @param {string|null} taskId - ID задачи (если null, берется из формы)
 */
async function deleteTask(taskId = null) {
    const idToDelete = taskId || document.getElementById('taskId').value;
    if (!idToDelete) return;

    try {
        const response = await fetch(`${API_BASE_URL}/api/tasks/${idToDelete}`, {
            method: 'DELETE',
            headers: getAuthHeaders()
        });

        const data = await parseResponse(response);

        if (!response.ok) {
            throw new Error(data.message || 'Ошибка удаления задачи');
        }

        taskModal.hide();
        await loadTasks();
        showAlert('Задача удалена успешно', 'success');
    } catch (error) {
        showAlert(error.message, 'danger');
    }
}

// ========== SORTABLE DRAG AND DROP ========== //

/**
 * Инициализация функционала перетаскивания задач
 */
function initSortable() {
    const containers = document.querySelectorAll('.tasks-container');

    if (!containers.length) {
        console.warn('Не найдены контейнеры для задач');
        return;
    }

    containers.forEach(container => {
        new Sortable(container, {
            group: 'kanban',
            animation: 150,
            ghostClass: 'sortable-ghost',
            draggable: '.task-card',
            onEnd: async (evt) => {
                const taskId = evt.item.dataset.taskId;
                const newStatus = evt.to.dataset.status;

                try {
                    const response = await fetch(`${API_BASE_URL}/api/tasks/${taskId}/status`, {
                        method: 'PATCH',
                        headers: getAuthHeaders(),
                        body: JSON.stringify({ status: newStatus })
                    });

                    if (!response.ok) {
                        const errorData = await response.json().catch(() => ({}));
                        throw new Error(errorData.message || 'Ошибка перемещения задачи');
                        // Возвращаем задачу на место при ошибке
                        evt.from.insertBefore(evt.item, evt.oldIndex > evt.newIndex ?
                            evt.item.nextSibling : evt.item.previousSibling);
                    }
                } catch (error) {
                    console.error('Ошибка перемещения задачи:', error);
                    showAlert(error.message, 'danger');
                }
            }
        });
    });
}

// ========== ВСПОМОГАТЕЛЬНЫЕ ФУНКЦИИ ========== //

/**
 * Получение заголовков с токеном авторизации
 * @returns {Object} Заголовки для запросов
 */
function getAuthHeaders() {
    const token = localStorage.getItem(AUTH_TOKEN_KEY);
    if (!token) {
        handleLogout();
        throw new Error('Требуется авторизация');
    }
    return {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
    };
}

/**
 * Очистка всех задач из интерфейса
 */
function clearTasks() {
    document.querySelectorAll('.tasks-container').forEach(container => {
        container.innerHTML = '';
    });
}

/**
 * Показать уведомление
 * @param {string} message - Текст сообщения
 * @param {string} type - Тип сообщения (success, danger, info и т.д.)
 */
function showAlert(message, type) {
    const alertDiv = document.createElement('div');
    alertDiv.className = `alert alert-${type} alert-dismissible fade show position-fixed top-0 end-0 m-3`;
    alertDiv.style.zIndex = '1100';
    alertDiv.innerHTML = `
        ${escapeHtml(message)}
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
    `;

    document.body.appendChild(alertDiv);

    setTimeout(() => {
        alertDiv.classList.remove('show');
        setTimeout(() => alertDiv.remove(), 150);
    }, 3000);
}

/**
 * Показать индикатор загрузки
 * @param {HTMLElement} form - Форма, в которой нужно показать загрузку
 */
function showLoader(form) {
    const submitButton = form.querySelector('button[type="submit"]');
    if (submitButton) {
        submitButton.disabled = true;
        submitButton.innerHTML = '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Загрузка...';
    }
}

/**
 * Скрыть индикатор загрузки
 * @param {HTMLElement} form - Форма, в которой нужно скрыть загрузку
 */
function hideLoader(form) {
    const submitButton = form.querySelector('button[type="submit"]');
    if (submitButton) {
        submitButton.disabled = false;
        submitButton.textContent = submitButton.dataset.originalText || 'Отправить';
    }
}

/**
 * Экранирование HTML-символов
 * @param {string} unsafe - Необработанная строка
 * @returns {string} Экранированная строка
 */
function escapeHtml(unsafe) {
    if (!unsafe) return '';
    return unsafe
        .toString()
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/"/g, "&quot;")
        .replace(/'/g, "&#039;");
}

/**
 * Обработка ответа сервера
 * @param {Response} response - Объект ответа
 * @returns {Promise<Object>} Распарсенные данные
 */
async function parseResponse(response) {
    try {
        const data = await response.json();

        if (!response.ok) {
            if (response.status === 401) {
                handleLogout();
            }
            throw new Error(data.message || `Request failed with status ${response.status}`);
        }

        return data;
    } catch (error) {
        console.error('Error parsing response:', error);
        throw error;
    }
}