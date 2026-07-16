// api.js — tiny fetch wrapper shared by every page.
// Assumes the frontend is served from the same origin as the FitCore.API project
// (i.e. dropped into FitCore.API/wwwroot), so relative "/api/..." URLs resolve correctly.

const FitCoreApi = {
    async request(method, url, body) {
        const token = getToken();

        const options = {
            method,
            headers: {
                'Content-Type': 'application/json'
            },
        };

        if (token) {
            options.headers['Authorization'] = `Bearer ${token}`;
        }

        if (body !== undefined) options.body = JSON.stringify(body);

        const response = await fetch(url, options);

        if (response.status === 401) {
            logout();
            throw new Error("Session expired. Please log in again.");
        }

        const text = await response.text();
        const data = text ? JSON.parse(text) : null;

        if (!response.ok) {
            const message = (data && (data.message || data.Message)) || `Request failed (${response.status})`;
            throw new Error(message);
        }
        return data;
    },

    get(url) { return this.request('GET', url); },
    post(url, body) { return this.request('POST', url, body ?? {}); },
    put(url, body) { return this.request('PUT', url, body ?? {}); },
    patch(url) { return this.request('PATCH', url); },
    delete(url) { return this.request('DELETE', url); },
};

// Normalizes API responses that may come back camelCase or PascalCase
// (System.Text.Json vs. manually-built objects) into a single shape.
function pick(obj, ...keys) {
    for (const key of keys) {
        if (obj && obj[key] !== undefined && obj[key] !== null) return obj[key];
    }
    return undefined;
}
