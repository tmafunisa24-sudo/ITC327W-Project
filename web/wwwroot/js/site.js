document.addEventListener('DOMContentLoaded', function () {
    console.log('ServiceHub IT ready for development.');

    const notificationCount = document.getElementById('notificationCount');
    const notificationItems = document.getElementById('notificationItems');
    const markAllButton = document.getElementById('markAllNotificationsRead');
    const notificationDropdown = document.getElementById('notificationDropdown');
    const toastContainer = document.getElementById('toastContainer');
    const toastMessages = document.getElementById('toastMessages');
    let lastNotificationUpdate = 0;

    function createToast(message, variant = 'success') {
        if (!toastContainer || !message) {
            return;
        }

        const toastElement = document.createElement('div');
        toastElement.className = `toast align-items-center text-bg-${variant} border-0 mb-2`;
        toastElement.setAttribute('role', 'alert');
        toastElement.setAttribute('aria-live', 'assertive');
        toastElement.setAttribute('aria-atomic', 'true');
        toastElement.innerHTML = `
            <div class="d-flex">
                <div class="toast-body">${message}</div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
            </div>`;

        toastContainer.appendChild(toastElement);
        const toast = new bootstrap.Toast(toastElement, { delay: 4500 });
        toast.show();
    }

    function renderPendingToast() {
        if (!toastMessages) {
            return;
        }

        const successMessage = toastMessages.dataset.success?.trim();
        const errorMessage = toastMessages.dataset.error?.trim();
        if (successMessage) {
            createToast(successMessage, 'success');
            fetchNotifications();
        } else if (errorMessage) {
            createToast(errorMessage, 'danger');
        }
    }

    async function fetchNotifications() {
        if (!notificationCount || !notificationItems) {
            console.error('notificationCount or notificationItems not found', { notificationCount, notificationItems });
            return;
        }

        try {
            const response = await fetch('/Notifications/Recent', {
                credentials: 'same-origin',
                headers: {
                    'Accept': 'application/json'
                }
            });

            console.log('Notification API response status:', response.status, 'ok=', response.ok, 'content-type=', response.headers.get('content-type'));
            if (!response.ok) {
                if (response.status === 401) {
                    console.warn('Notification API returned unauthorized. User session may be invalid.');
                }
                throw new Error(`Unable to load notifications. Status: ${response.status}`);
            }

            const contentType = response.headers.get('content-type') || '';
            if (!contentType.toLowerCase().includes('application/json')) {
                throw new Error(`Unexpected response content type: ${contentType}`);
            }

            const data = await response.json();
            console.log('Notification API response:', data);
            console.log('Unread notification count:', data.unreadCount);
            renderNotifications(data);
            lastNotificationUpdate = Date.now();
        } catch (error) {
            console.error('Notification fetch failed:', error);
            if (notificationItems) {
                notificationItems.innerHTML = '<div class="dropdown-item text-center text-danger py-4">Unable to load notifications.</div>';
            }
        }
    }

    function renderNotifications(data) {
        if (!notificationCount || !notificationItems) {
            return;
        }

        const unreadCount = Number(data?.unreadCount || 0);
        console.log('Notification count element:', notificationCount);
        console.log('Unread notification count in renderNotifications:', unreadCount);

        notificationCount.textContent = unreadCount > 99 ? '99+' : unreadCount.toString();
        notificationCount.style.display = unreadCount > 0 ? 'inline-flex' : 'none';

        if (!Array.isArray(data.notifications) || data.notifications.length === 0) {
            notificationItems.innerHTML = '<div class="dropdown-item notification-empty">No notifications yet. Check back soon.</div>';
            return;
        }

        notificationItems.innerHTML = data.notifications.map((notification) => {
            const title = notification.title || 'Notification';
            const message = notification.message || '';
            const time = notification.createdAt ? new Date(notification.createdAt).toLocaleString([], { hour: '2-digit', minute: '2-digit', month: 'short', day: 'numeric' }) : '';
            const readClass = notification.isRead ? '' : 'unread';
            const link = notification.link || '/Notifications/Index';
            const icon = notification.icon || 'bi-bell';
            const badge = notification.isRead
                ? '<span class="notification-badge bg-secondary">Read</span>'
                : '<span class="notification-badge">New</span>';

            return `
                <a href="${link}" class="dropdown-item notification-item ${readClass}" data-notification-id="${notification.id || ''}">
                    <div class="notification-item-link">
                        <div class="notification-avatar"><i class="bi ${icon}"></i></div>
                        <div class="notification-body">
                            <h6 class="notification-title">${title}</h6>
                            <p class="notification-message">${message}</p>
                            <div class="notification-meta">
                                <span>${time}</span>
                                ${badge}
                            </div>
                        </div>
                    </div>
                </a>`;
        }).join('');
    }

    function shouldRefreshNotifications() {
        return Date.now() - lastNotificationUpdate > 20000;
    }

    if (notificationDropdown) {
        const dropdownParent = notificationDropdown.closest('.dropdown');
        if (dropdownParent) {
            dropdownParent.addEventListener('shown.bs.dropdown', function () {
                fetchNotifications();
            });
        }
    }

    if (markAllButton) {
        markAllButton.addEventListener('click', async function (event) {
            event.preventDefault();
            try {
                const response = await fetch('/Notifications/MarkAllAsRead', {
                    method: 'POST',
                    headers: {
                        'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
                    }
                });

                if (response.ok) {
                    await fetchNotifications();
                }
            } catch {
                // ignore
            }
        });
    }

    document.addEventListener('click', function (event) {
        const notificationItem = event.target.closest('.notification-item');
        if (!notificationItem) {
            return;
        }

        const notificationId = notificationItem.getAttribute('data-notification-id');
        const destination = notificationItem.getAttribute('href');
        if (!destination) {
            return;
        }

        event.preventDefault();
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
        const markReadRequest = notificationId
            ? fetch(`/Notifications/MarkAsRead/${encodeURIComponent(notificationId)}`, {
                method: 'POST',
                headers: {
                    'RequestVerificationToken': token
                }
            })
            : Promise.resolve();

        markReadRequest.catch(function () {
            // Ignore client-side errors and still navigate.
        }).finally(function () {
            window.location.assign(destination);
        });
    });

    if (notificationCount && notificationItems) {
        fetchNotifications();
        renderPendingToast();
        setInterval(() => {
            if (shouldRefreshNotifications()) {
                fetchNotifications();
            }
        }, 30000);
    } else {
        renderPendingToast();
    }
});
