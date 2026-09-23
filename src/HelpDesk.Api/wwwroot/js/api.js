(function (global) {
    const TOKEN_KEY = "helpdesk.token";
    const USER_KEY = "helpdesk.user";

    function extractError(data) {
        if (!data) {
            return null;
        }
        if (typeof data === "string") {
            return data;
        }
        if (data.errors) {
            const keys = Object.keys(data.errors);
            for (let i = 0; i < keys.length; i += 1) {
                const value = data.errors[keys[i]];
                if (Array.isArray(value) && value.length > 0) {
                    return value[0];
                }
            }
        }
        return data.detail || data.title || data.message || null;
    }

    async function request(method, path, body) {
        const headers = {};
        const token = localStorage.getItem(TOKEN_KEY);
        if (token) {
            headers.Authorization = "Bearer " + token;
        }

        let payload;
        if (body !== undefined && body !== null) {
            headers["Content-Type"] = "application/json";
            payload = JSON.stringify(body);
        }

        const response = await fetch("/api" + path, {
            method: method,
            headers: headers,
            body: payload
        });

        if (response.status === 401) {
            localStorage.removeItem(TOKEN_KEY);
            localStorage.removeItem(USER_KEY);
            if (!location.pathname.endsWith("login.html")) {
                location.href = "login.html?expired=1";
            }
            throw new Error("Your session has expired. Please sign in again.");
        }

        if (response.status === 204) {
            return null;
        }

        const text = await response.text();
        let data = null;
        if (text) {
            try {
                data = JSON.parse(text);
            } catch (error) {
                data = text;
            }
        }

        if (!response.ok) {
            const message = extractError(data) || "Request failed with status " + response.status;
            const requestError = new Error(message);
            requestError.status = response.status;
            requestError.data = data;
            throw requestError;
        }

        return data;
    }

    global.API = {
        tokenKey: TOKEN_KEY,
        userKey: USER_KEY,
        get: function (path) {
            return request("GET", path);
        },
        post: function (path, body) {
            return request("POST", path, body);
        },
        put: function (path, body) {
            return request("PUT", path, body);
        },
        del: function (path) {
            return request("DELETE", path);
        }
    };
})(window);
