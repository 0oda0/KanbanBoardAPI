/**
 * KANBAN BOARD - CLIENT SIDE
 * Complete implementation with all original functionality
 */

// ========== CONSTANTS AND GLOBAL VARIABLES ========== //
const API_BASE_URL = window.location.origin;
const AUTH_TOKEN_KEY = 'kanban_auth_token';
let authModal, taskModal;

// ========== INITIALIZATION ========== //
document.addEventListener('DOMContentLoaded', function () {
    console.log('Initializing Kanban Board...');

    // Initialize modals
    authModal = new bootstrap.Modal(document.getElementById('authModal'));
    taskModal = new bootstrap.Modal(document.getElementById('taskModal'));

    // Check authentication status
    checkAuthStatus();

    // Setup event listeners
    setupEventListeners();

    // Initialize drag and drop
    initSortable();

    console.log('Initialization complete');
});

// ========== AUTHENTICATION FUNCTIONS ========== //
async function checkAuthStatus() {
    const token = localStorage.getItem(AUTH_TOKEN_KEY);
    if (token) {
        try {
            console.log('User is authenticated, loading data...');
            await fetchCurrentUser();
            await loadTasks();
        } catch (error) {
            console.error('Authentication check failed:', error);
            handleLogout();
        }
    } else {
        console.log('No authentication token found');
    }
}

async function fetchCurrentUser() {
    try {
        console.log('Fetching current user...');
        const response = await fetch(`${API_BASE_URL}/api/auth/me`, {
            headers: getAuthHeaders()
        });

        const data = await parseResponse(response);

        if (!data.success) {
            throw new Error(data.message || 'Failed to fetch user data');
        }

        document.getElementById('username').textContent = data.data.username;
        document.getElementById('user-info').classList.remove('d-none');
        document.getElementById('login-section').classList.add('d-none');

        console.log('User data loaded successfully');
    } catch (error) {
        console.error('Error fetching user:', error);
        throw error;
    }
}

async function handleLogin(e) {
    e.preventDefault();
    const form = e.target;

    try {
        showLoader(form);
        console.log('Attempting login...');

        const response = await fetch(`${API_BASE_URL}/api/auth/login`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                username: form.username.value.trim(),
                password: form.password.value
            })
        });

        const data = await parseResponse(response);

        if (!data.success) {
            throw new Error(data.message || 'Login failed');
        }

        localStorage.setItem(AUTH_TOKEN_KEY, data.data.token);
        authModal.hide();
        form.reset();

        await fetchCurrentUser();
        await loadTasks();

        showAlert('Login successful', 'success');
        console.log('Login successful');
    } catch (error) {
        console.error('Login error:', error);
        showAlert(error.message || 'Login failed', 'danger');
    } finally {
        hideLoader(form);
    }
}

function handleLogout() {
    console.log('Logging out user...');
    localStorage.removeItem(AUTH_TOKEN_KEY);
    document.getElementById('user-info').classList.add('d-none');
    document.getElementById('login-section').classList.remove('d-none');
    clearTasks();
    showAlert('You have been logged out', 'info');
}

// ========== TASK MANAGEMENT FUNCTIONS ========== //
async function loadTasks() {
    try {
        console.log('Loading tasks...');
        const response = await fetch(`${API_BASE_URL}/api/tasks`, {
            headers: getAuthHeaders()
        });

        const data = await parseResponse(response);

        if (!data.success) {
            throw new Error(data.message || 'Failed to load tasks');
        }

        renderTasks(data.data);
        console.log('Tasks loaded successfully');
    } catch (error) {
        console.error('Error loading tasks:', error);
        showAlert('Failed to load tasks: ' + error.message, 'danger');
    }
}

function renderTasks(tasks) {
    console.log('Rendering tasks...');
    clearTasks();

    if (!Array.isArray(tasks)) {
        console.error('Invalid tasks data:', tasks);
        return;
    }

    tasks.forEach(task => {
        if (!task || !task.status) {
            console.warn('Invalid task data:', task);
            return;
        }

        const normalizedStatus = task.status.toLowerCase().trim();
        const container = document.querySelector(`.tasks-container[data-status="${normalizedStatus}"]`);

        if (container) {
            container.appendChild(createTaskElement(task));
        } else {
            console.error(`No container found for status: ${normalizedStatus}`);
        }
    });

    console.log(`Rendered ${tasks.length} tasks`);
}

function createTaskElement(task) {
    const element = document.createElement('div');
    element.className = 'card task-card mb-3';
    element.dataset.taskId = task.taskId;

    const deadline = task.deadline
        ? new Date(task.deadline).toLocaleDateString()
        : 'No deadline';

    element.innerHTML = `
        <div class="card-body">
            <h5 class="card-title">${escapeHtml(task.title)}</h5>
            <p class="card-text">${escapeHtml(task.description || '')}</p>
            <div class="d-flex justify-content-between align-items-center">
                <small class="text-muted">Deadline: ${deadline}</small>
                <div>
                    <button class="btn btn-sm btn-outline-primary edit-btn">✏️</button>
                    <button class="btn btn-sm btn-outline-danger delete-btn">🗑️</button>
                </div>
            </div>
        </div>
    `;

    element.querySelector('.edit-btn').addEventListener('click', () => openTaskModal(task));
    element.querySelector('.delete-btn').addEventListener('click', () => confirmDeleteTask(task.taskId));

    return element;
}

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

    if (!taskData.title) {
        showAlert('Task title is required', 'danger');
        return;
    }

    try {
        showLoader(form);
        console.log(isEdit ? 'Updating task...' : 'Creating new task...');

        const url = `${API_BASE_URL}/api/tasks${isEdit ? `/${taskId}` : ''}`;
        const method = isEdit ? 'PUT' : 'POST';

        const response = await fetch(url, {
            method,
            headers: getAuthHeaders(),
            body: JSON.stringify(taskData)
        });

        const data = await parseResponse(response);

        if (!data.success) {
            throw new Error(data.message || 'Task operation failed');
        }

        taskModal.hide();
        await loadTasks();

        showAlert(`Task ${isEdit ? 'updated' : 'created'} successfully`, 'success');
        console.log(`Task ${isEdit ? 'updated' : 'created'} successfully`);
    } catch (error) {
        console.error('Error saving task:', error);
        showAlert(error.message, 'danger');
    } finally {
        hideLoader(form);
    }
}

async function deleteTask(taskId) {
    if (!taskId) return;

    try {
        console.log(`Deleting task ${taskId}...`);
        const response = await fetch(`${API_BASE_URL}/api/tasks/${taskId}`, {
            method: 'DELETE',
            headers: getAuthHeaders()
        });

        const data = await parseResponse(response);

        if (!data.success) {
            throw new Error(data.message || 'Failed to delete task');
        }

        taskModal.hide();
        await loadTasks();

        showAlert('Task deleted successfully', 'success');
        console.log('Task deleted successfully');
    } catch (error) {
        console.error('Error deleting task:', error);
        showAlert(error.message, 'danger');
    }
}

function confirmDeleteTask(taskId) {
    if (confirm('Are you sure you want to delete this task?')) {
        deleteTask(taskId);
    }
}

// ========== DRAG AND DROP FUNCTIONALITY ========== //
function initSortable() {
    console.log('Initializing drag and drop...');

    document.querySelectorAll('.tasks-container').forEach(container => {
        new Sortable(container, {
            group: 'kanban',
            animation: 150,
            ghostClass: 'sortable-ghost',
            draggable: '.task-card',
            onEnd: async (evt) => {
                const taskId = evt.item.dataset.taskId;
                const newStatus = evt.to.dataset.status;

                try {
                    console.log(`Moving task ${taskId} to ${newStatus}...`);
                    const response = await fetch(`${API_BASE_URL}/api/tasks/${taskId}/status`, {
                        method: 'PATCH',
                        headers: getAuthHeaders(),
                        body: JSON.stringify({ status: newStatus })
                    });

                    const data = await parseResponse(response);

                    if (!data.success) {
                        throw new Error(data.message || 'Failed to move task');
                    }

                    console.log('Task moved successfully');
                } catch (error) {
                    console.error('Error moving task:', error);
                    evt.from.insertBefore(evt.item, evt.oldIndex > evt.newIndex ?
                        evt.item.nextSibling : evt.item.previousSibling);
                    showAlert('Failed to move task: ' + error.message, 'danger');
                }
            }
        });
    });

    console.log('Drag and drop initialized');
}

// ========== HELPER FUNCTIONS ========== //
function setupEventListeners() {
    console.log('Setting up event listeners...');

    // Auth forms
    document.getElementById('loginForm')?.addEventListener('submit', handleLogin);
    document.getElementById('registerForm')?.addEventListener('submit', handleRegister);
    document.getElementById('logout-btn')?.addEventListener('click', handleLogout);

    // Task buttons
    document.getElementById('saveTaskBtn')?.addEventListener('click', saveTask);
    document.getElementById('deleteTaskBtn')?.addEventListener('click', deleteTask);

    // Add task buttons
    document.querySelectorAll('.add-task-btn').forEach(btn => {
        btn.addEventListener('click', function () {
            const status = this.dataset.status;
            openTaskModal(null, status);
        });
    });

    console.log('Event listeners setup complete');
}

function openTaskModal(task, status = null) {
    const isEdit = !!task;
    const form = document.getElementById('taskForm');

    document.getElementById('taskModalTitle').textContent = isEdit ? 'Edit Task' : 'New Task';
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

function getAuthHeaders() {
    const token = localStorage.getItem(AUTH_TOKEN_KEY);
    if (!token) {
        handleLogout();
        throw new Error('Authentication required');
    }
    return {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
    };
}

async function parseResponse(response) {
    const text = await response.text();

    if (!response.ok) {
        if (response.status === 401) {
            handleLogout();
        }
        throw new Error(text || `Request failed with status ${response.status}`);
    }

    return JSON.parse(text);
}

function showAlert(message, type) {
    const alert = document.createElement('div');
    alert.className = `alert alert-${type} alert-dismissible fade show position-fixed top-0 end-0 m-3`;
    alert.style.zIndex = '1100';
    alert.innerHTML = `
        ${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    `;
    document.body.appendChild(alert);

    setTimeout(() => {
        alert.classList.remove('show');
        setTimeout(() => alert.remove(), 150);
    }, 3000);
}

function clearTasks() {
    document.querySelectorAll('.tasks-container').forEach(container => {
        container.innerHTML = '';
    });
}

function showLoader(form) {
    const submitButton = form.querySelector('button[type="submit"]');
    if (submitButton) {
        submitButton.disabled = true;
        submitButton.innerHTML = '<span class="spinner-border spinner-border-sm"></span> Loading...';
    }
}

function hideLoader(form) {
    const submitButton = form.querySelector('button[type="submit"]');
    if (submitButton) {
        submitButton.disabled = false;
        submitButton.textContent = submitButton.dataset.originalText || 'Submit';
    }
}

function escapeHtml(unsafe) {
    return unsafe?.toString()
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/"/g, "&quot;")
        .replace(/'/g, "&#039;") || '';
}