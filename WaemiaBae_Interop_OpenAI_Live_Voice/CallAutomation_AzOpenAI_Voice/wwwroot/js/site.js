// Poll an API endpoint at a given interval
function startPolling(url, callback, intervalMs) {
    async function poll() {
        try {
            const resp = await fetch(url);
            if (resp.ok) {
                const data = await resp.json();
                callback(data);
            }
        } catch (e) {
            console.warn('Poll error:', e);
        }
    }
    poll();
    return setInterval(poll, intervalMs || 3000);
}

// Format duration from seconds
function formatDuration(durationStr) {
    return durationStr || '00:00:00';
}
