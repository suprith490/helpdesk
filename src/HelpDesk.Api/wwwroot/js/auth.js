(function (global) {
    function getUser() {
        const raw = localStorage.getItem(API.userKey);
        if (!raw) {
            return null;
        }
        try {
            return JSON.parse(raw);
        } catch (error) {
            return null;
        }
    }

    function setSession(authResponse) {
        localStorage.setItem(API.tokenKey, authResponse.token);
        localStorage.setItem(API.userKey, JSON.stringify(authResponse.user));
    }

    function isLoggedIn() {
        return !!localStorage.getItem(API.tokenKey);
    }

    function logout() {
        localStorage.removeItem(API.tokenKey);
        localStorage.removeItem(API.userKey);
        location.href = "login.html";
    }

    function requireAuth() {
        if (!isLoggedIn()) {
            location.href = "login.html";
            return false;
        }
        return true;
    }

    function isAgent() {
        const user = getUser();
        return !!user && (user.role === "SupportAgent" || user.role === "Admin");
    }

    function isAdmin() {
        const user = getUser();
        return !!user && user.role === "Admin";
    }

    function requireRole(roles) {
        const user = getUser();
        if (!user || roles.indexOf(user.role) === -1) {
            location.href = "dashboard.html";
            return false;
        }
        return true;
    }

    global.Auth = {
        getUser: getUser,
        setSession: setSession,
        isLoggedIn: isLoggedIn,
        logout: logout,
        requireAuth: requireAuth,
        isAgent: isAgent,
        isAdmin: isAdmin,
        requireRole: requireRole
    };
})(window);
